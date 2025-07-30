// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemInvokedEventArgs.cpp, commit 46f9da3

using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the NavigationView.ItemInvoked event.
/// </summary>
public partial class NavigationViewItemInvokedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the NavigationViewItemInvokedEventArgs class.
	/// </summary>
	public NavigationViewItemInvokedEventArgs()
	{
	}

	/// <summary>
	/// Gets a reference to the invoked item.
	/// </summary>
	public object InvokedItem { get; internal set; }

	/// <summary>
	/// Gets a value that indicates whether the InvokedItem
	/// is the menu item for Settings.
	/// </summary>
	public bool IsSettingsInvoked { get; internal set; }

	/// <summary>
	/// Gets the container for the invoked item.
	/// </summary>
	public NavigationViewItemBase InvokedItemContainer { get; internal set; }

	/// <summary>
	/// Gets the navigation transition recommended for the direction
	/// of the navigation.
	/// </summary>
	public NavigationTransitionInfo RecommendedNavigationTransitionInfo { get; internal set; }
}
