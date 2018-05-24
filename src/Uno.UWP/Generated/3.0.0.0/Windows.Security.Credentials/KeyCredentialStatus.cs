#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum KeyCredentialStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserCanceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserPrefersPassword,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CredentialAlreadyExists,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecurityDeviceLocked,
		#endif
	}
	#endif
}
