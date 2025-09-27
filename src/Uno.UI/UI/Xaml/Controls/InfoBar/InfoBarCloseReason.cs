// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBar.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that indicate the cause of the InfoBar closure.
/// </summary>
public enum InfoBarCloseReason
{
	/// <summary>
	/// The InfoBar was closed by the user clicking the close button.
	/// </summary>
	CloseButton = 0,

	/// <summary>
	/// The InfoBar was programmatically closed.
	/// </summary>
	Programmatic = 1
}
