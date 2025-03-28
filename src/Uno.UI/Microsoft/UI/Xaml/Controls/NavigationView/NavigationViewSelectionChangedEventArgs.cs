// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewSelectionChangedEventArgs.cpp, commit d883cf3

using Windows.UI.Xaml.Media.Animation;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides data for the NavigationView.SelectionChanged event.
/// </summary>
public partial class NavigationViewSelectionChangedEventArgs
{
	/// <summary>
	/// Gets the newly selected menu item.
	/// </summary>
	public object SelectedItem { get; internal set; }

	/// <summary>
	/// Gets a value that indicates whether the SelectedItem
	/// is the menu item for Settings.
	/// </summary>
	public bool IsSettingsSelected { get; internal set; }

	/// <summary>
	/// Gets the container for the selected item.
	/// </summary>
	public NavigationViewItemBase SelectedItemContainer { get; internal set; }

	/// <summary>
	/// Gets the navigation transition recommended for the direction
	/// of the navigation.
	/// </summary>
	public NavigationTransitionInfo RecommendedNavigationTransitionInfo { get; internal set; }
}
