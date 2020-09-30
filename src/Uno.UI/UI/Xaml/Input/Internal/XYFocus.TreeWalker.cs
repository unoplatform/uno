// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// TreeWalker.h, TreeWalker.cpp

#nullable enable

using System.Collections.Generic;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal static class XYFocusTreeWalker
	{
		internal const int InitialCandidateListCapacity = 50;

		internal static List<XYFocus.XYFocusParameters> FindElements(
			DependencyObject? startRoot,
			DependencyObject? currentElement,
			DependencyObject? activeScroller,
			bool ignoreClipping,
			bool shouldConsiderXYFocusKeyboardNavigation)
		{

			bool isScrolling = (activeScroller != null);
			List<XYFocus.XYFocusParameters> focusList = new List<XYFocus.XYFocusParameters>(InitialCandidateListCapacity);
			var collection = FocusProperties.GetFocusChildren(startRoot);

			if (collection == null) // || collection.IsLeaving())
			{
				return focusList;
			}

			var kidCount = collection.Length;

			//Iterate though every node in the tree that is focusable
			for (uint i = 0; i < kidCount; i++)
			{
				DependencyObject child = collection[i];

				if (child == null) { continue; }

				bool isEngagementEnabledButNotEngaged = FocusProperties.IsFocusEngagementEnabled(child) && !FocusProperties.IsFocusEngaged(child);

				//This is an element that can be focused
				if (child != currentElement && XYFocusFocusability.IsValidCandidate(child))
				{
					var parameters = new XYFocus.XYFocusParameters();

					if (isScrolling)
					{
						var scrollCandidate = child;
						var bounds = GetBoundsForRanking(scrollCandidate, ignoreClipping);

						//Include all elements participating in scrolling or
						//elements that are currently not occluded (in view) or
						//elements that are currently occluded but part of a parent scrolling surface.
						if (IsCandidateParticipatingInScroll(scrollCandidate, activeScroller) ||
							!IsOccluded(scrollCandidate, bounds) ||
							IsCandidateChildOfAncestorScroller(scrollCandidate, activeScroller))
						{
							parameters.Element = scrollCandidate;
							parameters.Bounds = bounds;

							focusList.Add(parameters);
						}
					}
					else
					{
						var bounds = GetBoundsForRanking(child, ignoreClipping);

						parameters.Element = child;
						parameters.Bounds = bounds;

						focusList.Add(parameters);
					}
				}

				if (IsValidFocusSubtree(child, shouldConsiderXYFocusKeyboardNavigation) && !isEngagementEnabledButNotEngaged)
				{
					var subFocusList = FindElements(child, currentElement, activeScroller, ignoreClipping, shouldConsiderXYFocusKeyboardNavigation);
					focusList.AddRange(subFocusList);
				}
			}

			return focusList;
		}

		//Evaluate if the Sub-tree under the current element potentially can contain focusable items
		//Note: This isn't the same as IsValidCandidate because a sub-tree under a valid element might not be focusable.
		private static bool IsValidFocusSubtree(DependencyObject element, bool shouldConsiderXYFocusKeyboardNavigation)
		{
			bool isDirectionalRegion =
				shouldConsiderXYFocusKeyboardNavigation &&
				element is UIElement &&
				IsDirectionalRegion(element);

			return FocusProperties.IsVisible(element) &&
				FocusProperties.IsEnabled(element) &&
				!FocusProperties.ShouldSkipFocusSubTree(element) &&
				(!shouldConsiderXYFocusKeyboardNavigation || isDirectionalRegion);

		}

		private static bool IsCandidateParticipatingInScroll(
			DependencyObject candidate,
			DependencyObject? activeScroller)
		{
			if (activeScroller == null)
			{
				return false;
			}

			DependencyObject? parent = candidate;
			while (parent != null)
			{
				var element = parent as UIElement;
				if (element != null && element.IsScroller())
				{
					return (parent == activeScroller);
				}
				parent = parent.GetParent() as DependencyObject;
			}
			return false;
		}

		//Walks up the tree from the active scrolling surface to find another scrolling surface that can potentially contain a candidate
		//We do this to consider elements that are currently scrolled out of view (wrt the parent scrolling surface), hence occluded.
		private static bool IsCandidateChildOfAncestorScroller(
			DependencyObject candidate,
			DependencyObject? activeScroller)
		{
			if (activeScroller == null)
			{
				return false;
			}

			DependencyObject? parent = activeScroller.GetParent() as DependencyObject;
			while (parent != null)
			{
				var element = parent as UIElement;
				if (element != null && element.IsScroller())
				{
					if (parent.IsAncestorOf(candidate))
					{
						return true;
					}
					//We want to continue walking up the tree to look for more scrolling surfaces
					//who could scroll our candidate into view
				}
				parent = parent.GetParent() as DependencyObject;
			}
			return false;
		}

		private static bool IsDirectionalRegion(DependencyObject element)
		{
			if (!(element is UIElement uiElement))
			{
				return false;
			}

			var mode = (XYFocusKeyboardNavigationMode)uiElement.GetValue(UIElement.XYFocusKeyboardNavigationProperty);
			return mode != XYFocusKeyboardNavigationMode.Disabled;
		}

		internal static Rect GetBoundsForRanking(
			DependencyObject? element,
			bool ignoreClipping)
		{
			if (element is Hyperlink hyperlink)
			{
				element = hyperlink.GetContainingFrameworkElement();
			}

			var bounds = (element as UIElement)?.GetGlobalBoundsLogical(ignoreClipping) ?? Rect.Infinite;
			return bounds;
		}

		internal static bool IsOccluded(DependencyObject? element, Rect elementBounds)
		{
			if (element is Hyperlink hyperlink)
			{
				element = hyperlink.GetContainingFrameworkElement();
			}

			var root = VisualTree.GetRootOrIslandForElement(element);

			try
			{
				var isOccluded = root?.IsOccluded(element as UIElement, elementBounds) ?? true;
				return isOccluded;
			}
			catch
			{
				return true; //Ignore element if it fails Occlusivity Testing
			}
		}
	}
}
