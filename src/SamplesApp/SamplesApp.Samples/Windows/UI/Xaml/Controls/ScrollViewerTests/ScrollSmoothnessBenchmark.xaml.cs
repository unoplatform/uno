#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

/// <summary>
/// Measures scroll smoothness as the *variance of the per-frame offset delta*, which is what the eye
/// actually perceives — a constant-velocity scroll that advances 8px every frame looks smooth, while
/// one averaging 8px but alternating 0/16 looks like judder even at a nominal 60 FPS.
/// </summary>
[Sample("ScrollViewer", Name = "ScrollSmoothnessBenchmark", IsManualTest = true,
	Description = "Reports per-frame offset delta statistics while scrolling. Lower jitter is better.")]
public sealed partial class ScrollSmoothnessBenchmark : UserControl
{
	private const int ItemCount = 2000;

	// Below this the scroll is considered stopped, and the frame is excluded from the statistics.
	private const double MovingThreshold = 0.01;

	private readonly List<double> _deltas = new();
	private readonly List<double> _frameIntervalsMs = new();
	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private ScrollViewer? _scrollViewer;
	private EventHandler<object>? _renderingHandler;
	private double _lastOffset;
	private TimeSpan _lastFrameTime;
	private bool _hasLastFrame;

	private bool _autoScrolling;
	private double _autoScrollVelocity = 900; // px/s

	public ScrollSmoothnessBenchmark()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		BuildContent(0);
		_renderingHandler = OnRendering;
		CompositionTarget.Rendering += _renderingHandler;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_renderingHandler is not null)
		{
			CompositionTarget.Rendering -= _renderingHandler;
			_renderingHandler = null;
		}
	}

	private void OnContentChanged(object sender, SelectionChangedEventArgs e)
	{
		if (IsLoaded)
		{
			BuildContent(ContentSelector.SelectedIndex);
		}
	}

	private void OnReset(object sender, RoutedEventArgs e) => ResetStats();

	private void OnAutoScrollToggled(object sender, RoutedEventArgs e)
	{
		_autoScrolling = AutoScrollToggle.IsOn;
		ResetStats();
	}

	private void ResetStats()
	{
		_deltas.Clear();
		_frameIntervalsMs.Clear();
		_hasLastFrame = false;
		StatsText.Text = "Collecting…";
	}

	private void BuildContent(int index)
	{
		var items = Enumerable.Range(0, ItemCount).ToArray();

		FrameworkElement content = index switch
		{
			0 => new Microsoft.UI.Xaml.Controls.ListView
			{
				ItemsSource = items,
				ItemTemplate = (DataTemplate)Resources["SimpleItemTemplate"],
			},
			1 => BuildScrollViewerWithPanel(items, heavy: false),
			_ => BuildScrollViewerWithPanel(items, heavy: true),
		};

		Host.Content = content;
		_scrollViewer = content as ScrollViewer;
		ResetStats();
	}

	private static ScrollViewer BuildScrollViewerWithPanel(int[] items, bool heavy)
	{
		var panel = new StackPanel();
		foreach (var i in items)
		{
			panel.Children.Add(heavy ? CreateHeavyItem(i) : CreateSimpleItem(i));
		}

		return new ScrollViewer { Content = panel };
	}

	private static FrameworkElement CreateSimpleItem(int i) => new TextBlock
	{
		Text = "Item " + i.ToString(CultureInfo.InvariantCulture),
		Height = 36,
		Padding = new Thickness(8, 6, 8, 6),
	};

	private static FrameworkElement CreateHeavyItem(int i)
	{
		var inner = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		for (var j = 0; j < 6; j++)
		{
			inner.Children.Add(new Border
			{
				Width = 44,
				Height = 28,
				CornerRadius = new CornerRadius(4),
				BorderThickness = new Thickness(1),
				BorderBrush = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumLowBrush"],
				Child = new TextBlock { Text = j.ToString(CultureInfo.InvariantCulture), Margin = new Thickness(4, 2, 4, 2) },
			});
		}

		var row = new StackPanel { Spacing = 4, Padding = new Thickness(8) };
		row.Children.Add(new TextBlock { Text = "Row " + i.ToString(CultureInfo.InvariantCulture) });
		row.Children.Add(inner);
		return row;
	}

	private void OnRendering(object? sender, object args)
	{
		if (_scrollViewer is not { } sv)
		{
			// The ListView case hosts its own ScrollViewer inside its template.
			_scrollViewer = sv = FindScrollViewer(Host);
			if (sv is null)
			{
				return;
			}
		}

		var now = _clock.Elapsed;
		var offset = sv.VerticalOffset;

		if (_autoScrolling && _hasLastFrame)
		{
			var dt = (now - _lastFrameTime).TotalSeconds;
			var next = offset + _autoScrollVelocity * dt;
			if (next >= sv.ScrollableHeight)
			{
				next = 0;
			}

			sv.ChangeView(null, next, null, true);
		}

		if (_hasLastFrame)
		{
			var delta = Math.Abs(offset - _lastOffset);
			var intervalMs = (now - _lastFrameTime).TotalMilliseconds;

			if (delta > MovingThreshold)
			{
				_deltas.Add(delta);
				_frameIntervalsMs.Add(intervalMs);

				if (_deltas.Count % 15 == 0)
				{
					UpdateStats();
				}
			}
		}

		_lastOffset = offset;
		_lastFrameTime = now;
		_hasLastFrame = true;
	}

	private static ScrollViewer? FindScrollViewer(DependencyObject root)
	{
		var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			if (child is ScrollViewer sv)
			{
				return sv;
			}

			if (FindScrollViewer(child) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private void UpdateStats()
	{
		var n = _deltas.Count;
		if (n < 2)
		{
			return;
		}

		var meanDelta = _deltas.Average();
		var varianceDelta = _deltas.Sum(d => (d - meanDelta) * (d - meanDelta)) / n;
		var stdDelta = Math.Sqrt(varianceDelta);

		// Coefficient of variation: the scale-free jitter measure. A perfectly paced scroll → 0.
		var cv = meanDelta > 0 ? stdDelta / meanDelta : 0;

		var meanInterval = _frameIntervalsMs.Average();
		var sorted = _frameIntervalsMs.OrderBy(static x => x).ToArray();
		var p95Interval = sorted[(int)(sorted.Length * 0.95)];
		var maxInterval = sorted[^1];

		// A frame taking >1.5x the median interval is a visible hitch.
		var median = sorted[sorted.Length / 2];
		var hitches = _frameIntervalsMs.Count(x => x > median * 1.5);

		var sb = new StringBuilder();
		sb.Append(CultureInfo.InvariantCulture, $"samples {n,5}   ");
		sb.Append(CultureInfo.InvariantCulture, $"Δoffset mean {meanDelta,6:F2}px  sd {stdDelta,6:F2}  ");
		sb.Append(CultureInfo.InvariantCulture, $"CV {cv,5:F3}\n");
		sb.Append(CultureInfo.InvariantCulture, $"frame  mean {meanInterval,6:F2}ms  median {median,6:F2}  ");
		sb.Append(CultureInfo.InvariantCulture, $"p95 {p95Interval,6:F2}  max {maxInterval,6:F2}\n");
		sb.Append(CultureInfo.InvariantCulture, $"effective {1000 / meanInterval,6:F1} FPS   hitches (>1.5x median) {hitches}");

		StatsText.Text = sb.ToString();
	}
}
