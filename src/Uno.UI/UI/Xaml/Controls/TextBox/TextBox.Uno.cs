using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Contains additional attached properties that provide Uno-specific
/// behavior for TextBox control.
/// </summary>
public class TextBox
{
#if __CROSSRUNTIME__
	/// <summary>
	/// Gets the value indicating whether the pointer capture on pointer press should be required.
	/// </summary>
	/// <remarks>
	/// This flag is only applied in case of WebAssembly.
	/// </remarks>
	/// <param name="textBox">TextBox.</param>
	/// <returns>Value.</returns>
	public static bool GetIsPointerCaptureRequired(Microsoft.UI.Xaml.Controls.TextBox textBox) =>
		(bool)textBox.GetValue(IsPointerCaptureRequiredProperty);

	/// <summary>
	/// Sets the value indicating whether the pointer capture on pointer press should be required.
	/// </summary>
	/// <remarks>
	/// This flag is only applied in case of WebAssembly.
	/// </remarks>
	/// <param name="textBox">TextBox.</param>
	/// <param name="value">Value.</param>
	public static void SetIsPointerCaptureRequired(Microsoft.UI.Xaml.Controls.TextBox textBox, bool value) =>
		textBox.SetValue(IsPointerCaptureRequiredProperty, value);

	/// <summary>
	/// Identifies the IsPointerCaptureRequired attached property.
	/// </summary>
	public static DependencyProperty IsPointerCaptureRequiredProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsPointerCaptureRequired",
			typeof(bool),
			typeof(TextBox),
			new FrameworkPropertyMetadata(
				// We should not capture the pointer on WASM by default because it would prevent the user from scrolling through text on selection.
				// See https://github.com/unoplatform/uno/pull/16982, https://issues.chromium.org/issues/344491566
				false));
#endif
}
