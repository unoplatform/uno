// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusProperties.h, FocusProperties.cpp

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Input
{
	internal static class FocusProperties
	{
		internal static DependencyObject[] GetFocusChildren(DependencyObject? dependencyObject)
		{
			if (dependencyObject is RichTextBlock)
			{
				//focusChildren = GetFocusChildren<CDOCollection>(static_cast<CRichTextBlock*>(object));
				//Uno TODO: RichTextBlock not implemented yet
				return Array.Empty<DependencyObject>();
			}
			else if (dependencyObject is RichTextBlockOverflow)
			{
				// RichTextOverflow does not have any focusable children, since its master
				// (RichTextBlock) returns those.
				return Array.Empty<DependencyObject>();
			}
			else if (dependencyObject is TextBlock textBlock)
			{
				// TODO Uno: Should call a specific version of the GetFocusChildren, for now fall back to UIElement behavior
				//focusChildren = GetFocusChildren<CDOCollection>(static_cast<CTextBlock*>(object));
				return VisualTreeHelper.GetChildren(textBlock).ToArray();
			}
#if __ANDROID__ || __IOS__ // TODO Uno specific: NativeScrollContentPresenter does not return its children
			else if (dependencyObject is NativeScrollContentPresenter scrollContentPresenter)
			{
				if (scrollContentPresenter.Content is DependencyObject child)
				{
					return new[] { child };
				}
			}
#endif
			else if (dependencyObject is UIElement uiElement)
			{
				return VisualTreeHelper.GetChildren(uiElement).ToArray();
			}

			return Array.Empty<DependencyObject>();
		}

		internal static bool IsVisible(DependencyObject? dependencyObject)
		{
			bool isVisible = true;
			if (dependencyObject is UIElement uiElement)
			{
				isVisible = uiElement.Visibility == Visibility.Visible;
			}

			return isVisible;
		}

		internal static bool AreAllAncestorsVisible(DependencyObject? dependencyObject)
		{
			if (dependencyObject is UIElement objectAsUIElement)
			{
				return objectAsUIElement.AreAllAncestorsVisible();
			}

			if (dependencyObject is TextElement objectAsTextElement)
			{
				var containingElement = objectAsTextElement.GetContainingFrameworkElement();
				return AreAllAncestorsVisible(containingElement);
			}

			return true;
		}

		internal static bool IsEnabled(DependencyObject? dependencyObject)
		{
			bool isEnabled = true;

			if (dependencyObject is Control control)
			{
				isEnabled = control.IsEnabled;
			}

			return isEnabled;
		}

		/// <summary>
		/// Determine if a particular DependencyObject cares to take focus.
		/// </summary>
		/// <param name="dependencyObject">Input.</param>
		/// <returns>A value indicating whether focus is allowed.</returns>
		internal static bool IsFocusable(DependencyObject? dependencyObject)
		{
			bool isFocusable = false;

			var objectAsUI = dependencyObject as UIElement;

			if (objectAsUI != null && objectAsUI.SkipFocusSubtree)
			{
				return false;
			}

			if (dependencyObject is Control control)
			{
				isFocusable = control.IsFocusable;
			}
			else if (FocusableHelper.GetIFocusableForDO(dependencyObject) is { } ifocusable)
			{
				isFocusable = ifocusable.IsFocusable();
			}
			else if (objectAsUI != null)
			{
				isFocusable = objectAsUI.IsFocusable;
			}

			// NULL objects, UIElements, panels, etc. are not focusable
			return isFocusable;
		}

		internal static bool IsPotentialTabStop(DependencyObject? dependencyObject)
		{
			if (dependencyObject != null)
			{
				if (dependencyObject is Control ||
					FocusableHelper.IsFocusableDO(dependencyObject))
				{
					return true;
				}
				else if (dependencyObject is TextBlock || dependencyObject is RichTextBlock)
				{
					return GetCaretBrowsingModeEnable() || (dependencyObject as UIElement)?.IsTabStop == true;
				}
				else
				{
					if (dependencyObject is UIElement uiElement && uiElement.IsTabStop)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Caret browsing mode is a Windows-specific accessibility feature.
		/// </summary>
		private static bool GetCaretBrowsingModeEnable() => false;

		/// <summary>
		/// Returns true if there is a focusable child.
		/// </summary>
		/// <param name="parent">Parent.</param>
		/// <returns>True if there is a focusable child.</returns>
		internal static bool CanHaveFocusableChildren(DependencyObject? parent)
		{
			bool isFocusable = false;

			if (parent == null)
			{
				return false;
			}

			if (parent is UIElement uiElement && uiElement.SkipFocusSubtree)
			{
				return false;
			}

			var collection = GetFocusChildren(parent);

			if (collection != null) // && !collection.IsLeaving())
			{
				var kidCount = collection.Length;

				for (var i = 0; i < kidCount && isFocusable == false; i++)
				{
					var child = collection[i];

					if (child != null && IsVisible(child))
					{
						// Ignore TextBlock/RichTextBlock/RIchTextBlockOverflow here since we don?t want them
						// to get focus through FocusManager operations e.g. tab navigation.
						// We only allow focus on them through direct pointer input.  If we don?t ignore them here,
						// then tabbing into a control with Text elements may cause us to reset focus to the first
						// focusable item on the page, since we report CanHaveFocusableChildren=TRUE but since
						// IsTabStop=FALSE the focus operation fails.
						if (child is TextBlock ||
							child is RichTextBlock ||
							child is RichTextBlockOverflow)
						{
							if (IsFocusable(child) && IsPotentialTabStop(child))
							{
								isFocusable = true;
							}
							else
							{
								if (CanHaveFocusableChildren(child))
								{
									isFocusable = true;
								}
							}
						}
						else
						{
							if (IsFocusable(child))
							{
								isFocusable = true;
							}
							else
							{
								if (CanHaveFocusableChildren(child))
								{
									isFocusable = true;
								}
							}
						}
					}
				}
			}

			return isFocusable;
		}

		internal static bool IsFocusEngagementEnabled(DependencyObject dependencyObject)
		{
			var control = dependencyObject as Control;

			bool isEngagedEnabled = false;

			if (control != null)
			{
				isEngagedEnabled = control.IsFocusEngagementEnabled;
			}

			return isEngagedEnabled;
		}

		internal static bool IsFocusEngaged(DependencyObject dependencyObject)
		{
			var control = dependencyObject as Control;

			bool isEngaged = false;

			if (control != null)
			{
				isEngaged = control.IsFocusEngaged;
			}

			return isEngaged;
		}

		internal static bool IsGamepadFocusCandidate(DependencyObject dependencyObject) =>
			(dependencyObject as UIElement)?.IsGamepadFocusCandidate ?? true;

		internal static bool ShouldSkipFocusSubTree(DependencyObject parent) =>
			parent is UIElement uiElement && uiElement.SkipFocusSubtree;

		internal static bool HasFocusedElement(DependencyObject element)
		{
			//if (element->IsActive())
			{
				var focusManager = VisualTree.GetFocusManagerForElement(element);

				if (element == focusManager?.FocusedElement)
				{
					return true;
				}

				var collection = GetFocusChildren(element);

				if (collection != null && collection.Length > 0)
				{
					for (var i = 0; i < collection.Length; i++)
					{
						var child = collection[i];

						if (child != null)
						{
							if (HasFocusedElement(child))
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		internal static IEnumerable<DependencyObject?> GetFocusChildrenInTabOrder(DependencyObject parent)
		{
			if (parent is UIElement uiElement)
			{
				return uiElement.GetChildrenInTabFocusOrderInternal() ?? Array.Empty<DependencyObject>();
			}

			return GetFocusChildren(parent);
		}

		internal static void IsScrollable(DependencyObject element, ref bool horizontally, ref bool vertically)
		{
			if (element is ScrollContentPresenter)
			{
				horizontally = false;
				vertically = false;
				//VERIFYHR(CFxCallbacks::UIElement_IsScrollViewerContentScrollable(
				//	static_cast<CUIElement*>(element),
				//	horizontally,
				//	vertically));
			}
			else
			{
				var uielement = element as UIElement;
				if (uielement != null && uielement is ScrollViewer scrollViewer)
				{
					//TODO Uno: Check for actual scrollability here, instead of just scroll mode.
					horizontally = scrollViewer.HorizontalScrollMode == ScrollMode.Enabled;
					vertically = scrollViewer.VerticalScrollMode == ScrollMode.Enabled;

					//var desiredSize = uielement.DesiredSize;
					//var unclippedDesiredSize = uielement.UnclippedDesiredSize;


					//horizontally = unclippedDesiredSize.Width > desiredSize.Width;

					//vertically = unclippedDesiredSize.Height > desiredSize.Height;
				}
			}
		}
	}
}
