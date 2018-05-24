#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GestureSettings 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoubleTap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HoldWithMouse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightTap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Drag,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslateX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslateY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslateRailsX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslateRailsY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationRotate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationScale,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationTranslateInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationRotateInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationScaleInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CrossSlide,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManipulationMultipleFingerPanning,
		#endif
	}
	#endif
}
