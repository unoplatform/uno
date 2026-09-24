#nullable enable

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Exposes the validation state of a control whose validation property is bound to an
/// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> source.
/// </summary>
/// <remarks>
/// Uno only. Requires <see cref="Uno.UI.FeatureConfiguration.InputValidation.IsEnabled"/> to be set before the
/// first binding is registered, and the control's type to declare a <see cref="InputValidationPropertyAttribute"/>
/// or to be registered in <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/>.
/// <para>
/// <see cref="HasErrorsProperty"/> and <see cref="ErrorsProperty"/> are a read model for the application's own
/// markup to bind to. The built-in visuals are driven from the same values by
/// <c>Control.UpdateValidationStates</c>.
/// </para>
/// </remarks>
public static partial class Validation
{
	/// <summary>
	/// Whether the control participates in input validation, and how its errors are presented. Defaults to
	/// <see cref="InputValidationMode.Disabled"/> — validation is opt-in, per control.
	/// </summary>
	/// <remarks>
	/// Unlike WinUI, where this property only gates the visuals, in Uno it also gates the subscription to the
	/// binding source, which is why it defaults to disabled rather than to the enum's zero value.
	/// </remarks>
	public static DependencyProperty InputValidationModeProperty
	{
		[DynamicDependency(nameof(GetInputValidationMode))]
		[DynamicDependency(nameof(SetInputValidationMode))]
		get;
	} = DependencyProperty.RegisterAttached(
		"InputValidationMode",
		typeof(InputValidationMode),
		typeof(Validation),
		new FrameworkPropertyMetadata(InputValidationMode.Disabled, OnInputValidationModeChanged));

	/// <summary>
	/// How the control presents its validation errors. Defaults to <see cref="InputValidationKind.Auto"/>,
	/// which behaves as <see cref="InputValidationKind.Compact"/>.
	/// </summary>
	public static DependencyProperty InputValidationKindProperty
	{
		[DynamicDependency(nameof(GetInputValidationKind))]
		[DynamicDependency(nameof(SetInputValidationKind))]
		get;
	} = DependencyProperty.RegisterAttached(
		"InputValidationKind",
		typeof(InputValidationKind),
		typeof(Validation),
		new FrameworkPropertyMetadata(InputValidationKind.Auto));

	/// <summary>
	/// Whether the control's binding source currently reports errors for the bound property.
	/// </summary>
	/// <remarks>Written by the framework; setting it from application code has no meaningful effect.</remarks>
	public static DependencyProperty HasErrorsProperty
	{
		[DynamicDependency(nameof(GetHasErrors))]
		[DynamicDependency(nameof(SetHasErrors))]
		get;
	} = DependencyProperty.RegisterAttached(
		"HasErrors",
		typeof(bool),
		typeof(Validation),
		new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// The errors reported by the control's binding source for the bound property, as returned by
	/// <see cref="System.ComponentModel.INotifyDataErrorInfo.GetErrors"/>.
	/// </summary>
	/// <remarks>
	/// The element type is whatever the source produced — this slice applies no policy to it, and ships no
	/// converter to render it. A fresh instance is assigned on every synchronization, so that a binding to
	/// this property is notified even when the source reuses its error collection.
	/// </remarks>
	public static DependencyProperty ErrorsProperty
	{
		[DynamicDependency(nameof(GetErrors))]
		[DynamicDependency(nameof(SetErrors))]
		get;
	} = DependencyProperty.RegisterAttached(
		"Errors",
		typeof(IEnumerable),
		typeof(Validation),
		new FrameworkPropertyMetadata(Array.Empty<object>()));

	/// <summary>
	/// Gets whether <paramref name="control"/> participates in input validation, and how its errors are presented.
	/// </summary>
	public static InputValidationMode GetInputValidationMode(Control control)
		=> (InputValidationMode)control.GetValue(InputValidationModeProperty);

	/// <summary>
	/// Sets whether <paramref name="control"/> participates in input validation, and how its errors are presented.
	/// </summary>
	public static void SetInputValidationMode(Control control, InputValidationMode value)
		=> control.SetValue(InputValidationModeProperty, value);

	/// <summary>
	/// Gets how <paramref name="control"/> presents its validation errors.
	/// </summary>
	public static InputValidationKind GetInputValidationKind(Control control)
		=> (InputValidationKind)control.GetValue(InputValidationKindProperty);

	/// <summary>
	/// Sets how <paramref name="control"/> presents its validation errors.
	/// </summary>
	public static void SetInputValidationKind(Control control, InputValidationKind value)
		=> control.SetValue(InputValidationKindProperty, value);

	/// <summary>
	/// Whether <paramref name="control"/> participates in input validation.
	/// </summary>
	/// <remarks>
	/// Mirrors <c>CControl::IsValidationEnabled</c>: every mode but <see cref="InputValidationMode.Disabled"/>
	/// counts as enabled.
	/// </remarks>
	internal static bool IsValidationEnabled(Control control)
		=> GetInputValidationMode(control) != InputValidationMode.Disabled;

	/// <summary>
	/// Gets whether <paramref name="control"/>'s binding source currently reports errors.
	/// </summary>
	public static bool GetHasErrors(Control control) => (bool)control.GetValue(HasErrorsProperty);

	internal static void SetHasErrors(Control control, bool value) => control.SetValue(HasErrorsProperty, value);

	/// <summary>
	/// Gets the errors reported by <paramref name="control"/>'s binding source.
	/// </summary>
	public static IEnumerable GetErrors(Control control) => (IEnumerable)control.GetValue(ErrorsProperty);

	internal static void SetErrors(Control control, IEnumerable value) => control.SetValue(ErrorsProperty, value);
}
