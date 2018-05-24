#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WalletBarcodeSymbology 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Upca,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Upce,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ean13,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ean8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Itf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Code39,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Code128,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Qr,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pdf417,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aztec,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
