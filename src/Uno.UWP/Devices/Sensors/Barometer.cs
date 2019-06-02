using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private static bool _initializationAttempted = false;
		private static Barometer _instance = null;

		private Barometer()
		{

		}

		public BarometerReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member BarometerReading Barometer.GetCurrentReading() is not implemented in Uno.");
		}

		public uint ReportInterval { get; set; } = 0;		

		public uint MinimumReportInterval { get; } = 0;

		public uint ReportLatency { get; set; } = 0;

		public uint MaxBatchSize { get; set; } = 0;

		public event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> ReadingChanged
		{
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
			}			
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.Barometer", "event TypedEventHandler<Barometer, BarometerReadingChangedEventArgs> Barometer.ReadingChanged");
			}
		}
	}
}
