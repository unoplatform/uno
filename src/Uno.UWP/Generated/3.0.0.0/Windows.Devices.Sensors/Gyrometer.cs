#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Gyrometer 
	{
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Gyrometer.ReportInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Gyrometer", "uint Gyrometer.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Gyrometer.MinimumReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Graphics.Display.DisplayOrientations ReadingTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayOrientations Gyrometer.ReadingTransform is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Gyrometer", "DisplayOrientations Gyrometer.ReadingTransform");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Gyrometer.ReportLatency is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Gyrometer", "uint Gyrometer.ReportLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MaxBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Gyrometer.MaxBatchSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Gyrometer.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.GyrometerReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member GyrometerReading Gyrometer.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.MinimumReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReportInterval.set
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReadingChanged.add
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReadingChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReadingTransform.set
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReadingTransform.get
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReportLatency.set
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.ReportLatency.get
		// Forced skipping of method Windows.Devices.Sensors.Gyrometer.MaxBatchSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string Gyrometer.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.Gyrometer> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Gyrometer> Gyrometer.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Sensors.Gyrometer GetDefault()
		{
			throw new global::System.NotImplementedException("The member Gyrometer Gyrometer.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.Gyrometer, global::Windows.Devices.Sensors.GyrometerReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Gyrometer", "event TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> Gyrometer.ReadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Gyrometer", "event TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> Gyrometer.ReadingChanged");
			}
		}
		#endif
	}
}
