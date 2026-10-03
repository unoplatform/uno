// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Uno.Extras.Input;

/// <summary>
/// Whether a control participates in input validation.
/// </summary>
public enum InputValidationMode
{
	/// <summary>
	/// Validation is enabled.
	/// </summary>
	Auto,

	/// <summary>
	/// Validation is enabled. Behaves as <see cref="Auto"/>.
	/// </summary>
	/// <remarks>
	/// In WinUI this is the sentinel an InputValidationCommand uses to defer to the control's own value.
	/// Uno does not implement that command, so the two are indistinguishable here.
	/// </remarks>
	Default,

	/// <summary>
	/// Validation is disabled. The default in Uno, where this also gates the error subscription.
	/// </summary>
	Disabled,
}
