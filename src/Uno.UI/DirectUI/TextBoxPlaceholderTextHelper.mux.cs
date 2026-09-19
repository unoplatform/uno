// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/TextBoxPlaceholderTextHelper.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DirectUI;

partial class TextBoxPlaceholderTextHelper
{
	private static DependencyObject? GetTextBlockFromOwner(UIElement spOwner, bool considerCollapsedTextBlocks)
	{
		DependencyObject? textBlock = null;
		UIElement? spPlaceholderTextPresenter = null;

		if (spOwner is Control control)
		{
			spPlaceholderTextPresenter = control.GetTemplateChild("PlaceholderTextContentPresenter") as UIElement;
		}

		if (spPlaceholderTextPresenter == null)
		{
			// No-op if the PlaceholderTextContentPresenter template part does not exist
			return textBlock;
		}

		if (!considerCollapsedTextBlocks)
		{
			var visibility = spPlaceholderTextPresenter.Visibility;

			if (visibility == Visibility.Collapsed)
			{
				return textBlock;
			}
		}

		//If the placeholder text is a TextBlock instead of a ContentControl, return that textblock
		if (spPlaceholderTextPresenter is TextBlock)
		{
			textBlock = spPlaceholderTextPresenter;
			return textBlock;
		}

		var childrenCount = VisualTreeHelper.GetChildrenCount(spPlaceholderTextPresenter);

		for (var childIndex = 0; childIndex < childrenCount; childIndex++)
		{
			DependencyObject spTextBlock = VisualTreeHelper.GetChild(spPlaceholderTextPresenter, childIndex);

			if (spTextBlock is ContentPresenter)
			{
				// In this scenario, the placeholder text is a ContentControl instead of a ContentPresenter.
				// If there is actual placeholder text, the ContentControl's first child will be the presenter.
				// As a result meaning we will need to call GetChildStatic again, looking for the '0th' child.
				// If there is no placeholder text, the content presenter will have no children.
				if (spTextBlock is not null && VisualTreeHelper.GetChildrenCount(spTextBlock) == 0)
				{
					return textBlock;
				}

				spTextBlock = VisualTreeHelper.GetChild(spTextBlock, 0);
			}

			if (spTextBlock is TextBlock)
			{
				textBlock = spTextBlock;
				return textBlock;
			}
		}

		return textBlock;
	}


	public static void SetupPlaceholderTextBlockDescribedBy(UIElement spOwner)
	{
		var textBlock = GetTextBlockFromOwner(spOwner, false /*considerCollapsedTextBlocks*/);

		if (textBlock != null)
		{
			//If the placeholder text is not visible, do not add it to the DescribedBy list
			if (((UIElement)textBlock).Visibility == Visibility.Collapsed)
			{
				return;
			}

			var describedByList = AutomationProperties.GetDescribedBy(spOwner);
#if HAS_UNO
			// TODO Uno: Native GetDescribedByStatic creates the collection on demand.
			if (describedByList is null)
			{
				describedByList = new List<DependencyObject>();
				spOwner.SetValue(AutomationProperties.DescribedByProperty, describedByList);
			}
#endif

			if (!describedByList.Contains(textBlock))
			{
				describedByList.Add(textBlock);
			}
		}
	}

	internal static void UpdatePlaceholderTextPresenterVisibility(
		UIElement textBox,
		UIElement placeholderTextAsUIElement,
		bool isEnabled)
	{
		if (isEnabled && ShouldMakePlaceholderTextVisible(placeholderTextAsUIElement, textBox))
		{
			placeholderTextAsUIElement.Visibility = Visibility.Visible;

			// Visible placeholder text must participate in the UIA Control view.
			AutomationProperties.SetAccessibilityView(placeholderTextAsUIElement, AccessibilityView.Control);
		}
		else
		{
			ClearPlaceholderTextBlockDescribedBy(textBox);
			placeholderTextAsUIElement.Visibility = Visibility.Collapsed;
		}
	}

	private static void ClearPlaceholderTextBlockDescribedBy(UIElement textBox)
	{
		var spOwner = textBox;
		var textBlock = GetTextBlockFromOwner(spOwner, true /*considerCollapsedTextBlocks*/);

		// ShouldCollapsePlaceholderText returns true only if the placeholder text actually has content and
		// the DescribedBy list is not empty.
		if (textBlock is UIElement placeholderText && ShouldCollapsePlaceholderText(placeholderText, textBox))
		{
			//GetDescribedByStatic creates a list if none exists.
			var describedByList = AutomationProperties.GetDescribedBy(spOwner);
			var index = describedByList?.IndexOf(textBlock) ?? -1;
			if (index >= 0)
			{
				describedByList!.RemoveAt(index);
			}
		}
	}

	internal static bool ShouldMakePlaceholderTextVisible(
		UIElement? placeholderTextAsUIElement,
		UIElement? textControlAsUIElement)
	{
		if (placeholderTextAsUIElement is null || textControlAsUIElement is null)
		{
			return false;
		}

		//If the containing text control is not empty, we should never make placeholder text visible.
		if (!IsTextControlEmpty(textControlAsUIElement))
		{
			return false;
		}

		// If the placeholder text is a TextBlock, not a ContentControl or ContentPresenter,
		// we only want to set visibility to visible if placeholder text actually exists.
		if (placeholderTextAsUIElement is TextBlock spPlaceholderTextAsTextBlock)
		{
			// A null HSTRING projects to String.Empty in C#.
			return !string.IsNullOrEmpty(spPlaceholderTextAsTextBlock.Text);
		}

		return true;
	}

	private static bool ShouldCollapsePlaceholderText(
		UIElement? placeholderTextAsUIElement,
		UIElement? textControlAsUIElement)
	{
		if (placeholderTextAsUIElement is null || textControlAsUIElement is null)
		{
			return false;
		}

		//If the DescribedBy list does not exist, do not change placeholder visibility
		if (textControlAsUIElement.GetValue(AutomationProperties.DescribedByProperty) is not IList<DependencyObject>)
		{
			return false;
		}

		//If the containing text control is not empty, we should collapse placeholder text.
		if (IsTextControlEmpty(textControlAsUIElement))
		{
			// If the placeholder text is a TextBlock, not a ContentControl or ContentPresenter,
			// we only want to set visibility to collapsed if placeholder text actually exists.
			if (placeholderTextAsUIElement is TextBlock spPlaceholderTextAsTextBlock)
			{
				return !string.IsNullOrEmpty(spPlaceholderTextAsTextBlock.Text);
			}
		}

		return true;
	}
}
