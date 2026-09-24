// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\controls\microsoft.ui.xaml.controls.controls.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for <see cref="IInputValidationControl.HasValidationErrorsChanged"/>.
/// </summary>
public partial class HasValidationErrorsChangedEventArgs
{
	internal HasValidationErrorsChangedEventArgs(bool newValue) => NewValue = newValue;

	/// <summary>
	/// Whether the control now has validation errors.
	/// </summary>
	public bool NewValue { get; }
}
