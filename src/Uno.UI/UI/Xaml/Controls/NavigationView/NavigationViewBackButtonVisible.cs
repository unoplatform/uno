// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationView.idl, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether the back button is visible in NavigationView.
/// </summary>
public enum NavigationViewBackButtonVisible
{
	/// <summary>
	/// Do not display the back button in NavigationView,
	/// and do not reserve space for it in layout.
	/// </summary>
	Collapsed = 0,

	/// <summary>
	/// Display the back button in NavigationView.
	/// </summary>
	Visible = 1,

	/// <summary>
	/// The system chooses whether or not to display
	/// the back button, depending on the device/form factor.
	/// On phones, tablets, desktops, and hubs, the back button
	/// is visible (except on Android, where it is not displayed
	/// as per Android guidelines). On Xbox/TV, the back button
	/// is collapsed.
	/// </summary>
	Auto = 2,
}
