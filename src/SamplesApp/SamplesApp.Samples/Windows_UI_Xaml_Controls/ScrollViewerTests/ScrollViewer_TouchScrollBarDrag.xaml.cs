using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Uno.UI.Toolkit;

// UITests.Windows_UI_Xaml_Controls.ScrollBar is a sibling namespace, which shadows the type name here.
using ScrollBarControl = Microsoft.UI.Xaml.Controls.Primitives.ScrollBar;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample(
		"Scrolling",
		Description = "ScrollBarExtensions.IsTouchThumbDragEnabled: the scroll bar thumb of a ScrollViewer, and of a standalone bar, can be dragged with a finger.",
		IsManualTest = true)]
	public sealed partial class ScrollViewer_TouchScrollBarDrag : Page
	{
		private ScrollBarControl _verticalScrollBar;
		private ScrollBarControl _horizontalScrollBar;
		private FrameworkElement _verticalInteractiveRoot;
		private FrameworkElement _verticalThumb;
		private readonly HashSet<ScrollBarControl> _wiredScrollBars = new();
		private string _lastScroll = "none";

		public ScrollViewer_TouchScrollBarDrag()
		{
			this.InitializeComponent();

			Rows.ItemsSource = Enumerable.Range(1, 60).Select(i => $"Row {i}").ToArray();

			Viewport.Loaded += OnViewportLoaded;
			Viewport.LayoutUpdated += OnViewportLayoutUpdated;
			Viewport.ViewChanged += (_, _) => UpdateStatus();
			StandaloneScrollBar.ValueChanged += (_, _) => UpdateStatus();
		}

		private void OnViewportLoaded(object sender, RoutedEventArgs e) => TryResolveScrollBars();

		// The ScrollViewer template defers its bars (x:Load="False"), so with Auto visibility they only appear
		// once an axis actually overflows - after this page has loaded. Keep looking until they show up.
		private void OnViewportLayoutUpdated(object sender, object e) => TryResolveScrollBars();

		private void TryResolveScrollBars()
		{
			_verticalScrollBar ??= FindTemplateChild<ScrollBarControl>(Viewport, "VerticalScrollBar");
			_horizontalScrollBar ??= FindTemplateChild<ScrollBarControl>(Viewport, "HorizontalScrollBar");

			if (_verticalScrollBar is { } verticalScrollBar)
			{
				_verticalInteractiveRoot ??= FindTemplateChild<FrameworkElement>(verticalScrollBar, "VerticalRoot");
				_verticalThumb ??= FindTemplateChild<FrameworkElement>(verticalScrollBar, "VerticalThumb");
			}

			foreach (var scrollBar in GetScrollBars())
			{
				if (scrollBar != StandaloneScrollBar && _wiredScrollBars.Add(scrollBar))
				{
					scrollBar.Scroll += ScrollBar_Scroll;
				}
			}

			ApplyOptIn();
		}

		private void OptIn_Changed(object sender, RoutedEventArgs e) => ApplyOptIn();

		private void WideContent_Changed(object sender, RoutedEventArgs e)
		{
			// Auto bars: with the content narrower than the viewport there is nothing to scroll on that axis,
			// so no bar should show - not even while the opt-in holds the other one interactive.
			Rows.Width = WideContent.IsChecked is true ? 2000 : double.NaN;
			UpdateStatus();
		}

		private void ApplyOptIn()
		{
			var isEnabled = OptIn.IsChecked is true;

			// In XAML this reads as toolkit:ScrollBarExtensions.IsTouchThumbDragEnabled="True" on the ScrollBar
			// itself, or as a Setter in an implicit ScrollBar Style to reach the bars inside another control's
			// template. Here it is set from code so the same page can be validated both ways without a rebuild.
			foreach (var scrollBar in GetScrollBars())
			{
				scrollBar.SetIsTouchThumbDragEnabled(isEnabled);
			}

			UpdateStatus();
		}

		private IEnumerable<ScrollBarControl> GetScrollBars()
			=> new[] { _verticalScrollBar, _horizontalScrollBar, StandaloneScrollBar }.Where(bar => bar is not null);

		private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
		{
			var scrollBar = (ScrollBarControl)sender;
			var name = scrollBar == StandaloneScrollBar ? "standalone" : scrollBar.Orientation.ToString().ToLowerInvariant();

			_lastScroll = $"{name} {e.ScrollEventType}";
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			// IndicatorMode stays on the touch indicator once a finger has panned: what decides whether the
			// thumb can be grabbed is the bar's own visual state, so report that rather than the property.
			var bars = _verticalScrollBar is null
				? "no managed scroll bars on this target"
				: $"vertical bar: indicator={_verticalScrollBar.IndicatorMode}"
					+ $" draggable={_verticalInteractiveRoot?.IsHitTestVisible.ToString() ?? "?"}"
					+ $" thumbOpacity={_verticalThumb?.Opacity.ToString("F1") ?? "?"}";

			Status.Text = $"opt-in: {OptIn.IsChecked is true} — offset: {Viewport.HorizontalOffset:F0},{Viewport.VerticalOffset:F0}"
				+ $" — bars shown: h={Viewport.ComputedHorizontalScrollBarVisibility} v={Viewport.ComputedVerticalScrollBarVisibility}"
				+ $" — {bars} — last scroll event: {_lastScroll}";
			StandaloneValue.Text = $"{StandaloneScrollBar.Value:F0}";
		}

		private static T FindTemplateChild<T>(DependencyObject root, string name)
			where T : FrameworkElement
		{
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				if (child is T match && match.Name == name)
				{
					return match;
				}

				if (FindTemplateChild<T>(child, name) is { } descendant)
				{
					return descendant;
				}
			}

			return null;
		}
	}
}
