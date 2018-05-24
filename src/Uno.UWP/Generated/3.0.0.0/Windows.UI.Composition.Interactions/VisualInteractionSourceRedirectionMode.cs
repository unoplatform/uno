#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition.Interactions
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VisualInteractionSourceRedirectionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Off,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CapableTouchpadOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PointerWheelOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CapableTouchpadAndPointerWheel,
		#endif
	}
	#endif
}
