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
		// (C++ source line 1078 — Skia-focused port: header ownership and DManip
		//  view-snapshotting paths are stubbed; all other behavior preserved.)
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

			appliedOffsetX = 0.0;
			appliedOffsetY = 0.0;

			// Handle cases where we don't have to do anything
			isEmpty = rectangle.IsEmpty || rectangle.Width == 0 || rectangle.Height == 0;
			isEmpty = isEmpty || visual is null || visual == this;
			if (!isEmpty)
			{
				// TODO Uno: IsAncestorOfAndMostAncestorPageBetween — Page tracking
				// is required only by GetFullScreenPageBottomAppBarHeight (an
				// app-bar/full-screen accommodation that is Win32 specific). We
				// approximate by walking the parent chain via IsAncestorOf and
				// not tracking the intervening Page.
				isAncestor = this.IsAncestorOf(visual);
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
				// TODO Uno: Phase 6 — when headers are ported, replace with a real
				// GetZoomedHeadersSize(out sizeHeaders).
				sizeHeaders = new global::Windows.Foundation.Size(0, 0);

				// Adjust the target rectangle based on those headers
				rectangle.X -= sizeHeaders.Width;
				rectangle.Y -= sizeHeaders.Height;

				isScrollClient = IsScrollClient();
				if (isScrollClient)
				{
					// Check if visual belongs to a header.
					// TODO Uno: Phase 6 — replace with a real GetHeaderOwnership(...)
					// once headers are ported. For now treat every descendant as
					// part of the scrollable content.
					isVisualDirectChild = false;
					isVisualInTopLeftHeader = false;
					isVisualInTopHeader = false;
					isVisualInLeftHeader = false;
					isVisualInContent = true;

					if (!isVisualInTopLeftHeader)
					{
						var spScrollOwner = GetScrollOwner();
						ScrollViewer spScrollViewer = spScrollOwner as ScrollViewer;

						// Initialize the viewport
						if (spScrollViewer is not null && useAnimation)
						{
#if false
							// TODO Uno: Phase 4 DM wiring — when DM adapter lands, restore the
							// in-manipulation snapshot of the DManip view + target view so
							// MakeVisible during a manipulation merges with the ongoing animation.
							if (spScrollViewer.IsInManipulation())
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
#else
							// Phase-4 stub: fall back to the cached IScrollInfo offsets.
							horizontalOffset = GetHorizontalOffset();
							verticalOffset = GetVerticalOffset();
#endif
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
							// TODO Uno: GetFullScreenPageBottomAppBarHeight is Win32-only and stubbed to 0 here.
							double pageBottomAppBarScrollOffset = 0.0;

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

#if false
							// TODO Uno: Phase 4 DM wiring — animated MakeVisible flows through
							// ScrollViewer.ChangeViewInternal which is not yet ported. For now
							// fall through to the non-animated SetHorizontalOffsetPrivate /
							// SetVerticalOffsetPrivate path.
							// No need to call ChangeView during a manipulation if the requested view coincides with the final view.
							if (!spScrollViewer.IsInManipulation() ||
								!DoubleUtil.AreClose(horizontalOffset, targetHorizontalOffset) ||
								!DoubleUtil.AreClose(verticalOffset, targetVerticalOffset))
							{
								spScrollViewer.ChangeViewInternal(
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
									true /*isForMakeVisible*/,
									out handled);

								if (handled)
								{
									// Make sure the resulting minX/minY offsets are within bounds so the final viewport is correctly evaluated below.
									double scrollableDim = spScrollViewer.ScrollableWidth;
									minX = (float)Math.Min(targetHorizontalOffset, scrollableDim);

									scrollableDim = spScrollViewer.ScrollableHeight;
									minY = (float)Math.Min(targetVerticalOffset, scrollableDim);
								}
							}
#else
							// Phase-4 stub: synchronous scroll. ChangeViewInternal will be wired
							// in Phase 4 once the DM adapter and ChangeView pipeline are ported.
							if (horizontalOffset != minX)
							{
								SetHorizontalOffsetPrivate((double)minX, out var isScrollRequested, out var currentOffset, out var requestedOffset);
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
								SetVerticalOffsetPrivate((double)minY, out var isScrollRequested, out var currentOffset, out var requestedOffset);
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
#endif
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

			// Suppress unused-variable warnings for stub-only locals.
			_ = isVisualDirectChild;
			_ = isVisualInContent;
			_ = zoomFactor;
			_ = targetZoomFactor;

			// Return the rectangle
			return rectangle;
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
			var spTemplatedParent = TemplatedParent;
			var spScrollContainer = spTemplatedParent as ScrollViewer;

			// If our content is not an IScrollInfo, we should have selected a style
			// that contains one.
			if (spScrollContainer is not null)
			{
				// 1. Try our content...
				// TODO Uno: Phase 4/5 wiring — once IScrollInfo is implemented on SCP and IScrollOwner
				// on SV (and the ItemsPresenter→inner-IScrollInfo lookup is wired up via
				// IOrientedVirtualizingPanel), call:
				//   spScrollContainer.PutScrollInfo(spScrollInfo);
				//   PutScrollOwner(spScrollContainer);
				// For now, the cross-platform `ScrollOwner` setter handles wiring.
				ScrollOwner = spScrollContainer;
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
			// TODO: Add back when we have RichTextBox
			//    ctl::ComPtr<RichTextBox> spRichTextBoxParent;
			TextWrapping wrapping = TextWrapping.NoWrap;
			ScrollBarVisibility visibility = ScrollBarVisibility.Disabled;
			Thickness scrollViewerPadding = default;
			double availableWidth = 0.0;
			double availableHeight = 0.0;
			global::Windows.Foundation.Rect clipRect = default;

			var spTemplatedParent = TemplatedParent;
			var spScrollViewer = spTemplatedParent as ScrollViewer;
			var pScrollData = GetScrollData();
			extentWidth = pScrollData.m_extent.Width;
			viewportWidth = pScrollData.m_viewport.Width;
			offset = pScrollData.GetOffsetX();

			var spTemplatedGrandParent = spScrollViewer?.TemplatedParent;
			var spTextBoxParent = spTemplatedGrandParent as TextBox;
			// TODO: Add back when we have RichTextBox
			//    spRichTextBoxParent = spTemplatedGrandParent.AsOrNull<xaml_controls::IRichTextBox>();

			// Detemine the TextWrapping and HorizontalScrollBarVisiblity properties.
			if (spTextBoxParent is not null)
			{
				wrapping = spTextBoxParent.TextWrapping;
				// TODO: Add back when TextBox has a HorizontalScrollBarVisibility property
				//        IFC(spTextBoxParent->get_HorizontalScrollBarVisibility(&visibility));
			}
			// TODO: Add back when we have RichTextBox
			//    else if (spRichTextBoxParent)
			//    {
			//        IFC(spRichTextBoxParent->get_TextWrapping(&wrapping));
			//        IFC(spRichTextBoxParent->get_HorizontalScrollBarVisibility(&visibility));
			//    }

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

				// TODO: Add back when we have ITextBoxView/IRichTextBoxView
				//    IFC(get_TemplatedParent(&pTemplatedParent));
				//    IFC(get_Content(&pContent));
				//
				//    if (ctl::is<IScrollViewer>(ctl::as_iinspectable(pTemplatedParent)) &&
				//        (ctl::is<ITextBoxView>(pContentAsII) || ctl::is<IRichTextBoxView>(pContentAsII)))
				//    {
				//        // We may need to allow glyphs to overhang into the ScrollViewers padding
				//        IFC(CalculateTextBoxClipRect(availableSize, &clip));
				//        clipRect = clip;
				//    }
				//    else
				//    {
				clipRect.X = clipRect.Y = 0;
				clipRect.Width = availableSize.Width;
				clipRect.Height = availableSize.Height;
				//    }
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
				// TODO Uno: layout-cycle warning context recording (StoreLayoutCycleWarningContext) is not ported.
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

		// Updates the zoom factor.
		// (C++ source line 3338)
		internal void SetZoomFactor(float newZoomFactor)
		{
			m_fZoomFactor = newZoomFactor;

			if (IsScrollClient())
			{
				InvalidateMeasure();
			}
			else
			{
				// TODO Uno: Phase 5 — when an inner IManipulationDataProvider is the IScrollInfo, push
				// SetZoomFactor onto it. Until that contract lands, this branch is a no-op.
				// var spProvider = GetScrollOwner_Mux() as IManipulationDataProvider;
				// spProvider?.SetZoomFactor(m_fZoomFactor);
			}
		}

		// Called by the owning ScrollViewer when the Content property is changing.
		// (C++ source line 3493)
		internal void OnContentChanging(object pOldContent)
		{
			if (pOldContent is UIElement spOldChild)
			{
				// TODO Uno: ResetGlobalScaleFactor exists on the C++ UIElement; in Uno the equivalent
				// scale-factor reset is implicit. No-op for now.
				// spOldChild.ResetGlobalScaleFactor();
			}
		}

		// Called when the parent of this ScrollContentPresenter changed.
		// (C++ source line 3506)
		internal void OnTreeParentUpdatedCore(object pNewParent, bool isParentAlive)
		{
			if (pNewParent is null)
			{
				// TODO Uno: Phase 6 — once UnparentHeaders is ported, call it here. Headers are not
				// yet wired up so leaving the call site commented out for now.
				// UnparentHeaders();
				m_trTopLeftHeader = null;
				m_trTopHeader = null;
				m_trLeftHeader = null;
			}
		}

		// Called when a ScrollContentPresenter dependency property changed.
		// (C++ source line 3536)
		internal void OnPropertyChanged2Core(DependencyProperty changedProperty)
		{
			// TODO Uno: Phase 4 — once SCP exposes CanContentRenderOutsideBoundsProperty on Skia
			// (it's currently NotImplemented in Generated), wire this up. For now, no-op.
			// if (changedProperty == CanContentRenderOutsideBoundsProperty)
			// {
			//     InvalidateArrange();
			// }
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
		// (C++ source line 1612 — simplified Skia port that omits headers, IManipulationDataProvider,
		//  CalendarPanel, and SemanticZoom branches. Phase 6 will reintroduce them.)
		// TODO Uno: Phase 4 — switch the cross-platform Skia MeasureOverride (ScrollContentPresenter.cs:163,
		// in the UNO_HAS_MANAGED_SCROLL_PRESENTER || __WASM__ block) to delegate to this method via
		// `protected override Size MeasureOverride(Size s) => MeasureOverridePort(s);` once the dependent
		// pieces of the existing Managed.cs scroll path (PointerWheelScroll, ValidateInputOffset 3-arg
		// signature) are migrated. For now this method is dormant on Skia.
		internal global::Windows.Foundation.Size MeasureOverridePort(global::Windows.Foundation.Size availableSize)
		{
			// TODO Uno: Phase 6 — header support (TopLeftHeader/TopHeader/LeftHeader). For now skip.

			// Use the cross-platform `Content` field as the primary child.
			var spChild = Content as UIElement;

			// If there is no child but scroll data exists, it should be updated with an extent of 0.
			var pScrollData = GetScrollData();

			if (!IsScrollClient())
			{
				// Custom IScrollInfo implementations are not supported here. Defer to base measure.
				if (spChild is not null)
				{
					return base.MeasureOverride(availableSize);
				}
				return default;
			}

			var spScrollOwner = pScrollData.GetScrollOwner();
			var spScrollViewer = spScrollOwner as ScrollViewer;

			var childAvailableSize = availableSize;

			// TODO Uno: Phase 6 — SizesContentToTemplatedParent honour the SV's GetLatestAvailableSize().
			// For now use the passed availableSize directly.

			if (pScrollData.m_canHorizontallyScroll)
			{
				// TODO Uno: Phase 5 — honour child's WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility.
				if (!m_isSemanticZoomPresenter)
				{
					childAvailableSize.Width = double.PositiveInfinity;
				}
			}

			if (pScrollData.m_canVerticallyScroll)
			{
				// TODO Uno: Phase 5 — honour child's WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility.
				if (!m_isSemanticZoomPresenter)
				{
					childAvailableSize.Height = double.PositiveInfinity;
				}
			}

			float zoomFactor = 1.0f;
			if (spScrollOwner is not null)
			{
				zoomFactor = spScrollOwner.GetZoomFactor();
				global::System.Diagnostics.Debug.Assert(zoomFactor == m_fZoomFactor);
			}

			global::Windows.Foundation.Size desiredSize = default;
			global::Windows.Foundation.Size desiredSizeZoomed = default;

			if (spChild is not null)
			{
				spChild.Measure(childAvailableSize);
				desiredSize = spChild.DesiredSize;

				// TODO Uno: Phase 6 — CalendarPanel desired-viewport-size adjustment (toBeAdjustedDesiredSize).
			}

			if (pScrollData is not null)
			{
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
					global::Windows.Foundation.Size layoutSize = default;
					if (spScrollViewer is not null)
					{
						layoutSize = spScrollViewer.GetLayoutSize();
					}

					if (layoutSize.Width != 0.0f && layoutSize.Height != 0.0f)
					{
						// SemanticZoom case — TODO Uno Phase 5.
						desiredSizeZoomed.Width = (float)((layoutSize.Width) * zoomFactor);
						desiredSizeZoomed.Height = (float)((layoutSize.Height) * zoomFactor);
					}
					else
					{
						desiredSizeZoomed.Width = (float)(desiredSize.Width * zoomFactor);
						desiredSizeZoomed.Height = (float)(desiredSize.Height * zoomFactor);
					}

					// TODO Uno: Phase 6 — m_isChildActualWidth/HeightUsedAsExtent special mode handling.

					if (!m_isChildActualWidthUsedAsExtent && !m_isChildActualHeightUsedAsExtent)
					{
						// If we're handling scrolling (as the physical scrolling client, validate properties).
						VerifyScrollData(pScrollData.m_viewport /*viewport*/, desiredSizeZoomed /*extent*/);
					}
				}
			}

			if (layoutSizeNonZero(spScrollViewer, out var ls))
			{
				// SemanticZoom's ScrollViewer case. Use the enforced layoutSize rather than the child's desiredSize.
				desiredSize.Width = Math.Min(availableSize.Width, ls.Width);
				desiredSize.Height = Math.Min(availableSize.Height, ls.Height);
			}
			else
			{
				desiredSize.Width = Math.Min(availableSize.Width, desiredSize.Width);
				desiredSize.Height = Math.Min(availableSize.Height, desiredSize.Height);
			}

			m_isChildActualWidthUpdated = true;
			m_isChildActualHeightUpdated = true;

			// Let ScrollViewer know that child sizes might have changed.
			// TODO Uno: Phase 5 — ScrollViewer.OnScrollContentPresenterMeasured already exists in Anchoring partial.
			// spScrollViewer?.OnScrollContentPresenterMeasured();

			return desiredSize;

			static bool layoutSizeNonZero(ScrollViewer sv, out global::Windows.Foundation.Size size)
			{
				if (sv is not null)
				{
					size = sv.GetLayoutSize();
					return size.Width != 0.0f && size.Height != 0.0f;
				}
				size = default;
				return false;
			}
		}

		// Provides the behavior for the Arrange pass of layout. Classes can
		// override this method to define their own Arrange pass behavior.
		// (C++ source line 2094 — simplified Skia port that omits headers, IManipulationDataProvider,
		//  m_isChildActualWidth/HeightUsedAsExtent special mode, and the layout-cycle warning context.)
		// TODO Uno: Phase 4 — same as MeasureOverridePort: switch the cross-platform Skia ArrangeOverride
		// (ScrollContentPresenter.cs:231) to delegate here once the existing Managed.cs scroll path is
		// migrated. For now this method is dormant on Skia.
		internal global::Windows.Foundation.Size ArrangeOverridePort(global::Windows.Foundation.Size finalSize)
		{
			// Loop while the inner arrange marks an additional scroll request.
			do
			{
				// NOTE: We are updating the clip only if there is a scroll owner that hosts
				// this control. This is a limited fix for 22803.
				// TODO Uno: Phase 4 — port UpdateClip(finalSize). For now defer to base.

				// TODO Uno: Phase 6 — header arrangement (TopLeftHeader/TopHeader/LeftHeader).

				var spChild = Content as UIElement;

				// Verifies IScrollInfo properties & invalidates ScrollViewer if necessary.
				m_scrollRequested = false;

				var pScrollData = GetScrollData();
				var spScrollOwner = pScrollData?.GetScrollOwner();
				var spScrollViewer = spScrollOwner as ScrollViewer;
				var isScrollClient = IsScrollClient();

				if (isScrollClient && pScrollData is not null)
				{
					var extentSize = pScrollData.m_extent;
					// TODO Uno: Phase 6 — m_isChildActualWidth/HeightUsedAsExtent special mode exit.
					VerifyScrollData(finalSize /*viewport*/, extentSize /*extent*/);
				}

				if (spChild is not null && isScrollClient)
				{
					// TODO Uno: Phase 4 — DM completion + pre-DM-offset bookkeeping. For now use ComputedOffset directly.
					// var currentZoomFactor = spScrollOwner?.GetZoomFactor() ?? 1.0f;

					var desiredSize = spChild.DesiredSize;

					var childRect = new global::Windows.Foundation.Rect(
						0,
						0,
						Math.Max(desiredSize.Width, finalSize.Width),
						Math.Max(desiredSize.Height, finalSize.Height));

					spChild.Arrange(childRect);

					// TODO Uno: Phase 6 — actual-size-as-extent mode entry/exit (StartUseOfActualWidth/HeightAsExtent +
					// CanUseActualWidth/HeightAsExtent + LayoutRound + AreWithinTolerance comparison).
				}
			}
			while (m_scrollRequested);

			return finalSize;
		}

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
