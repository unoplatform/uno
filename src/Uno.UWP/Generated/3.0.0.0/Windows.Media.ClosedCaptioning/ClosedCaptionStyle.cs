#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.ClosedCaptioning
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ClosedCaptionStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MonospacedWithSerifs,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProportionalWithSerifs,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MonospacedWithoutSerifs,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProportionalWithoutSerifs,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Casual,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cursive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallCapitals,
		#endif
	}
	#endif
}
