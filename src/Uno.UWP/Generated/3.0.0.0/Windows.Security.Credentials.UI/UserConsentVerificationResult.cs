#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials.UI
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserConsentVerificationResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Verified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNotPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotConfiguredForUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceBusy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RetriesExhausted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
	}
	#endif
}
