using Windows.Foundation;

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public
#else
internal
#endif
	partial class InputFocusController
#if HAS_UNO_WINUI
	: global::Microsoft.UI.Input.InputObject
#endif
{
	internal InputFocusController()
	{

	}

	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public FocusNavigationResult DepartFocus(FocusNavigationRequest request) => FocusNavigationResult.NotMoved;

#pragma warning disable CS0067 // Unused members
	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> GotFocus;

	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> LostFocus;
#pragma warning restore CS0067

	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	public bool HasFocus => false;
}
