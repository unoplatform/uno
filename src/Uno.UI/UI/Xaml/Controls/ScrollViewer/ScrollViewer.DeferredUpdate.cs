#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		#region Deferred update (i.e. ViewChanged) support
		private bool _hasPendingUpdate;
		private double _pendingHorizontalOffset;
		private double _pendingVerticalOffset;

		private void RequestUpdate()
		{
			if (_hasPendingUpdate)
			{
				return;
			}

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (_hasPendingUpdate)
				{
					Update(isIntermediate: true);
				}
			});
			_hasPendingUpdate = true;
		}

		private void Update(bool isIntermediate)
		{
			_hasPendingUpdate = false;

			var oldHorizontalOffset = HorizontalOffset;
			var oldVerticalOffset = VerticalOffset;

			HorizontalOffset = _pendingHorizontalOffset;
			VerticalOffset = _pendingVerticalOffset;

			if (!isIntermediate && (oldHorizontalOffset != HorizontalOffset || oldVerticalOffset != VerticalOffset))
			{
				// Mirrors ScrollContentPresenter_Partial.cpp:871 (SetHorizontalOffsetPrivate /
				// SetVerticalOffsetPrivate): discrete offset changes schedule an arrange so that
				// AnchoringArrangeOverride re-selects CurrentAnchor against the new viewport.
				// Intermediate ticks (DManip-driven inertia / drag) are skipped to match WinUI,
				// which lets the compositor drive offsets during manipulation without re-running
				// layout per frame.
				InvalidateArrange();
			}

			// Not ideal, and doesn't match WinUI. This can miss raising some automation events.
			if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged) &&
				GetAutomationPeer() is ScrollViewerAutomationPeer peer)
			{
				peer.RaiseAutomationEvents(
					ExtentWidth,
					ExtentHeight,
					ViewportWidth,
					ViewportHeight,
					MinHorizontalOffset,
					MinVerticalOffset,
					oldHorizontalOffset,
					oldVerticalOffset);
			}

			UpdatePartial(isIntermediate);

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });
		}

		partial void UpdatePartial(bool isIntermediate);
		#endregion

		#region SnapPoints enforcement
		private DispatcherQueueTimer? _snapPointsTimer;
		private double? _horizontalOffsetForSnapPoints;
		private double? _verticalOffsetForSnapPoints;

		private void DelayedMoveToSnapPoint()
		{
			var h = _horizontalOffsetForSnapPoints;
			var v = _verticalOffsetForSnapPoints;

			AdjustOffsetsForSnapPoints(ref h, ref v, ZoomFactor);

			if ((h == null || h == HorizontalOffset) && (v == null || v == VerticalOffset))
			{
				return; // already on a snap point
			}

			ChangeViewCore(
				horizontalOffset: h,
				verticalOffset: v,
				zoomFactor: null,
				disableAnimation: false,
				shouldSnap: false);

			_horizontalOffsetForSnapPoints = null;
			_verticalOffsetForSnapPoints = null;
		}
		#endregion
	}
}
