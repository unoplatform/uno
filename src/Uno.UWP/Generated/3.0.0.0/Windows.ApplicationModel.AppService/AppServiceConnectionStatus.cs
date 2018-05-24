#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppServiceConnectionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppNotInstalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppServiceUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteSystemUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteSystemNotSupportedByApp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAuthorized,
		#endif
	}
	#endif
}
