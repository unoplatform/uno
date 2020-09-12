#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PenButtonListener 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool PenButtonListener.IsSupported() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.IsSupportedChanged.add
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.IsSupportedChanged.remove
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonClicked.add
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonClicked.remove
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonDoubleClicked.add
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonDoubleClicked.remove
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonLongPressed.add
		// Forced skipping of method Windows.Devices.Input.PenButtonListener.TailButtonLongPressed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Input.PenButtonListener GetDefault()
		{
			throw new global::System.NotImplementedException("The member PenButtonListener PenButtonListener.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.PenButtonListener, object> IsSupportedChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, object> PenButtonListener.IsSupportedChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, object> PenButtonListener.IsSupportedChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.PenButtonListener, global::Windows.Devices.Input.PenTailButtonClickedEventArgs> TailButtonClicked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonClickedEventArgs> PenButtonListener.TailButtonClicked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonClickedEventArgs> PenButtonListener.TailButtonClicked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.PenButtonListener, global::Windows.Devices.Input.PenTailButtonDoubleClickedEventArgs> TailButtonDoubleClicked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonDoubleClickedEventArgs> PenButtonListener.TailButtonDoubleClicked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonDoubleClickedEventArgs> PenButtonListener.TailButtonDoubleClicked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.PenButtonListener, global::Windows.Devices.Input.PenTailButtonLongPressedEventArgs> TailButtonLongPressed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonLongPressedEventArgs> PenButtonListener.TailButtonLongPressed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.PenButtonListener", "event TypedEventHandler<PenButtonListener, PenTailButtonLongPressedEventArgs> PenButtonListener.TailButtonLongPressed");
			}
		}
		#endif
	}
}
