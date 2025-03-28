// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBoxPlaceholderTextHelper.cpp, tag winui3/release/1.4.2
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DirectUI;

internal class TextBoxPlaceholderTextHelper
{
	public static DependencyObject GetTextBlockFromOwner(UIElement spOwner, bool considerCollapsedTextBlocks)
	{
		DependencyObject textBlock = null;
		UIElement spPlaceholderTextPresenter = null;

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

		// If the placeholder text is a TextBlock instead of a ContentControl, return that textblock
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
				// As a result meaning we will need to call GetChild again, looking for the '0th' child.
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
			// If the placeholder text is not visible, do not add it to the DescribedBy list
			if ((textBlock as UIElement).Visibility == Visibility.Collapsed)
			{
				return;
			}

			var describedByList = AutomationProperties.GetDescribedBy(spOwner) ?? [];

			if (!describedByList.Contains(textBlock))
			{
				describedByList.Add(textBlock);
			}
		}
	}
}
