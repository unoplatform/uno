#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDCertificateType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PC,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Device,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Domain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Issuer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CrlSigner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Service,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Silverlight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Application,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Metering,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyFileSigner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Server,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LicenseSigner,
		#endif
	}
	#endif
}
