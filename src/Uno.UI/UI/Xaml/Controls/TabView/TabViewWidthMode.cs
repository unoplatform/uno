// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.idl, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the width of the tabs.
/// </summary>
public enum TabViewWidthMode
{
	/// <summary>
	/// Each tab has the same width.
	/// </summary>
	Equal = 0,

	/// <summary>
	/// Each tab adjusts its width to the content within the tab.
	/// </summary>
	SizeToContent = 1,

	/// <summary>
	/// Unselected tabs collapse to show only their icon. The selected
	/// tab adjusts to display the content within the tab.
	/// </summary>
	Compact = 2,
}
