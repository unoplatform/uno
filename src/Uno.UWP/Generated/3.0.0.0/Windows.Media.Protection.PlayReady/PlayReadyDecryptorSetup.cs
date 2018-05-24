#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlayReadyDecryptorSetup 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uninitialized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnDemand,
		#endif
	}
	#endif
}
