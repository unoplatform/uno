#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MagnetometerReading 
	{
		// Skipping already declared property DirectionalAccuracy
		// Skipping already declared property MagneticFieldX
		// Skipping already declared property MagneticFieldY
		// Skipping already declared property MagneticFieldZ
		// Skipping already declared property Timestamp
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? PerformanceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MagnetometerReading.PerformanceCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> MagnetometerReading.Properties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldX.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldY.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldZ.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.DirectionalAccuracy.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.PerformanceCount.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.Properties.get
	}
}
