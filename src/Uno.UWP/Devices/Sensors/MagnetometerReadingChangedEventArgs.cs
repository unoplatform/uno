#nullable disable

namespace Windows.Devices.Sensors
{
	public  partial class MagnetometerReadingChangedEventArgs 
	{
		internal MagnetometerReadingChangedEventArgs(MagnetometerReading reading) =>
			Reading = reading;

		public MagnetometerReading Reading { get; }
	}
}
