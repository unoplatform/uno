#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GyrometerDataThreshold 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double ZAxisInDegreesPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerDataThreshold.ZAxisInDegreesPerSecond is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.GyrometerDataThreshold", "double GyrometerDataThreshold.ZAxisInDegreesPerSecond");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double YAxisInDegreesPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerDataThreshold.YAxisInDegreesPerSecond is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.GyrometerDataThreshold", "double GyrometerDataThreshold.YAxisInDegreesPerSecond");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double XAxisInDegreesPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerDataThreshold.XAxisInDegreesPerSecond is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.GyrometerDataThreshold", "double GyrometerDataThreshold.XAxisInDegreesPerSecond");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.XAxisInDegreesPerSecond.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.XAxisInDegreesPerSecond.set
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.YAxisInDegreesPerSecond.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.YAxisInDegreesPerSecond.set
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.ZAxisInDegreesPerSecond.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerDataThreshold.ZAxisInDegreesPerSecond.set
	}
}
