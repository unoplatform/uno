// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TwoPaneView.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how panes are shown in a TwoPaneView in tall mode.
/// </summary>
public enum TwoPaneViewTallModeConfiguration
{
	/// <summary>
	/// Only the pane that has priority is shown, the other pane is hidden.
	/// </summary>
	SinglePane = 0,

	/// <summary>
	/// The pane that has priority is shown on top, the other pane is shown on the bottom.
	/// </summary>
	TopBottom = 1,

	/// <summary>
	/// The pane that has priority is shown on the bottom, the other pane is shown on top.
	/// </summary>
	BottomTop = 2,
}
