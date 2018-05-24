#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneCallTransport 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cellular,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Voip,
		#endif
	}
	#endif
}
