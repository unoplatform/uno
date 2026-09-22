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
/// first binding is registered, and the control's type to declare a <see cref="InputValidationPropertyAttribute"/>.
/// <para>
/// This slice ships no visuals: <see cref="HasErrorsProperty"/> and <see cref="ErrorsProperty"/> are a read
/// model for the application's own markup to bind to.
/// </para>
/// </remarks>
public static partial class Validation
{
	/// <summary>
	/// Whether the control participates in input validation. Defaults to false — validation is opt-in, per
	/// control.
	/// </summary>
	public static DependencyProperty IsEnabledProperty
	{
		[DynamicDependency(nameof(GetIsEnabled))]
		[DynamicDependency(nameof(SetIsEnabled))]
		get;
	} = DependencyProperty.RegisterAttached(
		"IsEnabled",
		typeof(bool),
		typeof(Validation),
		new FrameworkPropertyMetadata(default(bool), OnIsEnabledChanged));

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
	/// Gets whether <paramref name="control"/> participates in input validation.
	/// </summary>
	public static bool GetIsEnabled(Control control) => (bool)control.GetValue(IsEnabledProperty);

	/// <summary>
	/// Sets whether <paramref name="control"/> participates in input validation.
	/// </summary>
	public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

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
