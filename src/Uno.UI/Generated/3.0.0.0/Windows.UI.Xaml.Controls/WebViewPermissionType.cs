#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebViewPermissionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Geolocation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnlimitedIndexedDBQuota,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Media,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerLock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WebNotifications,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Screen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImmersiveView,
		#endif
	}
	#endif
}
