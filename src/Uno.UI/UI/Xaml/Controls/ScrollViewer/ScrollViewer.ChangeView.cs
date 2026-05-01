#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		public void ScrollToHorizontalOffset(double offset)
			=> ChangeView(offset, null, null, true);

		public void ScrollToVerticalOffset(double offset)
			=> ChangeView(null, offset, null, true);

#if !__SKIA__
		// On Skia these are provided by the WinUI port (ScrollViewer.mux.cs).

		/// <summary>
		/// Scroll content by one page to the left.
		/// </summary>
		internal void PageLeft()
			=> HandleHorizontalScroll(ScrollEventType.LargeDecrement);

		/// <summary>
		/// Scroll content by one line to the right.
		/// </summary>
		internal void LineLeft()
			=> HandleHorizontalScroll(ScrollEventType.SmallDecrement);

		/// <summary>
		/// Scroll content by one line to the right.
		/// </summary>
		internal void LineRight()
			=> HandleHorizontalScroll(ScrollEventType.SmallIncrement);

		/// <summary>
		/// Scroll content by one page to the right.
		/// </summary>
		internal void PageRight()
			=> HandleHorizontalScroll(ScrollEventType.LargeIncrement);

		/// <summary>
		/// Scroll content by one page to the top.
		/// </summary>
		internal void PageUp()
			=> HandleVerticalScroll(ScrollEventType.LargeDecrement);

		/// <summary>
		/// Scroll content by one line to the top.
		/// </summary>
		internal void LineUp()
			=> HandleVerticalScroll(ScrollEventType.SmallDecrement);

		/// <summary>
		/// Scroll content by one line to the bottom.
		/// </summary>
		internal void LineDown()
			=> HandleVerticalScroll(ScrollEventType.SmallIncrement);

		/// <summary>
		/// Scroll content by one page to the bottom.
		/// </summary>
		internal void PageDown()
			=> HandleVerticalScroll(ScrollEventType.LargeIncrement);

		/// <summary>
		/// Scroll content to the beginning.
		/// </summary>
		internal void PageHome()
			=> HandleVerticalScroll(ScrollEventType.First);

		/// <summary>
		/// Scroll content to the end.
		/// </summary>
		internal void PageEnd()
			=> HandleVerticalScroll(ScrollEventType.Last);
#endif

		/// <summary>
		/// Causes the ScrollViewer to load a new view into the viewport using the specified offsets and zoom factor, and optionally disables scrolling animation.
		/// </summary>
		/// <param name="horizontalOffset">A value between 0 and ScrollableWidth that specifies the distance the content should be scrolled horizontally.</param>
		/// <param name="verticalOffset">A value between 0 and ScrollableHeight that specifies the distance the content should be scrolled vertically.</param>
		/// <param name="zoomFactor">A value between MinZoomFactor and MaxZoomFactor that specifies the required target ZoomFactor.</param>
		/// <returns>true if the view is changed; otherwise, false.</returns>
		public bool ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor)
			=> ChangeView(horizontalOffset, verticalOffset, zoomFactor, false);

		/// <summary>
		/// Causes the ScrollViewer to load a new view into the viewport using the specified offsets and zoom factor, and optionally disables scrolling animation.
		/// </summary>
		/// <param name="horizontalOffset">A value between 0 and ScrollableWidth that specifies the distance the content should be scrolled horizontally.</param>
		/// <param name="verticalOffset">A value between 0 and ScrollableHeight that specifies the distance the content should be scrolled vertically.</param>
		/// <param name="zoomFactor">A value between MinZoomFactor and MaxZoomFactor that specifies the required target ZoomFactor.</param>
		/// <param name="disableAnimation">true to disable zoom/pan animations while changing the view; otherwise, false. The default is false.</param>
		/// <returns>true if the view is changed; otherwise, false.</returns>
		public bool ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ChangeView(horizontalOffset={horizontalOffset}, verticalOffset={verticalOffset}, zoomFactor={zoomFactor}, disableAnimation={disableAnimation})");
			}

			if (horizontalOffset == null && verticalOffset == null && zoomFactor == null)
			{
				return true; // nothing to do
			}

			var verticalOffsetChanged = verticalOffset != null && verticalOffset != VerticalOffset;
			var horizontalOffsetChanged = horizontalOffset != null && horizontalOffset != HorizontalOffset;
			var zoomFactorChanged = zoomFactor != null && zoomFactor != ZoomFactor;

			if (verticalOffsetChanged || horizontalOffsetChanged || zoomFactorChanged)
			{
				return ChangeViewCore(
					horizontalOffset,
					verticalOffset,
					zoomFactor,
					disableAnimation,
					shouldSnap: true);
			}
			else
			{
				return false;
			}
		}

		private bool ChangeViewCore(
			double? horizontalOffset,
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation,
			bool shouldSnap)
		{
			if (horizontalOffset is null && verticalOffset is null && zoomFactor is null)
			{
				return false;
			}

			if (shouldSnap)
			{
				AdjustOffsetsForSnapPoints(ref horizontalOffset, ref verticalOffset, zoomFactor, canBypassSingle: true);
			}

			return ChangeViewNative(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
		}
	}
}
