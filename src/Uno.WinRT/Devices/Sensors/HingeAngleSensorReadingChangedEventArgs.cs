#if __ANDROID__
namespace Windows.Devices.Sensors
{
	/// <summary>
	/// Provides data for the ReadingChanged event.
	/// </summary>
	public partial class HingeAngleSensorReadingChangedEventArgs
	{
		internal HingeAngleSensorReadingChangedEventArgs(HingeAngleReading reading) => Reading = reading;

		/// <summary>
		/// Gets the data exposed by the hinge angle sensor in a dual-screen device.
		/// </summary>
		public HingeAngleReading Reading { get; }
	}
}
#endif
