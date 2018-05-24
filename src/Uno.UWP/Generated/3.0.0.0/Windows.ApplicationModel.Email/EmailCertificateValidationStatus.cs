#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EmailCertificateValidationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoMatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidUsage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCertificate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Revoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChainRevoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RevocationServerFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Untrusted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
	}
	#endif
}
