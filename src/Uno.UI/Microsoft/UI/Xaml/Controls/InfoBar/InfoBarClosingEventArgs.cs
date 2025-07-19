// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBar.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the InfoBar.Closing event.
/// </summary>
public partial class InfoBarClosingEventArgs
{
	internal InfoBarClosingEventArgs(InfoBarCloseReason reason) =>
		Reason = reason;

	/// <summary>
	/// Gets a constant that specifies whether the cause of the Closing event
	/// was due to user interaction (Close button click) or programmatic closure.
	/// </summary>
	public InfoBarCloseReason Reason { get; }

	/// <summary>
	/// Gets or sets a value that indicates whether the Closing event should be canceled in the InfoBar.
	/// </summary>
	public bool Cancel { get; set; } = false;
}
