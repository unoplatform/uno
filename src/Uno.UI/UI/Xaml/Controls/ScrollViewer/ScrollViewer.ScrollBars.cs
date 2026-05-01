#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		#region Managed scroll bars support
		private bool _isTemplateApplied;
		private ScrollBar? _verticalScrollbar;
		private ScrollBar? _horizontalScrollbar;
		private bool _isVerticalScrollBarMaterialized;
		private bool _isHorizontalScrollBarMaterialized;

		internal ScrollBar? ElementHorizontalScrollBar => _horizontalScrollbar;
		internal ScrollBar? ElementVerticalScrollBar => _verticalScrollbar;

		private void MaterializeVerticalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isVerticalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			_verticalScrollbar = (GetTemplateChild(Parts.WinUI3.VerticalScrollBar) ?? GetTemplateChild(Parts.Uwp.VerticalScrollBar)) as ScrollBar;
			_isVerticalScrollBarMaterialized = true;

			if (_verticalScrollbar is null)
			{
				return;
			}

			DetachScrollBars();
			AttachScrollBars();
		}

		private void MaterializeHorizontalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isHorizontalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			_horizontalScrollbar = (GetTemplateChild(Parts.WinUI3.HorizontalScrollBar) ?? GetTemplateChild(Parts.Uwp.HorizontalScrollBar)) as ScrollBar;
			_isHorizontalScrollBarMaterialized = true;

			if (_horizontalScrollbar is null)
			{
				return;
			}

			DetachScrollBars();
			AttachScrollBars();
		}

		private static void DetachScrollBars(object sender, RoutedEventArgs e) // OnUnloaded
			=> (sender as ScrollViewer)?.DetachScrollBars();

		private void DetachScrollBars()
		{
			if (_verticalScrollbar != null)
			{
				_verticalScrollbar.Scroll -= OnVerticalScrollBarScrolled;
				_verticalScrollbar.PointerEntered -= ShowScrollBarSeparator;
				_verticalScrollbar.PointerExited -= HideScrollBarSeparator;
			}

			if (_horizontalScrollbar != null)
			{
				_horizontalScrollbar.Scroll -= OnHorizontalScrollBarScrolled;
				_horizontalScrollbar.PointerEntered -= ShowScrollBarSeparator;
				_horizontalScrollbar.PointerExited -= HideScrollBarSeparator;
			}

			PointerMoved -= ShowScrollIndicator;
		}

		private void EnsureAttachScrollBars()
		{
			DetachScrollBars(); // Avoid double subscribe due to OnApplyTemplate
			AttachScrollBars();
		}

		private void AttachScrollBars()
		{
			bool hasManagedVerticalScrollBar;
			if (_verticalScrollbar is { } vertical)
			{
				vertical.Scroll += OnVerticalScrollBarScrolled;
				hasManagedVerticalScrollBar = true;

				PointerMoved += ShowScrollIndicator;
			}
			else
			{
				hasManagedVerticalScrollBar = false;
			}

			bool hasManagedHorizontalScrollBar;
			if (_horizontalScrollbar is { } horizontal)
			{
				horizontal.Scroll += OnHorizontalScrollBarScrolled;
				hasManagedHorizontalScrollBar = true;

				if (!hasManagedVerticalScrollBar)
				{
					PointerMoved += ShowScrollIndicator;
				}
			}
			else
			{
				hasManagedHorizontalScrollBar = false;
			}

			if (hasManagedVerticalScrollBar && hasManagedHorizontalScrollBar)
			{
				_verticalScrollbar!.PointerEntered += ShowScrollBarSeparator;
				_horizontalScrollbar!.PointerEntered += ShowScrollBarSeparator;
				_verticalScrollbar!.PointerExited += HideScrollBarSeparator;
				_horizontalScrollbar!.PointerExited += HideScrollBarSeparator;
			}
		}

		private void OnVerticalScrollBarScrolled(object sender, ScrollEventArgs e)
		{
			// We animate only if the user clicked in the scroll bar, and disable otherwise
			// (especially, we disable animation when dragging the thumb)

			// On Windows, ScrollViewer ignores ScrollBar's SmallChange/LargeChange values.
			// No matter how SmallChange/LargeChange are set, ScrollViewer will always scroll by 16 (instead of SmallChange)
			// or ScrollViewer's Height (instead of LargeChange).
			var (immediate, offset) = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => (false, VerticalOffset + ActualHeight),
				ScrollEventType.LargeDecrement => (false, VerticalOffset - ActualHeight),
				ScrollEventType.SmallIncrement => (false, VerticalOffset + 16),
				ScrollEventType.SmallDecrement => (false, VerticalOffset - 16),
				_ => (true, e.NewValue)
			};

			ChangeViewCore(
				horizontalOffset: null,
				verticalOffset: offset,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}

		private void OnHorizontalScrollBarScrolled(object sender, ScrollEventArgs e)
		{
			// We animate only if the user clicked in the scroll bar, and disable otherwise
			// (especially, we disable animation when dragging the thumb)

			// On Windows, ScrollViewer ignores ScrollBar's SmallChange/LargeChange values.
			// No matter how SmallChange/LargeChange are set, ScrollViewer will always scroll by 16 (instead of SmallChange)
			// or ScrollViewer's Width (instead of LargeChange).
			var (immediate, offset) = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => (false, HorizontalOffset + ActualWidth),
				ScrollEventType.LargeDecrement => (false, HorizontalOffset - ActualWidth),
				ScrollEventType.SmallIncrement => (false, HorizontalOffset + 16),
				ScrollEventType.SmallDecrement => (false, HorizontalOffset - 16),
				_ => (true, e.NewValue)
			};

			ChangeViewCore(
				horizontalOffset: offset,
				verticalOffset: null,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}
		#endregion
	}
}
