#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MagnetometerDataThreshold 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float ZAxisMicroteslas
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerDataThreshold.ZAxisMicroteslas is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.MagnetometerDataThreshold", "float MagnetometerDataThreshold.ZAxisMicroteslas");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float YAxisMicroteslas
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerDataThreshold.YAxisMicroteslas is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.MagnetometerDataThreshold", "float MagnetometerDataThreshold.YAxisMicroteslas");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float XAxisMicroteslas
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerDataThreshold.XAxisMicroteslas is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.MagnetometerDataThreshold", "float MagnetometerDataThreshold.XAxisMicroteslas");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.XAxisMicroteslas.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.XAxisMicroteslas.set
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.YAxisMicroteslas.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.YAxisMicroteslas.set
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.ZAxisMicroteslas.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerDataThreshold.ZAxisMicroteslas.set
	}
}
