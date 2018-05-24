#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Cortana
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CortanaPermission 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BrowsingHistory,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Calendar,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CallHistory,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Contacts,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Email,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InputPersonalization,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Location,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Messaging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Microphone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Personalization,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneCall,
		#endif
	}
	#endif
}
