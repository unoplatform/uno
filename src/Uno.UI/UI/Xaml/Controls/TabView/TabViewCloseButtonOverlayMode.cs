// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.idl, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that describe the behavior of the close button contained within each TabViewItem.
/// </summary>
public enum TabViewCloseButtonOverlayMode
{
	/// <summary>
	/// Behavior is defined by the framework. Default.
	/// This value maps to Always.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// The selected tab always shows the close button if it is closable. Unselected tabs show the close
	/// button when the tab is closable and the user has their pointer over the tab.
	/// </summary>
	OnPointerOver = 1,

	/// <summary>
	/// The selected tab always shows the close button if it is closable.
	/// Unselected tabs always show the close button if they are closable.
	/// </summary>
	Always = 2,
}
