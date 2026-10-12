#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics2DSK;

namespace UITests.Shared.Windows_UI_Composition.GitHubContributions;

[Sample(
	"Cool Graphics",
	Name = "GitHubContributions",
	IsManualTest = true,
	Description =
		"A contributions calendar that fills in on a diagonal wave and scatters when cleared. Each " +
		"square runs its own spring, integrated per frame rather than keyframed, so the squares " +
		"overshoot and settle instead of easing to a stop. Press Run, or click the grid.")]
public sealed partial class GitHubContributions : Page
{
	private static readonly (string Label, ColorScheme Scheme)[] Schemes =
	{
		("GitHub", ColorSchemes.GitHub),
		("Blue", ColorSchemes.Blue),
		("Purple", ColorSchemes.Purple),
	};

	private ContributionsCanvas? _canvas;
	private bool _rendering;
	private bool _ranOnce;

	public GitHubContributions()
	{
		InitializeComponent();

		if (!SKCanvasElement.IsSupportedOnCurrentPlatform())
		{
			host.Children.Add(new TextBlock { Text = "This sample is not supported on this platform." });
			return;
		}

		schemeCombo.ItemsSource = Schemes.Select(s => s.Label).ToList();
		schemeCombo.SelectedIndex = 0;
		schemeCombo.SelectionChanged += (_, _) =>
		{
			if (_canvas is { } c)
			{
				c.ColorScheme = Schemes[Math.Max(0, schemeCombo.SelectedIndex)].Scheme;
			}
		};

		toggleButton.Click += (_, _) => Toggle();

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_canvas is null)
		{
			_canvas = new ContributionsCanvas();
			_canvas.Tapped += (_, _) => Toggle();
			host.Children.Insert(0, _canvas);
		}

		if (!_rendering)
		{
			_rendering = true;
			CompositionTarget.Rendering += OnRendering;
		}

		if (!_ranOnce)
		{
			_ranOnce = true;

			// Run it once unprompted: a sample that opens on an empty grey grid looks broken, and the
			// wave is the whole point. The button and the grid still toggle it afterwards.
			var kick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
			kick.Tick += (s2, _) =>
			{
				((DispatcherTimer)s2!).Stop();
				Toggle();
			};
			kick.Start();
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

	private void Toggle()
	{
		_canvas?.Toggle();
		toggleButton.Content = _canvas?.Filled == true ? "Clear" : "Run";
	}

	private void OnRendering(object? sender, object e)
	{
		_canvas?.Invalidate();
		if (_canvas is { } c)
		{
			statsText.Text = $"{c.SquareCount,4} squares";
		}
	}
}
