#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlayToConnectionError 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNotResponding,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceLocked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtectedPlaybackFailed,
		#endif
	}
	#endif
}
