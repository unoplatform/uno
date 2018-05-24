#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDCertificateFeature 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Transmitter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Receiver,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SharedCertificate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecureClock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AntiRollBackClock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CRLS,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PlayReady3Features,
		#endif
	}
	#endif
}
