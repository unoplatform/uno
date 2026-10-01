#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;
#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace Uno.Extras.Input;

/// <summary>
/// Input validation: surfaces the <see cref="System.ComponentModel.INotifyDataErrorInfo"/> errors of a binding
/// source on the control bound to it.
/// </summary>
/// <remarks>
/// Requires <c>Uno.UI.FeatureConfiguration.InputValidation.IsEnabled</c>, and a control whose type declares its
/// input through <c>Uno.UI.Xaml.Controls.InputValidationPropertyAttribute</c> or is registered in
/// <c>FeatureConfiguration.InputValidation.ValidationProperties</c> — TextBox, PasswordBox, ComboBox and
/// AutoSuggestBox are. No effect on Windows.
/// </remarks>
public static class Validation
{
	static Validation()
	{
#if HAS_UNO
		InputValidationProperties.Initialize(ModeProperty, KindProperty, ErrorTemplateProperty, HasErrorsProperty, ErrorsProperty);
#endif
	}

	#region Mode

	/// <summary>
	/// Whether the control participates in input validation. Defaults to <see cref="InputValidationMode.Disabled"/>.
	/// </summary>
	public static DependencyProperty ModeProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Mode",
			typeof(InputValidationMode),
			typeof(Validation),
			new PropertyMetadata(InputValidationMode.Disabled, OnModeChanged));

	public static InputValidationMode GetMode(Control control) => (InputValidationMode)control.GetValue(ModeProperty);

	public static void SetMode(Control control, InputValidationMode value) => control.SetValue(ModeProperty, value);

	private static void OnModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
#if HAS_UNO
		if (sender is Control control)
		{
			control.OnValidationModeChanged();
		}
#endif
	}

	#endregion

	#region Kind

	/// <summary>
	/// How the control presents its validation errors. Defaults to <see cref="InputValidationKind.Auto"/>.
	/// </summary>
	public static DependencyProperty KindProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Kind",
			typeof(InputValidationKind),
			typeof(Validation),
			new PropertyMetadata(InputValidationKind.Auto, OnKindChanged));

	public static InputValidationKind GetKind(Control control) => (InputValidationKind)control.GetValue(KindProperty);

	public static void SetKind(Control control, InputValidationKind value) => control.SetValue(KindProperty, value);

	private static void OnKindChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
#if HAS_UNO
		if (sender is Control control)
		{
			control.OnValidationKindChanged();
		}
#endif
	}

	#endregion

	#region ErrorTemplate

	/// <summary>
	/// The template used to present <see cref="ErrorsProperty"/>. Its root gets the control as DataContext.
	/// </summary>
	public static DependencyProperty ErrorTemplateProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ErrorTemplate",
			typeof(DataTemplate),
			typeof(Validation),
			new PropertyMetadata(null, OnErrorTemplateChanged));

	public static DataTemplate? GetErrorTemplate(Control control) => (DataTemplate?)control.GetValue(ErrorTemplateProperty);

	public static void SetErrorTemplate(Control control, DataTemplate? value) => control.SetValue(ErrorTemplateProperty, value);

	private static void OnErrorTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
#if HAS_UNO
		if (sender is Control control)
		{
			control.OnValidationErrorTemplateChanged();
		}
#endif
	}

	#endregion

	#region HasErrors

	/// <summary>
	/// Whether <see cref="ErrorsProperty"/> is non-empty. Written by the framework.
	/// </summary>
	public static DependencyProperty HasErrorsProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HasErrors",
			typeof(bool),
			typeof(Validation),
			new PropertyMetadata(false, OnHasErrorsChanged));

	public static bool GetHasErrors(Control control) => (bool)control.GetValue(HasErrorsProperty);

	private static void OnHasErrorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
#if HAS_UNO
		if (sender is Control control)
		{
			control.OnHasValidationErrorsChanged((bool)args.NewValue);
		}
#endif
	}

	#endregion

	#region Errors

	/// <summary>
	/// The errors currently reported for the control's input.
	/// </summary>
	/// <remarks>
	/// Created on first read and stable for the life of the control, so never null on Uno. Mutable: an
	/// application may add to it to report an error that does not come from the binding source.
	/// </remarks>
	public static DependencyProperty ErrorsProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Errors",
			typeof(IObservableVector<InputValidationError>),
			typeof(Validation),
			new PropertyMetadata(null));

	public static IObservableVector<InputValidationError>? GetErrors(Control control)
#if HAS_UNO
		=> control.GetOrCreateValidationErrors(ErrorsProperty);
#else
		=> (IObservableVector<InputValidationError>?)control.GetValue(ErrorsProperty);
#endif

	#endregion
}
