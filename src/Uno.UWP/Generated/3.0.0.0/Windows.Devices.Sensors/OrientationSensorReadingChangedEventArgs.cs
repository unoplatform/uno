#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OrientationSensorReadingChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sensors.OrientationSensorReading Reading
		{
			get
			{
				throw new global::System.NotImplementedException("The member OrientationSensorReading OrientationSensorReadingChangedEventArgs.Reading is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.OrientationSensorReadingChangedEventArgs.Reading.get
	}
}
