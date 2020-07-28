#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProximitySensorDataThreshold : global::Windows.Devices.Sensors.ISensorDataThreshold
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ProximitySensorDataThreshold( global::Windows.Devices.Sensors.ProximitySensor sensor) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.ProximitySensorDataThreshold", "ProximitySensorDataThreshold.ProximitySensorDataThreshold(ProximitySensor sensor)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensorDataThreshold.ProximitySensorDataThreshold(Windows.Devices.Sensors.ProximitySensor)
		// Processing: Windows.Devices.Sensors.ISensorDataThreshold
	}
}
