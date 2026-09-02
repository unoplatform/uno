#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
#if __CROSSRUNTIME__
		private void UpdateZoomedContentAlignment() => (_presenter as UIElement)?.InvalidateArrange();
#endif

#if !__SKIA__
		// On Skia these are provided by the WinUI port (ScrollViewer.mux.cs).

		/// <summary>
		/// Handles the vertical ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			var targetOffset = scrollEventType switch
			{
				ScrollEventType.ThumbPosition or ScrollEventType.ThumbTrack => offset,
				ScrollEventType.LargeDecrement => VerticalOffset - ViewportHeight,
				ScrollEventType.LargeIncrement => VerticalOffset + ViewportHeight,
				ScrollEventType.SmallDecrement => VerticalOffset - ScrollViewerLineDelta,
				ScrollEventType.SmallIncrement => VerticalOffset + ScrollViewerLineDelta,
				ScrollEventType.First => 0.0,
				ScrollEventType.Last => ScrollableHeight,
				_ => VerticalOffset,
			};

			targetOffset = global::System.Math.Clamp(targetOffset, 0.0, ScrollableHeight);
			if (targetOffset != VerticalOffset)
			{
				ChangeViewCore(
					horizontalOffset: null,
					verticalOffset: targetOffset,
					zoomFactor: null,
					disableAnimation: true,
					shouldSnap: false);
			}
		}

		/// <summary>
		/// Handles the horizontal ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleHorizontalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			var targetOffset = scrollEventType switch
			{
				ScrollEventType.ThumbPosition or ScrollEventType.ThumbTrack => offset,
				ScrollEventType.LargeDecrement => HorizontalOffset - ViewportWidth,
				ScrollEventType.LargeIncrement => HorizontalOffset + ViewportWidth,
				ScrollEventType.SmallDecrement => HorizontalOffset - ScrollViewerLineDelta,
				ScrollEventType.SmallIncrement => HorizontalOffset + ScrollViewerLineDelta,
				ScrollEventType.First => 0.0,
				ScrollEventType.Last => ScrollableWidth,
				_ => HorizontalOffset,
			};

			targetOffset = global::System.Math.Clamp(targetOffset, 0.0, ScrollableWidth);
			if (targetOffset != HorizontalOffset)
			{
				ChangeViewCore(
					horizontalOffset: targetOffset,
					verticalOffset: null,
					zoomFactor: null,
					disableAnimation: true,
					shouldSnap: false);
			}
		}
#endif
	}
}
