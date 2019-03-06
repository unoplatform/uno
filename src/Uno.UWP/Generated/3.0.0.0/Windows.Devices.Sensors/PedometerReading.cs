#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PedometerReading 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int CumulativeSteps
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PedometerReading.CumulativeSteps is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan CumulativeStepsDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan PedometerReading.CumulativeStepsDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.PedometerStepKind StepKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member PedometerStepKind PedometerReading.StepKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset PedometerReading.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.PedometerReading.StepKind.get
		// Forced skipping of method Windows.Devices.Sensors.PedometerReading.CumulativeSteps.get
		// Forced skipping of method Windows.Devices.Sensors.PedometerReading.Timestamp.get
		// Forced skipping of method Windows.Devices.Sensors.PedometerReading.CumulativeStepsDuration.get
	}
}
