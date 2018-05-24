#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PenLineJoin 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Miter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bevel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Round,
		#endif
	}
	#endif
}
