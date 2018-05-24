#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SecondaryAuthenticationFactorDevicePresenceMonitoringMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unsupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppManaged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemManaged,
		#endif
	}
	#endif
}
