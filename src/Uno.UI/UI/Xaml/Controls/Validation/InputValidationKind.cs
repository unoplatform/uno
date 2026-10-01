// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Uno.Extras.Input;

/// <summary>
/// How a control presents the validation errors of its input.
/// </summary>
public enum InputValidationKind
{
	/// <summary>
	/// Let the control decide. Resolves to <see cref="Compact"/>.
	/// </summary>
	Auto,

	/// <summary>
	/// Show an error icon beside the input, with the errors in its tooltip.
	/// </summary>
	Compact,

	/// <summary>
	/// Show the errors below the input.
	/// </summary>
	Inline,
}
