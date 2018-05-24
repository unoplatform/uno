#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ActivityType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Idle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stationary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fidgeting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Walking,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Running,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InVehicle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Biking,
		#endif
	}
	#endif
}
