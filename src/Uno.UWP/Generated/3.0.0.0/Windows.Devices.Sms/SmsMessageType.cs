#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsMessageType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Binary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Text,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		App,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Broadcast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Voicemail,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Status,
		#endif
	}
	#endif
}
