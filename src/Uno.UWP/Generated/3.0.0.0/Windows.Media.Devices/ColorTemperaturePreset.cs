#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ColorTemperaturePreset 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manual,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cloudy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Daylight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Flash,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fluorescent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tungsten,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Candlelight,
		#endif
	}
	#endif
}
