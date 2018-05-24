#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CryptographicPrivateKeyBlobType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pkcs8RawPrivateKeyInfo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pkcs1RsaPrivateKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BCryptPrivateKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Capi1PrivateKey,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BCryptEccFullPrivateKey,
		#endif
	}
	#endif
}
