// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationView.idl, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how and where the NavigationView pane is shown.
/// </summary>
public enum NavigationViewPaneDisplayMode
{
	/// <summary>
	/// The pane is shown on the left side of the control,
	/// and changes between minimal, compact, and full
	/// states depending on the width of the window.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// The pane is shown on the left side of the control
	/// in its fully open state.
	/// </summary>
	Left = 1,

	/// <summary>
	/// The pane is shown at the top of the control.
	/// </summary>
	Top = 2,

	/// <summary>
	/// The pane is shown on the left side of the control.
	/// Only the pane icons are shown by default.
	/// </summary>
	LeftCompact = 3,

	/// <summary>
	/// The pane is shown on the left side of the control.
	/// Only the pane menu button is shown by default.
	/// </summary>
	LeftMinimal = 4
}
