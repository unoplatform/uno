#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyboardDeliveryInterceptor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInterceptionEnabledWhenInForeground
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool KeyboardDeliveryInterceptor.IsInterceptionEnabledWhenInForeground is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.KeyboardDeliveryInterceptor", "bool KeyboardDeliveryInterceptor.IsInterceptionEnabledWhenInForeground");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.IsInterceptionEnabledWhenInForeground.get
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.IsInterceptionEnabledWhenInForeground.set
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.KeyDown.add
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.KeyDown.remove
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.KeyUp.add
		// Forced skipping of method Windows.UI.Input.KeyboardDeliveryInterceptor.KeyUp.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.KeyboardDeliveryInterceptor GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member KeyboardDeliveryInterceptor KeyboardDeliveryInterceptor.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.KeyboardDeliveryInterceptor, global::Windows.UI.Core.KeyEventArgs> KeyDown
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.KeyboardDeliveryInterceptor", "event TypedEventHandler<KeyboardDeliveryInterceptor, KeyEventArgs> KeyboardDeliveryInterceptor.KeyDown");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.KeyboardDeliveryInterceptor", "event TypedEventHandler<KeyboardDeliveryInterceptor, KeyEventArgs> KeyboardDeliveryInterceptor.KeyDown");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.KeyboardDeliveryInterceptor, global::Windows.UI.Core.KeyEventArgs> KeyUp
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.KeyboardDeliveryInterceptor", "event TypedEventHandler<KeyboardDeliveryInterceptor, KeyEventArgs> KeyboardDeliveryInterceptor.KeyUp");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.KeyboardDeliveryInterceptor", "event TypedEventHandler<KeyboardDeliveryInterceptor, KeyEventArgs> KeyboardDeliveryInterceptor.KeyUp");
			}
		}
		#endif
	}
}
