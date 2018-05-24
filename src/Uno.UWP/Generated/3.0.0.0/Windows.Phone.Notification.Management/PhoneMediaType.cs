#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhoneMediaType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideo,
		#endif
	}
	#endif
}
