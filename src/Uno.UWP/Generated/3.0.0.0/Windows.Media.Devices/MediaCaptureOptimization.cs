#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaCaptureOptimization 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Quality,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Latency,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Power,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LatencyThenQuality,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LatencyThenPower,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PowerAndQuality,
		#endif
	}
	#endif
}
