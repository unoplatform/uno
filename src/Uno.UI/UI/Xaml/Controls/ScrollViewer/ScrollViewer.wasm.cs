#nullable enable
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Uno.UI;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		internal bool CancelNextNativeScroll { get; private set; }
		internal Size ScrollBarSize => (_presenter as ScrollContentPresenter)?.ScrollBarSize ?? default;

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			if (zoomFactor.HasValue)
			{
				_log.Warn("ZoomFactor not supported yet on WASM target.");
			}

			if (_presenter != null)
			{
				_presenter.ScrollTo(horizontalOffset, verticalOffset, disableAnimation);
				return true;
			}
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Warn("Cannot ChangeView as ScrollContentPresenter is not ready yet.");
			}
			return false;
		}

		partial void UpdatePartial(bool isIntermediate)
		{
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(HorizontalOffset), HorizontalOffset);
				UpdateDOMXamlProperty(nameof(VerticalOffset), VerticalOffset);
			}
		}

		/// <summary>
		/// Trim excess scroll, which can be present if the content size is reduced.
		/// </summary>
		partial void TrimOverscroll(Orientation orientation)
		{
			if (_presenter is not null)
			{
				var (contentExtent, presenterViewportSize, offset) = orientation switch
				{
					Orientation.Vertical => (ExtentHeight, ViewportHeight, VerticalOffset),
					_ => (ExtentWidth, ViewportWidth, HorizontalOffset),
				};
				var viewportEnd = offset + presenterViewportSize;
				var overscroll = contentExtent - viewportEnd;
				if (offset > 0 && overscroll < -0.5)
				{
					ChangeViewForOrientation(orientation, overscroll);
				}
			}
		}

		private void ChangeViewForOrientation(Orientation orientation, double scrollAdjustment)
		{
			if (orientation == Orientation.Vertical)
			{
				ChangeView(null, VerticalOffset + scrollAdjustment, null, disableAnimation: true);
			}
			else
			{
				ChangeView(HorizontalOffset + scrollAdjustment, null, null, disableAnimation: true);
			}
		}

		private partial void OnLoadedPartial()
		{
			AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
			AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
		}

		private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			// There is no way to distinguish the cause of a `scroll` DOM event, so we need to discriminate between
			// wheel scrolling and keyboard scrolling before we get the `scroll` event. We only need to cancel keyboard
			// scrolling (due to space, etc.)
			CancelNextNativeScroll = false;
		}

		private partial void OnUnloadedPartial()
		{
			RemoveHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
			RemoveHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged));
		}

		private void OnKeyDown(object sender, KeyRoutedEventArgs args)
		{
			// event got handled before it reached ScrollViewer, cancel scrolling
			CancelNextNativeScroll = args.Handled;
		}
	}
}
