// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationView.idl, commit 34031a0

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether item selection changes when keyboard focus changes in a NavigationView.
/// </summary>
public enum NavigationViewSelectionFollowsFocus
{
	/// <summary>
	/// Selection does not change when keyboard focus changes.
	/// </summary>
	Disabled,

	/// <summary>
	/// Selection changes when keyboard focus changes.
	/// </summary>
	Enabled
}
