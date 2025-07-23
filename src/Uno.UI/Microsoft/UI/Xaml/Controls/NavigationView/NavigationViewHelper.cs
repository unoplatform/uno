// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewHelper.h, commit 65718e2813

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

internal enum NavigationViewVisualStateDisplayMode
{
	Compact,
	Expanded,
	Minimal,
	MinimalWithBackButton
}

internal enum NavigationViewRepeaterPosition
{
	LeftNav,
	TopPrimary,
	TopOverflow,
	LeftFooter,
	TopFooter
}

internal enum NavigationViewPropagateTarget
{
	LeftListView,
	TopListView,
	OverflowListView,
	All
}

// TODO:
internal class NavigationViewItemHelper
{
	internal const string c_OnLeftNavigationReveal = "OnLeftNavigationReveal";
	internal const string c_OnLeftNavigation = "OnLeftNavigation";
	internal const string c_OnTopNavigationPrimary = "OnTopNavigationPrimary";
	internal const string c_OnTopNavigationPrimaryReveal = "OnTopNavigationPrimaryReveal";
	internal const string c_OnTopNavigationOverflow = "OnTopNavigationOverflow";
}

// Since RS5, a lot of functions in NavigationViewItem is moved to NavigationViewItemPresenter. So they both share some common codes.
// This class helps to initialize and maintain the status of SelectionIndicator and ToolTip
internal class NavigationViewItemHelper<T>
{
	internal NavigationViewItemHelper(object owner)
	{
		m_owner = owner;
	}

	internal UIElement GetSelectionIndicator() { return m_selectionIndicator; }

	internal void Init(FrameworkElement controlProtected)
	{
		m_selectionIndicator = controlProtected.GetTemplateChild(c_selectionIndicatorName) as UIElement;
	}

	private object m_owner = null;

	private UIElement m_selectionIndicator;

	private const string c_selectionIndicatorName = "SelectionIndicator";
};
