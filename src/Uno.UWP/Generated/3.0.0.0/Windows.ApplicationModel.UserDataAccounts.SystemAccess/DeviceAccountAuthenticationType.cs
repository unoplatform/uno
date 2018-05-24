#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.SystemAccess
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceAccountAuthenticationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Basic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OAuth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SingleSignOn,
		#endif
	}
	#endif
}
