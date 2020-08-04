#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompassReading 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double HeadingMagneticNorth
		{
			get
			{
				throw new global::System.NotImplementedException("The member double CompassReading.HeadingMagneticNorth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double? HeadingTrueNorth
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? CompassReading.HeadingTrueNorth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset CompassReading.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? PerformanceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? CompassReading.PerformanceCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> CompassReading.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sensors.MagnetometerAccuracy HeadingAccuracy
		{
			get
			{
				throw new global::System.NotImplementedException("The member MagnetometerAccuracy CompassReading.HeadingAccuracy is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.HeadingMagneticNorth.get
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.HeadingTrueNorth.get
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.HeadingAccuracy.get
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.PerformanceCount.get
		// Forced skipping of method Windows.Devices.Sensors.CompassReading.Properties.get
	}
}
