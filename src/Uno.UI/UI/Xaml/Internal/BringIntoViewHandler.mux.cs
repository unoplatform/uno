// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference BringIntoViewHandler.cpp (dxaml/xcp/core/input), tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

// TODO Uno: The input pane, sticky app bar, popup and caret handling of the source are not ported.
// Global bounds are in logical pixels, so its conversions to physical pixels are not needed either,
// except for the innermost scroller sizes, see GetInnermostScrollableScrollerWidthAndHeight.
internal static class BringIntoViewHandler
{
	private const float ExtraPixelsForBringIntoView = 20;

	// Returns the first scroller that's parent of the specified element.
	private static ScrollContentPresenter? GetInnerScroller(UIElement element)
	{
		var parent = VisualTreeHelper.GetParent(element);

		while (parent is not null)
		{
			if (parent is ScrollContentPresenter scroller)
			{
				return scroller;
			}

			parent = VisualTreeHelper.GetParent(parent);
		}

		return null;
	}

	private static void GetInnermostScrollableScrollerWidthAndHeight(
		ScrollContentPresenter innerScroller,
		out double innermostHorizontallyScrollableScrollerWidth,
		out double innermostVerticallyScrollableScrollerHeight)
	{
		DependencyObject? current = innerScroller;
		var horizontalFound = false;
		var verticalFound = false;
		innermostHorizontallyScrollableScrollerWidth = 0.0;
		innermostVerticallyScrollableScrollerHeight = 0.0;

		while (current is not null && !(horizontalFound && verticalFound))
		{
			if (current is ScrollContentPresenter { ScrollOwner: ScrollViewer scrollViewer } presenter)
			{
				// Is current Scrollable - in horizontal/vertical
				scrollViewer.IsContentScrollable(
					ignoreScrollMode: false,
					ignoreScrollBarVisibility: true,
					out var horizontalContentScrollable,
					out var verticalContentScrollable);

				if (horizontalContentScrollable || verticalContentScrollable)
				{
					var rectCurrentScrollerGlobal = GetScrollerGlobalBounds(presenter);

					// WinUI leaves these sizes in DIPs but compares them with the focused rect in physical pixels,
					// so above 100% scale the padding gets dropped earlier. Uno works in DIPs, so divide to match.
					var scale = RootScale.GetRasterizationScaleForElement(presenter);
					rectCurrentScrollerGlobal.Width /= scale;
					rectCurrentScrollerGlobal.Height /= scale;

					if (!horizontalFound && horizontalContentScrollable)
					{
						horizontalFound = true;
						innermostHorizontallyScrollableScrollerWidth = rectCurrentScrollerGlobal.Width;
					}

					if (!verticalFound && verticalContentScrollable)
					{
						verticalFound = true;
						innermostVerticallyScrollableScrollerHeight = rectCurrentScrollerGlobal.Height;
					}
				}
			}

			current = VisualTreeHelper.GetParent(current);
		}
	}

	// Ensure the focused element is visible
	internal static void EnsureFocusedElementBringIntoView(UIElement focusedUIElement, bool animateIfBringIntoView)
	{
		BringIntoViewOptions options = new()
		{
			AnimationDesired = animateIfBringIntoView,
		};

		// TODO Uno: Without an inner scroller the source only handles a root ScrollViewer, which Uno does not have.
		if (GetInnerScroller(focusedUIElement) is { } innerScroller)
		{
			options.TargetRect = BringFocusedElementIntoView(focusedUIElement, innerScroller);
		}

		// WinUI routes BringIntoViewRequested asynchronously, so a scroll issued along with the focus change (see
		// Selector.SetFocusedItem) cannot override it.
		_ = focusedUIElement.DispatcherQueue.TryEnqueue(() => focusedUIElement.StartBringIntoView(options));
	}

	// The rectangle to bring into view, local to the focused element.
	private static Rect BringFocusedElementIntoView(UIElement focusedElement, ScrollContentPresenter innerScroller)
	{
		var globalTransform = focusedElement.TransformToVisual(null);
		var localBounds = new Rect(0, 0, focusedElement.ActualSize.X, focusedElement.ActualSize.Y);

		// Init the focused element's rect in global coordinates
		var rectFocusedElement = globalTransform.TransformBounds(localBounds);

		// Get the Focus Visual bounds (the bounds by which it extends outside the element bounds)
		var focusVisualMargin = (focusedElement as FrameworkElement)?.FocusVisualMargin ?? default;

		GetInnermostScrollableScrollerWidthAndHeight(
			innerScroller,
			out var innermostHorizontalScrollableScrollerWidth,
			out var innermostVerticalScrollableScrollerHeight);

		var rectBeforePadding = rectFocusedElement;

		GetFocusedElementBoundingRectForPadding(
			innermostHorizontalScrollableScrollerWidth,
			innermostVerticalScrollableScrollerHeight,
			focusVisualMargin,
			ref rectFocusedElement);

		// Transform the padding from global to local, because BringIntoView takes local coordinates.
		// TODO Uno: The source inverts the whole rect, which here accumulates float error in the resulting offset.
		var scaleX = localBounds.Width > 0 ? rectBeforePadding.Width / localBounds.Width : 1.0;
		var scaleY = localBounds.Height > 0 ? rectBeforePadding.Height / localBounds.Height : 1.0;

		return new Rect(
			(rectFocusedElement.X - rectBeforePadding.X) / scaleX,
			(rectFocusedElement.Y - rectBeforePadding.Y) / scaleY,
			localBounds.Width + (rectFocusedElement.Width - rectBeforePadding.Width) / scaleX,
			localBounds.Height + (rectFocusedElement.Height - rectBeforePadding.Height) / scaleY);
	}

	// Potentially expands the bounding rectangle for the focused element to land away from the scroller edges.
	private static void GetFocusedElementBoundingRectForPadding(
		double innermostHorizontallyScrollableScrollerWidth,
		double innermostVerticallyScrollableScrollerHeight,
		Thickness focusVisualMargin,
		ref Rect focusedElementRect)
	{
		// If focused element width or height is 0, do not add padding.
		// Adding padding to zero height/width is not going to help anyway.
		// It fixes a regression in Edge where a DcompVisualElement has 0 global bounds
		if (focusedElementRect.Width > 0 && focusedElementRect.Height > 0)
		{
			// adjust for focus visual margin if larger than 20 logical pixels, if not just use 20 logical pixels.
			// Negative values show by how much the focus visual goes outside the element
			var left = Math.Max(
				Math.Min(focusVisualMargin.Left < 0 ? -focusVisualMargin.Left : 0, innermostHorizontallyScrollableScrollerWidth - focusedElementRect.Width),
				ExtraPixelsForBringIntoView);

			var top = Math.Max(
				Math.Min(focusVisualMargin.Top < 0 ? -focusVisualMargin.Top : 0, innermostVerticallyScrollableScrollerHeight - focusedElementRect.Height),
				ExtraPixelsForBringIntoView);

			var right = Math.Max(
				Math.Min(focusVisualMargin.Right < 0 ? -focusVisualMargin.Right : 0, innermostHorizontallyScrollableScrollerWidth - focusedElementRect.Width),
				ExtraPixelsForBringIntoView);

			var bottom = Math.Max(
				Math.Min(focusVisualMargin.Bottom < 0 ? -focusVisualMargin.Bottom : 0, innermostVerticallyScrollableScrollerHeight - focusedElementRect.Height),
				ExtraPixelsForBringIntoView);

			if (focusedElementRect.Width + left < innermostHorizontallyScrollableScrollerWidth)
			{
				focusedElementRect.X -= left;
				focusedElementRect.Width += left;
			}

			if (focusedElementRect.Height + top < innermostVerticallyScrollableScrollerHeight)
			{
				focusedElementRect.Y -= top;
				focusedElementRect.Height += top;
			}

			if (focusedElementRect.Width + right < innermostHorizontallyScrollableScrollerWidth)
			{
				focusedElementRect.Width += right;
			}

			if (focusedElementRect.Height + bottom < innermostVerticallyScrollableScrollerHeight)
			{
				focusedElementRect.Height += bottom;
			}
		}
	}

	private static Rect GetScrollerGlobalBounds(ScrollContentPresenter scroller) =>
		scroller.TransformToVisual(null).TransformBounds(new Rect(0, 0, scroller.ActualWidth, scroller.ActualHeight));
}
