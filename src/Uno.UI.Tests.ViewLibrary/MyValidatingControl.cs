#nullable enable

using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.ViewLibrary;

/// <summary>
/// A control that participates in input validation from outside Uno.UI, so it sees only the public and
/// protected surface.
/// </summary>
/// <remarks>
/// This project is deliberately absent from Uno.UI's InternalsVisibleTo list, which makes the participation
/// contract a compile-time one: if a member a third-party control needs goes back to internal, this stops
/// building. Note the metadata type — <see cref="FrameworkPropertyMetadata"/>'s (value, callback) overload is
/// internal, so a control outside Uno.UI registers with <see cref="PropertyMetadata"/>.
/// </remarks>
public partial class MyValidatingControlBase : Control, IInputValidationControl
{
	public static DependencyProperty InputValidationModeProperty { get; } =
		DependencyProperty.Register(
			nameof(InputValidationMode),
			typeof(InputValidationMode),
			typeof(MyValidatingControlBase),
			new PropertyMetadata(InputValidationMode.Disabled, Control.OnInputValidationModeChanged));

	public static DependencyProperty InputValidationKindProperty { get; } =
		DependencyProperty.Register(
			nameof(InputValidationKind),
			typeof(InputValidationKind),
			typeof(MyValidatingControlBase),
			new PropertyMetadata(InputValidationKind.Auto, Control.OnInputValidationKindChanged));

	public static DependencyProperty HasValidationErrorsProperty { get; } =
		DependencyProperty.Register(
			nameof(HasValidationErrors),
			typeof(bool),
			typeof(MyValidatingControlBase),
			new PropertyMetadata(default(bool), Control.OnHasValidationErrorsChanged));

	public static DependencyProperty ValidationErrorsProperty { get; } =
		DependencyProperty.Register(
			nameof(ValidationErrors),
			typeof(IObservableVector<InputValidationError>),
			typeof(MyValidatingControlBase),
			new PropertyMetadata(default(IObservableVector<InputValidationError>)));

	public static DependencyProperty ErrorTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ErrorTemplate),
			typeof(DataTemplate),
			typeof(MyValidatingControlBase),
			new PropertyMetadata(default(DataTemplate)));

	public InputValidationMode InputValidationMode
	{
		get => (InputValidationMode)GetValue(InputValidationModeProperty);
		set => SetValue(InputValidationModeProperty, value);
	}

	public InputValidationKind InputValidationKind
	{
		get => (InputValidationKind)GetValue(InputValidationKindProperty);
		set => SetValue(InputValidationKindProperty, value);
	}

	public bool HasValidationErrors => (bool)GetValue(HasValidationErrorsProperty);

	public IObservableVector<InputValidationError> ValidationErrors
		=> GetOrCreateValidationErrors(ValidationErrorsProperty);

	public DataTemplate? ErrorTemplate
	{
		get => (DataTemplate?)GetValue(ErrorTemplateProperty);
		set => SetValue(ErrorTemplateProperty, value);
	}

	public event TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> HasValidationErrorsChanged
	{
		add => AddHasValidationErrorsChangedHandler(value);
		remove => RemoveHasValidationErrorsChangedHandler(value);
	}

	public event TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> ValidationError
	{
		add => AddValidationErrorHandler(value);
		remove => RemoveValidationErrorHandler(value);
	}

	public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
	{
		add => AddErrorChangedHandler(value);
		remove => RemoveErrorChangedHandler(value);
	}
}

/// <summary>
/// Declares its input through the attribute, which is the route a control that owns its own source takes.
/// </summary>
[InputValidationProperty(nameof(Text))]
public partial class MyValidatingControl : MyValidatingControlBase
{
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(MyValidatingControl),
			new PropertyMetadata(string.Empty));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
}

/// <summary>
/// Carries no attribute, so its input has to be registered in
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/> — the route left for a
/// control whose source cannot be annotated.
/// </summary>
public partial class MyMappedValidatingControl : MyValidatingControlBase
{
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(
			nameof(Value),
			typeof(string),
			typeof(MyMappedValidatingControl),
			new PropertyMetadata(string.Empty));

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
}
