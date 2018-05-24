#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RadialControllerMenuKnownIcon 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Scroll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Zoom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UndoRedo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Volume,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NextPreviousTrack,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ruler,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InkColor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InkThickness,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PenType,
		#endif
	}
	#endif
}
