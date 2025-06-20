#nullable enable

using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
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

	private DependencyObject? GetNextFocusCandidate(FocusNavigationDirection navigationDirection, bool isPageNavigation)
	{
		MUX_ASSERT(Presenter is not null);
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
