#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PedometerDataThreshold : global::Windows.Devices.Sensors.ISensorDataThreshold
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PedometerDataThreshold( global::Windows.Devices.Sensors.Pedometer sensor,  int stepGoal) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.PedometerDataThreshold", "PedometerDataThreshold.PedometerDataThreshold(Pedometer sensor, int stepGoal)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.PedometerDataThreshold.PedometerDataThreshold(Windows.Devices.Sensors.Pedometer, int)
		// Processing: Windows.Devices.Sensors.ISensorDataThreshold
	}
}
