#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum EnrollKeyUsages 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Decryption,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Signing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyAgreement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
	}
	#endif
}
