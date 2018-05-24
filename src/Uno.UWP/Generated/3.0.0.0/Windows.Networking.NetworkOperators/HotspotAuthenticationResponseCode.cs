#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HotspotAuthenticationResponseCode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoginSucceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoginFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RadiusServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkAdministratorError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoginAborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessGatewayInternalError,
		#endif
	}
	#endif
}
