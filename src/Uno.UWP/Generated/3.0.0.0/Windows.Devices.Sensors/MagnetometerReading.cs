#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MagnetometerReading 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.MagnetometerAccuracy DirectionalAccuracy
		{
			get
			{
				throw new global::System.NotImplementedException("The member MagnetometerAccuracy MagnetometerReading.DirectionalAccuracy is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float MagneticFieldX
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerReading.MagneticFieldX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float MagneticFieldY
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerReading.MagneticFieldY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float MagneticFieldZ
		{
			get
			{
				throw new global::System.NotImplementedException("The member float MagnetometerReading.MagneticFieldZ is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset MagnetometerReading.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? PerformanceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MagnetometerReading.PerformanceCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> MagnetometerReading.Properties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldX.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldY.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.MagneticFieldZ.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.DirectionalAccuracy.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.PerformanceCount.get
		// Forced skipping of method Windows.Devices.Sensors.MagnetometerReading.Properties.get
	}
}
