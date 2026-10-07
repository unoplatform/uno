#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics2DSK;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

[Sample(
	"Cool Graphics",
	Name = "SculpturesInWords",
	IsManualTest = true,
	Description =
		"A sculpture built from thousands of letters, each one a textured quad projected and depth-sorted " +
		"per frame. The scans are point clouds baked from public-domain museum meshes; the procedural " +
		"figures are signed distance fields. Pick a figure and drag the slider to trade letter count " +
		"against letter size.")]
public sealed partial class SculpturesInWords : Page
{
	private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.,;:'\"!?-";

	private sealed record Figure(string Label, string? Asset, FigureKind Kind = FigureKind.Bust);

	private static readonly Figure[] Figures =
	{
		new("Woman's Head (scan)", "womans-head.bin"),
		new("Einstein (scan)", "einstein.bin"),
		new("Aphrodite (scan)", "aphrodite.bin"),
		new("Pan (scan)", "pan.bin"),
		new("Bust (procedural)", null, FigureKind.Bust),
		new("Thinker (procedural)", null, FigureKind.Thinker),
		new("Winged (procedural)", null, FigureKind.Winged),
	};

	private readonly Dictionary<string, byte[]> _scans = new();

	private GlyphAtlas? _atlas;
	private SculptureCanvas? _canvas;
	private MeshCanvas? _solid;
	private readonly System.Diagnostics.Stopwatch _spin = System.Diagnostics.Stopwatch.StartNew();
	private bool _rendering;
	private bool _ready;
	private int _frame;

	public SculpturesInWords()
	{
		InitializeComponent();

		if (!SKCanvasElement.IsSupportedOnCurrentPlatform())
		{
			host.Children.Add(new TextBlock { Text = "This sample is not supported on this platform." });
			return;
		}

		figureCombo.ItemsSource = Figures.Select(f => f.Label).ToList();
		figureCombo.SelectedIndex = 0;

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		countSlider.ValueChanged += async (_, e) => await RebuildAsync((int)e.NewValue);
		figureCombo.SelectionChanged += async (_, _) => await RebuildAsync((int)countSlider.Value);
		compareCheck.Checked += (_, _) => UpdateCompareVisibility();
		compareCheck.Unchecked += (_, _) => UpdateCompareVisibility();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_atlas ??= GlyphAtlas.Create(Alphabet, SKTypeface.Default, 48f);
		_ready = true;
		await RebuildAsync((int)countSlider.Value);

		if (!_rendering)
		{
			_rendering = true;
			CompositionTarget.Rendering += OnRendering;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_rendering)
		{
			_rendering = false;
			CompositionTarget.Rendering -= OnRendering;
		}
	}

	private void UpdateCompareVisibility()
	{
		// Collapse the column rather than moving the other pane between columns.
		var show = compareCheck.IsChecked == true;
		shadedHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
		shadedColumn.Width = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
	}

	private void OnRendering(object? sender, object e)
	{
		// One clock for both panes: the figure and its words have to turn together to be comparable.
		var angle = (float)(_spin.Elapsed.TotalSeconds * 0.45);
		if (_canvas is { } lhs)
		{
			lhs.Angle = angle;
		}

		if (_solid is { } rhs)
		{
			rhs.Angle = angle;
			rhs.Invalidate();
		}

		_canvas?.Invalidate();
		if (_canvas is not { } c)
		{
			return;
		}

		var fps = c.FrameMilliseconds > 0 ? 1000.0 / c.FrameMilliseconds : 0;
		statsText.Text = $"{c.LetterCount,6:N0} letters   {c.FrameMilliseconds,5:F1} ms";

		if (++_frame % 120 == 0)
		{
			Console.WriteLine($"SCULPTPERF letters={c.LetterCount} frame={c.FrameMilliseconds:F2}ms fps={fps:F0}");
		}
	}

	private async Task RebuildAsync(int count)
	{
		if (!_ready || _atlas is null)
		{
			return;
		}

		var figure = Figures[Math.Clamp(figureCombo.SelectedIndex, 0, Figures.Length - 1)];
		var wanted = Math.Max(100, count);

		CloudPoint[] cloud;
		if (figure.Asset is { } asset)
		{
			var bytes = await LoadScanAsync(asset);
			if (bytes is null)
			{
				return;
			}

			cloud = PointCloud.FromBytes(bytes, wanted);
		}
		else
		{
			cloud = PointCloud.CreateFigure(wanted, figure.Kind);
		}

		var letters = BuildLetters(cloud.Length);

		host.Children.Clear();
		_canvas = new SculptureCanvas(cloud, _atlas, letters);
		host.Children.Add(_canvas);

		shadedHost.Children.Clear();
		_solid = null;
		if (figure.Asset is { } meshAsset)
		{
			var meshBytes = await LoadScanAsync(System.IO.Path.ChangeExtension(meshAsset, ".mesh"));
			if (meshBytes is { Length: > 8 })
			{
				_solid = new MeshCanvas(MeshData.FromBytes(meshBytes));
				shadedHost.Children.Add(_solid);
			}
		}

		if (_solid is null)
		{
			// Procedural figures have no scan behind them, so the left pane shows the cloud itself.
			shadedHost.Children.Add(new SculptureCanvas(cloud, _atlas, letters) { UseGlyphs = false });
		}

		UpdateCompareVisibility();



	}

	private async Task<byte[]?> LoadScanAsync(string name)
	{
		if (_scans.TryGetValue(name, out var cached))
		{
			return cached;
		}

		try
		{
			var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(
				new Uri($"ms-appx:///Assets/Sculptures/{name}"));
			using var stream = (await file.OpenReadAsync()).AsStreamForRead();
			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms);
			return _scans[name] = ms.ToArray();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SCULPT scan '{name}' failed to load: {ex.Message}");
			return null;
		}
	}

	private static string BuildLetters(int count)
	{
		// Stand-in for an article: letter frequencies close enough to prose that the atlas sees the
		// same distribution it will in the real thing.
		const string Source =
			"the quick brown fox jumps over the lazy dog while a marble figure turns slowly on its " +
			"turntable and every letter finds the place that has been waiting for it since the first line ";
		var buffer = new char[count];
		var s = 0;
		for (var i = 0; i < count; i++)
		{
			char c;
			do
			{
				c = Source[s++ % Source.Length];
			}
			while (c == ' ');
			buffer[i] = c;
		}

		return new string(buffer);
	}
}
