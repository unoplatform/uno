#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Uno.WinUI.Graphics2DSK;

namespace UITests.Shared.Windows_UI_Composition.ScrollableShapes;

[Sample(
	"Cool Graphics",
	Name = "ScrollableShapes",
	IsManualTest = true,
	Description =
		"Three thousand points that are a sphere, a cube, a torus and a heart at once: every shape " +
		"shares an index, so moving between pages lerps each point from one form to the next while " +
		"the cloud keeps turning. Use the flip arrows, swipe, or the arrow keys.")]
public sealed partial class ScrollableShapes : Page
{
	private ShapesCanvas? _canvas;
	private ScrollViewer? _flipScroller;
	private bool _rendering;

	public ScrollableShapes()
	{
		InitializeComponent();

		if (!SKCanvasElement.IsSupportedOnCurrentPlatform())
		{
			host.Children.Add(new TextBlock { Text = "This sample is not supported on this platform." });
			return;
		}

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += (_, _) =>
		{
			if (_canvas is { } c)
			{
				c.PageWidth = ActualWidth;
			}
		};

		flip.SelectionChanged += (_, _) => _canvas?.GoTo(flip.SelectedIndex);

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
		if (_canvas is null)
		{
			_canvas = new ShapesCanvas { PageWidth = Math.Max(1, ActualWidth), IsHitTestVisible = false };
			host.Children.Insert(0, _canvas);

			flip.ItemsSource = Enumerable.Range(0, _canvas.ShapeCount).ToList();
			flip.SelectedIndex = 0;
		}

		AttachFlipScroller();

		// Without this the arrow keys go nowhere: the flip never takes focus on its own, because
		// every one of its pages is an empty transparent grid with nothing focusable inside.
		Focus(FocusState.Programmatic);

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
			if (_canvas is not { } c || _flipScroller.ViewportWidth <= 0)
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

		_canvas?.Invalidate();
	}
}
