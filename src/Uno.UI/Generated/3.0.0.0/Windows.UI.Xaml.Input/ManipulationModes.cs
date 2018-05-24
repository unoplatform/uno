#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ManipulationModes 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TranslateX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TranslateY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TranslateRailsX,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TranslateRailsY,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rotate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Scale,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TranslateInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RotateInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScaleInertia,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		System,
		#endif
	}
	#endif
}
