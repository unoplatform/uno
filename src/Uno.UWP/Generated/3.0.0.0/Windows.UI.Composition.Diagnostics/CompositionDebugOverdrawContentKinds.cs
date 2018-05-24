#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CompositionDebugOverdrawContentKinds 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OffscreenRendered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Colors,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Effects,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Shadows,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lights,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Surfaces,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SwapChains,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
	}
	#endif
}
