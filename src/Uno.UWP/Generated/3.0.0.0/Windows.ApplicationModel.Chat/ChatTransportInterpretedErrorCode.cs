#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChatTransportInterpretedErrorCode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidRecipientAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkConnectivity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Timeout,
		#endif
	}
	#endif
}
