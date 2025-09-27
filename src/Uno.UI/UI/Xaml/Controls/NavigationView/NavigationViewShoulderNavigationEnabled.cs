// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationView.idl, commit 34031a0

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify when gamepad bumpers
/// can be used to navigate the top-level navigation items
/// in a NavigationView.
/// </summary>
public enum NavigationViewShoulderNavigationEnabled
{
	/// <summary>
	/// Gamepad bumpers navigate the top-level
	/// navigation items when the SelectionFollowsFocus
	/// property is Enabled.
	/// </summary>
	WhenSelectionFollowsFocus = 0,

	/// <summary>
	/// Gamepad bumpers always navigate the top-level
	/// navigation items.
	/// </summary>
	Always = 1,

	/// <summary>
	/// Gamepad bumpers never navigate the top-level
	/// navigation items.
	/// </summary>
	Never = 2
}
