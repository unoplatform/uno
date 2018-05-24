#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GazeDeviceConfigurationStatePreview 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ready,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Configuring,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScreenSetupNeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserCalibrationNeeded,
		#endif
	}
	#endif
}
