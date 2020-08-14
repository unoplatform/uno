#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NoFocusCandidateFoundEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool NoFocusCandidateFoundEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs", "bool NoFocusCandidateFoundEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Input.FocusNavigationDirection Direction
		{
			get
			{
				throw new global::System.NotImplementedException("The member FocusNavigationDirection NoFocusCandidateFoundEventArgs.Direction is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Input.FocusInputDeviceKind InputDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member FocusInputDeviceKind NoFocusCandidateFoundEventArgs.InputDevice is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs.Direction.get
		// Forced skipping of method Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Input.NoFocusCandidateFoundEventArgs.InputDevice.get
	}
}
