#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Magnetometer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Magnetometer.ReportInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Magnetometer", "uint Magnetometer.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Magnetometer.MinimumReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Graphics.Display.DisplayOrientations ReadingTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayOrientations Magnetometer.ReadingTransform is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Magnetometer", "DisplayOrientations Magnetometer.ReadingTransform");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Magnetometer.ReportLatency is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Magnetometer", "uint Magnetometer.ReportLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MaxBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Magnetometer.MaxBatchSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Magnetometer.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.MagnetometerReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member MagnetometerReading Magnetometer.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.MinimumReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReportInterval.set
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReadingChanged.add
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReadingChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReadingTransform.set
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReadingTransform.get
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReportLatency.set
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.ReportLatency.get
		// Forced skipping of method Windows.Devices.Sensors.Magnetometer.MaxBatchSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string Magnetometer.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.Magnetometer> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Magnetometer> Magnetometer.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Sensors.Magnetometer GetDefault()
		{
			throw new global::System.NotImplementedException("The member Magnetometer Magnetometer.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.Magnetometer, global::Windows.Devices.Sensors.MagnetometerReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Magnetometer", "event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> Magnetometer.ReadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Magnetometer", "event TypedEventHandler<Magnetometer, MagnetometerReadingChangedEventArgs> Magnetometer.ReadingChanged");
			}
		}
		#endif
	}
}
