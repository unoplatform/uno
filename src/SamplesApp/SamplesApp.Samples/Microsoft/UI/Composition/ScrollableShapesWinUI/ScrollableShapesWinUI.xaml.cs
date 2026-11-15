#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Composition.ScrollableShapesWinUI;

[Sample(
	"Cool Graphics",
	Name = "ScrollableShapesWinUI",
	IsManualTest = true,
	Description =
		"The same morphing cloud as the Skia sample, built only from public WinUI API: every point " +
		"is a Composition ellipse repositioned each frame, sorted into three depth bins, over a XAML " +
		"radial-gradient glow. Use the flip arrows, swipe, or the arrow keys.")]
public sealed partial class ScrollableShapesWinUI : Page
{
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly List<Ellipse> _dots = new();

	private ShapeCloud? _cloud;
	private ScrollViewer? _flipScroller;
	private bool _rendering;
	private double _lastSeconds;

	public ScrollableShapesWinUI()
	{
		InitializeComponent();

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += (_, _) =>
		{
			glow.Width = ActualWidth * 1.2;
			glow.Height = ActualWidth * 1.2;
		};

		flip.SelectionChanged += (_, _) => _cloud?.GoTo(flip.SelectedIndex);

		// Driven here rather than left to the flip: its pages hold nothing focusable, so the key
		// never reaches it on its own.
		IsTabStop = true;
		KeyDown += (_, e) =>
		{
			var step = e.Key switch
			{
				Windows.System.VirtualKey.Right or Windows.System.VirtualKey.Down => 1,
				Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Up => -1,
				_ => 0,
			};

			if (step == 0 || flip.Items.Count == 0)
			{
				return;
			}

			flip.SelectedIndex = Math.Clamp(flip.SelectedIndex + step, 0, flip.Items.Count - 1);
			e.Handled = true;
		};
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_cloud is null)
		{
			_cloud = new ShapeCloud(cloudHost);

			flip.ItemsSource = Enumerable.Range(0, _cloud.ShapeCount).ToList();
			flip.SelectedIndex = 0;

			for (var i = 0; i < _cloud.ShapeCount; i++)
			{
				var dot = new Ellipse
				{
					Width = 12,
					Height = 12,
					Fill = new SolidColorBrush(Colors.White),
					RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
					RenderTransform = new ScaleTransform(),
				};

				_dots.Add(dot);
				paginator.Children.Add(dot);
			}
		}

		AttachFlipScroller();

		// Without this the arrow keys go nowhere: the flip never takes focus on its own, because
		// every one of its pages is an empty transparent grid with nothing focusable inside.
		Focus(FocusState.Programmatic);

		if (!_rendering)
		{
			_rendering = true;
			_lastSeconds = _clock.Elapsed.TotalSeconds;
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

	/// <summary>
	/// FlipView only reports a landed page, which would make the morph jump between shapes. Its
	/// template hosts a ScrollViewer, and reading that mid-swipe is what keeps the blend continuous.
	/// Falling back to the selection alone still works, it just steps.
	/// </summary>
	private void AttachFlipScroller()
	{
		if (_flipScroller is not null)
		{
			return;
		}

		_flipScroller = FindDescendant<ScrollViewer>(flip);
		if (_flipScroller is null)
		{
			return;
		}

		_flipScroller.ViewChanged += (_, _) =>
		{
			if (_cloud is not { } c || _flipScroller.ViewportWidth <= 0)
			{
				return;
			}

			c.Track(_flipScroller.HorizontalOffset / _flipScroller.ViewportWidth);
		};
	}

	private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				return match;
			}

			if (FindDescendant<T>(child) is { } nested)
			{
				return nested;
			}
		}

		return null;
	}

	private void OnRendering(object? sender, object e)
	{
		// The template is built after the first layout pass, so keep trying until the scroller exists.
		if (_flipScroller is null)
		{
			AttachFlipScroller();
		}

		if (_cloud is not { } cloud)
		{
			return;
		}

		var seconds = _clock.Elapsed.TotalSeconds;
		var dt = (float)(seconds - _lastSeconds);
		_lastSeconds = seconds;

		cloud.Update((float)ActualWidth, (float)ActualHeight, seconds, dt);

		var pos = cloud.MorphPosition;
		for (var i = 0; i < _dots.Count; i++)
		{
			// Nearness to this page, so the active dot grows and brightens as the morph crosses it.
			var near = Math.Clamp(1f - MathF.Abs(pos - i), 0f, 1f);
			_dots[i].Opacity = (60 + 195 * near) / 255.0;
			((ScaleTransform)_dots[i].RenderTransform).ScaleX = 0.5 + 0.5 * near;
			((ScaleTransform)_dots[i].RenderTransform).ScaleY = 0.5 + 0.5 * near;
		}
	}
}
