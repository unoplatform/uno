#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ToastTemplateType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastImageAndText01,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastImageAndText02,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastImageAndText03,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastImageAndText04,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastText01,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastText02,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastText03,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToastText04,
		#endif
	}
	#endif
}
