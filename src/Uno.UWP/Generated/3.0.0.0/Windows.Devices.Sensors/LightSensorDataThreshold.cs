#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LightSensorDataThreshold 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float LuxPercentage
		{
			get
			{
				throw new global::System.NotImplementedException("The member float LightSensorDataThreshold.LuxPercentage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.LightSensorDataThreshold", "float LightSensorDataThreshold.LuxPercentage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float AbsoluteLux
		{
			get
			{
				throw new global::System.NotImplementedException("The member float LightSensorDataThreshold.AbsoluteLux is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.LightSensorDataThreshold", "float LightSensorDataThreshold.AbsoluteLux");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.LightSensorDataThreshold.LuxPercentage.get
		// Forced skipping of method Windows.Devices.Sensors.LightSensorDataThreshold.LuxPercentage.set
		// Forced skipping of method Windows.Devices.Sensors.LightSensorDataThreshold.AbsoluteLux.get
		// Forced skipping of method Windows.Devices.Sensors.LightSensorDataThreshold.AbsoluteLux.set
	}
}
