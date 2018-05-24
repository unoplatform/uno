#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WalletSummaryViewPosition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hidden,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Field1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Field2,
		#endif
	}
	#endif
}
