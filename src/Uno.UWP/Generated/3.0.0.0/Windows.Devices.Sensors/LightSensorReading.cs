#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class LightSensorReading 
	{
		// Skipping already declared property IlluminanceInLux
		// Skipping already declared property Timestamp
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? PerformanceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? LightSensorReading.PerformanceCount is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%3F%20LightSensorReading.PerformanceCount");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> LightSensorReading.Properties is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20object%3E%20LightSensorReading.Properties");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.LightSensorReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.LightSensorReading.IlluminanceInLux.get
		// Forced skipping of method Windows.Devices.Sensors.LightSensorReading.PerformanceCount.get
		// Forced skipping of method Windows.Devices.Sensors.LightSensorReading.Properties.get
	}
}
