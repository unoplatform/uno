
namespace Windows.Devices.Power
{
	public partial class Battery
	{
		public static Battery AggregateBattery => new Battery();

		public BatteryReport GetReport() => new BatteryReport();

	}
}
