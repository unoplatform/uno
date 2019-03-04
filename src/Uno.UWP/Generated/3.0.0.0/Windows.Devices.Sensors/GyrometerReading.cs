#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GyrometerReading 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double AngularVelocityX
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerReading.AngularVelocityX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double AngularVelocityY
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerReading.AngularVelocityY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double AngularVelocityZ
		{
			get
			{
				throw new global::System.NotImplementedException("The member double GyrometerReading.AngularVelocityZ is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset GyrometerReading.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? PerformanceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? GyrometerReading.PerformanceCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> GyrometerReading.Properties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.AngularVelocityX.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.AngularVelocityY.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.AngularVelocityZ.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.PerformanceCount.get
		// Forced skipping of method Windows.Devices.Sensors.GyrometerReading.Properties.get
	}
}
