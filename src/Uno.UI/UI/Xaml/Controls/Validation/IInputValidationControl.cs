// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Implemented by a control that reports the validation errors of its input.
/// </summary>
/// <remarks>
/// Public so that third-party controls can participate. A control also has to declare its input through
/// <see cref="Uno.UI.Xaml.Controls.InputValidationPropertyAttribute"/>, or have it registered in
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/>, and to opt in through
/// <see cref="Uno.UI.Xaml.Controls.Validation.InputValidationModeProperty"/>; implementing this interface on
/// its own reports nothing.
/// <para>
/// In WinUI every member below is a dependency property registered on each participating control. In Uno they
/// are attached properties on <see cref="Uno.UI.Xaml.Controls.Validation"/>, which an implementer forwards to,
/// so that no control needs storage of its own and a third-party control participates without registering any.
/// </para>
/// </remarks>
public interface IInputValidationControl
{
	/// <summary>
	/// The errors currently reported for the control's input.
	/// </summary>
	/// <remarks>
	/// Mutable: an application may add to it to report an error that does not come from an
	/// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> source.
	/// </remarks>
	IObservableVector<InputValidationError> ValidationErrors { get; }

	/// <summary>
	/// Whether <see cref="ValidationErrors"/> is non-empty.
	/// </summary>
	bool HasValidationErrors { get; }

	// Uno: InputValidationContext is not ported, so the member it types is commented out rather than
	// declared against a type that does not exist. Nothing reads MemberName, and IsInputRequired is the
	// required-field indicator, which needs a template column before it means anything.
	//
	// InputValidationContext ValidationContext { get; set; }

	/// <summary>
	/// The template used to present <see cref="ValidationErrors"/>.
	/// </summary>
	DataTemplate? ErrorTemplate { get; set; }

	/// <summary>
	/// Whether the control participates in input validation, and how its errors are presented.
	/// </summary>
	InputValidationMode InputValidationMode { get; set; }

	/// <summary>
	/// How the control presents its validation errors.
	/// </summary>
	InputValidationKind InputValidationKind { get; set; }

	/// <summary>
	/// Raised when <see cref="HasValidationErrors"/> changes.
	/// </summary>
	event TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> HasValidationErrorsChanged;

	/// <summary>
	/// Raised once per error added to or removed from <see cref="ValidationErrors"/>.
	/// </summary>
	event TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> ValidationError;

	/// <summary>
	/// Raised when the binding source reports a change to the errors of the validated property.
	/// </summary>
	/// <remarks>
	/// Uno only, and kept alongside the two WinUI events because it is the one that carries the
	/// <see cref="DataErrorsChangedEventArgs"/> of the source unchanged, so that a consumer of several
	/// controls sharing one source can tell which property changed.
	/// </remarks>
	event EventHandler<DataErrorsChangedEventArgs> ErrorChanged;
}
