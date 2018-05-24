#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AdvancedColorKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StandardDynamicRange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WideColorGamut,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighDynamicRange,
		#endif
	}
	#endif
}
