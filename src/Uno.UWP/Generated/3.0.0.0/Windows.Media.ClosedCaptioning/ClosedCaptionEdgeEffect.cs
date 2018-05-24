#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ClosedCaptioning
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ClosedCaptionEdgeEffect 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Raised,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Depressed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uniform,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DropShadow,
		#endif
	}
	#endif
}
