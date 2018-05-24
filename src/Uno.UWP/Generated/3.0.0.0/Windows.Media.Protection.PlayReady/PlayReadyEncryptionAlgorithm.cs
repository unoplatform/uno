#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlayReadyEncryptionAlgorithm 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unprotected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aes128Ctr,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cocktail,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aes128Cbc,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uninitialized,
		#endif
	}
	#endif
}
