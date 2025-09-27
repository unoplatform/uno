// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPager.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how the navigation buttons of the PipsPager are displayed.
/// </summary>
public enum PipsPagerButtonVisibility
{
	/// <summary>
	/// The navigation button is visible and enabled, but hidden when content
	/// is at one or the other extent. For example, the Previous button is hidden
	/// when the current page is the first page, and the Next button is hidden
	/// when the current page is the last page.
	/// </summary>
	Visible,

	/// <summary>
	/// The button behavior is the same as Visible except the button is visible
	/// only when the pointer cursor is over the pager, or keyboard focus is inside
	/// the pager or on a navigation button.
	/// </summary>
	VisibleOnPointerOver,

	/// <summary>
	/// The button is not visible and does not take up layout space.
	/// </summary>
	Collapsed
}
