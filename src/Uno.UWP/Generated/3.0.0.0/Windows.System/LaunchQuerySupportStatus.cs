#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LaunchQuerySupportStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Available,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppNotInstalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}
