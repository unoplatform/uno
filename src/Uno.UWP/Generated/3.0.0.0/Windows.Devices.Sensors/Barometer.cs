#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || false || false || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Barometer 
	{
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Barometer.ReportInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "uint Barometer.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Barometer.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Barometer.MinimumReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ReportLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Barometer.ReportLatency is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "uint Barometer.ReportLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Barometer.MaxBatchSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sensors.BarometerDataThreshold ReportThreshold
		{
			get
			{
				throw new global::System.NotImplementedException("The member BarometerDataThreshold Barometer.ReportThreshold is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sensors.BarometerReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member BarometerReading Barometer.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.Barometer.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.Barometer.MinimumReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReportInterval.set
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReadingChanged.add
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReadingChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReportLatency.set
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReportLatency.get
		// Forced skipping of method Windows.Devices.Sensors.Barometer.MaxBatchSize.get
		// Forced skipping of method Windows.Devices.Sensors.Barometer.ReportThreshold.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.Barometer> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Barometer> Barometer.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string Barometer.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Windows.Devices.Sensors.Barometer GetDefault()
		{
			throw new global::System.NotImplementedException("The member Barometer Barometer.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.Barometer, global::Windows.Devices.Sensors.BarometerReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
			}
			[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
			}
		}
		#endif
	}
}
