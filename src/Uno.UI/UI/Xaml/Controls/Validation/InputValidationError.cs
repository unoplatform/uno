// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Uno.Extras.Input;

/// <summary>
/// A single validation error reported for a control's input.
/// </summary>
/// <remarks>Not creatable from XAML — it carries no parameterless constructor by design.</remarks>
public partial class InputValidationError
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InputValidationError"/> class.
	/// </summary>
	/// <param name="errorMessage">The message to display for this error.</param>
	public InputValidationError(string errorMessage) => ErrorMessage = errorMessage;

	/// <summary>
	/// The message to display for this error.
	/// </summary>
	public string ErrorMessage { get; }
}
