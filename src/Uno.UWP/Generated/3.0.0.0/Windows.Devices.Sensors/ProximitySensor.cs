#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProximitySensor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProximitySensor.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint? MaxDistanceInMillimeters
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ProximitySensor.MaxDistanceInMillimeters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint? MinDistanceInMillimeters
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? ProximitySensor.MinDistanceInMillimeters is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensor.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensor.MaxDistanceInMillimeters.get
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensor.MinDistanceInMillimeters.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.ProximitySensorReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member ProximitySensorReading ProximitySensor.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensor.ReadingChanged.add
		// Forced skipping of method Windows.Devices.Sensors.ProximitySensor.ReadingChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.ProximitySensorDisplayOnOffController CreateDisplayOnOffController()
		{
			throw new global::System.NotImplementedException("The member ProximitySensorDisplayOnOffController ProximitySensor.CreateDisplayOnOffController() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Sensors.ProximitySensorReading> GetReadingsFromTriggerDetails( global::Windows.Devices.Sensors.SensorDataThresholdTriggerDetails triggerDetails)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ProximitySensorReading> ProximitySensor.GetReadingsFromTriggerDetails(SensorDataThresholdTriggerDetails triggerDetails) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string ProximitySensor.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Sensors.ProximitySensor FromId( string sensorId)
		{
			throw new global::System.NotImplementedException("The member ProximitySensor ProximitySensor.FromId(string sensorId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.ProximitySensor, global::Windows.Devices.Sensors.ProximitySensorReadingChangedEventArgs> ReadingChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.ProximitySensor", "event TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs> ProximitySensor.ReadingChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.ProximitySensor", "event TypedEventHandler<ProximitySensor, ProximitySensorReadingChangedEventArgs> ProximitySensor.ReadingChanged");
			}
		}
		#endif
	}
}
