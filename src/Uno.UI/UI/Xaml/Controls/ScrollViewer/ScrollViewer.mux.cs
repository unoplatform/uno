// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewer_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
#if __SKIA__
#pragma warning disable IDE0051 // Private member is unused (placeholder for full impl)

		// #region Foundational ScrollViewer.cpp methods (Phase 3)

		// Note: IsAnimationEnabled() is provided by the cross-platform ScrollViewer.cs partial.

		// Initializes a new instance of the ScrollViewer class.
		// Note: this is the field-init logic from the C++ constructor. The actual public C# constructor
		// lives on the cross-platform partial (ScrollViewer.cs) and calls InitializePartial(), which we
		// hook into via InitializePartial below.
		partial void InitializePartial()
		{
			m_inMeasure = false;
			m_inChildInvalidateMeasure = false;
			m_ignoreSemanticZoomNavigationInput = false;
			m_handleScrollInfoWheelEvent = true;
			m_scrollVisibilityX = Visibility.Visible;
			m_scrollVisibilityY = Visibility.Visible;
			m_xOffset = 0.0;
			m_yOffset = 0.0;
			m_xMinOffset = 0.0;
			m_yMinOffset = 0.0;
			m_xPixelOffset = 0.0;
			m_yPixelOffset = 0.0;
			m_unboundHorizontalOffset = 0.0;
			m_unboundVerticalOffset = 0.0;
			m_xViewport = 0.0;
			m_yViewport = 0.0;
			m_xPixelViewport = 0.0;
			m_yPixelViewport = 0.0;
			m_xExtent = 0.0;
			m_yExtent = 0.0;
			m_xPixelExtent = 0.0;
			m_yPixelExtent = 0.0;
			m_contentWidthRequested = -1;
			m_contentHeightRequested = -1;
			m_xPixelOffsetRequested = -1;
			m_yPixelOffsetRequested = -1;
			m_cLevelsFromRootCallForZoom = 0;
			m_initialMaxZoomFactor = 0.0f;
			m_initialZoomFactor = 0.0f;
			m_requestedMaxZoomFactor = 0.0f;
			m_requestedZoomFactor = 0.0f;
			m_preDirectManipulationOffsetX = 0.0f;
			m_preDirectManipulationOffsetY = 0.0f;
			m_preDirectManipulationZoomFactor = 0.0f;
			m_preDirectManipulationNonVirtualizedTranslationCorrection = 0.0f;
			m_currentHorizontalScrollMode = ScrollMode.Disabled;
			m_currentVerticalScrollMode = ScrollMode.Disabled;
			m_currentZoomMode = ZoomMode.Disabled;
			m_currentHorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			m_currentVerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			m_currentHorizontalAlignment = DMAlignment.None;
			m_currentVerticalAlignment = DMAlignment.None;
			m_currentIsHorizontalRailEnabled = false;
			m_currentIsVerticalRailEnabled = false;
			m_currentIsScrollInertiaEnabled = false;
			m_currentIsZoomInertiaEnabled = false;
			m_pDMStateChangeHandler = null;
			m_hManipulationHandler = null;
			m_canManipulateElementsByTouch = false;
			m_canManipulateElementsNonTouch = false;
			m_canManipulateElementsWithBringIntoViewport = false;
			m_canManipulateElementsWithAsyncBringIntoViewport = false;
			m_touchConfiguration = DMConfigurations.None;
			m_nonTouchConfiguration = DMConfigurations.None;
			m_activeHorizontalAlignment = DMAlignment.None;
			m_activeVerticalAlignment = DMAlignment.None;
			m_overridingMinZoomFactor = 0.0f;
			m_overridingMaxZoomFactor = 0.0f;
			m_isInDirectManipulation = false;
			m_isDirectManipulationStopped = false;
			m_isInDirectManipulationCompletion = false;
			m_isInDirectManipulationZoom = false;
			m_isInDirectManipulationSync = false;
			m_isInChangeViewBringIntoViewport = false;
			m_isInZoomFactorSync = false;
			m_isManipulationHandlerInterestedInNotifications = false;
			m_isDirectManipulationZoomFactorChange = false;
			m_isDirectManipulationZoomFactorChangeIgnored = false;
			m_isOffsetChangeIgnored = false;
			m_areViewportConfigurationsInvalid = false;
			m_isCanManipulateElementsInvalid = false;
			m_isScrollBarIgnoringUserInputInvalid = false;
			m_shouldFocusOnRightTapUnhandled = false;
			m_showingMouseIndicators = false;
			m_preferMouseIndicators = false;
			m_isTargetHorizontalOffsetValid = false;
			m_isTargetVerticalOffsetValid = false;
			m_isTargetZoomFactorValid = false;
			m_targetHorizontalOffset = 0.0;
			m_targetVerticalOffset = 0.0;
			m_targetZoomFactor = 1.0f;
			m_targetChangeViewHorizontalOffset = -1.0;
			m_targetChangeViewVerticalOffset = -1.0;
			m_targetChangeViewZoomFactor = -1.0f;
			m_inertiaEndHorizontalOffset = 0.0f;
			m_inertiaEndVerticalOffset = 0.0f;
			m_inertiaEndZoomFactor = 1.0f;
			m_isInertiaEndTransformValid = false;
			m_isInertial = false;
			m_isViewChangingDelayed = false;
			m_isViewChangedDelayed = false;
			m_isDelayedViewChangedIntermediate = false;
			m_isInIntermediateViewChangedMode = false;
			m_isViewChangedRaisedInIntermediateMode = false;
			m_iViewChangingDelay = 0;
			m_iViewChangedDelay = 0;
			m_inSemanticZoomAnimation = false;
			m_keepIndicatorsShowing = false;
			m_isPointerOverVerticalScrollbar = false;
			m_isPointerOverHorizontalScrollbar = false;
			m_isLoaded = false;
			m_blockIndicators = false;
			m_isDraggingThumb = false;
			m_horizontalOffsetCached = -1.0;
			m_verticalOffsetCached = -1.0;
			m_isTopLeftHeaderAssociated = false;
			m_isTopHeaderAssociated = false;
			m_isLeftHeaderAssociated = false;
			m_isNearVerticalAlignmentForced = false;
			m_isHorizontalStretchAlignmentTreatedAsNear = false;
			m_isVerticalStretchAlignmentTreatedAsNear = false;
			m_cForcePanXConfiguration = 0;
			m_cForcePanYConfiguration = 0;
			m_layoutSize = new global::Windows.Foundation.Size(0, 0);

			m_horizontalOverpanMode = DMOverpanMode.Default;
			m_verticalOverpanMode = DMOverpanMode.Default;
		}

#if false
		// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
		// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.

		// Original destructor logic (not executed):
		// ~ScrollViewer()
		// {
		//     // Releasing and unhooking of template parts and events
		//     IGNOREHR(UnhookTemplate());
		//
		//     m_trManipulatableElement.Clear();
		//
		//     if (m_spZoomSnapPoints)
		//     {
		//         IGNOREHR(m_spZoomSnapPoints->remove_VectorChanged(m_ZoomSnapPointsVectorChangedToken));
		//         ZeroMemory(&m_ZoomSnapPointsVectorChangedToken, sizeof(m_ZoomSnapPointsVectorChangedToken));
		//     }
		//
		//     if (m_coreInputViewOcclusionsChangedToken.value != 0)
		//     {
		//         ... // CoreInputView occlusion handler removal — N/A on Uno managed targets.
		//     }
		//
		//     IGNOREHR(UnhookScrollSnapPointsInfoEvents(TRUE /*isForHorizontalSnapPoints*/));
		//     IGNOREHR(UnhookScrollSnapPointsInfoEvents(FALSE /*isForHorizontalSnapPoints*/));
		//
		//     if (auto dxamlCore = DXamlCore::GetCurrent())
		//     {
		//         dxamlCore->UnregisterFromDynamicScrollbarsSettingChanged(this);
		//     }
		// }
#endif

		// Scrolls the view in the specified direction.
		internal void ScrollInDirection(
			VirtualKey key,
			bool animate = false)
		{
			if (animate)
			{
				// Let DManip animate the scroll within a ListViewBase header or footer.

				// No special processing is required here for right-to-left scenarios,
				// CInputServices::ProcessInputMessageWithDirectManipulation does it instead.
				// For PageUp/Down, Home and End keys though, that method must ignore the RightToLeft
				// flow direction, otherwise a move to the opposite direction is performed.
				// TODO Uno: ProcessInputMessage forwards the keystroke to the DM service. Until the
				// DM adapter exists, fall through to non-animated scrolling.
				// ProcessInputMessage(key == VirtualKey.PageUp || key == VirtualKey.PageDown ||
				//                     key == VirtualKey.Home  || key == VirtualKey.End,
				//                     out var isHandled);
			}

			{
				var direction = FlowDirection;
				var invert = direction == FlowDirection.RightToLeft;

				switch (key)
				{
					case VirtualKey.Up:
						LineUp();
						break;
					case VirtualKey.Down:
						LineDown();
						break;
					case VirtualKey.Left:
						if (invert)
						{
							LineRight();
						}
						else
						{
							LineLeft();
						}
						break;
					case VirtualKey.Right:
						if (invert)
						{
							LineLeft();
						}
						else
						{
							LineRight();
						}
						break;
					case VirtualKey.PageUp:
						PageUp();
						break;
					case VirtualKey.PageDown:
						PageDown();
						break;
					case VirtualKey.Home:
						PageHome();
						break;
					case VirtualKey.End:
						PageEnd();
						break;
					default:
						// Do nothing
						break;
				}
			}
		}

		// Public and deprecated version of ScrollToHorizontalOffsetInternal.
		public void ScrollToHorizontalOffset(double offset) => ScrollToHorizontalOffsetInternal(offset);

		// Scrolls the content within the ScrollViewer to the specified
		// horizontal offset position.
		internal void ScrollToHorizontalOffsetInternal(double offset)
			=> HandleHorizontalScroll(ScrollEventType.ThumbPosition, offset);

		// Public and deprecated version of ScrollToVerticalOffsetInternal.
		public void ScrollToVerticalOffset(double offset) => ScrollToVerticalOffsetInternal(offset);

		// Scrolls the content within the ScrollViewer to the specified vertical
		// offset position.
		internal void ScrollToVerticalOffsetInternal(double offset)
			=> HandleVerticalScroll(ScrollEventType.ThumbPosition, offset);

		// Scroll content by one page to the left.
		internal void PageLeft() => HandleHorizontalScroll(ScrollEventType.LargeDecrement);

		// Scroll content by one line to the left.
		internal void LineLeft() => HandleHorizontalScroll(ScrollEventType.SmallDecrement);

		// Scroll content by one line to the right.
		internal void LineRight() => HandleHorizontalScroll(ScrollEventType.SmallIncrement);

		// Scroll content by one page to the right.
		internal void PageRight() => HandleHorizontalScroll(ScrollEventType.LargeIncrement);

		// Scroll content by one page to the top.
		internal void PageUp() => HandleVerticalScroll(ScrollEventType.LargeDecrement);

		// Scroll content by one line to the top.
		internal void LineUp() => HandleVerticalScroll(ScrollEventType.SmallDecrement);

		// Scroll content by one line to the bottom.
		internal void LineDown() => HandleVerticalScroll(ScrollEventType.SmallIncrement);

		// Scroll content by one page to the bottom.
		internal void PageDown() => HandleVerticalScroll(ScrollEventType.LargeIncrement);

		// Scroll content to the beginning.
		internal void PageHome() => HandleHorizontalScroll(ScrollEventType.First);

		// Scroll content to the end.
		internal void PageEnd() => HandleHorizontalScroll(ScrollEventType.Last);

		// Handles the vertical ScrollBar.Scroll event and updates the UI.
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0.0)
		{
			// If style changes and Content cannot be found - just exit.
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			var oldOffset = spScrollInfo.VerticalOffset;
			var newOffset = oldOffset;

			switch (scrollEventType)
			{
				case ScrollEventType.EndScroll:
					LeaveIntermediateViewChangedMode(raiseFinalViewChanged: true);
					break;
				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
					if (scrollEventType == ScrollEventType.ThumbTrack)
					{
						EnterIntermediateViewChangedMode();
					}
					newOffset = offset;
					break;
				case ScrollEventType.LargeDecrement:
					spScrollInfo.PageUp();
					break;
				case ScrollEventType.LargeIncrement:
					spScrollInfo.PageDown();
					break;
				case ScrollEventType.SmallDecrement:
					spScrollInfo.LineUp();
					break;
				case ScrollEventType.SmallIncrement:
					spScrollInfo.LineDown();
					break;
				case ScrollEventType.First:
					newOffset = double.MinValue;
					break;
				case ScrollEventType.Last:
					newOffset = double.MaxValue;
					break;
			}

			var isCarouselPanel = IsPanelACarouselPanel(isForHorizontalOrientation: false);
			// Do not clamp offset for carousel-ing panel.
			if (!isCarouselPanel)
			{
				newOffset = Math.Max(newOffset, 0.0);
				// Clamp the new offset at this stage to prevent unnecessary layout.
				var scrollable = ScrollableHeight;
				newOffset = Math.Min(scrollable, newOffset);
			}

			// If newOffset does not match oldOffset, apply it
			if (!DoubleUtil.AreClose(oldOffset, newOffset))
			{
				// potentially block updating the scrollinfo while thumb dragging is occurring
				var isDeferring = false;
				if (m_isDraggingThumb)
				{
					isDeferring = IsDeferredScrollingEnabled;
					if (isDeferring)
					{
						m_verticalOffsetCached = newOffset;
					}
				}

				if (!isDeferring)
				{
					spScrollInfo.SetVerticalOffset(newOffset);
				}
			}
		}

		// Handles the horizontal ScrollBar.Scroll event and updates the UI.
		internal void HandleHorizontalScroll(ScrollEventType scrollEventType, double offset = 0.0)
		{
			// If style changes and Content cannot be found - just exit.
			var spScrollInfo = GetScrollInfo();
			if (spScrollInfo is null)
			{
				return;
			}

			var oldOffset = spScrollInfo.HorizontalOffset;
			var newOffset = oldOffset;

			switch (scrollEventType)
			{
				case ScrollEventType.EndScroll:
					LeaveIntermediateViewChangedMode(raiseFinalViewChanged: true);
					break;
				case ScrollEventType.ThumbPosition:
				case ScrollEventType.ThumbTrack:
					if (scrollEventType == ScrollEventType.ThumbTrack)
					{
						EnterIntermediateViewChangedMode();
					}
					newOffset = offset;
					break;
				case ScrollEventType.LargeDecrement:
					spScrollInfo.PageLeft();
					break;
				case ScrollEventType.LargeIncrement:
					spScrollInfo.PageRight();
					break;
				case ScrollEventType.SmallDecrement:
					spScrollInfo.LineLeft();
					break;
				case ScrollEventType.SmallIncrement:
					spScrollInfo.LineRight();
					break;
				case ScrollEventType.First:
					newOffset = double.MinValue;
					break;
				case ScrollEventType.Last:
					newOffset = double.MaxValue;
					break;
			}

			newOffset = Math.Max(newOffset, 0.0);

			// Clamp the new offset at this stage to prevent unnecessary layout.
			var scrollable = ScrollableWidth;
			newOffset = Math.Min(scrollable, newOffset);

			// If newOffset does not match oldOffset, apply it
			if (!DoubleUtil.AreClose(oldOffset, newOffset))
			{
				// potentially block updating the scrollinfo while thumb dragging is occurring
				var isDeferring = false;
				if (m_isDraggingThumb)
				{
					isDeferring = IsDeferredScrollingEnabled;
					if (isDeferring)
					{
						m_horizontalOffsetCached = newOffset;
					}
				}

				if (!isDeferring)
				{
					spScrollInfo.SetHorizontalOffset(newOffset);
				}
			}
		}

		// #region Stubs awaiting later phases

		// TODO Uno: Phase 3 — port `get_ScrollInfo` body. For now return SCP cast as IScrollInfo via shim.
		private IScrollInfo GetScrollInfo()
		{
			// TODO Uno: WinUI's get_ScrollInfo returns the m_wrScrollInfo weak reference target, which
			// the SCP sets to itself in HookupScrollingComponents. Until that's wired up, fall back to
			// the existing _presenter field exposed by the cross-platform partial.
			return null;
		}

		// TODO Uno: Phase 5 — port `IsPanelACarouselPanel`. CarouselPanel is a virtualizing panel used
		// by ComboBox; until it's hooked up here, return false (which is the safe default for
		// non-carousel scenarios — the offset gets clamped, which is correct for normal Scroll panels).
		private bool IsPanelACarouselPanel(bool isForHorizontalOrientation) => false;

		// EnterIntermediateViewChangedMode / LeaveIntermediateViewChangedMode are defined in
		// ScrollViewer.ViewChange.mux.cs and visible to all calls within this partial class.

		// #endregion

		// #endregion

#pragma warning restore IDE0051
#endif
	}
}
