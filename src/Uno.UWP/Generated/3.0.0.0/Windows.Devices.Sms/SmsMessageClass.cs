#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsMessageClass 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Class0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Class1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Class2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Class3,
		#endif
	}
	#endif
}
