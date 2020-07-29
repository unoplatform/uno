#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SimpleOrientationSensor 
	{
		// Skipping already declared property ReadingTransform
		// Skipping already declared property DeviceId
		// Skipping already declared method Windows.Devices.Sensors.SimpleOrientationSensor.GetCurrentOrientation()
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.OrientationChanged.add
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.OrientationChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.ReadingTransform.set
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.ReadingTransform.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string SimpleOrientationSensor.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.SimpleOrientationSensor> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SimpleOrientationSensor> SimpleOrientationSensor.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Devices.Sensors.SimpleOrientationSensor.GetDefault()
		// Skipping already declared event Windows.Devices.Sensors.SimpleOrientationSensor.OrientationChanged
	}
}
