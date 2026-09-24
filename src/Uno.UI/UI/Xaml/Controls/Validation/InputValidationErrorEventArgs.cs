// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for <see cref="IInputValidationControl.ValidationError"/>.
/// </summary>
public sealed partial class InputValidationErrorEventArgs
{
	internal InputValidationErrorEventArgs(InputValidationErrorEventAction action, InputValidationError error)
	{
		Action = action;
		Error = error;
	}

	/// <summary>
	/// Whether the error was added or removed.
	/// </summary>
	public InputValidationErrorEventAction Action { get; }

	/// <summary>
	/// The error that was added or removed.
	/// </summary>
	public InputValidationError Error { get; }
}
