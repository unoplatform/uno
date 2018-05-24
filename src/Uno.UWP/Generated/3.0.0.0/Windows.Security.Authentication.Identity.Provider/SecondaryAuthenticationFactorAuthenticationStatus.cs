#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SecondaryAuthenticationFactorAuthenticationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Started,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownDevice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidAuthenticationStage,
		#endif
	}
	#endif
}
