#nullable enable

using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Uno.UI.Helpers.Boxes;
using UnoValidation = Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

[UnoValidation.InputValidationProperty(nameof(Password))]
public partial class PasswordBox : IInputValidationControl
{
	/// <summary>
	/// Identifies the <see cref="InputValidationMode"/> dependency property.
	/// </summary>
	public static DependencyProperty InputValidationModeProperty { get; } =
		DependencyProperty.Register(
			nameof(InputValidationMode),
			typeof(InputValidationMode),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(
				InputValidationMode.Disabled,
				OnInputValidationModeChanged));

	/// <summary>
	/// Identifies the <see cref="InputValidationKind"/> dependency property.
	/// </summary>
	public static DependencyProperty InputValidationKindProperty { get; } =
		DependencyProperty.Register(
			nameof(InputValidationKind),
			typeof(InputValidationKind),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(
				InputValidationKind.Auto,
				OnInputValidationKindChanged));

	/// <summary>
	/// Identifies the <see cref="HasValidationErrors"/> dependency property.
	/// </summary>
	public static DependencyProperty HasValidationErrorsProperty { get; } =
		DependencyProperty.Register(
			nameof(HasValidationErrors),
			typeof(bool),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(
				BoolBoxes.False,
				OnHasValidationErrorsChanged));

	/// <summary>
	/// Identifies the <see cref="ValidationErrors"/> dependency property.
	/// </summary>
	public static DependencyProperty ValidationErrorsProperty { get; } =
		DependencyProperty.Register(
			nameof(ValidationErrors),
			typeof(IObservableVector<InputValidationError>),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(default(IObservableVector<InputValidationError>)));

	/// <summary>
	/// Identifies the <see cref="ErrorTemplate"/> dependency property.
	/// </summary>
	public static DependencyProperty ErrorTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ErrorTemplate),
			typeof(DataTemplate),
			typeof(PasswordBox),
			new FrameworkPropertyMetadata(
				default(DataTemplate),
				OnErrorTemplateChanged));

	/// <inheritdoc />
	public InputValidationMode InputValidationMode
	{
		get => (InputValidationMode)GetValue(InputValidationModeProperty);
		set => SetValue(InputValidationModeProperty, value);
	}

	/// <inheritdoc />
	public InputValidationKind InputValidationKind
	{
		get => (InputValidationKind)GetValue(InputValidationKindProperty);
		set => SetValue(InputValidationKindProperty, value);
	}

	/// <inheritdoc />
	/// <remarks>Written by the framework. Uno has no read-only dependency property to express that.</remarks>
	public bool HasValidationErrors => (bool)GetValue(HasValidationErrorsProperty);

	/// <inheritdoc />
	public IObservableVector<InputValidationError> ValidationErrors
		=> GetOrCreateValidationErrors(ValidationErrorsProperty);

	/// <inheritdoc />
	public DataTemplate? ErrorTemplate
	{
		get => (DataTemplate?)GetValue(ErrorTemplateProperty);
		set => SetValue(ErrorTemplateProperty, value);
	}

	/// <inheritdoc />
	public event TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> HasValidationErrorsChanged
	{
		add => AddHasValidationErrorsChangedHandler(value);
		remove => RemoveHasValidationErrorsChangedHandler(value);
	}

	/// <inheritdoc />
	public event TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> ValidationError
	{
		add => AddValidationErrorHandler(value);
		remove => RemoveValidationErrorHandler(value);
	}

	/// <inheritdoc />
	public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
	{
		add => AddErrorChangedHandler(value);
		remove => RemoveErrorChangedHandler(value);
	}
}
