#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UnifiedPosErrorReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownErrorReason,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoService,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Illegal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoHardware,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Offline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Timeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Busy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Extended,
		#endif
	}
	#endif
}
