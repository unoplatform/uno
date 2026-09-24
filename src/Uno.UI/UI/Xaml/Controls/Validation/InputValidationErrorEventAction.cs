// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// What happened to the error carried by an <see cref="InputValidationErrorEventArgs"/>.
/// </summary>
public enum InputValidationErrorEventAction
{
	/// <summary>
	/// The error was added to the control's errors.
	/// </summary>
	Added,

	/// <summary>
	/// The error was removed from the control's errors.
	/// </summary>
	Removed,
}
