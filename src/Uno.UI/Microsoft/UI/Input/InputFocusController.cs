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
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public FocusNavigationResult DepartFocus(FocusNavigationRequest request) => FocusNavigationResult.NotMoved;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> GotFocus;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> LostFocus;

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool HasFocus => false;
}
