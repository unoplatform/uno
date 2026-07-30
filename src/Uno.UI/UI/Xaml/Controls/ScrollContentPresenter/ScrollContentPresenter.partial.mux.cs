// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollContentPresenter_Partial.cpp, commit dc46907e92

#nullable disable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
#if __SKIA__
	// MUX Reference ScrollContentPresenter_Partial.h:64 — `class ScrollContentPresenter : IScrollInfo`.
	partial class ScrollContentPresenter : IScrollInfo
	{
	}
#endif

	partial class ScrollContentPresenter
	{
#if __SKIA__
#pragma warning disable IDE0051 // Private member is unused

		// #region Foundational IScrollInfo implementation ported from ScrollContentPresenter_Partial.cpp

		// Gets a value indicating whether the current ScrollContentPresenter is a scrolling client.
		internal bool IsScrollClient()
			=> GetCurrentScrollInfo() is { } scrollInfo &&
				(scrollInfo == this || scrollInfo is ManipulationDataProviderScrollInfo);

		private IScrollInfo GetCurrentScrollInfo()
			=> m_wrScrollInfo is { } scrollInfoReference && scrollInfoReference.TryGetTarget(out var scrollInfo)
				? scrollInfo
				: null;

		internal void SetHeaders(UIElement topLeftHeader, UIElement topHeader, UIElement leftHeader)
		{
			var scrollViewer = GetScrollOwner() as ScrollViewer;
			var changed = false;

			if (m_trTopLeftHeader != topLeftHeader)
			{
				RemoveTopLeftHeader(scrollViewer, removeFromChildrenCollection: true);
				m_trTopLeftHeader = topLeftHeader;
				changed = true;
			}

			if (m_trTopHeader != topHeader)
			{
				RemoveTopHeader(scrollViewer, removeFromChildrenCollection: true);
				m_trTopHeader = topHeader;
				changed = true;
			}

			if (m_trLeftHeader != leftHeader)
			{
				RemoveLeftHeader(scrollViewer, removeFromChildrenCollection: true);
				m_trLeftHeader = leftHeader;
				changed = true;
			}

			if (changed)
			{
				InvalidateMeasure();
			}
		}

		// Adds a header to this ScrollContentPresenter's children.
		private void AddHeader(
			ScrollViewer scrollViewer,
			UIElement topLeftHeader,
			UIElement topHeader,
			UIElement leftHeader,
			bool isTopHeader,
			bool isLeftHeader)
		{
			var isTopLeftHeader = isTopHeader && isLeftHeader;
			var childCount = VisualTreeHelper.GetChildrenCount(this);

			global::System.Diagnostics.Debug.Assert(scrollViewer is not null);
			global::System.Diagnostics.Debug.Assert(isTopHeader || isLeftHeader);

			if (isTopLeftHeader)
			{
				global::System.Diagnostics.Debug.Assert(!m_isTopLeftHeaderChild);
				global::System.Diagnostics.Debug.Assert(topLeftHeader is not null);
				AddChild(topLeftHeader, childCount);
				m_isTopLeftHeaderChild = true;
			}
			else if (isTopHeader)
			{
				// TopLeftHeader element must be added after the TopHeader so the correct z-order gets applied.
				if (m_isTopLeftHeaderChild)
				{
					childCount--;
				}

				global::System.Diagnostics.Debug.Assert(!m_isTopHeaderChild);
				global::System.Diagnostics.Debug.Assert(topHeader is not null);
				AddChild(topHeader, childCount);
				m_isTopHeaderChild = true;
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(isLeftHeader);
				// TopLeftHeader and TopHeader elements must be added after the LeftHeader so the correct z-order gets applied.
				if (m_isTopHeaderChild)
				{
					childCount--;
				}
				if (m_isTopLeftHeaderChild)
				{
					childCount--;
				}

				global::System.Diagnostics.Debug.Assert(!m_isLeftHeaderChild);
				global::System.Diagnostics.Debug.Assert(leftHeader is not null);
				AddChild(leftHeader, childCount);
				m_isLeftHeaderChild = true;
			}
		}

		// Removes the top-left header from this ScrollContentPresenter's children
		// when removeFromChildrenCollection is True, resets its global scale factor
		// and notifies the owning ScrollViewer.
		private void RemoveTopLeftHeader(ScrollViewer scrollViewer, bool removeFromChildrenCollection)
		{
			if (m_isTopLeftHeaderChild && m_trTopLeftHeader is { } topLeftHeader)
			{
				if (removeFromChildrenCollection)
				{
					RemoveChild(topLeftHeader);
				}
				m_isTopLeftHeaderChild = false;
				topLeftHeader.ResetGlobalScaleFactor();

				// If spTopLeftHeader was the last header shown, also reset the GlobalScaleFactor sparse storage for the primary child.
				if (!m_isLeftHeaderChild && !m_isTopHeaderChild)
				{
					ResetPrimaryChildGlobalScaleFactor();
				}
			}
		}

		// Removes the top header from this ScrollContentPresenter's children
		// when removeFromChildrenCollection is True, resets its global scale factor
		// and notifies the owning ScrollViewer.
		private void RemoveTopHeader(ScrollViewer scrollViewer, bool removeFromChildrenCollection)
		{
			if (m_isTopHeaderChild && m_trTopHeader is { } topHeader)
			{
				if (removeFromChildrenCollection)
				{
					RemoveChild(topHeader);
				}
				m_isTopHeaderChild = false;
				topHeader.ResetGlobalScaleFactor();

				// If spTopHeader was the last header shown, also reset the GlobalScaleFactor sparse storage for the primary child.
				if (!m_isLeftHeaderChild && !m_isTopLeftHeaderChild)
				{
					ResetPrimaryChildGlobalScaleFactor();
				}
			}
		}

		// Removes the left header from this ScrollContentPresenter's children
		// when removeFromChildrenCollection is True, resets its global scale factor
		// and notifies the owning ScrollViewer.
		private void RemoveLeftHeader(ScrollViewer scrollViewer, bool removeFromChildrenCollection)
		{
			if (m_isLeftHeaderChild && m_trLeftHeader is { } leftHeader)
			{
				if (removeFromChildrenCollection)
				{
					RemoveChild(leftHeader);
				}
				m_isLeftHeaderChild = false;
				leftHeader.ResetGlobalScaleFactor();

				// If spLeftHeader was the last header shown, also reset the GlobalScaleFactor sparse storage for the primary child.
				if (!m_isTopHeaderChild && !m_isTopLeftHeaderChild)
				{
					ResetPrimaryChildGlobalScaleFactor();
				}
			}
		}

		private void ResetPrimaryChildGlobalScaleFactor()
			=> (Content as UIElement)?.ResetGlobalScaleFactor();

		private void UnparentHeaders()
		{
			var scrollViewer = GetScrollOwner() as ScrollViewer;
			RemoveTopLeftHeader(scrollViewer, removeFromChildrenCollection: true);
			RemoveTopHeader(scrollViewer, removeFromChildrenCollection: true);
			RemoveLeftHeader(scrollViewer, removeFromChildrenCollection: true);
		}

		internal void GetHeaderOwnership(
			DependencyObject element,
			out bool isElementDirectChild,
			out bool isElementInTopLeftHeader,
			out bool isElementInTopHeader,
			out bool isElementInLeftHeader,
			out bool isElementInContent)
		{
			isElementDirectChild = false;
			var isTopLeftDirectChild = false;
			var isTopDirectChild = false;
			var isLeftDirectChild = false;
			var isContentDirectChild = false;
			isElementInTopLeftHeader = IsHeaderOwner(m_trTopLeftHeader, element, out isTopLeftDirectChild);
			isElementInTopHeader = !isElementInTopLeftHeader && IsHeaderOwner(m_trTopHeader, element, out isTopDirectChild);
			isElementInLeftHeader = !isElementInTopLeftHeader && !isElementInTopHeader && IsHeaderOwner(m_trLeftHeader, element, out isLeftDirectChild);
			isElementInContent = !isElementInTopLeftHeader &&
				!isElementInTopHeader &&
				!isElementInLeftHeader &&
				Content is UIElement content &&
				IsHeaderOwner(content, element, out isContentDirectChild);

			isElementDirectChild = isTopLeftDirectChild || isTopDirectChild || isLeftDirectChild || isContentDirectChild;
		}

		private static bool IsHeaderOwner(UIElement owner, DependencyObject element, out bool isDirectChild)
		{
			isDirectChild = owner == element;
			return isDirectChild || owner?.IsAncestorOf(element) == true;
		}

		private void GetZoomedHeadersSize(out Size size)
		{
			size = default;
			if (GetScrollOwner() is ScrollViewer scrollViewer)
			{
				scrollViewer.GetHeadersSize(out size);
				size.Width *= m_fZoomFactor;
				size.Height *= m_fZoomFactor;
			}
		}

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
		// Note: the new SCP MeasureOverridePort / ArrangeOverridePort don't run on
		// Skia yet (the cross-platform path is still active), so VerifyScrollData
		// never refreshes ScrollData.m_extent / m_viewport. Fall back to the cross-
		// platform ExtentWidth/Height + ViewportWidth/Height DPs which the existing
		// Skia pipeline maintains. Once the port's Measure/Arrange take over the
		// fallback becomes redundant.
		internal double GetExtentWidth()
		{
			if (!IsScrollClient()) return 0.0;
			var fromScrollData = GetScrollData().m_extent.Width;
			return fromScrollData != 0.0 ? fromScrollData : ExtentWidth;
		}

		// Gets the vertical size of the extent.
		internal double GetExtentHeight()
		{
			if (!IsScrollClient()) return 0.0;
			var fromScrollData = GetScrollData().m_extent.Height;
			return fromScrollData != 0.0 ? fromScrollData : ExtentHeight;
		}

		// Gets the horizontal size of the viewport for this content.
		internal double GetViewportWidth()
		{
			if (!IsScrollClient()) return 0.0;
			var fromScrollData = GetScrollData().m_viewport.Width;
			return fromScrollData != 0.0 ? fromScrollData : ViewportWidth;
		}

		// Gets the vertical size of the viewport for this content.
		internal double GetViewportHeight()
		{
			if (!IsScrollClient()) return 0.0;
			var fromScrollData = GetScrollData().m_viewport.Height;
			return fromScrollData != 0.0 ? fromScrollData : ViewportHeight;
		}

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
				GetZoomedHeadersSize(out var sizeHeaders);
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
				GetZoomedHeadersSize(out var sizeHeaders);
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
				GetZoomedHeadersSize(out var sizeHeaders);
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
				GetZoomedHeadersSize(out var sizeHeaders);
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

		// Set the HorizontalOffset to the passed value. The public
		// SetHorizontalOffset is declared in the cross-platform
		// ScrollContentPresenter.cs and routes through the managed Set() so the
		// SCP's HorizontalOffset DP gets updated. This Internal variant is the
		// C++ entry-point used by the new port for internal flows that should
		// not touch the cross-platform path.
		internal void SetHorizontalOffsetInternal(double offset) => SetHorizontalOffsetPrivate(offset, out _, out _, out _);

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
					// Keep the managed presenter and the WinUI ScrollData path synchronized.
					CoerceOffsets(out _);
					pScrollData.GetScrollOwner()?.InvalidateScrollInfoImpl();
					// Drive the cross-platform managed scroll path so the visual scroll
					// actually moves on Skia. Without this, only ScrollData state changes
					// and the rendered content stays in place.
					Set(horizontalOffset: scrollX, disableAnimation: true);
				}
			}
		}

		// Set the VerticalOffset to the passed value. See
		// SetHorizontalOffsetInternal for the public/internal split rationale.
		internal void SetVerticalOffsetInternal(double offset) => SetVerticalOffsetPrivate(offset, out _, out _, out _);

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
					CoerceOffsets(out _);
					pScrollData.GetScrollOwner()?.InvalidateScrollInfoImpl();
					Set(verticalOffset: scrollY, disableAnimation: true);
				}
			}
		}

		// Set the HorizontalOffset and VerticalOffset to the passed values, using the provided extents to determine the upper boundaries.
		internal void SetOffsetsWithExtents(
			double offsetX,
			double offsetY,
			double extentWidth,
			double extentHeight,
			float zoomFactor)
		{
			var bIsOffsetChanged = false;
			ScrollData pScrollData = null;
			double? appliedOffsetX = null;
			double? appliedOffsetY = null;

			var bCanHorizontallyScroll = GetCanHorizontallyScroll();
			if (bCanHorizontallyScroll)
			{
				var viewportWidth = GetViewportWidth();
				pScrollData = GetScrollData();
				ValidateInputOffset(offsetX, pScrollData.m_MinOffset.X, extentWidth - viewportWidth, out var scrollX);
				appliedOffsetX = scrollX;

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
				appliedOffsetY = scrollY;

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

			if (bIsOffsetChanged || !DoubleUtil.AreClose(ZoomFactor, zoomFactor))
			{
				Set(
					horizontalOffset: appliedOffsetX,
					verticalOffset: appliedOffsetY,
					zoomFactor,
					disableAnimation: true);
			}
		}

		// #region Explicit IScrollInfo implementation
		// SCP exposes the WinUI ABI as method-shaped internal members; the
		// IScrollInfo internal contract is bridged through explicit interface
		// implementation so each member visibility stays consistent with what
		// the WinUI source declared.
		bool IScrollInfo.GetCanVerticallyScroll() => GetCanVerticallyScroll();
		void IScrollInfo.PutCanVerticallyScroll(bool value) => PutCanVerticallyScroll(value);
		bool IScrollInfo.GetCanHorizontallyScroll() => GetCanHorizontallyScroll();
		void IScrollInfo.PutCanHorizontallyScroll(bool value) => PutCanHorizontallyScroll(value);
		double IScrollInfo.GetExtentWidth() => GetExtentWidth();
		double IScrollInfo.GetExtentHeight() => GetExtentHeight();
		double IScrollInfo.GetViewportWidth() => GetViewportWidth();
		double IScrollInfo.GetViewportHeight() => GetViewportHeight();
		double IScrollInfo.GetHorizontalOffset() => GetHorizontalOffset();
		double IScrollInfo.GetVerticalOffset() => GetVerticalOffset();
		double IScrollInfo.GetMinHorizontalOffset() => GetMinHorizontalOffset();
		double IScrollInfo.GetMinVerticalOffset() => GetMinVerticalOffset();
		IScrollOwner IScrollInfo.GetScrollOwner() => GetScrollOwner();
		void IScrollInfo.PutScrollOwner(IScrollOwner value) => PutScrollOwner(value);
		global::Windows.Foundation.Rect IScrollInfo.MakeVisible(
			UIElement visual,
			global::Windows.Foundation.Rect rectangle,
			bool useAnimation,
			double horizontalAlignmentRatio,
			double verticalAlignmentRatio,
			double offsetX,
			double offsetY,
			out double appliedOffsetX,
			out double appliedOffsetY)
			=> MakeVisible(visual, rectangle, useAnimation, horizontalAlignmentRatio, verticalAlignmentRatio, offsetX, offsetY, out appliedOffsetX, out appliedOffsetY);
		// #endregion

		// ScrollContentPresenter implementation of its public MakeVisible method.
		// Does not animate the move by default.
		// (C++ source line 1038)
		public global::Windows.Foundation.Rect MakeVisible(
			// The element that should become visible.
			UIElement visual,
			// A rectangle representing in the visual's coordinate space to
			// make visible.
			global::Windows.Foundation.Rect rectangle)
		{
			return MakeVisible(
				visual,
				rectangle,
				false /*useAnimation*/,
				DoubleUtil.NaN /*horizontalAlignmentRatio*/,
				DoubleUtil.NaN /*verticalAlignmentRatio*/,
				0.0 /*offsetX*/,
				0.0 /*offsetY*/,
				out _ /*appliedOffsetX*/,
				out _ /*appliedOffsetY*/);
		}

		// This scrolls to make the rectangle in the UIElement's coordinate
		// space visible.
		// Alignment ratios are either -1 (i.e. no alignment to apply) or between
		// 0 and 1. For instance when the alignment ratio is 0, the near edge of
		// the 'rectangle' needs to align with the near edge of the viewport.
		// 'offset' is an additional amount of scrolling requested, beyond the
		// normal amount to bring the target into view and potentially align it.
		// That additional offset is only applied when the 'rectangle' does not
		// step outside the extents.
		// The 'appliedOffset' returned specifies how much of 'offset' was applied
		// so that potential parent bring-into-view contributors can attempt to
		// apply the remainder offset.
		// (C++ source line 1078)
		internal global::Windows.Foundation.Rect MakeVisible(
			// The element that should become visible.
			UIElement visual,
			// A rectangle representing in the visual's coordinate space to make visible.
			global::Windows.Foundation.Rect rectangle,
			// When set to True, the DManip ZoomToRect method is invoked.
			bool useAnimation,
			double horizontalAlignmentRatio,
			double verticalAlignmentRatio,
			double offsetX,
			double offsetY,
			out double appliedOffsetX,
			out double appliedOffsetY)
		{
			bool isEmpty = false;
			bool isAncestor = false;
			bool isVisualDirectChild = false;
			bool isVisualInTopLeftHeader = false;
			bool isVisualInTopHeader = false;
			bool isVisualInLeftHeader = false;
			bool isVisualInContent = false;
			global::Windows.Foundation.Rect transformedRect = default;
			global::Windows.Foundation.Rect viewport = default;
			global::Windows.Foundation.Rect unhandledRect = default;
			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;
			double viewportWidth = 0.0;
			double viewportHeight = 0.0;
			float viewportLeft = 0.0f;
			float viewportRight = 0.0f;
			float viewportTop = 0.0f;
			float viewportBottom = 0.0f;
			float rectLeft = 0.0f;
			float rectRight = 0.0f;
			float rectTop = 0.0f;
			float rectBottom = 0.0f;
			float minX = 0.0f;
			float minY = 0.0f;
			float zoomFactor = 1.0f;
			float targetZoomFactor = 1.0f;
			double appliedOffsetXTmp = 0.0;
			double appliedOffsetYTmp = 0.0;
			global::Windows.Foundation.Size sizeHeaders = default;
			Page mostAncestorPageBetween = null;

			appliedOffsetX = 0.0;
			appliedOffsetY = 0.0;

			// Handle cases where we don't have to do anything
			isEmpty = rectangle.IsEmpty || rectangle.Width == 0 || rectangle.Height == 0;
			isEmpty = isEmpty || visual is null || visual == this;
			if (!isEmpty)
			{
				isAncestor = this.IsAncestorOf(visual);
				if (isAncestor)
				{
					for (var ancestor = VisualTreeHelper.GetParent(visual);
						ancestor is not null && ancestor != this;
						ancestor = VisualTreeHelper.GetParent(ancestor))
					{
						if (ancestor is Page page)
						{
							mostAncestorPageBetween = page;
						}
					}
				}
			}
			if (isEmpty || !isAncestor)
			{
				rectangle = default; // CreateEmptyRect equivalent
			}
			else
			{
				bool isScrollClient;
				bool handled = false;

				// Compute the child's rect relative to (0,0) in our coordinate space.
				var spChildTransform = visual.TransformToVisual(this);
				transformedRect = spChildTransform.TransformBounds(rectangle);

				rectangle = transformedRect;

				// Rectangle to return in case ChangeView is a no-op.
				unhandledRect = rectangle;

				// Compute the area taken up by the potential ScrollViewer headers
				GetZoomedHeadersSize(out sizeHeaders);

				// Adjust the target rectangle based on those headers
				rectangle.X -= sizeHeaders.Width;
				rectangle.Y -= sizeHeaders.Height;

				isScrollClient = IsScrollClient();
				if (isScrollClient)
				{
					// Check if visual belongs to a header.
					GetHeaderOwnership(
						visual,
						out isVisualDirectChild,
						out isVisualInTopLeftHeader,
						out isVisualInTopHeader,
						out isVisualInLeftHeader,
						out isVisualInContent);

					if (!isVisualInTopLeftHeader)
					{
						var spScrollOwner = GetScrollOwner();
						ScrollViewer spScrollViewer = spScrollOwner as ScrollViewer;

						// Initialize the viewport
						if (spScrollViewer is not null && useAnimation)
						{
							if (spScrollViewer.IsInManipulation)
							{
								double targetHorizontalOffset = 0.0;
								double targetVerticalOffset = 0.0;

								spScrollViewer.GetDManipView(out horizontalOffset, out verticalOffset, out zoomFactor);
								spScrollViewer.GetTargetView(out targetHorizontalOffset, out targetVerticalOffset, out targetZoomFactor);
								if (targetHorizontalOffset != -1.0 && targetVerticalOffset != -1.0 && targetZoomFactor != -1.0f)
								{
									global::System.Diagnostics.Debug.Assert(zoomFactor == targetZoomFactor);

									rectangle.X += (float)(horizontalOffset - targetHorizontalOffset);
									rectangle.Y += (float)(verticalOffset - targetVerticalOffset);

									horizontalOffset = targetHorizontalOffset;
									verticalOffset = targetVerticalOffset;
								}
							}
							else
							{
								// Take into account the overbounce offsets which are reflected in the spChildTransform transform.
								horizontalOffset = spScrollViewer.GetUnboundHorizontalOffset();
								verticalOffset = spScrollViewer.GetUnboundVerticalOffset();
							}
						}
						else
						{
							horizontalOffset = GetHorizontalOffset();
							verticalOffset = GetVerticalOffset();
						}

						// Compute the offsets required to minimally scroll the child maximally into view.

						if (isVisualInLeftHeader)
						{
							// visual is not allowed to scroll horizontally
							minX = (float)horizontalOffset;
						}
						else
						{
							viewportWidth = GetViewportWidth();
							viewport.X = (float)horizontalOffset;
							viewport.Width = Math.Max(0.0f, (float)viewportWidth - sizeHeaders.Width);
							rectangle.X += (float)horizontalOffset;

							var rectangleWithAlignment = rectangle;

							if (!DoubleUtil.IsNaN(horizontalAlignmentRatio))
							{
								// Account for the horizontal alignment ratio.
								global::System.Diagnostics.Debug.Assert(horizontalAlignmentRatio >= 0.0 && horizontalAlignmentRatio <= 1.0);
								rectangleWithAlignment.X += (float)((rectangleWithAlignment.Width - viewport.Width) * horizontalAlignmentRatio);
								rectangleWithAlignment.Width = viewport.Width;
							}

							viewportLeft = (float)viewport.X;
							viewportRight = (float)(viewport.X + viewport.Width);
							rectLeft = (float)rectangleWithAlignment.X;
							rectRight = (float)(rectangleWithAlignment.X + rectangleWithAlignment.Width);
							ComputeScrollOffsetWithMinimalScroll(viewportLeft, viewportRight, rectLeft, rectRight, out minX);

							// If the target offset is within bounds and an offset was provided, apply as much of it as possible while remaining within bounds.
							if (offsetX != 0.0 && minX >= 0.0f && spScrollViewer is not null)
							{
								double scrollableWidth = spScrollViewer.ScrollableWidth;
								if (minX <= scrollableWidth)
								{
									if (offsetX > 0.0)
									{
										appliedOffsetXTmp = Math.Min(minX, offsetX);
									}
									else
									{
										appliedOffsetXTmp = -Math.Min((float)scrollableWidth - minX, -offsetX);
									}
									minX -= (float)offsetX;
								}
							}
						}

						if (isVisualInTopHeader)
						{
							// visual is not allowed to scroll vertically
							minY = (float)verticalOffset;
						}
						else
						{
							// if applicable additionally reduce the viewport height by the space occluded by a page bottom appbar
							var pageBottomAppBarScrollOffset = GetFullScreenPageBottomAppBarHeight(mostAncestorPageBetween);

							viewportHeight = GetViewportHeight();
							viewport.Y = (float)verticalOffset;
							viewport.Height = Math.Max(0.0f, (float)(viewportHeight - pageBottomAppBarScrollOffset) - sizeHeaders.Height);
							rectangle.Y += (float)verticalOffset;

							var rectangleWithAlignment = rectangle;

							if (!DoubleUtil.IsNaN(verticalAlignmentRatio))
							{
								// Account for the vertical alignment ratio.
								global::System.Diagnostics.Debug.Assert(verticalAlignmentRatio >= 0.0 && verticalAlignmentRatio <= 1.0);
								rectangleWithAlignment.Y += (float)((rectangleWithAlignment.Height - viewport.Height) * verticalAlignmentRatio);
								rectangleWithAlignment.Height = viewport.Height;
							}

							viewportTop = (float)viewport.Y;
							viewportBottom = (float)(viewport.Y + viewport.Height);
							rectTop = (float)rectangleWithAlignment.Y;
							rectBottom = (float)(rectangleWithAlignment.Y + rectangleWithAlignment.Height);
							ComputeScrollOffsetWithMinimalScroll(viewportTop, viewportBottom, rectTop, rectBottom, out minY);

							// If the target offset is within bounds and an offset was provided, apply as much of it as possible while remaining within bounds.
							if (offsetY != 0.0 && minY >= 0.0f && spScrollViewer is not null)
							{
								double scrollableHeight = spScrollViewer.ScrollableHeight;
								if (minY <= scrollableHeight)
								{
									if (offsetY > 0.0)
									{
										appliedOffsetYTmp = Math.Min(minY, offsetY);
									}
									else
									{
										appliedOffsetYTmp = -Math.Min((float)scrollableHeight - minY, -offsetY);
									}
									minY -= (float)offsetY;
								}
							}
						}

						// We have computed the scrolling offsets; scroll to them.
						if (spScrollViewer is not null && useAnimation)
						{
							double targetHorizontalOffset = (double)Math.Max(0, minX);
							double targetVerticalOffset = (double)Math.Max(0, minY);

							// No need to call ChangeView during a manipulation if the requested view coincides with the final view.
							if (!spScrollViewer.IsInManipulation ||
								!DoubleUtil.AreClose(horizontalOffset, targetHorizontalOffset) ||
								!DoubleUtil.AreClose(verticalOffset, targetVerticalOffset))
							{
								handled = spScrollViewer.ChangeViewInternal(
									targetHorizontalOffset /*pHorizontalOffset*/,
									targetVerticalOffset /*pVerticalOffset*/,
									null /*pZoomFactor*/,
									null /*pOldZoomFactor*/,
									false /*forceChangeToCurrentView*/,
									true /*adjustWithMandatorySnapPoints*/,
									true /*skipDuringTouchContact*/,
									true /*skipAnimationWhileRunning*/,
									false /*disableAnimation*/,
									true /*applyAsManip*/,
									false /*transformIsInertiaEnd*/,
									true /*isForMakeVisible*/);

								if (handled)
								{
									// Make sure the resulting minX/minY offsets are within bounds so the final viewport is correctly evaluated below.
									double scrollableDim = spScrollViewer.ScrollableWidth;
									minX = (float)Math.Min(targetHorizontalOffset, scrollableDim);

									scrollableDim = spScrollViewer.ScrollableHeight;
									minY = (float)Math.Min(targetVerticalOffset, scrollableDim);
								}
							}
						}
						else
						{
							bool isScrollRequested = false;
							double currentOffset = 0.0;
							double requestedOffset = 0.0;

							// We fall back to calling SetHorizontalOffset/SetVerticalOffset when
							// ScrollViewer::ChangeView is not called.
							if (horizontalOffset != minX)
							{
								SetHorizontalOffsetPrivate((double)minX, out isScrollRequested, out currentOffset, out requestedOffset);

								// Make sure the resulting minX offset is within bounds so the final viewport is correctly evaluated below.
								if (isScrollRequested)
								{
									minX = (float)requestedOffset;
									handled = true;
								}
								else
								{
									minX = (float)currentOffset;
								}
							}

							if (verticalOffset != minY)
							{
								SetVerticalOffsetPrivate((double)minY, out isScrollRequested, out currentOffset, out requestedOffset);

								// Make sure the resulting minY offset is within bounds so the final viewport is correctly evaluated below.
								if (isScrollRequested)
								{
									minY = (float)requestedOffset;
									handled = true;
								}
								else
								{
									minY = (float)currentOffset;
								}
							}
						}
					}

					if (handled)
					{
						// Compute the visible rectangle of the child relative to the viewport.
						viewport.X = minX;
						viewport.Y = minY;

						// Do not include the applied offset so that potential parent bring-into-view contributors ignore that shift.
						viewport.X += (float)appliedOffsetXTmp;
						viewport.Y += (float)appliedOffsetYTmp;

						rectangle.Intersect(viewport);

						isEmpty = rectangle.IsEmpty || rectangle.Width == 0 || rectangle.Height == 0;
						if (!isEmpty)
						{
							rectangle.X = rectangle.X - viewport.X + sizeHeaders.Width;
							rectangle.Y = rectangle.Y - viewport.Y + sizeHeaders.Height;
						}
					}
					else
					{
						rectangle = unhandledRect;
					}
				}
			}

			appliedOffsetX = appliedOffsetXTmp;
			appliedOffsetY = appliedOffsetYTmp;

			// Suppress unused-variable warnings for header ownership state.
			_ = isVisualDirectChild;
			_ = isVisualInContent;

			// Return the rectangle
			return rectangle;
		}

		private static double GetFullScreenPageBottomAppBarHeight(Page page)
		{
			const double pageApplyingLayoutBoundsTolerance = 1.0;

			if (page?.XamlRoot is not { } xamlRoot ||
				page.BottomAppBar is not FrameworkElement bottomAppBar)
			{
				return 0.0;
			}

			var currentWindowBounds = xamlRoot.Size;
			if (Math.Abs(currentWindowBounds.Width - page.ActualWidth) < pageApplyingLayoutBoundsTolerance &&
				Math.Abs(currentWindowBounds.Height - page.ActualHeight) < pageApplyingLayoutBoundsTolerance)
			{
				return bottomAppBar.ActualHeight;
			}

			return 0.0;
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

		// Apply a template to the ScrollContentPresenter.
		// (C++ source line 2587 — ScrollContentPresenter_Partial.cpp)
		protected override void OnApplyTemplate()
		{
			if (m_isChildActualWidthUsedAsExtent)
			{
				// Since a new Content is set, assume that the default behavior of using its desired size as the IScrollInfo extent is acceptable.
				StopUseOfActualWidthAsExtent();
			}

			if (m_isChildActualHeightUsedAsExtent)
			{
				// Since a new Content is set, assume that the default behavior of using its desired size as the IScrollInfo extent is acceptable.
				StopUseOfActualHeightAsExtent();
			}

			base.OnApplyTemplate();

			// Get our scrolling owner and content talking.
			HookupScrollingComponents();
		}

		// Helper method to get our owner and its scrolling content talking.
		// Method introduces the current owner/content, and clears a from any previous content.
		// (C++ source line 2649)
		internal void HookupScrollingComponents()
		{
			// We need to introduce our IScrollInfo to our ScrollViewer (and break any
			// previous links).
			// MUX Reference: C++ uses get_TemplatedParent() to find the owning SV.
			// Uno's cross-platform path explicitly sets `presenter.ScrollOwner = this`
			// from SV.OnApplyTemplate (ScrollViewer.cs:997) without setting TemplatedParent
			// on the SCP, so we need to consult ScrollOwner first and fall back to
			// TemplatedParent for parity with WinUI.
			var spScrollContainer = (ScrollOwner as ScrollViewer)
				?? (TemplatedParent as ScrollViewer);
			var spCurrentScrollInfo = GetCurrentScrollInfo();

			// If our content is not an IScrollInfo, we should have selected a style
			// that contains one.
			if (spScrollContainer is not null)
			{
				m_manipulationDataProviderScrollInfo = null;

				// 1. Try our content...
				var spScrollInfo = Content as IScrollInfo;

				// 2. Our child might be an ItemsPresenter. In this case check its panel for being an IScrollInfo.
				if (Content is ItemsPresenter itemsPresenter)
				{
					spScrollInfo = itemsPresenter.Panel as IScrollInfo;
					if (spScrollInfo is null && itemsPresenter is IManipulationDataProvider itemsPresenterProvider)
					{
						m_manipulationDataProviderScrollInfo = new ManipulationDataProviderScrollInfo(this, itemsPresenterProvider);
						spScrollInfo = m_manipulationDataProviderScrollInfo;
					}
				}

				// 3. As a final fallback, we use ourself.
				if (spScrollInfo is null)
				{
					m_manipulationDataProviderScrollInfo = null;
					spScrollInfo = this;
				}

				if (spScrollInfo != spCurrentScrollInfo && spCurrentScrollInfo is not null)
				{
					if (spCurrentScrollInfo == this || spCurrentScrollInfo is ManipulationDataProviderScrollInfo)
					{
						m_pScrollData = null;
						GetScrollData();
					}
					else
					{
						spCurrentScrollInfo.PutScrollOwner(null);
					}
				}

				m_wrScrollInfo = new WeakReference<IScrollInfo>(spScrollInfo);
				spScrollInfo.PutScrollOwner(spScrollContainer);
				spScrollContainer.PutScrollInfo(spScrollInfo);
				ScrollOwner = spScrollContainer;

				if (spScrollInfo is IManipulationDataProvider provider)
				{
					provider.SetZoomFactor(m_fZoomFactor);
				}
			}
			else if (spCurrentScrollInfo is not null)
			{
				if (spCurrentScrollInfo.GetScrollOwner() is { } spScrollOwner)
				{
					spScrollOwner.SetScrollInfo(null);
				}

				spCurrentScrollInfo.PutScrollOwner(null);
				m_wrScrollInfo = null;
				m_manipulationDataProviderScrollInfo = null;
				m_pScrollData = null;
			}
		}

		// Register this instance as under control of a semanticzoom control.
		// (C++ source line 2781)
		internal void RegisterAsSemanticZoomPresenter()
		{
			m_isSemanticZoomPresenter = true;
		}

		// (C++ source line 2789)
		internal void CalculateTextBoxClipRect(
			global::Windows.Foundation.Size availableSize,
			out global::Windows.Foundation.Rect pClipRect)
		{
			// Special case for a scroll content presenter containing the text of a
			// TextBox or a RichtextBox: we don't want to clip to the layout boundaries
			// of the text, as that will clip any ovehanging glyph strokes, such as the
			// bottom of a lowercase italic f in a Latin font like Times New Roman, or a
			// Lam or Alif in any Arabic font. See bug 82041 for an example.
			//
			// If this scroll content presenter hosts a TextBoxView or a
			// RichTextBoxView, and if either end of the text is fully in view, then we
			// allow glyphs at those ends to overhang into the padding of the containing
			// ScrollViewer and the 1 pixel selection highlight border by extending the
			// clipping rectangle.

			double glyphOverhangLeft = 0.0;
			double glyphOverhangRight = 0.0;
			double extentWidth = 0.0;
			double viewportWidth = 0.0;
			double offset = 0.0;
			TextWrapping wrapping = TextWrapping.NoWrap;
			ScrollBarVisibility visibility = ScrollBarVisibility.Disabled;
			Thickness scrollViewerPadding = default;
			double availableWidth = 0.0;
			double availableHeight = 0.0;
			global::Windows.Foundation.Rect clipRect = default;

			var spTemplatedParent = TemplatedParent ?? ScrollOwner;
			var spScrollViewer = spTemplatedParent as ScrollViewer;
			var pScrollData = GetScrollData();
			extentWidth = pScrollData.m_extent.Width;
			viewportWidth = pScrollData.m_viewport.Width;
			offset = pScrollData.GetOffsetX();

			var spTemplatedGrandParent = spScrollViewer?.TemplatedParent;
			var spTextBoxParent = spTemplatedGrandParent as TextBox;

			// Detemine the TextWrapping and HorizontalScrollBarVisiblity properties.
			if (spTextBoxParent is not null)
			{
				wrapping = spTextBoxParent.TextWrapping;
				visibility = ScrollViewer.GetHorizontalScrollBarVisibility(spTextBoxParent);
			}

			// Determine the space to reserve for left and right glyph overhang
			scrollViewerPadding = spScrollViewer is not null ? spScrollViewer.Padding : default;
			if (wrapping == TextWrapping.Wrap)
			{
				// If TextWrapping="wrap" then the text always fits the margins and we
				// always want to allow glyphs to overhang into both margins.
				glyphOverhangLeft = scrollViewerPadding.Left + 1.0;
				glyphOverhangRight = scrollViewerPadding.Right + 1.0;
			}
			else
			{
				// We're not wrapping.
				// The left end is quite easy:
				if (viewportWidth > extentWidth || offset == 0)
				{
					// Left end of content is fully in view
					glyphOverhangLeft = scrollViewerPadding.Left + 1.0;
				}

				// The right end is not so easy, because when client disables the
				// horizontal scrollbar we don't bother to measure the extent beyond
				// the viewport width. So with a disabled horizontal scrollbar we can
				// only trust the extent measurement when it is less than the viewport
				// width.
				if (viewportWidth > extentWidth ||
					(visibility != ScrollBarVisibility.Disabled &&
					Math.Abs(extentWidth - offset + viewportWidth) <= 1.0))
				{
					// Right end of content is fully in view
					glyphOverhangRight = scrollViewerPadding.Right + 1.0;
				}
			}

			// Note that we only want to expand the clip. We use Math.Max to
			// enforce this for cases where the client provides negative values
			// for padding left and/or right.
			glyphOverhangLeft = Math.Max(0.0, glyphOverhangLeft);
			glyphOverhangRight = Math.Max(0.0, glyphOverhangRight);

			// Return the clipping rectangle with the calculated overhangs.
			availableWidth = availableSize.Width;
			availableHeight = availableSize.Height;
			clipRect.X = (float)-glyphOverhangLeft;
			clipRect.Y = 0;
			clipRect.Width = (float)(availableWidth + glyphOverhangLeft + glyphOverhangRight);
			clipRect.Height = (float)availableHeight;
			pClipRect = clipRect;
		}

		// ScrollContentPresenter clips its content to arrange size.
		// No clip is applied if its CanContentRenderOutsideBounds property is set to True though.
		// (C++ source line 2907)
		internal void UpdateClip(global::Windows.Foundation.Size availableSize)
		{
			bool canContentRenderOutsideBounds = CanContentRenderOutsideBounds;

			if (canContentRenderOutsideBounds)
			{
				if (m_isClipPropertySet)
				{
					Clip = null;
					m_isClipPropertySet = false;
				}
			}
			else
			{
				if (!m_isClipPropertySet)
				{
					var spClippingGeometry = new RectangleGeometry();
					m_tpClippingRectangle = spClippingGeometry;
					Clip = m_tpClippingRectangle;
					m_isClipPropertySet = true;
				}

				global::Windows.Foundation.Rect clipRect = default;
				var scrollViewer = (TemplatedParent as ScrollViewer) ?? (ScrollOwner as ScrollViewer);
				if (scrollViewer?.TemplatedParent is TextBox)
				{
					// We may need to allow glyphs to overhang into the ScrollViewer's padding.
					CalculateTextBoxClipRect(availableSize, out clipRect);
				}
				else
				{
					clipRect.X = clipRect.Y = 0;
					clipRect.Width = availableSize.Width;
					clipRect.Height = availableSize.Height;
				}
				m_tpClippingRectangle.Rect = clipRect;
			}
		}

		// Called when a criteria for the CanUseActualWidthAsExtent or CanUseActualHeightAsExtent evaluation changed.
		// Calls InvalidateMeasure when the evaluation actually changes so the special
		// mode can be entered or exited.
		// (C++ source line 2961)
		internal void RefreshUseOfActualSizeAsExtent(UIElement pManipulatedElement)
		{
			bool isScrollClient = IsScrollClient();
			var pScrollData = GetScrollData();
			if (isScrollClient && pScrollData is not null)
			{
				bool canUseActualWidthAsExtent = false;
				bool canUseActualHeightAsExtent = false;

				var spScrollOwner = GetScrollOwner();
				var spScrollViewer = spScrollOwner as ScrollViewer;
				var spContentFE = pManipulatedElement as FrameworkElement;

				CanUseActualWidthAsExtent(
					spScrollOwner,
					spScrollViewer,
					spContentFE,
					out canUseActualWidthAsExtent);

				if (canUseActualWidthAsExtent == m_isChildActualWidthUsedAsExtent)
				{
					CanUseActualHeightAsExtent(
						spScrollOwner,
						spScrollViewer,
						spContentFE,
						out canUseActualHeightAsExtent);
				}

				if (m_isChildActualWidthUsedAsExtent != canUseActualWidthAsExtent || m_isChildActualHeightUsedAsExtent != canUseActualHeightAsExtent)
				{
					InvalidateMeasure();
				}
			}
		}

		// Determines whether the mode that uses the child's actual size for the IScrollInfo extent is applicable or not.
		// The answer is partially evaluated with a temporary reg key.
		// (C++ source line 3016)
		internal static void CanUseActualWidthAsExtent(
			IScrollOwner pScrollOwner,
			ScrollViewer pScrollViewer,
			FrameworkElement pContentFE,
			out bool pCanUseActualWidthAsExtent)
		{
			pCanUseActualWidthAsExtent = false;

			if (pContentFE is null)
			{
				return;
			}

			HorizontalAlignment horizontalContentFEAlignment = pContentFE.HorizontalAlignment;
			if (horizontalContentFEAlignment != HorizontalAlignment.Stretch)
			{
				// In order to minimize the risks for regressions, we only stop using
				// the child's desired size in known problematic situations. Bugs have
				// only surfaced when the Stretched alignment is used.
				// Do not enter the special mode unless a Stretch alignment is used.
				return;
			}

			// FrameworkElement::IsWidthSpecified() returns true when Width, MinWidth or
			// MaxWidth was set to a non-default value (NaN width, 0 min, +Inf max).
			if (!double.IsNaN(pContentFE.Width) ||
				pContentFE.MinWidth != 0.0 ||
				!double.IsPositiveInfinity(pContentFE.MaxWidth))
			{
				// When the child has a non-default Width, MinWidth or MaxWidth, the
				// desired width reflects the correct extent to push via IScrollInfo,
				// while the actual width does not.
				return;
			}

			if (pScrollViewer is not null)
			{
				// Do not enter the special mode when the ScrollViewer is using an imposed layout size.
				// This situation arises with the SemanticZoom control which imposes a size for the
				// ScrollContentPresenter's child. See how ScrollContentPresenter::MeasureOverride
				// uses GetLayoutSize() for the desired size and IScrollInfo extent size.
				var layoutSize = pScrollViewer.GetLayoutSize();
				if (layoutSize.Width != 0.0f)
				{
					return;
				}
			}

			pCanUseActualWidthAsExtent = true;
		}

		// (C++ source line 3067)
		internal static void CanUseActualHeightAsExtent(
			IScrollOwner pScrollOwner,
			ScrollViewer pScrollViewer,
			FrameworkElement pContentFE,
			out bool pCanUseActualHeightAsExtent)
		{
			pCanUseActualHeightAsExtent = false;

			if (pContentFE is null)
			{
				return;
			}

			VerticalAlignment verticalContentFEAlignment = pContentFE.VerticalAlignment;
			if (verticalContentFEAlignment != VerticalAlignment.Stretch)
			{
				// In order to minimize the risks for regressions, we only stop using
				// the child's desired size in known problematic situations. Bugs have
				// only surfaced when the Stretched alignment is used.
				// Do not enter the special mode unless a Stretch alignment is used.
				return;
			}

			// FrameworkElement::IsHeightSpecified() returns true when Height, MinHeight or
			// MaxHeight was set to a non-default value.
			if (!double.IsNaN(pContentFE.Height) ||
				pContentFE.MinHeight != 0.0 ||
				!double.IsPositiveInfinity(pContentFE.MaxHeight))
			{
				// When the child has a non-default Height, MinHeight or MaxHeight, the
				// desired height reflects the correct extent to push via IScrollInfo,
				// while the actual height does not.
				return;
			}

			if (pScrollViewer is not null)
			{
				// Do not enter the special mode when the ScrollViewer is using an imposed layout size.
				// This situation arises with the SemanticZoom control which imposes a size for the
				// ScrollContentPresenter's child. See how ScrollContentPresenter::MeasureOverride
				// uses GetLayoutSize() for the desired size and IScrollInfo extent size.
				var layoutSize = pScrollViewer.GetLayoutSize();
				if (layoutSize.Height != 0.0f)
				{
					return;
				}
			}

			pCanUseActualHeightAsExtent = true;
		}

		// Verifies scrolling data using the passed viewport and extent as
		// newly computed values.  Checks the X/Y offset and coerces them
		// into the range [0, Extent - ViewportSize].  If extent, viewport,
		// or the newly coerced offsets are different than the existing
		// offset, caches are updated and InvalidateScrollInfo() is called.
		// (C++ source line 3123)
		internal void VerifyScrollData(global::Windows.Foundation.Size viewport, global::Windows.Foundation.Size extent)
		{
			// Update cache values of viewport/extent sizes first, then coerce offsets
			// as these sizes may have changed.
			var pScrollData = GetScrollData();
			var oldViewportWidth = (float)pScrollData.m_viewport.Width;
			var oldViewportHeight = (float)pScrollData.m_viewport.Height;
			var valid = (oldViewportWidth == viewport.Width && oldViewportHeight == viewport.Height);
			pScrollData.m_viewport.Width = viewport.Width;
			pScrollData.m_viewport.Height = viewport.Height;

			var oldExtentWidth = (float)pScrollData.m_extent.Width;
			var oldExtentHeight = (float)pScrollData.m_extent.Height;
			valid &= (oldExtentWidth == extent.Width && oldExtentHeight == extent.Height);
			pScrollData.m_extent.Width = extent.Width;
			pScrollData.m_extent.Height = extent.Height;

			CoerceOffsets(out var coerce);
			valid &= coerce;

			m_fLastZoomFactorApplied = m_fZoomFactor;

			var spScrollOwner = pScrollData.GetScrollOwner();
			if (!valid && spScrollOwner is not null)
			{
				if (!DoubleUtil.AreClose(HorizontalOffset, pScrollData.m_ComputedOffset.X) ||
					!DoubleUtil.AreClose(VerticalOffset, pScrollData.m_ComputedOffset.Y))
				{
					Set(
						horizontalOffset: pScrollData.m_ComputedOffset.X,
						verticalOffset: pScrollData.m_ComputedOffset.Y,
						disableAnimation: true);
				}

				spScrollOwner.InvalidateScrollInfoImpl();
			}
		}

		// Coerce both of the offsets using CoerceOffset method and store them as the
		// new computed offsets if they've changed.
		// (C++ source line 3170)
		internal void CoerceOffsets(out bool pIsValid)
		{
			global::System.Diagnostics.Debug.Assert(IsScrollClient());

			var pScrollData = GetScrollData();

			var offset = pScrollData.GetOffsetX();
			var extent = pScrollData.m_extent.Width;
			var viewport = pScrollData.m_viewport.Width;
			var newX = CoerceOffset(offset, extent, viewport);

			offset = pScrollData.GetOffsetY();
			extent = pScrollData.m_extent.Height;
			viewport = pScrollData.m_viewport.Height;
			var newY = CoerceOffset(offset, extent, viewport);

			var computedX = pScrollData.m_ComputedOffset.X;
			var computedY = pScrollData.m_ComputedOffset.Y;
			var valid = DoubleUtil.AreClose(newX, computedX) && DoubleUtil.AreClose(newY, computedY);

			pScrollData.m_ComputedOffset.X = newX;
			pScrollData.m_ComputedOffset.Y = newY;

			if (!pScrollData.m_canHorizontallyScroll)
			{
				// Reset the horizontal offset when m_canHorizontallyScroll becomes False (for example
				// when HorizontalScrollbarVisibility becomes Disabled while there is an existing offset)
				global::System.Diagnostics.Debug.Assert(pScrollData.m_ComputedOffset.X == 0.0);
				if (pScrollData.GetOffsetX() != 0.0f)
				{
					pScrollData.SetOffsetX(0.0f);
				}
			}

			if (!pScrollData.m_canVerticallyScroll)
			{
				// Reset the vertical offset when m_canVerticallyScroll becomes False (for example
				// when VerticalScrollbarVisibility becomes Disabled while there is an existing offset)
				global::System.Diagnostics.Debug.Assert(pScrollData.m_ComputedOffset.Y == 0.0);
				if (pScrollData.GetOffsetY() != 0.0f)
				{
					pScrollData.SetOffsetY(0.0f);
				}
			}

			pIsValid = valid;
		}

		// Called to let the peer know when InputPane is showing.
		internal void NotifyInputPaneStateChange(bool isInputPaneShow)
		{
			m_isInputPaneShow = isInputPaneShow;
		}

		// Called to let the peer know when InputPane transition is applied.
		internal void ApplyInputPaneTransition(bool isInputPaneTransitionEnabled)
		{
			m_tpInputPaneThemeTransition ??= new RepositionThemeTransition();
			ContentTransitions ??= new TransitionCollection();

			var shouldApplyTransition = isInputPaneTransitionEnabled && m_isInputPaneShow;
			if (shouldApplyTransition && !ContentTransitions.Contains(m_tpInputPaneThemeTransition))
			{
				ContentTransitions.Add(m_tpInputPaneThemeTransition);
			}
			else if (!shouldApplyTransition)
			{
				ContentTransitions.Remove(m_tpInputPaneThemeTransition);
			}
		}

		// Updates the zoom factor.
		// (C++ source line 3338)
		internal void SetZoomFactor(float newZoomFactor)
		{
			m_fZoomFactor = newZoomFactor;

			if (IsScrollClient())
			{
				InvalidateMeasure();
			}

			if (GetCurrentScrollInfo() is IManipulationDataProvider provider)
			{
				provider.SetZoomFactor(m_fZoomFactor);
			}
		}

		// Called by the owning ScrollViewer when the Content property is changing.
		// (C++ source line 3493)
		internal void OnContentChanging(object pOldContent)
		{
			if (pOldContent is UIElement spOldChild)
			{
				spOldChild.ResetGlobalScaleFactor();
			}
		}

		// Called when the parent of this ScrollContentPresenter changed.
		// (C++ source line 3506)
		internal void OnTreeParentUpdatedCore(object pNewParent, bool isParentAlive)
		{
			if (pNewParent is null)
			{
				UnparentHeaders();
				m_trTopLeftHeader = null;
				m_trTopHeader = null;
				m_trLeftHeader = null;
			}
		}

		// Called when a ScrollContentPresenter dependency property changed.
		// (C++ source line 3536)
		internal void OnPropertyChanged2Core(DependencyProperty changedProperty)
		{
			if (changedProperty == CanContentRenderOutsideBoundsProperty)
			{
				InvalidateArrange();
			}
		}

		// Enters the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 6034)
		internal void StartUseOfActualWidthAsExtent()
		{
			global::System.Diagnostics.Debug.Assert(!m_isChildActualWidthUsedAsExtent);
			m_isChildActualWidthUsedAsExtent = true;

			var spScrollOwner = GetScrollOwner();
			var spScrollViewer = spScrollOwner as ScrollViewer;
			if (spScrollViewer is not null)
			{
				spScrollViewer.StartUseOfActualSizeAsExtent(true /*isHorizontal*/);
			}
		}

		// Leaves the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 6055)
		internal void StopUseOfActualWidthAsExtent()
		{
			global::System.Diagnostics.Debug.Assert(m_isChildActualWidthUsedAsExtent);
			m_unpublishedExtentSize.Width = 0.0f;
			m_isChildActualWidthUsedAsExtent = false;

			var spScrollOwner = GetScrollOwner();
			var spScrollViewer = spScrollOwner as ScrollViewer;
			if (spScrollViewer is not null)
			{
				spScrollViewer.StopUseOfActualSizeAsExtent(true /*isHorizontal*/);
			}
		}

		// Enters the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 6077)
		internal void StartUseOfActualHeightAsExtent()
		{
			global::System.Diagnostics.Debug.Assert(!m_isChildActualHeightUsedAsExtent);
			m_isChildActualHeightUsedAsExtent = true;

			var spScrollOwner = GetScrollOwner();
			var spScrollViewer = spScrollOwner as ScrollViewer;
			if (spScrollViewer is not null)
			{
				spScrollViewer.StartUseOfActualSizeAsExtent(false /*isHorizontal*/);
			}
		}

		// Leaves the mode where the child's actual size is used for
		// the extent exposed through IScrollInfo.
		// (C++ source line 6098)
		internal void StopUseOfActualHeightAsExtent()
		{
			global::System.Diagnostics.Debug.Assert(m_isChildActualHeightUsedAsExtent);
			m_unpublishedExtentSize.Height = 0.0f;
			m_isChildActualHeightUsedAsExtent = false;

			var spScrollOwner = GetScrollOwner();
			var spScrollViewer = spScrollOwner as ScrollViewer;
			if (spScrollViewer is not null)
			{
				spScrollViewer.StopUseOfActualSizeAsExtent(false /*isHorizontal*/);
			}
		}

		// Provides the behavior for the Measure pass of layout. Classes can
		// override this method to define their own Measure pass behavior.
		internal global::Windows.Foundation.Size MeasureOverridePort(global::Windows.Foundation.Size availableSize)
		{
			var spChild = Content as UIElement;
			var spChildAsFE = spChild as FrameworkElement;
			var spTopLeftHeader = m_trTopLeftHeader;
			var spTopHeader = m_trTopHeader;
			var spLeftHeader = m_trLeftHeader;
			var pScrollData = GetScrollData();
			var desiredSize = default(global::Windows.Foundation.Size);
			var desiredSizeZoomed = default(global::Windows.Foundation.Size);
			var topLeftHeaderDesiredSize = default(global::Windows.Foundation.Size);
			var topHeaderDesiredSize = default(global::Windows.Foundation.Size);
			var leftHeaderDesiredSize = default(global::Windows.Foundation.Size);
			var headersDesiredSize = default(global::Windows.Foundation.Size);
			var toBeAdjustedDesiredSize = default(global::Windows.Foundation.Size);
			var layoutSize = default(global::Windows.Foundation.Size);
			var adjustDesiredSize = false;
			ScrollViewer spScrollViewer = null;

			if (!IsScrollClient())
			{
				if (spTopLeftHeader is not null || spTopHeader is not null || spLeftHeader is not null)
				{
					// Custom IScrollInfo implementations are not supported when a header is set.
					throw new NotSupportedException("ScrollViewer headers cannot be used with a custom IScrollInfo implementation.");
				}

				if (spChild is not null)
				{
					return base.MeasureOverride(availableSize);
				}
				return default;
			}

			if (spChildAsFE is not null && (spTopLeftHeader is not null || spTopHeader is not null || spLeftHeader is not null))
			{
				// Check if the ScrollContentPresenter's content is a FrameworkElement or
				// if the default ControlTemplate with Grid/TextBlock is used instead.
				if (Content is FrameworkElement)
				{
					if (spChildAsFE.HorizontalAlignment != HorizontalAlignment.Left)
					{
						// Only the Left horizontal alignment is supported when a header is set.
						throw new NotSupportedException("ScrollViewer content must use HorizontalAlignment.Left when a header is set.");
					}

					if (spChildAsFE.VerticalAlignment != VerticalAlignment.Top)
					{
						// Only the Top vertical alignment is supported when a header is set.
						throw new NotSupportedException("ScrollViewer content must use VerticalAlignment.Top when a header is set.");
					}
				}
				// else: no alignment check is done when the default Grid/TextBlock ControlTemplate is used.
			}

			var spScrollOwner = pScrollData.GetScrollOwner();
			spScrollViewer = spScrollOwner as ScrollViewer;

			if (spScrollViewer is not null)
			{
				if (spLeftHeader is not null && !m_isLeftHeaderChild)
				{
					// Add the left header as a child.
					AddHeader(spScrollViewer, spTopLeftHeader, spTopHeader, spLeftHeader, false /*isTopHeader*/, true /*isLeftHeader*/);
				}

				if (spTopHeader is not null && !m_isTopHeaderChild)
				{
					// Add the top header as a child.
					AddHeader(spScrollViewer, spTopLeftHeader, spTopHeader, spLeftHeader, true /*isTopHeader*/, false /*isLeftHeader*/);
				}

				if (spTopLeftHeader is not null && !m_isTopLeftHeaderChild)
				{
					// Add the top-left header as a child.
					AddHeader(spScrollViewer, spTopLeftHeader, spTopHeader, spLeftHeader, true /*isTopHeader*/, true /*isLeftHeader*/);
				}
			}

			var childAvailableSize = availableSize;
			// when set to true, this means that we wanted to set to infinity but were blocked in doing it.
			var childPreventsInfiniteAvailableWidth = false;
			var childPreventsInfiniteAvailableHeight = false;
			bool sizesContentToTemplatedParent = false;

			if (spScrollViewer is not null)
			{
				// When ScrollContentPresenter.SizesContentToTemplatedParent is True, the child's available size
				// is set to the templated parent's (typically the ScrollViewer's) available size.
				sizesContentToTemplatedParent = SizesContentToTemplatedParent;
				if (sizesContentToTemplatedParent)
				{
					// Note: Accessing the templated parent with get_TemplatedParent and using LayoutInformation::GetAvailableSize
					// would not work because it returns an out-of-date available size. So the owning ScrollViewer is asked directly
					// what its latest available size was.
					childAvailableSize = spScrollViewer.GetLatestAvailableSize();
				}
			}

			if (pScrollData.m_canHorizontallyScroll)
			{
				childPreventsInfiniteAvailableWidth = spChildAsFE is not null &&
					!spChildAsFE.WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation.Horizontal);

				// An infinite available width is given to the child unless:
				// - this ScrollContentPresenter belongs to a SemanticZoom control.
				// - the child FrameworkElement blocks an infinite available width — this is the case for a ModernCollectionBasePanel that is virtualizing vertically.
				// - this ScrollContentPresenter's SizesContentToTemplatedParent property is set to True.
				if (!m_isSemanticZoomPresenter && !childPreventsInfiniteAvailableWidth && !sizesContentToTemplatedParent)
				{
					childAvailableSize.Width = double.PositiveInfinity;
				}
			}
			else if (spChildAsFE is not null &&
				FlowDirection != spChildAsFE.FlowDirection &&
				!sizesContentToTemplatedParent)
			{
				childAvailableSize.Width = double.PositiveInfinity;
			}

			if (pScrollData.m_canVerticallyScroll)
			{
				childPreventsInfiniteAvailableHeight = spChildAsFE is not null &&
					!spChildAsFE.WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation.Vertical);

				// An infinite available height is given to the child unless:
				// - this ScrollContentPresenter belongs to a SemanticZoom control.
				// - the child FrameworkElement blocks an infinite available height — this is the case for a ModernCollectionBasePanel that is virtualizing horizontally.
				// - this ScrollContentPresenter's SizesContentToTemplatedParent property is set to True.
				if (!m_isSemanticZoomPresenter && !childPreventsInfiniteAvailableHeight && !sizesContentToTemplatedParent)
				{
					childAvailableSize.Height = double.PositiveInfinity;
				}
			}

			var headersAreNonClipping = false;

			// We wanted to set to infinity, but we didn't. Certain panels can deal with non clipping subtrees.
			if (spChildAsFE is ItemsPresenter itemsPresenter)
			{
				headersAreNonClipping = itemsPresenter.EvaluateAndSetNonClippingBehavior(
					childPreventsInfiniteAvailableWidth || childPreventsInfiniteAvailableHeight);
			}

			float zoomFactor = 1.0f;
			if (spScrollOwner is not null)
			{
				zoomFactor = spScrollOwner.GetZoomFactor();
				global::System.Diagnostics.Debug.Assert(zoomFactor == m_fZoomFactor);
			}

			if (spTopLeftHeader is not null || spTopHeader is not null || spLeftHeader is not null)
			{
				// When at least one header element is shown, the plateau scale returned by RootScale is combined with the owning ScrollViewer's ZoomFactor
				// so the CUIElement::LayoutRound method can correctly snap the four quadrants based on both factors. put_GlobalScaleFactor is pushing the global
				// scale factor into sparse storage for whichever of the four quadrants exist.
				var globalScaleFactor = RootScale.GetRasterizationScaleForElement(this) * zoomFactor;

				if (spTopLeftHeader is not null)
				{
					spTopLeftHeader.PutGlobalScaleFactor(globalScaleFactor);
					spTopLeftHeader.Measure(childAvailableSize);
					topLeftHeaderDesiredSize = spTopLeftHeader.DesiredSize;
				}
				if (spTopHeader is not null)
				{
					spTopHeader.PutGlobalScaleFactor(globalScaleFactor);
					spTopHeader.IsNonClippingSubtree = headersAreNonClipping;
					spTopHeader.Measure(childAvailableSize);
					topHeaderDesiredSize = spTopHeader.DesiredSize;
				}
				if (spLeftHeader is not null)
				{
					spLeftHeader.PutGlobalScaleFactor(globalScaleFactor);
					spLeftHeader.Measure(childAvailableSize);
					leftHeaderDesiredSize = spLeftHeader.DesiredSize;
				}
				if (spChild is not null)
				{
					spChild.PutGlobalScaleFactor(globalScaleFactor);
				}
			}

			headersDesiredSize.Width = Math.Max(topLeftHeaderDesiredSize.Width, leftHeaderDesiredSize.Width);
			headersDesiredSize.Height = Math.Max(topLeftHeaderDesiredSize.Height, topHeaderDesiredSize.Height);

			if (spChild is not null)
			{
				spChild.Measure(childAvailableSize);
				desiredSize = spChild.DesiredSize;

				if (spChild is Primitives.CalendarPanel calendarPanel)
				{
					var desiredViewportSizeFromPanel = calendarPanel.GetDesiredViewportSize();
					adjustDesiredSize = true;
					// In CalendarPanel, the SCP's desired size should be determined by the Panel so Panel can decide
					// the numbers (rows and cols) of items showing in the viewport.
					// Note: the SCP's scrollcontent extent is still determined by Panel's desired size.
					// see more details from CalendarView::MeasureOverride in file CalendarView_Partial.cpp
					toBeAdjustedDesiredSize.Width = desiredViewportSizeFromPanel.Width - desiredSize.Width;
					toBeAdjustedDesiredSize.Height = desiredViewportSizeFromPanel.Height - desiredSize.Height;
				}

				// Give opportunity to the content to define the viewport size itself.
				(spChild as ICustomScrollInfo)?.ApplyViewport(ref desiredSize);
			}

			desiredSize.Width += headersDesiredSize.Width;
			desiredSize.Height += headersDesiredSize.Height;

			if (spChild is null)
			{
				// Irrespective of the presence of headers, the desired size is (0, 0) when ScrollViewer.Content is null.
				global::System.Diagnostics.Debug.Assert(desiredSizeZoomed.Width == 0.0f);
				global::System.Diagnostics.Debug.Assert(desiredSizeZoomed.Height == 0.0f);
				if (m_isChildActualWidthUsedAsExtent)
				{
					// No need to use the actual child width as the extent width.
					StopUseOfActualWidthAsExtent();
				}
				if (m_isChildActualHeightUsedAsExtent)
				{
					// No need to use the actual child height as the extent height.
					StopUseOfActualHeightAsExtent();
				}
				VerifyScrollData(pScrollData.m_viewport /*viewport*/, desiredSizeZoomed /*extent*/);
			}
			else
			{
				if (spScrollViewer is not null)
				{
					layoutSize = spScrollViewer.GetLayoutSize();
				}

				// blow over the reported size to use the passed in size. This will increase the extent.
				if (layoutSize.Width != 0.0f && layoutSize.Height != 0.0f)
				{
					// This situation only arises with the SemanticZoom control which applies a pseudo-LayoutTransform to the ScrollViewer.Content element.
					// The SemanticZoom provides a layout size to the ScrollViewer to be imposed to its ScrollContentPresenter.
					desiredSizeZoomed.Width = (layoutSize.Width + headersDesiredSize.Width) * zoomFactor;
					desiredSizeZoomed.Height = (layoutSize.Height + headersDesiredSize.Height) * zoomFactor;

					if (m_isChildActualWidthUsedAsExtent)
					{
						// In case the actual size was being used as the extent, use the imposed layoutSize instead.
						StopUseOfActualWidthAsExtent();
					}
					if (m_isChildActualHeightUsedAsExtent)
					{
						// In case the actual size was being used as the extent, use the imposed layoutSize instead.
						StopUseOfActualHeightAsExtent();
					}
				}
				else
				{
					desiredSizeZoomed.Width = desiredSize.Width * zoomFactor;
					desiredSizeZoomed.Height = desiredSize.Height * zoomFactor;

					var setExtent = false;
					var extentSize = pScrollData.m_extent;
					var canUseActualSizeAsExtent = false;

					if (m_isChildActualWidthUsedAsExtent)
					{
						CanUseActualWidthAsExtent(spScrollOwner, spScrollViewer, spChildAsFE, out canUseActualSizeAsExtent);
						if (!canUseActualSizeAsExtent)
						{
							StopUseOfActualWidthAsExtent();
						}
						else if (pScrollData.m_canHorizontallyScroll && desiredSize.Width >= pScrollData.m_viewport.Width)
						{
							// After switching to the mode where the child's actual width is used as extent, push an extent that is larger
							// than the viewport as early as possible to the owning ScrollViewer. This is important for ModernCollectionBasePanel
							// which may trigger a call to ScrollViewer::ChangeViewInternal inside its ArrangeOverride.
							setExtent = true;

							if (sizesContentToTemplatedParent && desiredSize.Width == pScrollData.m_viewport.Width)
							{
								desiredSizeZoomed.Width = extentSize.Width;
							}
							else
							{
								extentSize.Width = desiredSizeZoomed.Width;
							}
						}
					}

					if (m_isChildActualHeightUsedAsExtent)
					{
						CanUseActualHeightAsExtent(spScrollOwner, spScrollViewer, spChildAsFE, out canUseActualSizeAsExtent);
						if (!canUseActualSizeAsExtent)
						{
							StopUseOfActualHeightAsExtent();
						}
						else if (pScrollData.m_canVerticallyScroll && desiredSize.Height >= pScrollData.m_viewport.Height)
						{
							// After switching to the mode where the child's actual height is used as extent, push an extent that is larger
							// than the viewport as early as possible to the owning ScrollViewer. This is important for ModernCollectionBasePanel
							// which may trigger a call to ScrollViewer::ChangeViewInternal inside its ArrangeOverride.
							setExtent = true;

							if (sizesContentToTemplatedParent && desiredSize.Height == pScrollData.m_viewport.Height)
							{
								desiredSizeZoomed.Height = extentSize.Height;
							}
							else
							{
								extentSize.Height = desiredSizeZoomed.Height;
							}
						}
					}

					if (!m_isChildActualWidthUsedAsExtent && m_isChildActualHeightUsedAsExtent)
					{
						// Only the actual height is used as extent. Make sure the latest desired width is used as extent.
						setExtent = true;
						extentSize.Width = desiredSizeZoomed.Width;
						// The extent height pushed to the ScrollViewer in the VerifyScrollData call below is not up-to-date.
						// The updated height will be pushed to the ScrollViewer in the VerifyScrollData call made in the coming ArrangeOverride.
						// The m_isChildActualHeightUpdated flag is temporarily set to False during the VerifyScrollData call below so the
						// ScrollViewer does not prematurely reset its m_contentHeightRequested field in its InvalidateScrollInfo implementation.
						m_isChildActualHeightUpdated = false;
					}
					else if (m_isChildActualWidthUsedAsExtent && !m_isChildActualHeightUsedAsExtent)
					{
						// Only the actual width is used as extent. Make sure the latest desired height is used as extent.
						setExtent = true;
						extentSize.Height = desiredSizeZoomed.Height;
						// The extent width pushed to the ScrollViewer in the VerifyScrollData call below is not up-to-date.
						// The updated width will be pushed to the ScrollViewer in the VerifyScrollData call made in the coming ArrangeOverride.
						// The m_isChildActualWidthUpdated flag is temporarily set to False during the VerifyScrollData call below so the
						// ScrollViewer does not prematurely reset its m_contentWidthRequested field in its InvalidateScrollInfo implementation.
						m_isChildActualWidthUpdated = false;
					}

					if (setExtent)
					{
						VerifyScrollData(pScrollData.m_viewport, extentSize);
					}
				}

				// Do not attempt to update the IScrollInfo extent when this ScrollContentPresenter
				// operates in the mode where the child's actual size is used as extent.
				if (m_isChildActualWidthUsedAsExtent)
				{
					m_unpublishedExtentSize.Width = desiredSizeZoomed.Width;
				}
				if (m_isChildActualHeightUsedAsExtent)
				{
					m_unpublishedExtentSize.Height = desiredSizeZoomed.Height;
				}

				if (!m_isChildActualWidthUsedAsExtent && !m_isChildActualHeightUsedAsExtent)
				{
					// If we're handling scrolling (as the physical scrolling client, validate properties).
					VerifyScrollData(pScrollData.m_viewport /*viewport*/, desiredSizeZoomed /*extent*/);
				}
			}

			if (adjustDesiredSize)
			{
				// When we need to adjust desired size, we ignore the available size.
				desiredSize.Width += toBeAdjustedDesiredSize.Width;
				desiredSize.Height += toBeAdjustedDesiredSize.Height;
			}
			else if (layoutSize.Width != 0.0f && layoutSize.Height != 0.0f)
			{
				// SemanticZoom's ScrollViewer case. Use the enforced layoutSize rather than the child's desiredSize.
				// This matches how desiredSizeZoomed is evaluated for the VerifyScrollData call above.
				desiredSize.Width = Math.Min(availableSize.Width, layoutSize.Width);
				desiredSize.Height = Math.Min(availableSize.Height, layoutSize.Height);
			}
			else
			{
				desiredSize.Width = Math.Min(availableSize.Width, desiredSize.Width);
				desiredSize.Height = Math.Min(availableSize.Height, desiredSize.Height);
			}

			m_isChildActualWidthUpdated = true;
			m_isChildActualHeightUpdated = true;

			// Let ScrollViewer know that child sizes might have changed
			spScrollViewer?.OnScrollContentPresenterMeasured();

			return desiredSize;
		}

		// Provides the behavior for the Arrange pass of layout. Classes can
		// override this method to define their own Arrange pass behavior.
		internal global::Windows.Foundation.Size ArrangeOverridePort(global::Windows.Foundation.Size finalSize)
		{
			do
			{
				// NOTE: We are updating the clip only if there is a scroll owner that hosts
				// this control. This is a limited fix for 22803.
				if (TemplatedParent is not null || ScrollOwner is not null)
				{
					UpdateClip(finalSize);
				}

				var spChild = Content as UIElement;
				var spChildAsFE = spChild as FrameworkElement;
				var spTopLeftHeader = m_trTopLeftHeader;
				var spTopHeader = m_trTopHeader;
				var spLeftHeader = m_trLeftHeader;
				var topLeftHeaderDesiredSize = default(global::Windows.Foundation.Size);
				var topHeaderDesiredSize = default(global::Windows.Foundation.Size);
				var leftHeaderDesiredSize = default(global::Windows.Foundation.Size);
				var isHeaderArranged = false;

				// Verifies IScrollInfo properties & invalidates ScrollViewer if necessary.
				m_scrollRequested = false;

				var pScrollData = GetScrollData();
				var spScrollOwner = pScrollData?.GetScrollOwner();
				var spScrollViewer = spScrollOwner as ScrollViewer;
				var isScrollClient = IsScrollClient();

				if (isScrollClient && pScrollData is not null)
				{
					var extentSize = pScrollData.m_extent;

					if (m_isChildActualWidthUsedAsExtent &&
						m_unpublishedExtentSize.Width > 0 &&
						m_unpublishedExtentSize.Width == finalSize.Width &&
						extentSize.Width != m_unpublishedExtentSize.Width)
					{
						// Use the unpublished desired width which ends up being the final arrangement width.
						extentSize.Width = m_unpublishedExtentSize.Width;
						StopUseOfActualWidthAsExtent();
					}

					if (m_isChildActualHeightUsedAsExtent &&
						m_unpublishedExtentSize.Height > 0 &&
						m_unpublishedExtentSize.Height == finalSize.Height &&
						extentSize.Height != m_unpublishedExtentSize.Height)
					{
						// Use the unpublished desired height which ends up being the final arrangement height.
						extentSize.Height = m_unpublishedExtentSize.Height;
						StopUseOfActualHeightAsExtent();
					}

					VerifyScrollData(finalSize /*viewport*/, extentSize /*extent*/);
				}

				if (m_isTopLeftHeaderChild && spTopLeftHeader is not null)
				{
					topLeftHeaderDesiredSize = spTopLeftHeader.DesiredSize;
					isHeaderArranged = true;
				}
				if (m_isTopHeaderChild && spTopHeader is not null)
				{
					topHeaderDesiredSize = spTopHeader.DesiredSize;
					isHeaderArranged = true;
				}
				if (m_isLeftHeaderChild && spLeftHeader is not null)
				{
					leftHeaderDesiredSize = spLeftHeader.DesiredSize;
					isHeaderArranged = true;
				}

				var currentZoomFactor = 1.0f;
				if (spChild is not null && isScrollClient)
				{
					if (spScrollOwner is not null)
					{
						currentZoomFactor = spScrollOwner.GetZoomFactor();
						global::System.Diagnostics.Debug.Assert(currentZoomFactor == m_fZoomFactor);
					}

					if (spScrollViewer is not null && spScrollViewer.IsInDirectManipulationCompletion())
					{
						spScrollViewer.PostDirectManipulationLayoutRefreshed();
					}
				}
				else if (isHeaderArranged && spScrollOwner is not null)
				{
					currentZoomFactor = spScrollOwner.GetZoomFactor();
					global::System.Diagnostics.Debug.Assert(currentZoomFactor == m_fZoomFactor);
				}

				if (spTopLeftHeader is not null)
				{
					spTopLeftHeader.Arrange(new global::Windows.Foundation.Rect(
						0,
						0,
						topLeftHeaderDesiredSize.Width,
						topLeftHeaderDesiredSize.Height));
				}

				if (spTopHeader is not null)
				{
					spTopHeader.Arrange(new global::Windows.Foundation.Rect(
						Math.Max(topLeftHeaderDesiredSize.Width, leftHeaderDesiredSize.Width),
						0,
						topHeaderDesiredSize.Width,
						topHeaderDesiredSize.Height));
				}

				if (spLeftHeader is not null)
				{
					spLeftHeader.Arrange(new global::Windows.Foundation.Rect(
						0,
						Math.Max(topLeftHeaderDesiredSize.Height, topHeaderDesiredSize.Height),
						leftHeaderDesiredSize.Width,
						leftHeaderDesiredSize.Height));
				}

				if (spChild is not null)
				{
					var desiredSize = spChild.DesiredSize;
					var childRect = new global::Windows.Foundation.Rect(
						Math.Max(topLeftHeaderDesiredSize.Width, leftHeaderDesiredSize.Width),
						Math.Max(topLeftHeaderDesiredSize.Height, topHeaderDesiredSize.Height),
						Math.Max(desiredSize.Width, finalSize.Width),
						Math.Max(desiredSize.Height, finalSize.Height));

					spChild.Arrange(childRect);

					// Give opportunity to the content to define the viewport size itself.
					(spChild as ICustomScrollInfo)?.ApplyViewport(ref finalSize);

					if (isScrollClient && pScrollData is not null)
					{
						if (spChild.IsArrangeDirty)
						{
							if (m_isChildActualWidthUsedAsExtent || m_isChildActualHeightUsedAsExtent)
							{
								// When operating in the mode where the child's actual width or height is used for the IScrollInfo extent
								// and the child is still marked dirty for layout, make sure that this ScrollContentPresenter::ArrangeOverride
								// is invoked again so that the correct content extent can be pushed to the owning ScrollViewer with a call to
								// VerifyScrollData in the 'else' branch below once the child got arranged.
								InvalidateArrange();
							}
						}
						else
						{
							var extentSize = pScrollData.m_extent;
							var canUseActualSizeAsExtent = false;

							// Check if the mode where the child's actual width is used for the IScrollInfo extent must be entered.
							// To minimize the occurrences of this mode, it is restricted to cases that use a Stretch alignment.
							CanUseActualWidthAsExtent(spScrollOwner, spScrollViewer, spChildAsFE, out canUseActualSizeAsExtent);
							global::System.Diagnostics.Debug.Assert(canUseActualSizeAsExtent || !m_isChildActualWidthUsedAsExtent);
							if (canUseActualSizeAsExtent)
							{
								// Determine the child's actual width, including the margins which are included in the IScrollInfo extent.
								global::System.Diagnostics.Debug.Assert(spChildAsFE is not null);
								var margins = spChildAsFE.Margin;
								var actualWidth = Math.Max(0.0, spChildAsFE.ActualWidth + margins.Left + margins.Right);
								var useLayoutRounding = GetUseLayoutRounding();
								if (useLayoutRounding)
								{
									// Apply the same rounding on the content width as for the viewport width, i.e. finalSize, provided as a parameter.
									// This is to avoid situations where the content width ends up being slightly larger than the viewport width and
									// incorrectly causes the horizontal scrollbar to appear.
									actualWidth = spChild.LayoutRound(actualWidth);
								}

								// Limit the width to the viewport width when horizontal scrolling is disabled.
								if (!pScrollData.m_canHorizontallyScroll && actualWidth > pScrollData.m_viewport.Width)
								{
									actualWidth = pScrollData.m_viewport.Width;
								}

								if (m_isChildActualWidthUsedAsExtent ||
									(pScrollData.m_extent.Width > 0 &&
										!DoubleUtil.AreWithinTolerance(
											actualWidth * currentZoomFactor,
											pScrollData.m_extent.Width,
											ScrollViewer.ScrollViewerScrollRoundingTolerance)))
								{
									var actualWidthWithRoundedDownMarginsMatchesExtentWidth = false;
									var scale = RootScale.GetRasterizationScaleForElement(this);
									var roundingStep = 1.0 / scale;

									if (!m_isChildActualWidthUsedAsExtent &&
										useLayoutRounding &&
										margins.Left + margins.Right >= roundingStep)
									{
										// c.f. RS5 bug 18604282. The desired width and computed actual width may differ by a single rounding step because the FrameworkElement.ActualWidth was rounded up
										// while it was not in the desired size.
										// Example at global scale factor of 1.5: FrameworkElement.ActualWidth is rounded up from 238 to 238.66 in CFrameworkElement::ArrangeCore. With a Margin.Left of 11px
										// and a Margin.Right of 12px, the DesiredSize.Width is set to LayoutRound(238 + LayoutRound(23)) == 261.33 in CFrameworkElement::MeasureCore. Thus the check above
										// "Is LayoutRound(238.66 + 23)==262 equal to 261.33?" fails.
										// Verifying if that is the case below.
										var actualWidthWithRoundedDownMargins = spChild.LayoutRound(actualWidth - roundingStep);
										actualWidthWithRoundedDownMarginsMatchesExtentWidth =
											DoubleUtil.AreWithinTolerance(
												actualWidthWithRoundedDownMargins * currentZoomFactor,
												pScrollData.m_extent.Width,
												ScrollViewer.ScrollViewerScrollRoundingTolerance);
									}

									if (!actualWidthWithRoundedDownMarginsMatchesExtentWidth)
									{
										// When m_isChildActualWidthUsedAsExtent==False, the extent previously set in MeasureOverride does not match the resulting width after Arrange.
										// Override the extent based on the new actual width.
										// When m_isChildActualWidthUsedAsExtent==True, this ScrollContentPresenter already uses the child's actual width as the extent. This remains
										// the case until a new Content is set or CanUseActualWidthAsExtent returns False.
										if (!m_isChildActualWidthUsedAsExtent)
										{
											StartUseOfActualWidthAsExtent();
										}

										// Finally use the child's actual width for the IScrollInfo extent.
										extentSize.Width = actualWidth * currentZoomFactor;
									}
								}
							}

							// Check if the mode where the child's actual height is used for the IScrollInfo extent must be entered.
							// To minimize the occurrences of this mode, it is restricted to cases that use a Stretch alignment.
							CanUseActualHeightAsExtent(spScrollOwner, spScrollViewer, spChildAsFE, out canUseActualSizeAsExtent);
							global::System.Diagnostics.Debug.Assert(canUseActualSizeAsExtent || !m_isChildActualHeightUsedAsExtent);
							if (canUseActualSizeAsExtent)
							{
								// Determine the child's actual height, including the margins which are included in the IScrollInfo extent.
								global::System.Diagnostics.Debug.Assert(spChildAsFE is not null);
								var margins = spChildAsFE.Margin;
								var actualHeight = Math.Max(0.0, spChildAsFE.ActualHeight + margins.Top + margins.Bottom);
								var useLayoutRounding = GetUseLayoutRounding();
								if (useLayoutRounding)
								{
									// Apply the same rounding on the content height as for the viewport height, i.e. finalSize, provided as a parameter.
									// This is to avoid situations where the content height ends up being slightly larger than the viewport height and
									// incorrectly causes the vertical scrollbar to appear.
									actualHeight = spChild.LayoutRound(actualHeight);
								}

								// Limit the height to the viewport height when vertical scrolling is disabled.
								if (!pScrollData.m_canVerticallyScroll && actualHeight > pScrollData.m_viewport.Height)
								{
									actualHeight = pScrollData.m_viewport.Height;
								}

								if (m_isChildActualHeightUsedAsExtent ||
									(pScrollData.m_extent.Height > 0 &&
										!DoubleUtil.AreWithinTolerance(
											actualHeight * currentZoomFactor,
											pScrollData.m_extent.Height,
											ScrollViewer.ScrollViewerScrollRoundingTolerance)))
								{
									var actualHeightWithRoundedDownMarginsMatchesExtentHeight = false;
									var scale = RootScale.GetRasterizationScaleForElement(this);
									var roundingStep = 1.0 / scale;

									if (!m_isChildActualHeightUsedAsExtent &&
										useLayoutRounding &&
										margins.Top + margins.Bottom >= roundingStep)
									{
										// c.f. RS5 bug 18604282. The desired height and computed actual height may differ by a single rounding step because the FrameworkElement.ActualHeight was rounded up
										// while it was not in the desired size.
										// Example at global scale factor of 1.5: FrameworkElement.ActualHeight is rounded up from 238 to 238.66 in CFrameworkElement::ArrangeCore. With a Margin.Top of 11px
										// and a Margin.Bottom of 12px, the DesiredSize.Height is set to LayoutRound(238 + LayoutRound(23)) == 261.33 in CFrameworkElement::MeasureCore. Thus the check above
										// "Is LayoutRound(238.66 + 23)==262 equal to 261.33?" fails.
										// Verifying if that is the case below.
										var actualHeightWithRoundedDownMargins = spChild.LayoutRound(actualHeight - roundingStep);
										actualHeightWithRoundedDownMarginsMatchesExtentHeight =
											DoubleUtil.AreWithinTolerance(
												actualHeightWithRoundedDownMargins * currentZoomFactor,
												pScrollData.m_extent.Height,
												ScrollViewer.ScrollViewerScrollRoundingTolerance);
									}

									if (!actualHeightWithRoundedDownMarginsMatchesExtentHeight)
									{
										// When m_isChildActualHeightUsedAsExtent==False, the extent previously set in MeasureOverride does not match the resulting height after Arrange.
										// Override the extent based on the new actual height.
										// When m_isChildActualHeightUsedAsExtent==True, this ScrollContentPresenter already uses the child's actual height as the extent. This remains
										// the case until a new Content is set or CanUseActualHeightAsExtent returns False.
										if (!m_isChildActualHeightUsedAsExtent)
										{
											StartUseOfActualHeightAsExtent();
										}

										// Finally use the child's actual height for the IScrollInfo extent.
										extentSize.Height = actualHeight * currentZoomFactor;
									}
								}
							}

							if (m_isChildActualWidthUsedAsExtent || m_isChildActualHeightUsedAsExtent)
							{
								VerifyScrollData(pScrollData.m_viewport, extentSize);
							}
						}
					}
				}
			}
			while (m_scrollRequested);

			return finalSize;
		}

		// Override the default tab-based navigation order when headers are present such that
		// the tab order is top-left header -> top header -> left header -> content.
		// Handle scenarios where the default behavior is to exit the ScrollContentPresenter or remain inside.
		internal override TabStopProcessingResult ProcessTabStopOverride(
			DependencyObject focusedElement,
			DependencyObject candidateTabStopElement,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			if (!m_isTopLeftHeaderChild && !m_isTopHeaderChild && !m_isLeftHeaderChild)
			{
				// No custom navigation needed when there is no header element.
				return default;
			}

			// Determine where the currently focused element and new candidate are in
			// relation to the headers and content.
			AnalyzeTabbingElements(
				focusedElement,
				candidateTabStopElement,
				out var isFocusedElementInTopLeftHeader,
				out var isFocusedElementInTopHeader,
				out var isFocusedElementInLeftHeader,
				out var isFocusedElementInContent,
				out var isCandidateElementInTopLeftHeader,
				out var isCandidateElementInTopHeader,
				out var isCandidateElementInLeftHeader,
				out var isCandidateElementInContent);

			if ((isFocusedElementInTopLeftHeader && isCandidateElementInTopLeftHeader) ||
				(isFocusedElementInTopHeader && isCandidateElementInTopHeader) ||
				(isFocusedElementInLeftHeader && isCandidateElementInLeftHeader) ||
				(isFocusedElementInContent && isCandidateElementInContent))
			{
				// No custom navigation is needed when remaining within the same header or content.
				return default;
			}

			if (isFocusedElementInTopLeftHeader ||
				isFocusedElementInTopHeader ||
				isFocusedElementInLeftHeader ||
				isFocusedElementInContent)
			{
				return ProcessTabStopPrivate(
					isBackward,
					isFocusedElementInTopLeftHeader,
					isFocusedElementInTopHeader,
					isFocusedElementInLeftHeader,
					isFocusedElementInContent);
			}

			return default;
		}

		// Override the default tab-based navigation order when headers are present such that
		// the tab order is top-left header -> top header -> left header -> content.
		// Handle scenarios where the default behavior is to enter the ScrollContentPresenter from the outside.
		internal override TabStopProcessingResult ProcessCandidateTabStopOverride(
			DependencyObject focusedElement,
			DependencyObject candidateTabStopElement,
			DependencyObject overriddenCandidateTabStopElement,
			bool isBackward)
		{
			if (!m_isTopLeftHeaderChild && !m_isTopHeaderChild && !m_isLeftHeaderChild)
			{
				// No custom navigation needed when there is no header element.
				return default;
			}

			// Determine where the currently focused element and new candidate are in
			// relation to the headers and content.
			AnalyzeTabbingElements(
				focusedElement,
				candidateTabStopElement,
				out var isFocusedElementInTopLeftHeader,
				out var isFocusedElementInTopHeader,
				out var isFocusedElementInLeftHeader,
				out var isFocusedElementInContent,
				out var isCandidateElementInTopLeftHeader,
				out var isCandidateElementInTopHeader,
				out var isCandidateElementInLeftHeader,
				out var isCandidateElementInContent);

			// No custom navigation is needed when remaining within the same header or content.
			if ((isFocusedElementInTopLeftHeader && isCandidateElementInTopLeftHeader) ||
				(isFocusedElementInTopHeader && isCandidateElementInTopHeader) ||
				(isFocusedElementInLeftHeader && isCandidateElementInLeftHeader) ||
				(isFocusedElementInContent && isCandidateElementInContent))
			{
				return default;
			}

			// No custom navigation is needed when attempting to leave the ScrollContentPresenter.
			if (!isCandidateElementInTopLeftHeader &&
				!isCandidateElementInTopHeader &&
				!isCandidateElementInLeftHeader &&
				!isCandidateElementInContent)
			{
				return default;
			}

			if (!isFocusedElementInTopLeftHeader &&
				!isFocusedElementInTopHeader &&
				!isFocusedElementInLeftHeader &&
				!isFocusedElementInContent)
			{
				// Attempting to enter the ScrollContentPresenter.
				var candidateChild = GetDirectChild(
					isCandidateElementInTopLeftHeader,
					isCandidateElementInTopHeader,
					isCandidateElementInLeftHeader,
					isCandidateElementInContent);
				if (candidateChild is null)
				{
					return default;
				}

				// Check if the owning direct child has a TabIndex set.
				GetTabIndex(candidateChild, out _, out m_tabIndex);
				m_isTabIndexSet = true;
				try
				{
					var directChild = isBackward
						? GetFirstFocusableElementOverride()
						: GetLastFocusableElementOverride();
					var newTabStop = GetFocusableTarget(directChild, isBackward);
					return new(newTabStop is not null, newTabStop);
				}
				finally
				{
					m_isTabIndexSet = false;
				}
			}

			return default;
		}

		// Returns the first focusable element among the headers and content with a TabIndex equal to m_tabIndex.
		internal override DependencyObject GetFirstFocusableElementOverride()
		{
			var children = GetOrderedFocusableChildren();
			if (m_isTabIndexSet)
			{
				for (var index = children.Count - 1; index >= 0; index--)
				{
					if (children[index].TabIndex == m_tabIndex)
					{
						return children[index].Element;
					}
				}
			}
			else if (children.Count > 0)
			{
				return children[0].Element;
			}

			return null;
		}

		// Returns the last focusable element among the headers and content with a TabIndex equal to m_tabIndex.
		internal override DependencyObject GetLastFocusableElementOverride()
		{
			var children = GetOrderedFocusableChildren();
			if (m_isTabIndexSet)
			{
				for (var index = 0; index < children.Count; index++)
				{
					if (children[index].TabIndex == m_tabIndex)
					{
						return children[index].Element;
					}
				}
			}
			else if (children.Count > 0)
			{
				return children[^1].Element;
			}

			return null;
		}

		// Determines if a direct child has a custom TabIndex value set, while TabStop is True.
		private bool HasDirectChildWithTabIndexSet()
		{
			foreach (var child in GetOrderedFocusableChildren())
			{
				if (child.TabIndex != int.MaxValue)
				{
					return true;
				}
			}

			return false;
		}

		// Handles tab-based navigation when a custom TabIndex value is set for
		// a header or the content.
		private TabStopProcessingResult ProcessTabStopPrivate(
			bool isBackward,
			bool isFocusedElementInTopLeftHeader,
			bool isFocusedElementInTopHeader,
			bool isFocusedElementInLeftHeader,
			bool isFocusedElementInContent)
		{
			var children = GetOrderedFocusableChildren();
			var focusedIndex = -1;
			for (var index = 0; index < children.Count; index++)
			{
				var child = children[index];
				if ((isFocusedElementInTopLeftHeader && child.IsTopLeftHeader) ||
					(isFocusedElementInTopHeader && child.IsTopHeader) ||
					(isFocusedElementInLeftHeader && child.IsLeftHeader) ||
					(isFocusedElementInContent && child.IsContent))
				{
					focusedIndex = index;
					break;
				}
			}

			if (focusedIndex >= 0)
			{
				var targetIndex = focusedIndex + (isBackward ? -1 : 1);
				if (targetIndex >= 0 && targetIndex < children.Count)
				{
					var newTabStop = GetFocusableTarget(children[targetIndex].Element, isBackward);
					return new(newTabStop is not null, newTabStop);
				}
			}

			var focusManager = VisualTree.GetContentRootForElement(this)?.FocusManager;
			var outsideTabStop = isBackward
				? focusManager?.GetPreviousTabStop(this)
				: focusManager?.GetNextTabStop(this, true);
			return new(outsideTabStop is not null, outsideTabStop);
		}

		// Determines the location of the first focusable element in scenarios where a custom TabIndex value
		// is set for a header or the content.
		private void GetFirstFocusableElementPrivate(
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
		{
			var children = GetOrderedFocusableChildren();
			SetTabbingRegion(
				children.Count > 0 ? children[0] : default,
				out isTopLeftHeader,
				out isTopHeader,
				out isLeftHeader,
				out isContent);
		}

		// Determines the location of the last focusable element in scenarios where a custom TabIndex value
		// is set for a header or the content.
		private void GetLastFocusableElementPrivate(
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
		{
			var children = GetOrderedFocusableChildren();
			SetTabbingRegion(
				children.Count > 0 ? children[^1] : default,
				out isTopLeftHeader,
				out isTopHeader,
				out isLeftHeader,
				out isContent);
		}

		// Determines the next focusable element among the headers and content for scenarios
		// that involve a custom TabIndex value.
		private DependencyObject GetNextFocusableElementPrivate(
			bool isFocusedElementInTopLeftHeader,
			bool isFocusedElementInTopHeader,
			bool isFocusedElementInLeftHeader,
			bool isFocusedElementInContent,
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
			=> GetAdjacentFocusableElementPrivate(
				isBackward: false,
				isFocusedElementInTopLeftHeader,
				isFocusedElementInTopHeader,
				isFocusedElementInLeftHeader,
				isFocusedElementInContent,
				out isTopLeftHeader,
				out isTopHeader,
				out isLeftHeader,
				out isContent);

		// Determines the previous focusable element among the headers and content for scenarios
		// that involve a custom TabIndex value.
		private DependencyObject GetPreviousFocusableElementPrivate(
			bool isFocusedElementInTopLeftHeader,
			bool isFocusedElementInTopHeader,
			bool isFocusedElementInLeftHeader,
			bool isFocusedElementInContent,
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
			=> GetAdjacentFocusableElementPrivate(
				isBackward: true,
				isFocusedElementInTopLeftHeader,
				isFocusedElementInTopHeader,
				isFocusedElementInLeftHeader,
				isFocusedElementInContent,
				out isTopLeftHeader,
				out isTopHeader,
				out isLeftHeader,
				out isContent);

		private DependencyObject GetAdjacentFocusableElementPrivate(
			bool isBackward,
			bool isFocusedElementInTopLeftHeader,
			bool isFocusedElementInTopHeader,
			bool isFocusedElementInLeftHeader,
			bool isFocusedElementInContent,
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
		{
			var children = GetOrderedFocusableChildren();
			for (var index = 0; index < children.Count; index++)
			{
				var child = children[index];
				if ((isFocusedElementInTopLeftHeader && child.IsTopLeftHeader) ||
					(isFocusedElementInTopHeader && child.IsTopHeader) ||
					(isFocusedElementInLeftHeader && child.IsLeftHeader) ||
					(isFocusedElementInContent && child.IsContent))
				{
					var targetIndex = index + (isBackward ? -1 : 1);
					if (targetIndex >= 0 && targetIndex < children.Count)
					{
						var target = children[targetIndex];
						SetTabbingRegion(
							target,
							out isTopLeftHeader,
							out isTopHeader,
							out isLeftHeader,
							out isContent);
						return target.Element;
					}
					break;
				}
			}

			SetTabbingRegion(
				default,
				out isTopLeftHeader,
				out isTopHeader,
				out isLeftHeader,
				out isContent);
			return null;
		}

		// Determines where the currently focused element and new candidate are in
		// relation to the headers and content.
		private void AnalyzeTabbingElements(
			DependencyObject focusedElement,
			DependencyObject candidateTabStopElement,
			out bool isFocusedElementInTopLeftHeader,
			out bool isFocusedElementInTopHeader,
			out bool isFocusedElementInLeftHeader,
			out bool isFocusedElementInContent,
			out bool isCandidateElementInTopLeftHeader,
			out bool isCandidateElementInTopHeader,
			out bool isCandidateElementInLeftHeader,
			out bool isCandidateElementInContent)
		{
			isFocusedElementInTopLeftHeader = false;
			isFocusedElementInTopHeader = false;
			isFocusedElementInLeftHeader = false;
			isFocusedElementInContent = false;
			isCandidateElementInTopLeftHeader = false;
			isCandidateElementInTopHeader = false;
			isCandidateElementInLeftHeader = false;
			isCandidateElementInContent = false;

			if (focusedElement is not null)
			{
				GetHeaderOwnership(
					focusedElement,
					out _,
					out isFocusedElementInTopLeftHeader,
					out isFocusedElementInTopHeader,
					out isFocusedElementInLeftHeader,
					out isFocusedElementInContent);
			}

			if (candidateTabStopElement is not null)
			{
				GetHeaderOwnership(
					candidateTabStopElement,
					out _,
					out isCandidateElementInTopLeftHeader,
					out isCandidateElementInTopHeader,
					out isCandidateElementInLeftHeader,
					out isCandidateElementInContent);
			}
		}

		// Returns the requested direct child as a dependency object.
		private DependencyObject GetDirectChild(
			bool topLeftHeader,
			bool topHeader,
			bool leftHeader,
			bool content)
		{
			if (topLeftHeader)
			{
				return m_trTopLeftHeader;
			}
			if (topHeader)
			{
				return m_trTopHeader;
			}
			if (leftHeader)
			{
				return m_trLeftHeader;
			}
			return content ? Content as DependencyObject : null;
		}

		// Determines if an element is focusable or has a focusable child.
		private static void GetTabIndex(
			DependencyObject element,
			out bool isTabStop,
			out int tabIndex)
		{
			isTabStop = false;
			tabIndex = int.MaxValue;

			if (element is UIElement uiElement)
			{
				tabIndex = uiElement.TabIndex;
				isTabStop = uiElement.IsTabStop &&
					(uiElement is not Control control || control.IsEnabled);
				isTabStop |= FocusManager.FindFirstFocusableElement(element) is not null;
			}
			else if (element is Microsoft.UI.Xaml.Documents.Hyperlink hyperlink)
			{
				isTabStop = hyperlink.IsTabStop;
				tabIndex = hyperlink.TabIndex;
			}
			else
			{
				// The provided element is not a Control with a IsTabStop property but may have a child with one set to True.
				isTabStop = FocusManager.FindFirstFocusableElement(element) is not null;
			}
		}

		private List<TabbingChild> GetOrderedFocusableChildren()
		{
			var children = new List<TabbingChild>(4);
			Add(m_trTopLeftHeader, isTopLeftHeader: true, isTopHeader: false, isLeftHeader: false, isContent: false, order: 0);
			Add(m_trTopHeader, isTopLeftHeader: false, isTopHeader: true, isLeftHeader: false, isContent: false, order: 1);
			Add(m_trLeftHeader, isTopLeftHeader: false, isTopHeader: false, isLeftHeader: true, isContent: false, order: 2);
			Add(Content as DependencyObject, isTopLeftHeader: false, isTopHeader: false, isLeftHeader: false, isContent: true, order: 3);
			children.Sort(static (left, right) =>
			{
				var tabIndexComparison = left.TabIndex.CompareTo(right.TabIndex);
				return tabIndexComparison != 0 ? tabIndexComparison : left.Order.CompareTo(right.Order);
			});
			return children;

			void Add(
				DependencyObject element,
				bool isTopLeftHeader,
				bool isTopHeader,
				bool isLeftHeader,
				bool isContent,
				int order)
			{
				if (element is null)
				{
					return;
				}

				GetTabIndex(element, out var isTabStop, out var tabIndex);
				if (isTabStop)
				{
					children.Add(new(
						element,
						isTopLeftHeader,
						isTopHeader,
						isLeftHeader,
						isContent,
						tabIndex,
						order));
				}
			}
		}

		private static DependencyObject GetFocusableTarget(DependencyObject directChild, bool isBackward)
		{
			if (directChild is null)
			{
				return null;
			}

			var target = isBackward
				? FocusManager.FindLastFocusableElement(directChild)
				: FocusManager.FindFirstFocusableElement(directChild);
			if (target is not null)
			{
				return target;
			}

			if (directChild is UIElement uiElement &&
				uiElement.IsTabStop &&
				(uiElement is not Control control || control.IsEnabled))
			{
				return directChild;
			}

			return null;
		}

		private static void SetTabbingRegion(
			TabbingChild child,
			out bool isTopLeftHeader,
			out bool isTopHeader,
			out bool isLeftHeader,
			out bool isContent)
		{
			isTopLeftHeader = child.IsTopLeftHeader;
			isTopHeader = child.IsTopHeader;
			isLeftHeader = child.IsLeftHeader;
			isContent = child.IsContent;
		}

		private readonly record struct TabbingChild(
			DependencyObject Element,
			bool IsTopLeftHeader,
			bool IsTopHeader,
			bool IsLeftHeader,
			bool IsContent,
			int TabIndex,
			int Order);

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
