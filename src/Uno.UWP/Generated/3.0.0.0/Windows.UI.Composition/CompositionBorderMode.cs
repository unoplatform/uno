#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CompositionBorderMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inherit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Soft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hard,
		#endif
	}
	#endif
}
