#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsFilterActionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AcceptImmediately,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Drop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Peek,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Accept,
		#endif
	}
	#endif
}
