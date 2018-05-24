#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UIColorType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Background,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Foreground,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentDark3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentDark2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentDark1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Accent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentLight1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentLight2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccentLight3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Complement,
		#endif
	}
	#endif
}
