#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppServiceResponseStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceLimitsExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteSystemUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MessageSizeTooLarge,
		#endif
	}
	#endif
}
