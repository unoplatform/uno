#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CompositionStrokeCap 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Flat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Square,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Round,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Triangle,
		#endif
	}
	#endif
}
