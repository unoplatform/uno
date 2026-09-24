#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Routes the validation errors of a binding source to the control that bound to it.
/// </summary>
/// <remarks>
/// Uno only, and plumbing rather than API: the surface an application uses is
/// <see cref="IInputValidationControl"/> on the control itself. Requires
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.IsEnabled"/> to be set before the first binding is
/// registered, and the control's type to declare an <see cref="InputValidationPropertyAttribute"/> or to be
/// registered in <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/>.
/// </remarks>
public static partial class Validation
{
	/// <summary>
	/// Resolves one of the validation dependency properties on <paramref name="control"/>'s own type.
	/// </summary>
	/// <remarks>
	/// The stand-in for WinUI's <c>GetTargetHasErrorsProperty</c> / <c>GetTargetErrorsProperty</c>, which
	/// switch on a type index over a closed set of four controls. Resolving by name instead keeps
	/// third-party controls working, and costs nothing per call: <see cref="DependencyProperty.GetProperty"/>
	/// is already memoized, and it walks the base-type chain, so <c>CheckBox</c> finds what
	/// <c>ToggleButton</c> registered.
	/// </remarks>
	private static DependencyProperty? GetTargetProperty(Control control, string name)
		=> DependencyProperty.GetProperty(control.GetType(), name);

	/// <summary>
	/// Whether <paramref name="control"/> participates in input validation.
	/// </summary>
	/// <remarks>
	/// Mirrors <c>CControl::IsValidationEnabled</c>: every mode but <see cref="InputValidationMode.Disabled"/>
	/// counts as enabled, and a control that does not implement the interface is never enabled — which is
	/// what WinUI's type-index switch expresses by returning an unknown property index.
	/// </remarks>
	internal static bool IsValidationEnabled(Control control)
		=> control is IInputValidationControl { InputValidationMode: not InputValidationMode.Disabled };

	internal static void SetHasErrors(Control control, bool value)
	{
		if (GetTargetProperty(control, nameof(IInputValidationControl.HasValidationErrors)) is { } property)
		{
			control.SetValue(property, value);
		}
	}

	/// <summary>
	/// The errors of <paramref name="control"/>, or null when it has never reported one — so that a control
	/// which never reports an error never allocates a collection. Mirrors WinUI reading the equivalent
	/// property with <c>CheckOnDemandProperty</c>, which deliberately does not force-create.
	/// </summary>
	internal static IObservableVector<InputValidationError>? TryGetErrors(Control control)
		=> GetTargetProperty(control, nameof(IInputValidationControl.ValidationErrors)) is { } property
			? control.GetValue(property) as IObservableVector<InputValidationError>
			: null;

	/// <summary>
	/// Backs <see cref="IInputValidationControl.ValidationErrors"/> for every participating control: the
	/// collection is created on first read, and its identity then stays stable for the life of the control.
	/// </summary>
	internal static IObservableVector<InputValidationError> GetOrCreateErrors(Control control, DependencyProperty property)
	{
		if (control.GetValue(property) is not ValidationErrorsCollection errors)
		{
			errors = new ValidationErrorsCollection();
			control.SetValue(property, errors);
		}

		return errors;
	}
}
