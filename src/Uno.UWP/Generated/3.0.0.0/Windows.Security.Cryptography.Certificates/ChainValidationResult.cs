#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ChainValidationResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Untrusted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Revoked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IncompleteChain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidSignature,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WrongUsage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCertificateAuthorityPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BasicConstraintsError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownCriticalExtension,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RevocationInformationMissing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RevocationFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherErrors,
		#endif
	}
	#endif
}
