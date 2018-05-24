#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CryptographicPublicKeyBlobType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X509SubjectPublicKeyInfo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pkcs1RsaPublicKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BCryptPublicKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Capi1PublicKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BCryptEccFullPublicKey,
		#endif
	}
	#endif
}
