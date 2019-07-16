#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccelerometerShakenEventArgs 
	{
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset AccelerometerShakenEventArgs.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.AccelerometerShakenEventArgs.Timestamp.get
	}
}
