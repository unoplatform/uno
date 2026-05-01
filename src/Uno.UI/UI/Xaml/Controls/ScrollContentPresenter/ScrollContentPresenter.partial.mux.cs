// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollContentPresenter_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
#if __SKIA__
#pragma warning disable IDE0051 // Private member is unused (placeholder for full impl)

		// #region Foundational IScrollInfo implementation ported from ScrollContentPresenter_Partial.cpp

		// Gets a value indicating whether the current ScrollContentPresenter is a scrolling client.
		// In WinUI this is true iff the m_wrScrollInfo weak ref points to this instance — i.e. no inner
		// IManipulationDataProvider has registered as the IScrollInfo. In the Uno managed scroll path
		// we always answer true: logical scrolling delegated to an inner panel is not yet wired up here.
		// TODO Uno: revisit when virtualizing-panel logical scrolling lands on Skia.
		internal bool IsScrollClient() => true;

		// Get (or create on demand) the ScrollContentPresenter's scrolling state.
		internal ScrollData GetScrollData()
		{
			if (m_pScrollData is null)
			{
				m_pScrollData = ScrollData.Create();
			}
			return m_pScrollData;
		}

		// Property that controls how ScrollContentPresenter measures its
		// Child during layout.  If true, it measures child at infinite
		// space in this dimension.
		internal bool GetCanVerticallyScroll()
		{
			if (IsScrollClient())
			{
				return GetScrollData().m_canVerticallyScroll;
			}
			return false;
		}

		internal void PutCanVerticallyScroll(bool value)
		{
			if (IsScrollClient())
			{
				var pScrollData = GetScrollData();
				if (pScrollData.m_canVerticallyScroll != value)
				{
					pScrollData.m_canVerticallyScroll = value;
					InvalidateMeasure();
				}
			}
		}

		// Property that controls how ScrollContentPresenter measures its
		// Child during layout.  If true, it measures child at infinite
		// space in this dimension.
		internal bool GetCanHorizontallyScroll()
		{
			if (IsScrollClient())
			{
				return GetScrollData().m_canHorizontallyScroll;
			}
			return false;
		}

		internal void PutCanHorizontallyScroll(bool value)
		{
			if (IsScrollClient())
			{
				var pScrollData = GetScrollData();
				if (pScrollData.m_canHorizontallyScroll != value)
				{
					pScrollData.m_canHorizontallyScroll = value;
					InvalidateMeasure();
				}
			}
		}

		// Gets the horizontal size of the extent.
		internal double GetExtentWidth() => IsScrollClient() ? GetScrollData().m_extent.Width : 0.0;

		// Gets the vertical size of the extent.
		internal double GetExtentHeight() => IsScrollClient() ? GetScrollData().m_extent.Height : 0.0;

		// Gets the horizontal size of the viewport for this content.
		internal double GetViewportWidth() => IsScrollClient() ? GetScrollData().m_viewport.Width : 0.0;

		// Gets the vertical size of the viewport for this content.
		internal double GetViewportHeight() => IsScrollClient() ? GetScrollData().m_viewport.Height : 0.0;

		// Gets the horizontal offset of the scrolled content.
		internal double GetHorizontalOffset() => IsScrollClient() ? GetScrollData().m_ComputedOffset.X : 0.0;

		// Gets the vertical offset of the scrolled content.
		internal double GetVerticalOffset() => IsScrollClient() ? GetScrollData().m_ComputedOffset.Y : 0.0;

		// Gets the minimal horizontal offset of the scrolled content.
		internal double GetMinHorizontalOffset() => IsScrollClient() ? GetScrollData().m_MinOffset.X : 0.0;

		// Gets the minimal vertical offset of the scrolled content.
		internal double GetMinVerticalOffset() => IsScrollClient() ? GetScrollData().m_MinOffset.Y : 0.0;

		// ScrollOwner is the container that controls any scrollbars,
		// headers, etc... that are dependent on this IScrollInfo's
		// properties.  Implementers of IScrollInfo should call
		// InvalidateScrollInfo() on this object when properties change.
		internal IScrollOwner GetScrollOwner()
		{
			if (IsScrollClient())
			{
				return GetScrollData().GetScrollOwner();
			}
			return null;
		}

		internal void PutScrollOwner(IScrollOwner value)
		{
			if (IsScrollClient())
			{
				GetScrollData().SetScrollOwner(value);
			}
		}

		// Scroll content by one line to the top.
		public void LineUp()
		{
			if (IsScrollClient())
			{
				var offset = GetVerticalOffset();
				SetVerticalOffset(offset - ScrollViewer.ScrollViewerLineDelta);
			}
		}

		// Scroll content by one line to the bottom.
		public void LineDown()
		{
			if (IsScrollClient())
			{
				var offset = GetVerticalOffset();
				SetVerticalOffset(offset + ScrollViewer.ScrollViewerLineDelta);
			}
		}

		// Scroll content by one line to the left.
		public void LineLeft()
		{
			if (IsScrollClient())
			{
				var offset = GetHorizontalOffset();
				SetHorizontalOffset(offset - ScrollViewer.ScrollViewerLineDelta);
			}
		}

		// Scroll content by one line to the right.
		public void LineRight()
		{
			if (IsScrollClient())
			{
				var offset = GetHorizontalOffset();
				SetHorizontalOffset(offset + ScrollViewer.ScrollViewerLineDelta);
			}
		}

		// Scroll content by one page to the top.
		public void PageUp()
		{
			if (IsScrollClient())
			{
				var offset = GetVerticalOffset();
				var viewport = GetViewportHeight();
				// TODO Uno: GetZoomedHeadersSize stub returns zero until headers land in Phase 6.
				var sizeHeaders = new Size(0, 0);
				viewport = Math.Max(ScrollViewer.ScrollViewerLineDelta, viewport - sizeHeaders.Height);
				SetVerticalOffset(offset - viewport);
			}
		}

		// Scroll content by one page to the bottom.
		public void PageDown()
		{
			if (IsScrollClient())
			{
				var offset = GetVerticalOffset();
				var viewport = GetViewportHeight();
				var sizeHeaders = new Size(0, 0);
				viewport = Math.Max(ScrollViewer.ScrollViewerLineDelta, viewport - sizeHeaders.Height);
				SetVerticalOffset(offset + viewport);
			}
		}

		// Scroll content by one page to the left.
		public void PageLeft()
		{
			if (IsScrollClient())
			{
				var offset = GetHorizontalOffset();
				var viewport = GetViewportWidth();
				var sizeHeaders = new Size(0, 0);
				viewport = Math.Max(ScrollViewer.ScrollViewerLineDelta, viewport - sizeHeaders.Width);
				SetHorizontalOffset(offset - viewport);
			}
		}

		// Scroll content by one page to the right.
		public void PageRight()
		{
			if (IsScrollClient())
			{
				var offset = GetHorizontalOffset();
				var viewport = GetViewportWidth();
				var sizeHeaders = new Size(0, 0);
				viewport = Math.Max(ScrollViewer.ScrollViewerLineDelta, viewport - sizeHeaders.Width);
				SetHorizontalOffset(offset + viewport);
			}
		}

		// Scroll content by one line to the top.
		public void MouseWheelUp() => MouseWheelUp(ScrollViewer.ScrollViewerDefaultMouseWheelDelta);

		// IScrollInfo::MouseWheelUp implementation which takes the mouse wheel delta into account.
		public void MouseWheelUp(uint mouseWheelDelta)
		{
			if (IsScrollClient())
			{
				var size = DesiredSize;
				var canVerticallyScroll = GetCanVerticallyScroll();
				if (canVerticallyScroll)
				{
					var offset = GetVerticalOffset();
					SetVerticalOffset(offset - ScrollViewer.GetVerticalScrollWheelDelta(size, mouseWheelDelta));
				}
				else
				{
					var offset = GetHorizontalOffset();
					SetHorizontalOffset(offset - ScrollViewer.GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
				}
			}
		}

		// Scroll content by one line to the bottom.
		public void MouseWheelDown() => MouseWheelDown(ScrollViewer.ScrollViewerDefaultMouseWheelDelta);

		// IScrollInfo::MouseWheelDown implementation which takes the mouse wheel delta into account.
		public void MouseWheelDown(uint mouseWheelDelta)
		{
			if (IsScrollClient())
			{
				var size = DesiredSize;
				var canVerticallyScroll = GetCanVerticallyScroll();
				if (canVerticallyScroll)
				{
					var offset = GetVerticalOffset();
					SetVerticalOffset(offset + ScrollViewer.GetVerticalScrollWheelDelta(size, mouseWheelDelta));
				}
				else
				{
					var offset = GetHorizontalOffset();
					SetHorizontalOffset(offset + ScrollViewer.GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
				}
			}
		}

		// Scroll content by one page to the left.
		public void MouseWheelLeft() => MouseWheelLeft(ScrollViewer.ScrollViewerDefaultMouseWheelDelta);

		// IScrollInfo::MouseWheelLeft implementation which takes the mouse wheel delta into account.
		public void MouseWheelLeft(uint mouseWheelDelta)
		{
			if (IsScrollClient())
			{
				var size = DesiredSize;
				var offset = GetHorizontalOffset();
				SetHorizontalOffset(offset - ScrollViewer.GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
			}
		}

		// Scroll content by one page to the right.
		public void MouseWheelRight() => MouseWheelRight(ScrollViewer.ScrollViewerDefaultMouseWheelDelta);

		// IScrollInfo::MouseWheelRight implementation which takes the mouse wheel delta into account.
		public void MouseWheelRight(uint mouseWheelDelta)
		{
			if (IsScrollClient())
			{
				var size = DesiredSize;
				var offset = GetHorizontalOffset();
				SetHorizontalOffset(offset + ScrollViewer.GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
			}
		}

		// Set the HorizontalOffset to the passed value.
		public void SetHorizontalOffset(double offset) => SetHorizontalOffsetPrivate(offset, out _, out _, out _);

		internal void SetHorizontalOffsetPrivate(
			double offset,
			out bool isScrollRequested,
			out double currentOffset,
			out double requestedOffset)
		{
			isScrollRequested = false;
			currentOffset = 0.0;
			requestedOffset = 0.0;

			var canHorizontallyScroll = GetCanHorizontallyScroll();
			if (canHorizontallyScroll)
			{
				var pScrollData = GetScrollData();
				var extentWidth = GetExtentWidth();
				var viewportWidth = GetViewportWidth();

				ValidateInputOffset(offset, pScrollData.m_MinOffset.X, extentWidth - viewportWidth, out var scrollX);

				var currentX = pScrollData.GetOffsetX();
				currentOffset = currentX;

				if (!DoubleUtil.AreClose(currentX, scrollX))
				{
					pScrollData.SetOffsetX(scrollX);
					InvalidateArrange();
					m_scrollRequested = true;
					isScrollRequested = true;
					requestedOffset = scrollX;
				}
			}
		}

		// Set the VerticalOffset to the passed value.
		public void SetVerticalOffset(double offset) => SetVerticalOffsetPrivate(offset, out _, out _, out _);

		internal void SetVerticalOffsetPrivate(
			double offset,
			out bool isScrollRequested,
			out double currentOffset,
			out double requestedOffset)
		{
			isScrollRequested = false;
			currentOffset = 0.0;
			requestedOffset = 0.0;

			var canVerticallyScroll = GetCanVerticallyScroll();
			if (canVerticallyScroll)
			{
				var pScrollData = GetScrollData();
				var extentHeight = GetExtentHeight();
				var viewportHeight = GetViewportHeight();

				ValidateInputOffset(offset, pScrollData.m_MinOffset.Y, extentHeight - viewportHeight, out var scrollY);

				var currentY = pScrollData.GetOffsetY();
				currentOffset = currentY;

				if (!DoubleUtil.AreClose(currentY, scrollY))
				{
					pScrollData.SetOffsetY(scrollY);
					InvalidateArrange();
					m_scrollRequested = true;
					isScrollRequested = true;
					requestedOffset = scrollY;
				}
			}
		}

		// Set the HorizontalOffset and VerticalOffset to the passed values, using the provided extents to determine the upper boundaries.
		internal void SetOffsetsWithExtents(
			double offsetX,
			double offsetY,
			double extentWidth,
			double extentHeight)
		{
			var bIsOffsetChanged = false;
			ScrollData pScrollData = null;

			var bCanHorizontallyScroll = GetCanHorizontallyScroll();
			if (bCanHorizontallyScroll)
			{
				var viewportWidth = GetViewportWidth();
				pScrollData = GetScrollData();
				ValidateInputOffset(offsetX, pScrollData.m_MinOffset.X, extentWidth - viewportWidth, out var scrollX);

				var currentX = pScrollData.GetOffsetX();
				if (!DoubleUtil.AreClose(currentX, scrollX))
				{
					pScrollData.SetOffsetX(scrollX);
					bIsOffsetChanged = true;
				}
			}

			var bCanVerticallyScroll = GetCanVerticallyScroll();
			if (bCanVerticallyScroll)
			{
				var viewportHeight = GetViewportHeight();
				pScrollData ??= GetScrollData();
				ValidateInputOffset(offsetY, pScrollData.m_MinOffset.Y, extentHeight - viewportHeight, out var scrollY);

				var currentY = pScrollData.GetOffsetY();
				if (!DoubleUtil.AreClose(currentY, scrollY))
				{
					pScrollData.SetOffsetY(scrollY);
					bIsOffsetChanged = true;
				}
			}

			if (bIsOffsetChanged)
			{
				InvalidateArrange();
				m_scrollRequested = true;
			}
		}

		// Determine how down we need to scroll to accommodate the desired view.
		internal static void ComputeScrollOffsetWithMinimalScroll(
			float topView,
			float bottomView,
			float topChild,
			float bottomChild,
			out float pOffset)
		{
			var above = DoubleUtil.LessThan(topChild, topView) && DoubleUtil.LessThan(bottomChild, bottomView);
			var below = DoubleUtil.GreaterThan(bottomChild, bottomView) && DoubleUtil.GreaterThan(topChild, topView);
			var larger = (bottomChild - topChild) > (bottomView - topView);

			// # CHILD POSITION       CHILD SIZE      SCROLL      REMEDY
			// 1 Above viewport       <= viewport     Down        Align top edge of child & viewport
			// 2 Above viewport       > viewport      Down        Align bottom edge of child & viewport
			// 3 Below viewport       <= viewport     Up          Align bottom edge of child & viewport
			// 4 Below viewport       > viewport      Up          Align top edge of child & viewport
			// 5 Entirely within viewport             NA          No scroll.
			// 6 Spanning viewport                    NA          No scroll.
			//
			// Note: "Above viewport" = childTop above viewportTop, childBottom above viewportBottom
			//       "Below viewport" = childTop below viewportTop, childBottom below viewportBottom
			// This child thus may overlap with the viewport, but will scroll the same direction
			if ((above && !larger) || (below && larger))
			{
				// Handle Cases:  1 & 4 above
				pOffset = topChild;
			}
			else if (above || below)
			{
				// Handle Cases: 2 & 3 above
				pOffset = bottomChild - (bottomView - topView);
			}
			else
			{
				// Handle cases: 5 & 6 above.
				pOffset = topView;
			}
		}

		// Ensure the offset we're scrolling to is valid.
		internal static void ValidateInputOffset(
			double offset,
			double minOffset,
			double maxOffset,
			out double pValidatedOffset)
		{
			if (double.IsNaN(offset))
			{
				// throw new ArgumentOutOfRangeException("offset");
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			pValidatedOffset = Math.Max(minOffset, Math.Min(offset, maxOffset));
		}

		// Returns an offset coerced into the [0, Extent - Viewport] range.
		internal static double CoerceOffset(
			double offset,
			double extent,
			double viewport)
		{
			if (offset > extent - viewport)
			{
				offset = extent - viewport;
			}

			if (offset < 0)
			{
				offset = 0;
			}

			return offset;
		}

		// Gets a value indicating whether the current ScrollData's m_Offset
		// and m_ComputedOffset are in sync or not.
		internal bool AreScrollOffsetsInSync()
		{
			var pScrollData = GetScrollData();
			if (pScrollData is not null)
			{
				return DoubleUtil.AreClose(pScrollData.GetOffsetX(), pScrollData.m_ComputedOffset.X) &&
					DoubleUtil.AreClose(pScrollData.GetOffsetY(), pScrollData.m_ComputedOffset.Y);
			}
			return false;
		}

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
