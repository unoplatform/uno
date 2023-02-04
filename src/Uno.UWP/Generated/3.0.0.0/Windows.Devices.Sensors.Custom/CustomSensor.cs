#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CustomSensor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.ReportInterval is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CustomSensor.ReportInterval");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "uint CustomSensor.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CustomSensor.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CustomSensor.DeviceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MinimumReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.MinimumReportInterval is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CustomSensor.MinimumReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ReportLatency
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.ReportLatency is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CustomSensor.ReportLatency");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "uint CustomSensor.ReportLatency");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaxBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CustomSensor.MaxBatchSize is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20CustomSensor.MaxBatchSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Sensors.Custom.CustomSensorReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member CustomSensorReading CustomSensor.GetCurrentReading() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CustomSensorReading%20CustomSensor.GetCurrentReading%28%29");
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::System.Guid interfaceId)
		{
			throw new global::System.NotImplementedException("The member string CustomSensor.GetDeviceSelector(Guid interfaceId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CustomSensor.GetDeviceSelector%28Guid%20interfaceId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Sensors.Custom.CustomSensor> FromIdAsync( string sensorId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CustomSensor> CustomSensor.FromIdAsync(string sensorId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCustomSensor%3E%20CustomSensor.FromIdAsync%28string%20sensorId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.Custom.CustomSensor, global::Windows.Devices.Sensors.Custom.CustomSensorReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "event TypedEventHandler<CustomSensor, CustomSensorReadingChangedEventArgs> CustomSensor.ReadingChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Custom.CustomSensor", "event TypedEventHandler<CustomSensor, CustomSensorReadingChangedEventArgs> CustomSensor.ReadingChanged");
			}
		}
		#endif
	}
}
