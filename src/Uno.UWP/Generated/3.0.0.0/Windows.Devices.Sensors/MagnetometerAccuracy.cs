#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MagnetometerAccuracy 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unreliable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Approximate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		High,
		#endif
	}
	#endif
}
