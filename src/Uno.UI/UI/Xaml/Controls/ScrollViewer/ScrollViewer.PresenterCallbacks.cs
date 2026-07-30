#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		// Presenter to Control, i.e. OnPresenterScrolled
		internal void OnPresenterScrolled(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			_pendingHorizontalOffset = horizontalOffset;
			_pendingVerticalOffset = verticalOffset;

#if __SKIA__
			// MUX Reference: keep the SV's tracked offsets in sync with the SCP's
			// rendered position so the Get*Offset family of methods (used by
			// ScrollForFocusNavigation, snap-points reaction, etc.) reflects
			// reality during touch-driven scroll/inertia.
			NotifyPresenterOffsetsChanged(horizontalOffset, verticalOffset, ZoomFactor);
#endif

			if (isIntermediate && UpdatesMode != Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous)
			{
				RequestUpdate();
				_snapPointsTimer?.Stop();
			}
			else
			{
				Update(isIntermediate);

				if (isIntermediate)
				{
					// when intermediate (aka manual) scrolling occurs,
					// we want to cancel any pending snapping, to prevent snapping to occur mid-scroll.
					_snapPointsTimer?.Stop();
				}
				if (!isIntermediate
#if __APPLE_UIKIT__ || __ANDROID__
					&& (_presenter as ListViewBaseScrollContentPresenter)?.NativePanel?.UseNativeSnapping != true
#endif
					)
				{
					if (HorizontalSnapPointsType != SnapPointsType.None
						|| VerticalSnapPointsType != SnapPointsType.None
						|| ShouldSnapToTouchTextBox())
					{
						_horizontalOffsetForSnapPoints = horizontalOffset;
						_verticalOffsetForSnapPoints = verticalOffset;

						if (_snapPointsTimer == null)
						{
							_snapPointsTimer = global::Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
							_snapPointsTimer.IsRepeating = false;
							_snapPointsTimer.Interval = FeatureConfiguration.ScrollViewer.SnapDelay;
							_snapPointsTimer.Tick += (snd, evt) =>
							{
								DelayedMoveToSnapPoint();
							};
						}

						_snapPointsTimer.Start();
					}
				}
			}

#if __WASM__
			// On WASM, a large wheel scroll can be a large number of OnScroll events in sequence.
			// In that case, the queue will be drowning with scroll events before any chance of layout
			// updates. The ScrollContentPresenter will scroll smoothly since the native scrolling/rendering
			// is on a separate thread, but the ScrollBars will be frozen until the end of the (long) scrolling
			// duration.
			_horizontalScrollbar?.Arrange(LayoutInformation.GetLayoutSlot(_horizontalScrollbar));
			_verticalScrollbar?.Arrange(LayoutInformation.GetLayoutSlot(_verticalScrollbar));
#endif
		}

		// Presenter to Control, i.e. OnPresenterZoomed
		internal void OnPresenterZoomed(float zoomFactor)
		{
			ZoomFactor = zoomFactor;

#if __SKIA__
			// Refresh pixel offsets with the new zoom factor.
			NotifyPresenterOffsetsChanged(HorizontalOffset, VerticalOffset, zoomFactor);
#endif

			// Note: We should also defer the intermediate zoom changes
			Update(isIntermediate: false);

			UpdateZoomedContentAlignment();
		}
	}
}
