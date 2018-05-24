#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Road,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aerial,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AerialWithRoads,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Terrain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aerial3D,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aerial3DWithRoads,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
