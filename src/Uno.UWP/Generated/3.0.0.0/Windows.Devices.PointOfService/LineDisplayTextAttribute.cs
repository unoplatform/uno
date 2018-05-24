#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LineDisplayTextAttribute 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Blink,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reverse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReverseBlink,
		#endif
	}
	#endif
}
