#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SensorType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Accelerometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActivitySensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Barometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Compass,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CustomSensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gyroscope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProximitySensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inclinometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LightSensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OrientationSensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pedometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RelativeInclinometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RelativeOrientationSensor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimpleOrientationSensor,
		#endif
	}
	#endif
}
