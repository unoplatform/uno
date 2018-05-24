#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SecondaryAuthenticationFactorDeviceCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecureStorage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StoreKeys,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConfirmUserIntentToAuthenticate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SupportSecureUserPresenceCheck,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransmittedDataIsEncrypted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HMacSha256,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CloseRangeDataTransmission,
		#endif
	}
	#endif
}
