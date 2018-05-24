#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RemoteLaunchUriStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtocolUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteSystemUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ValueSetTooLarge,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedByLocalSystem,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedByRemoteSystem,
		#endif
	}
	#endif
}
