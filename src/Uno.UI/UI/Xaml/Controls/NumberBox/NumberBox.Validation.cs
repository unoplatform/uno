#nullable enable

using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using UnoValidation = Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

[UnoValidation.InputValidationProperty(nameof(Value))]
public partial class NumberBox : IInputValidationControl
{
	/// <inheritdoc />
	public IObservableVector<InputValidationError> ValidationErrors => UnoValidation.Validation.GetErrors(this);

	/// <inheritdoc />
	public bool HasValidationErrors => UnoValidation.Validation.GetHasErrors(this);

	/// <inheritdoc />
	public DataTemplate? ErrorTemplate
	{
		get => UnoValidation.Validation.GetErrorTemplate(this);
		set => UnoValidation.Validation.SetErrorTemplate(this, value);
	}

	/// <inheritdoc />
	public InputValidationMode InputValidationMode
	{
		get => UnoValidation.Validation.GetInputValidationMode(this);
		set => UnoValidation.Validation.SetInputValidationMode(this, value);
	}

	/// <inheritdoc />
	public InputValidationKind InputValidationKind
	{
		get => UnoValidation.Validation.GetInputValidationKind(this);
		set => UnoValidation.Validation.SetInputValidationKind(this, value);
	}

	/// <inheritdoc />
	public event TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> HasValidationErrorsChanged
	{
		add => UnoValidation.Validation.AddHasValidationErrorsChangedHandler(this, value);
		remove => UnoValidation.Validation.RemoveHasValidationErrorsChangedHandler(this, value);
	}

	/// <inheritdoc />
	public event TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> ValidationError
	{
		add => UnoValidation.Validation.AddValidationErrorHandler(this, value);
		remove => UnoValidation.Validation.RemoveValidationErrorHandler(this, value);
	}

	/// <inheritdoc />
	public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
	{
		add => UnoValidation.Validation.AddErrorChangedHandler(this, value);
		remove => UnoValidation.Validation.RemoveErrorChangedHandler(this, value);
	}
}
