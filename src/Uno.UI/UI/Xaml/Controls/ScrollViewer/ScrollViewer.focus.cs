#nullable enable
using System;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollViewer
{
	private (bool shouldScroll, bool shouldMoveFocus) HandleKeyDownForXYNavigation(KeyRoutedEventArgs args)
	{
		bool shouldScroll = false;
		bool shouldMoveFocus = false;
		var originalKey = args.OriginalKey;
		bool isPageNavigation = FocusHelper.IsGamepadPageNavigationDirection(originalKey);
		double scrollAmountProportion = isPageNavigation ? 1.0 : 0.5;
		bool shouldProcessKeyEvent = true;
		FocusNavigationDirection navigationDirection;

		if (Presenter is null || XamlRoot is null)
		{
			return (shouldScroll, shouldMoveFocus);
		}

		if (isPageNavigation)
		{
			navigationDirection = FocusHelper.GetPageNavigationDirection(originalKey);
		}
		else
		{
			navigationDirection = FocusHelper.GetNavigationDirection(originalKey);
		}

		if (shouldProcessKeyEvent)
		{
			DependencyObject? nextElement = null;

			if (navigationDirection != FocusNavigationDirection.None)
			{
				nextElement = GetNextFocusCandidate(navigationDirection, isPageNavigation);
			}

			if (nextElement is not null && nextElement != FocusManager.GetFocusedElement(XamlRoot))
			{
				UIElement nextElementAsUIE = FocusHelper.GetUIElementForFocusCandidate(nextElement);
				MUX_ASSERT(nextElementAsUIE != null);

				var nextElementAsFe = nextElementAsUIE as FrameworkElement;
				if (nextElementAsFe is null || nextElementAsUIE is null)
				{
					shouldScroll = true;
					shouldMoveFocus = false;
					return (shouldScroll, shouldMoveFocus);
				}

				var rect = new Rect(0, 0, nextElementAsFe.ActualWidth, nextElementAsFe.ActualHeight);
				var elementBounds = nextElementAsUIE.TransformToVisual(Presenter).TransformBounds(rect);
				var viewport = new Rect(0, 0, Presenter.ActualWidth, Presenter.ActualHeight);

				// Extend the viewport in the direction we are moving:
				Rect extendedViewport = viewport;
				switch (navigationDirection)
				{
					case FocusNavigationDirection.Down:
						extendedViewport.Height += viewport.Height;
						break;
					case FocusNavigationDirection.Up:
						extendedViewport.Y -= viewport.Height;
						extendedViewport.Height += viewport.Height;
						break;
					case FocusNavigationDirection.Left:
						extendedViewport.X -= viewport.Width;
						extendedViewport.Width += viewport.Width;
						break;
					case FocusNavigationDirection.Right:
						extendedViewport.Width += viewport.Width;
						break;
				}

				bool isElementInExtendedViewport = RectHelper.Intersect(elementBounds, extendedViewport) != RectHelper.Empty;
				bool isElementFullyInExtendedViewport = RectHelper.Union(elementBounds, extendedViewport) == extendedViewport;

				if (isElementInExtendedViewport)
				{
					if (isPageNavigation)
					{
						// Always scroll for page navigation
						shouldScroll = true;

						if (isElementFullyInExtendedViewport)
						{
							// Move focus:
							shouldMoveFocus = true;
						}
					}
					else
					{
						// Non-paging scroll allows partial candidates
						shouldMoveFocus = true;
					}
				}
				else
				{
					// Element is outside extended viewport - scroll but don't focus.
					shouldScroll = true;
				}
			}
			else
			{
				// No focus candidate: scroll
				shouldScroll = true;
			}
		}

		return (shouldScroll, shouldMoveFocus);
	}

	// MUX Reference ScrollViewer_Partial.cpp, ProcessGamepadNavigation, tag winui3/release/2.5.1, commit ba3a8d59e
	// TODO Uno: only its page navigation. Item navigation stays with HandleKeyDownForXYNavigation's callers.
	private bool ProcessGamepadPageNavigation(KeyRoutedEventArgs args)
	{
		var isHandled = false;
		var direction = FocusHelper.GetPageNavigationDirection(args.OriginalKey);

		// only attempt to handle the page navigation key if we can scroll in the direction provided
		if (direction == FocusNavigationDirection.None || !CanScrollForFocusNavigation(args.Key, direction))
		{
			return false;
		}

		var (shouldScroll, shouldFocusCandidate) = HandleKeyDownForXYNavigation(args);

		// We need to scroll
		if (shouldScroll)
		{
			isHandled = ScrollForFocusNavigation(direction, viewportScrollPercent: 1.0);
		}

		if (shouldFocusCandidate
			&& XamlRoot is { } xamlRoot
			&& GetNextFocusCandidate(direction, isPageNavigation: true) is { } candidate)
		{
			// candidate overlaps the ScrollViewer viewport, set focus on the candidate.
			var oldFocusedElement = FocusManager.GetFocusedElement(xamlRoot);

			FocusHelper.GetUIElementForFocusCandidate(candidate)?.Focus(FocusState.Keyboard);

			//Make sure that the candidate for focus was not the item which already had focus before marking the navigation as handled.
			isHandled = isHandled || !ReferenceEquals(candidate, oldFocusedElement);
		}

		return isHandled;
	}

	// MUX Reference ScrollViewer_Partial.cpp, CanScrollForFocusNavigation and ShouldContinueRoutingKeyDownEvent, tag winui3/release/2.5.1, commit ba3a8d59e
	private bool CanScrollForFocusNavigation(VirtualKey key, FocusNavigationDirection direction)
	{
		double offset = 0.0;
		double edge = 0.0;
		bool chainingEnabled = false;
		var scrollMode = ScrollMode.Disabled;

		switch (key)
		{
			case VirtualKey.GamepadLeftTrigger:
				offset = VerticalOffset;
				chainingEnabled = IsVerticalScrollChainingEnabled;
				scrollMode = VerticalScrollMode;
				break;

			case VirtualKey.GamepadRightTrigger:
				offset = VerticalOffset;
				edge = ScrollableHeight;
				chainingEnabled = IsVerticalScrollChainingEnabled;
				scrollMode = VerticalScrollMode;
				break;

			case VirtualKey.GamepadLeftShoulder:
				offset = HorizontalOffset;
				edge = FlowDirection == FlowDirection.RightToLeft ? ScrollableWidth : 0.0;
				chainingEnabled = IsHorizontalScrollChainingEnabled;
				scrollMode = HorizontalScrollMode;
				break;

			case VirtualKey.GamepadRightShoulder:
				offset = HorizontalOffset;
				edge = FlowDirection == FlowDirection.LeftToRight ? ScrollableWidth : 0.0;
				chainingEnabled = IsHorizontalScrollChainingEnabled;
				scrollMode = HorizontalScrollMode;
				break;
		}

		// Methods get_xxxOffset() do not return fractional parts and get_ScrollableXxx() do,
		// define 'near' as within radius of 1 unit.
		var continueRouting = scrollMode != ScrollMode.Disabled
			&& chainingEnabled
			&& Math.Abs(edge - offset) <= 1.0;

		if (continueRouting)
		{
			return false;
		}

		// if scroll mode is disabled, we cannot scroll
		return direction switch
		{
			FocusNavigationDirection.Up or FocusNavigationDirection.Down => VerticalScrollMode != ScrollMode.Disabled,
			FocusNavigationDirection.Left or FocusNavigationDirection.Right => HorizontalScrollMode != ScrollMode.Disabled,
			_ => false,
		};
	}

	// MUX Reference ScrollViewer_Partial.cpp, ScrollForFocusNavigation, tag winui3/release/2.5.1, commit ba3a8d59e
	private bool ScrollForFocusNavigation(FocusNavigationDirection direction, double viewportScrollPercent)
	{
		var verticalOffset = VerticalOffset;
		var horizontalOffset = HorizontalOffset;

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

		return direction switch
		{
			FocusNavigationDirection.Up => ChangeView(null, verticalOffset - ViewportHeight * viewportScrollPercent, null, disableAnimation: false),
			FocusNavigationDirection.Down => ChangeView(null, verticalOffset + ViewportHeight * viewportScrollPercent, null, disableAnimation: false),
			FocusNavigationDirection.Left => ChangeView(horizontalOffset - ViewportWidth * viewportScrollPercent, null, null, disableAnimation: false),
			FocusNavigationDirection.Right => ChangeView(horizontalOffset + ViewportWidth * viewportScrollPercent, null, null, disableAnimation: false),
			_ => false,
		};
	}

	private DependencyObject? GetNextFocusCandidate(FocusNavigationDirection navigationDirection, bool isPageNavigation)
	{
		MUX_ASSERT(navigationDirection != FocusNavigationDirection.None);
		var scrollPresenter = Presenter;

		if (scrollPresenter is null)
		{
			return null;
		}

		FocusNavigationDirection focusDirection = navigationDirection;

		FindNextElementOptions findNextElementOptions = new FindNextElementOptions();
		findNextElementOptions.SearchRoot = scrollPresenter.Content as DependencyObject;

		if (isPageNavigation)
		{
			var localBounds = new Rect(0, 0, scrollPresenter.ActualWidth, scrollPresenter.ActualHeight);
			var globalBounds = scrollPresenter.TransformToVisual(null).TransformBounds(localBounds);
			int numPagesLookAhead = 2;

			var hintRect = globalBounds;
			switch (navigationDirection)
			{
				case FocusNavigationDirection.Down:
					hintRect.Y += globalBounds.Height * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Up:
					hintRect.Y -= globalBounds.Height * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Left:
					hintRect.X -= globalBounds.Width * numPagesLookAhead;
					break;
				case FocusNavigationDirection.Right:
					hintRect.X += globalBounds.Width * numPagesLookAhead;
					break;
				default:
					MUX_ASSERT(false);
					break;
			}

			findNextElementOptions.HintRect = hintRect;
			findNextElementOptions.ExclusionRect = hintRect;
			focusDirection = FocusHelper.GetOppositeDirection(navigationDirection);
		}

		return FocusManager.FindNextElement(focusDirection, findNextElementOptions);
	}
}
