// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TwoPaneView.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how panes are shown in a TwoPaneView in wide mode.
/// </summary>
public enum TwoPaneViewWideModeConfiguration
{
	/// <summary>
	/// Only the pane that has priority is shown, the other pane is hidden.
	/// </summary>
	SinglePane = 0,

	/// <summary>
	/// The pane that has priority is shown on the left, the other pane is shown on the right.
	/// </summary>
	LeftRight = 1,

	/// <summary>
	/// The pane that has priority is shown on the right, the other pane is shown on the left.
	/// </summary>
	RightLeft = 2,
}
