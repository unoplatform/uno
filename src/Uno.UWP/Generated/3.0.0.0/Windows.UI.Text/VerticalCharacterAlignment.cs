#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VerticalCharacterAlignment 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Top,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Baseline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bottom,
		#endif
	}
	#endif
}
