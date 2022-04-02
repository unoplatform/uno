using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Power
{
	public partial class Battery
	{
		public static Windows.Devices.Power.Battery AggregateBattery()
		{
			return new Battery();
		}

		public Windows.Devices.Power.BatteryReport GetReport()
		{
			return new BatteryReport();
		}

	}
}
