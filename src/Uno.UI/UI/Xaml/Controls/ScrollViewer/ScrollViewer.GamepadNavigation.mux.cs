// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ScrollViewer_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollViewer
{
	// Determines if key press should be forwarded or processed by DM.
	// The determination is made based on whether scrolling and chaining are
	// enabled and viewport is near an edge within the current item.
	private bool ShouldContinueRoutingKeyDownEvent(VirtualKey key)
	{
		double offset = 0.0;
		double edge = 0.0;
		bool chainingEnabled = false;
		ScrollMode scrollMode = ScrollMode.Disabled;
		FlowDirection direction = FlowDirection;

		switch (key)
		{
			case VirtualKey.GamepadLeftTrigger:
			case VirtualKey.Up:
				offset = VerticalOffset;
				chainingEnabled = IsVerticalScrollChainingEnabled;
				scrollMode = VerticalScrollMode;
				break;

			case VirtualKey.GamepadRightTrigger:
			case VirtualKey.Down:
				offset = VerticalOffset;
				edge = ScrollableHeight;
				chainingEnabled = IsVerticalScrollChainingEnabled;
				scrollMode = VerticalScrollMode;
				break;

			case VirtualKey.GamepadLeftShoulder:
			case VirtualKey.Left:
				offset = HorizontalOffset;
				if (direction == FlowDirection.RightToLeft)
				{
					edge = ScrollableWidth;
				}
				chainingEnabled = IsHorizontalScrollChainingEnabled;
				scrollMode = HorizontalScrollMode;
				break;

			case VirtualKey.GamepadRightShoulder:
			case VirtualKey.Right:
				offset = HorizontalOffset;
				if (direction == FlowDirection.LeftToRight)
				{
					edge = ScrollableWidth;
				}
				chainingEnabled = IsHorizontalScrollChainingEnabled;
				scrollMode = HorizontalScrollMode;
				break;
		}

		// Methods get_xxxOffset() do not return fractional parts and get_ScrollableXxx() do,
		// define 'near' as within radius of 1 unit.
		return scrollMode != ScrollMode.Disabled &&
			chainingEnabled &&
			Math.Abs(edge - offset) <= 1.0;
	}

	private bool CanScrollForFocusNavigation(VirtualKey key, FocusNavigationDirection direction)
	{
		// check if we can scroll
		// Check if current key should trigger chaining
		var canScroll = !ShouldContinueRoutingKeyDownEvent(key);
		if (canScroll)
		{
			var scrollMode = ScrollMode.Disabled;

			switch (direction)
			{
				case FocusNavigationDirection.Up:
				case FocusNavigationDirection.Down:
					scrollMode = VerticalScrollMode;
					break;

				case FocusNavigationDirection.Left:
				case FocusNavigationDirection.Right:
					scrollMode = HorizontalScrollMode;
					break;
			}

			// if scroll mode is disabled, we cannot scroll
			canScroll = scrollMode != ScrollMode.Disabled;
		}

		return canScroll;
	}

	// TODO Uno: Global bounds are in logical pixels, so the conversions by RootScale of the source are not needed.
	private Rect GetGlobalBounds() =>
		TransformToVisual(null).TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));

	private Rect GetScrollViewerGlobalBoundsWithoutHeaders()
	{
		var scrollViewerGlobalBounds = GetGlobalBounds();

		// account for top and left header //
		// Determine how large the SV's viewport is compared to the SV's entire size.
		var widthRatio = ActualWidth > 0 ? Math.Min(1.0, ViewportWidth / ActualWidth) : 1.0;
		var heightRatio = ActualHeight > 0 ? Math.Min(1.0, ViewportHeight / ActualHeight) : 1.0;

		// Set the real width and height on the SV's viewport.
		var physicalViewportWidth = scrollViewerGlobalBounds.Width * widthRatio;
		var physicalViewportHeight = scrollViewerGlobalBounds.Height * heightRatio;

		// Switch left/right for flow direction
		var left = FlowDirection == FlowDirection.RightToLeft
			? scrollViewerGlobalBounds.Left
			: scrollViewerGlobalBounds.Right - physicalViewportWidth;
		var top = scrollViewerGlobalBounds.Bottom - physicalViewportHeight;

		return new Rect(left, top, physicalViewportWidth, physicalViewportHeight);
	}

	// TODO Uno: There is no input pane or TV safe area adjustment, so the whole viewport is unobstructed.
	private Rect GetUnobstructedScrollViewerGlobalBounds() => GetScrollViewerGlobalBoundsWithoutHeaders();

	internal object? GetGamepadNavigationCandidate(
		FocusNavigationDirection direction,
		bool isPageNavigation,
		int numPagesLookAhead,
		float verticalViewportPadding)
	{
		object? spScrollContentPresenterAsI = _presenter;
		object? spCandidate;

		// get the next candidate from auto focus scoping candidates to just descendants of the ScrollViewer
		if (isPageNavigation)
		{
			var oppositeDirection = FocusNavigationDirection.None;
			var unobstructedViewportGlobalBounds = GetUnobstructedScrollViewerGlobalBounds();
			var viewportWidth = unobstructedViewportGlobalBounds.Width;
			var viewportHeight = unobstructedViewportGlobalBounds.Height - verticalViewportPadding;

			var hintRect = unobstructedViewportGlobalBounds;
			switch (direction)
			{
				case FocusNavigationDirection.Down:
					hintRect.Y += viewportHeight * numPagesLookAhead;
					oppositeDirection = FocusNavigationDirection.Up;
					break;
				case FocusNavigationDirection.Up:
					hintRect.Y -= viewportHeight * numPagesLookAhead;
					oppositeDirection = FocusNavigationDirection.Down;
					break;
				case FocusNavigationDirection.Left:
					hintRect.X -= viewportWidth * numPagesLookAhead;
					oppositeDirection = FocusNavigationDirection.Right;
					break;
				case FocusNavigationDirection.Right:
					hintRect.X += viewportWidth * numPagesLookAhead;
					oppositeDirection = FocusNavigationDirection.Left;
					break;
			}

			// let the exclusion rect be the same as the hint rect
			// that way, we look for an item that is completely inside the region in the direction specified
			// in other words, we will not get items that are intersecting with the hint rect
			var exclusionRect = hintRect;

			spCandidate = FocusManager.FindNextFocusWithSearchRootIgnoreEngagementWithHintRectImpl(
				oppositeDirection /* focusNavigationDirection */,
				spScrollContentPresenterAsI!, /* searchStart */
				hintRect,
				exclusionRect);
		}
		else
		{
			// item navigation - use the currently focused element as the hint (default)
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			spCandidate = focusManager?.FindNextFocusWithSearchRootIgnoreEngagementImpl(direction, spScrollContentPresenterAsI!);
		}

		return spCandidate;
	}

	internal bool ScrollForFocusNavigation(
		VirtualKey key,
		FocusNavigationDirection direction,
		double viewportScrollPercent)
	{
		var isHandled = false;

		if (CanScrollForFocusNavigation(key, direction))
		{
			double? spHorizontalOffset = null;
			double? spVerticalOffset = null;
			double horizontalOffset = 0;
			double verticalOffset = 0;
			var zoomFactor = -1.0f;

			var unobstructedScrollViewerGlobalBounds = GetUnobstructedScrollViewerGlobalBounds();
			var viewportWidth = unobstructedScrollViewerGlobalBounds.Width;
			var viewportHeight = unobstructedScrollViewerGlobalBounds.Height;

			if (IsInManipulation)
			{
				// When there is an ongoing inertia, do not use the current HorizontalOffset & VerticalOffset
				// dependency property values as the starting point, but rather the end-of-inertia transform.
				GetTargetView(out horizontalOffset, out verticalOffset, out zoomFactor);
			}

			if (zoomFactor == -1.0f)
			{
				// No end-of-inertia transform was retrieved. Use the current dependency property values.
				horizontalOffset = HorizontalOffset;
				verticalOffset = VerticalOffset;
			}

			// Switch left/right for flow direction
			if (FlowDirection == FlowDirection.RightToLeft)
			{
				if (direction == FocusNavigationDirection.Left)
				{
					direction = FocusNavigationDirection.Right;
				}
				else if (direction == FocusNavigationDirection.Right)
				{
					direction = FocusNavigationDirection.Left;
				}
			}

			switch (direction)
			{
				case FocusNavigationDirection.Up:
					spVerticalOffset = verticalOffset - viewportHeight * viewportScrollPercent;
					break;

				case FocusNavigationDirection.Down:
					spVerticalOffset = verticalOffset + viewportHeight * viewportScrollPercent;
					break;

				case FocusNavigationDirection.Left:
					spHorizontalOffset = horizontalOffset - viewportWidth * viewportScrollPercent;
					break;

				case FocusNavigationDirection.Right:
					spHorizontalOffset = horizontalOffset + viewportWidth * viewportScrollPercent;
					break;
			}

			isHandled = ChangeViewWithOptionalAnimation(
				spHorizontalOffset,
				spVerticalOffset,
				null /* pZoomFactor */,
				false /* disableAnimation */);
		}

		return isHandled;
	}

	internal bool ProcessGamepadNavigation(VirtualKey key, VirtualKey originalKey)
	{
		var isHandled = false;
		FocusNavigationDirection direction;
		var isPageNavigation = FocusHelper.IsGamepadPageNavigationDirection(originalKey);
		const double viewportLookaheadPercent = 1.0; // 100%  extend by one full viewport
		double viewportScrollPercent;
		var shouldFocusCandidate = false;
		var shouldProcessNavigationKey = true;

		if (isPageNavigation)
		{
			// page navigation
			direction = FocusHelper.GetPageNavigationDirection(originalKey);
			viewportScrollPercent = 1.0; // scroll by one viewport

			// only attempt to handle the page navigation key if we can scroll in the direction provided
			shouldProcessNavigationKey = CanScrollForFocusNavigation(key, direction);
		}
		else
		{
			// item navigation
			direction = FocusHelper.GetNavigationDirection(originalKey);
			viewportScrollPercent = 0.5; // scroll by half a viewport
		}

		if (direction != FocusNavigationDirection.None &&
			shouldProcessNavigationKey)
		{
			// ask auto focus for next item, scoping for items only under the scrollViewer
			double currentDManipHorizontalOffset = 0;
			double currentDManipVerticalOffset = 0;
			double horizontalOffset = 0;
			double verticalOffset = 0;
			var zoomFactor = -1.0f;
			var shouldScroll = false;

			var spCandidate = GetGamepadNavigationCandidate(
				direction,
				isPageNavigation,
				2 /* numPagesLookAhead */,
				0.0f /* verticalViewportPadding */);

			if (IsInManipulation)
			{
				// When there is an ongoing inertia, do not use the current HorizontalOffset & VerticalOffset
				// dependency property values as the starting point, but rather the end-of-inertia transform.
				// Adding 15% of the viewport to the current target results in a smooth curve adjustment and
				// prevents landing on random offsets.
				GetTargetView(out horizontalOffset, out verticalOffset, out zoomFactor);
			}

			if (zoomFactor == -1.0f)
			{
				// No end-of-inertia transform was retrieved. Use the current dependency property values.
				horizontalOffset = HorizontalOffset;
				verticalOffset = VerticalOffset;
				zoomFactor = ZoomFactor;
				currentDManipHorizontalOffset = horizontalOffset;
				currentDManipVerticalOffset = verticalOffset;
			}
			else if (spCandidate is not null)
			{
				// An end-of-inertia transform was retrieved. Access the current DManip transform in order to compute an accurate
				// position of the candidate.
				currentDManipHorizontalOffset = HorizontalOffset;
				currentDManipVerticalOffset = VerticalOffset;
			}

			if (spCandidate is not null)
			{
				// auto focus gave us a candidate.
				// check if the candidate is within the viewport
				var spCandidateAsUI = FocusHelper.GetUIElementForFocusCandidate((DependencyObject)spCandidate);

				var offsetRelativeToScrollViewer = spCandidateAsUI.TransformToVisual(this).TransformPoint(default);

				var spCandidateAsFE = (FrameworkElement)spCandidateAsUI;
				var candidateWidth = spCandidateAsFE.ActualWidth * zoomFactor;
				var candidateHeight = spCandidateAsFE.ActualHeight * zoomFactor;

				var viewportWidth = ViewportWidth;
				var viewportHeight = ViewportHeight;
				var unobstructedScrollViewerGlobalBounds = GetUnobstructedScrollViewerGlobalBounds();
				var unobstructedViewportWidth = unobstructedScrollViewerGlobalBounds.Width;
				var unobstructedViewportHeight = unobstructedScrollViewerGlobalBounds.Height;

				// candidate rectangle top left and bottom right points
				// These points are in relation to the top/left corner of the scrollable content,
				// whether the viewport is in inertia or not.
				var candidateTopLeft = new Point(
					offsetRelativeToScrollViewer.X + currentDManipHorizontalOffset,
					offsetRelativeToScrollViewer.Y + currentDManipVerticalOffset);
				var candidateBottomRight = new Point(
					candidateTopLeft.X + candidateWidth,
					candidateTopLeft.Y + candidateHeight);

				var scrollViewerBounds = GetGlobalBounds();

				var unobstructedViewportOffsetFromScrollViewer = new Point(
					unobstructedScrollViewerGlobalBounds.X - scrollViewerBounds.Left,
					unobstructedScrollViewerGlobalBounds.Y - scrollViewerBounds.Top);

				// viewport top left and bottom right points
				// These are the current viewport points, or the end-of-inertia points if the viewport is in inertia.
				// offset of tvsafe viewport in ScrollViewer = ScrollViewer global bounds - tv safe viewport global bounds
				var viewportTopLeft = new Point(horizontalOffset, verticalOffset);

				var viewportBottomRight = new Point(
					viewportTopLeft.X + viewportWidth,
					viewportTopLeft.Y + viewportHeight);

				// extend viewport by 100% in target direction and check if the candidate falls
				// within the next page.
				switch (direction)
				{
					case FocusNavigationDirection.Down:
						viewportTopLeft.Y += unobstructedViewportOffsetFromScrollViewer.Y;
						viewportBottomRight.Y = viewportTopLeft.Y + unobstructedViewportHeight;
						viewportBottomRight.Y += unobstructedViewportHeight * viewportLookaheadPercent;
						break;
					case FocusNavigationDirection.Up:
						viewportTopLeft.Y += unobstructedViewportOffsetFromScrollViewer.Y;
						viewportTopLeft.Y -= unobstructedViewportHeight * viewportLookaheadPercent;
						break;
					case FocusNavigationDirection.Right:
						viewportTopLeft.X += unobstructedViewportOffsetFromScrollViewer.X;
						viewportBottomRight.X = viewportTopLeft.X + unobstructedViewportWidth;
						viewportBottomRight.X += viewportTopLeft.X + unobstructedViewportWidth * viewportLookaheadPercent;
						break;
					case FocusNavigationDirection.Left:
						viewportTopLeft.X += unobstructedViewportOffsetFromScrollViewer.X;
						viewportTopLeft.X -= unobstructedViewportWidth * viewportLookaheadPercent;
						break;
				}

				// does the viewport + extention in the target direction overlap the chosen candidate
				var candidateOverlapsViewport = !(candidateTopLeft.X > viewportBottomRight.X ||
					viewportTopLeft.X > candidateBottomRight.X ||
					candidateTopLeft.Y > viewportBottomRight.Y ||
					viewportTopLeft.Y > candidateBottomRight.Y);

				if (candidateOverlapsViewport)
				{
					if (isPageNavigation)
					{
						var candidateInsideViewport = candidateTopLeft.X >= viewportTopLeft.X &&
							candidateTopLeft.Y >= viewportTopLeft.Y &&
							candidateBottomRight.X <= viewportBottomRight.X &&
							candidateBottomRight.Y <= viewportBottomRight.Y;
						bool candidateFitsViewportInDirection;

						if (direction is FocusNavigationDirection.Down or FocusNavigationDirection.Up)
						{
							candidateFitsViewportInDirection = candidateHeight <= unobstructedViewportHeight;
						}
						else
						{
							candidateFitsViewportInDirection = candidateWidth <= unobstructedViewportWidth;
						}

						shouldFocusCandidate = candidateInsideViewport || !candidateFitsViewportInDirection;
						shouldScroll = true;
					}
					else
					{
						// Allow focusing partially visible candidates for non-paging directional navigation.
						shouldFocusCandidate = true;
					}
				}
				else
				{
					// candidate does not overlap the viewport, scroll if possible
					shouldScroll = true;
				}
			}
			else
			{
				// No candidate was found. Scroll if possible
				shouldScroll = true;
			}

			// We need to scroll
			if (shouldScroll)
			{
				isHandled = ScrollForFocusNavigation(key, direction, viewportScrollPercent);
			}

			if (shouldFocusCandidate)
			{
				// candidate overlaps the ScrollViewer viewport, set focus on the candidate.
				var spOldFocusedElement = VisualTree.GetFocusManagerForElement(this)?.FocusedElement;

				this.SetFocusedElementWithDirection(
					(DependencyObject)spCandidate!,
					FocusState.Keyboard,
					!isPageNavigation /* animateIfBringIntoView */,
					direction,
					true /* forceBringIntoView */);

				//Make sure that the candidate for focus was not the item which already had focus before marking the navigation as handled.
				isHandled = isHandled || !ReferenceEquals(spCandidate, spOldFocusedElement);
			}
		}

		return isHandled;
	}

	internal void StopInertialManipulation()
	{
		global::System.Diagnostics.Debug.Assert(IsInManipulation);

		// TODO Uno: The presenter drives inertia rather than DirectManipulation.
		(_presenter as ScrollContentPresenter)?.StopInertialManipulation();
	}

	// TODO Uno: The presenter has no separate end-of-inertia transform, its target offsets stand in for it.
	private void GetTargetView(out double horizontalOffset, out double verticalOffset, out float zoomFactor)
	{
		horizontalOffset = Presenter?.TargetHorizontalOffset ?? HorizontalOffset;
		verticalOffset = Presenter?.TargetVerticalOffset ?? VerticalOffset;
		zoomFactor = ZoomFactor;
	}
}
