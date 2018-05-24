#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialInteractionPressKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Select,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Menu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Grasp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Touchpad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Thumbstick,
		#endif
	}
	#endif
}
