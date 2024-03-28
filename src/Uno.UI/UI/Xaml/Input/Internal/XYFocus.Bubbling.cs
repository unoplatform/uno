// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Bubbling.h, Bubbling.cpp

#nullable enable

using Uno.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal static class XYFocusBubbling
	{
		public static DependencyObject? GetDirectionOverride(
			DependencyObject element,
			DependencyObject? searchRoot,
			FocusNavigationDirection direction,
			bool ignoreFocusabililty = false)
		{
			DependencyObject? overrideElement = null;

			DependencyProperty? property = GetXYFocusPropertyIndex(element, direction);

			if (property != null)
			{
				overrideElement = element.GetValue(property) as DependencyObject;

				if (overrideElement != null && (!ignoreFocusabililty && !XYFocusFocusability.IsValidCandidate(overrideElement)))
				{
					return null;
				}

				// If an override was specified, but it is located outside the searchRoot, don't use it as the candidate.
				if (searchRoot != null &&
					overrideElement != null &&
					searchRoot.IsAncestorOf(overrideElement) == false) { return null; }
			}

			return overrideElement;
		}

		internal static DependencyObject? TryXYFocusBubble(
			DependencyObject? element,
			DependencyObject? candidate,
			DependencyObject? searchRoot,
			FocusNavigationDirection direction)
		{
			if (candidate == null)
			{
				return null;
			}

			DependencyObject nextFocusableElement = candidate;
			var directionOverrideRoot = GetDirectionOverrideRoot(element, searchRoot, direction);

			if (directionOverrideRoot != null)
			{
				bool isAncestor = directionOverrideRoot.IsAncestorOf(candidate);
				var rootOverride = GetDirectionOverride(directionOverrideRoot, searchRoot, direction);

				if (rootOverride != null && !isAncestor)
				{
					nextFocusableElement = rootOverride;
				}
			}

			return nextFocusableElement;
		}

		private static DependencyObject? GetDirectionOverrideRoot(
			DependencyObject? element,
			DependencyObject? searchRoot,
			FocusNavigationDirection direction)
		{
			var root = element;

			while (root != null && GetDirectionOverride(root, searchRoot, direction) == null)
			{
				root = root.GetParent() as DependencyObject;
			}

			return root;
		}

		internal static XYFocusNavigationStrategy GetStrategy(
			DependencyObject inputElement,
			FocusNavigationDirection direction,
			XYFocusNavigationStrategyOverride navigationStrategyOverride)
		{
			DependencyObject? element = inputElement;

			bool isAutoOverride = (navigationStrategyOverride == XYFocusNavigationStrategyOverride.Auto);
			bool isNavigationStrategySpecified = (navigationStrategyOverride != XYFocusNavigationStrategyOverride.None) && !isAutoOverride;

			if (isNavigationStrategySpecified)
			{
				//We can cast just by offsetting values because we have ensured that the XYFocusStrategy enums offset as expected
				return (XYFocusNavigationStrategy)((int)navigationStrategyOverride - 1);
			}
			else if (isAutoOverride)
			{
				//Skip the element if we have an var override and look at its parent's strategy
				element = element.GetParent() as DependencyObject;
			}

			if (!(element is UIElement))
			{
				return XYFocusNavigationStrategy.Projection;
			}

			UIElement? uiElement = (UIElement)element;

			var property = GetXYFocusNavigationStrategyPropertyIndex(element, direction);

			// Even though this is an inherited property, we still want to walk up the tree ourselves to check verify that a strategy was not set. We use
			// GetValueInternal instead of GetValueByIndex to ensure we do no go through the inherited code path (this code path will walk up the tree).

			while (uiElement != null && uiElement.GetValue(property) is XYFocusNavigationStrategy mode)
			{
				if (mode != XYFocusNavigationStrategy.Auto)
				{
					return mode;
				}

				uiElement = uiElement.GetParent() as UIElement;
			}

			// If we fall though here, return the default strategy mode
			return XYFocusNavigationStrategy.Projection;
		}

		private static DependencyProperty? GetXYFocusPropertyIndex(
			DependencyObject element,
			FocusNavigationDirection direction)
		{
			DependencyProperty? property = null;

			if (element.IsRightToLeft())
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

			if (direction == FocusNavigationDirection.Left)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusLeftProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusLeftPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Right)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusRightProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusRightPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Up)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusUpProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusUpPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Down)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusDownProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusDownPropertyIndex();
				}
			}

			return property;
		}

		private static DependencyProperty? GetXYFocusNavigationStrategyPropertyIndex(
			DependencyObject element,
			FocusNavigationDirection direction)
		{
			DependencyProperty? property = null;

			if (element.IsRightToLeft())
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

			if (direction == FocusNavigationDirection.Left)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusLeftNavigationStrategyProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusLeftNavigationStrategyPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Right)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusRightNavigationStrategyProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusRightNavigationStrategyPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Up)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusUpNavigationStrategyProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusUpNavigationStrategyPropertyIndex();
				}
			}
			else if (direction == FocusNavigationDirection.Down)
			{
				if (element is UIElement)
				{
					property = UIElement.XYFocusDownNavigationStrategyProperty;
				}
				else if (FocusableHelper.GetIFocusableForDO(element) is IFocusable focusable)
				{
					property = focusable.GetXYFocusDownNavigationStrategyPropertyIndex();
				}
			}

			return property;
		}

	}
}
