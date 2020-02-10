#if __ANDROID__
namespace Windows.Devices.Sensors
{
	public partial class HingeAngleSensorReadingChangedEventArgs
	{
		internal HingeAngleSensorReadingChangedEventArgs(HingeAngleReading reading) => Reading = reading;		

		public HingeAngleReading Reading { get; }
	}
}
#endif
