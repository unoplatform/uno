#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ScrollEventType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallDecrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallIncrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LargeDecrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LargeIncrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThumbPosition,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThumbTrack,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		First,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Last,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EndScroll,
		#endif
	}
	#endif
}
