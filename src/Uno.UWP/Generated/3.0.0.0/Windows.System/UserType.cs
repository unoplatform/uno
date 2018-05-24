#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LocalUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LocalGuest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteGuest,
		#endif
	}
	#endif
}
