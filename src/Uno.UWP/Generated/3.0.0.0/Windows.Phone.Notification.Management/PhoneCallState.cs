#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneCallState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ringing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Talking,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Held,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ended,
		#endif
	}
	#endif
}
