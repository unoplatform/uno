using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls;

partial class TextBox
{
#if __CROSSRUNTIME__
	/// <summary>
	/// Gets the value indicating whether the pointer capture on pointer press should be required.
	/// </summary>
	/// <remarks>
	/// This flag is only applied in case of WebAssembly.
	/// </remarks>
	public bool IsPointerCaptureRequired
	{
		get => (bool)GetValue(IsPointerCaptureRequiredProperty);
		set => SetValue(IsPointerCaptureRequiredProperty, value);
	}

	/// <summary>
	/// Identifies the IsPointerCaptureRequired attached property.
	/// </summary>
	public static DependencyProperty IsPointerCaptureRequiredProperty { get; } =
		DependencyProperty.Register(
			nameof(IsPointerCaptureRequired),
			typeof(bool),
			typeof(TextBox),
			new FrameworkPropertyMetadata(
				// We should not capture the pointer on WASM by default because it would prevent the user from scrolling through text on selection.
				// See https://github.com/unoplatform/uno/pull/16982, https://issues.chromium.org/issues/344491566
				false));
#endif
}
