#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SignatureValidationResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidParameter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BadMessage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidSignature,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherErrors,
		#endif
	}
	#endif
}
