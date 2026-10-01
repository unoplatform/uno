#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// The attached properties of <c>Uno.Extras.Input.Validation</c>, which Uno.UI.Extras owns and hands over
/// from its static constructor so that the engine here can read and write them.
/// </summary>
/// <remarks>
/// Null until that class is first touched, which is also before any control can have opted in: its Mode
/// defaults to Disabled.
/// </remarks>
internal static class InputValidationProperties
{
	internal static DependencyProperty? ModeProperty { get; private set; }

	internal static DependencyProperty? KindProperty { get; private set; }

	internal static DependencyProperty? ErrorTemplateProperty { get; private set; }

	internal static DependencyProperty? HasErrorsProperty { get; private set; }

	internal static DependencyProperty? ErrorsProperty { get; private set; }

	internal static void Initialize(
		DependencyProperty mode,
		DependencyProperty kind,
		DependencyProperty errorTemplate,
		DependencyProperty hasErrors,
		DependencyProperty errors)
	{
		ModeProperty = mode;
		KindProperty = kind;
		ErrorTemplateProperty = errorTemplate;
		HasErrorsProperty = hasErrors;
		ErrorsProperty = errors;
	}
}
