#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UITests.Shared.Windows_UI_Composition.SculpturesInWords;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWordsWinUI;

[Sample(
	"Cool Graphics",
	Name = "SculpturesInWordsWinUI",
	IsManualTest = true,
	Description =
		"The same sculpture of letters as the Skia sample, built only from public WinUI API: one " +
		"Composition sprite per letter over a glyph atlas baked once per ink step, since no public " +
		"brush tints an image. Pick a figure and drag the slider to trade letter count against size.")]
public sealed partial class SculpturesInWordsWinUI : Page
{
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
	private readonly Stopwatch _spin = Stopwatch.StartNew();

	private GlyphSprites? _field;
	private bool _rendering;
	private bool _ready;
	private long _lastTicks;
	private double _emaFrameMs;
	private int _frame;

	public SculpturesInWordsWinUI()
	{
		InitializeComponent();

		figureCombo.ItemsSource = Figures.Select(f => f.Label).ToList();
		figureCombo.SelectedIndex = 0;

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		countSlider.ValueChanged += async (_, e) => await RebuildAsync((int)e.NewValue);
		figureCombo.SelectionChanged += async (_, _) => await RebuildAsync((int)countSlider.Value);
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
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

	private async Task RebuildAsync(int count)
	{
		if (!_ready)
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

		_field = new GlyphSprites(host, cloud, BuildLetters(cloud.Length));
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
		// Stand-in for an article: letter frequencies close enough to prose that the figure sees the
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

	private void OnRendering(object? sender, object e)
	{
		if (_field is not { } field)
		{
			return;
		}

		var now = _spin.ElapsedTicks;
		if (_lastTicks != 0)
		{
			var ms = (now - _lastTicks) * 1000.0 / Stopwatch.Frequency;
			_emaFrameMs = _emaFrameMs == 0 ? ms : _emaFrameMs * 0.9 + ms * 0.1;
		}

		_lastTicks = now;

		field.Angle = (float)(_spin.Elapsed.TotalSeconds * 0.45);
		field.Update((float)host.ActualWidth, (float)host.ActualHeight);

		statsText.Text = $"{field.LetterCount,6:N0} letters   {_emaFrameMs,5:F1} ms";
		if (++_frame % 120 == 0)
		{
			Console.WriteLine($"SCULPTWINUI letters={field.LetterCount} frame={_emaFrameMs:F2}ms fps={(_emaFrameMs > 0 ? 1000.0 / _emaFrameMs : 0):F0}");
		}
	}
}
