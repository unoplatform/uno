#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CustomSensor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.ReportInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "uint CustomSensor.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CustomSensor.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.MinimumReportInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.ReportLatency is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "uint CustomSensor.ReportLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MaxBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.MaxBatchSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.Custom.CustomSensorReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member CustomSensorReading CustomSensor.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.MinimumReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReportInterval.set
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReportInterval.get
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReadingChanged.add
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReadingChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReportLatency.set
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.ReportLatency.get
		// Forced skipping of method Windows.Devices.Sensors.Custom.CustomSensor.MaxBatchSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector( global::System.Guid interfaceId)
		{
			throw new global::System.NotImplementedException("The member string CustomSensor.GetDeviceSelector(Guid interfaceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.Custom.CustomSensor> FromIdAsync( string sensorId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CustomSensor> CustomSensor.FromIdAsync(string sensorId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.Custom.CustomSensor, global::Windows.Devices.Sensors.Custom.CustomSensorReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "event TypedEventHandler<CustomSensor, CustomSensorReadingChangedEventArgs> CustomSensor.ReadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "event TypedEventHandler<CustomSensor, CustomSensorReadingChangedEventArgs> CustomSensor.ReadingChanged");
			}
		}
		#endif
	}
}
