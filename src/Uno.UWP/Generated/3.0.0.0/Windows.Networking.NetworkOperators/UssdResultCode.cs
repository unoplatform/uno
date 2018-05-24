#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UssdResultCode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoActionRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActionRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Terminated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherLocalClient,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkTimeout,
		#endif
	}
	#endif
}
