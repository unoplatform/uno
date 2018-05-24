#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum KeyProtectionLevel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoConsent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConsentOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConsentWithPassword,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConsentWithFingerprint,
		#endif
	}
	#endif
}
