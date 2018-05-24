#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.DialProtocol
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DialAppStopResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StopFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkFailure,
		#endif
	}
	#endif
}
