using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Power
{
	public partial class Battery
	{
		public static Battery AggregateBattery()
		{
			return new Battery();
		}

		public BatteryReport GetReport()
		{
			return new BatteryReport();
		}

	}
}
