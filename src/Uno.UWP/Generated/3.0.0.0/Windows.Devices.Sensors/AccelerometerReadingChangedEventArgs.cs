#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccelerometerReadingChangedEventArgs 
	{
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.AccelerometerReading Reading
		{
			get
			{
				throw new global::System.NotImplementedException("The member AccelerometerReading AccelerometerReadingChangedEventArgs.Reading is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs.Reading.get
	}
}
