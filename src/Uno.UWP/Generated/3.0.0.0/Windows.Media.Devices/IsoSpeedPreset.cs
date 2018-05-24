#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum IsoSpeedPreset 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso50,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso80,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso100,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso200,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso400,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso800,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso1600,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso3200,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso6400,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso12800,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Iso25600,
		#endif
	}
	#endif
}
