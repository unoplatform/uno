// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarItem.cpp, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class SelectorBarItem
{
	/// <summary>
	/// Initializes a new instance of the SelectorBarItem class.
	/// </summary>
	public SelectorBarItem()
	{
		// SELECTORBAR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		// EnsureProperties();
		this.SetDefaultStyleKey();
	}

	//SelectorBarItem.~public SelectorBarItem()
	//{
	//	SELECTORBAR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	protected override AutomationPeer OnCreateAutomationPeer() => new SelectorBarItemAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		// SELECTORBAR_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		base.OnApplyTemplate();

		//IControlProtected controlProtected{ this };

		_iconVisual = (ContentPresenter)GetTemplateChild(s_iconVisualPartName);
		_textVisual = (TextBlock)GetTemplateChild(s_textVisualPartName);

		UpdatePartsVisibility(true /*isForIcon*/, true /*isForText*/);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;
		bool isForIcon = dependencyProperty == IconProperty;
		bool isForText = dependencyProperty == TextProperty;

#if DBG
    SELECTORBAR_TRACE_VERBOSE(null, "%s[%p](property: %s)\n", METH_NAME, this, DependencyPropertyToString(dependencyProperty).c_str());
#endif

		if (isForIcon || isForText)
		{
			UpdatePartsVisibility(isForIcon, isForText);
		}
	}

	private void UpdatePartsVisibility(bool isForIcon, bool isForText)
	{
		MUX_ASSERT(isForIcon || isForText);

		// SELECTORBAR_TRACE_VERBOSE(this, TRACE_MSG_METH_INT_INT, METH_NAME, this, isForIcon, isForText);

		UIElement? iconParent = null, textParent = null;
		bool hasIcon = false, hasText = false;

		if (_iconVisual is { } iconVisual)
		{
			iconParent = VisualTreeHelper.GetParent(iconVisual) as UIElement;
			hasIcon = Icon != null;

			if (isForIcon)
			{
				iconVisual.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		if (_textVisual is { } textVisual)
		{
			textParent = VisualTreeHelper.GetParent(textVisual) as UIElement;
			hasText = !string.IsNullOrEmpty(Text);

			if (isForText)
			{
				textVisual.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		if (iconParent is not null && iconParent == textParent)
		{
			// When there is no icon and no text, and the PART_IconVisual / PART_TextVisual have a common parent (an unnamed
			// StackPanel by default), that common parent is collapsed so it does not take any real estate because of its Margin.
			textParent.Visibility = (hasIcon || hasText) ? Visibility.Visible : Visibility.Collapsed;
		}
	}

#if DBG

hstring DependencyPropertyToString(
     IDependencyProperty& dependencyProperty)
{
    if (dependencyProperty == IconProperty)
    {
        return "Icon";
    }
    else if (dependencyProperty == TextProperty)
    {
        return "Text";
    }
    else
    {
        return "UNKNOWN";
    }
}

#endif
}
