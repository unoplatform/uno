#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneNotificationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NewCall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CallChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LineChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneCallAudioEndpointChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneMuteChanged,
		#endif
	}
	#endif
}
