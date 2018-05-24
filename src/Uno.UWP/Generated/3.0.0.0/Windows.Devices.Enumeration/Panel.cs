#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum Panel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Front,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Back,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Top,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Left,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Right,
		#endif
	}
	#endif
}
