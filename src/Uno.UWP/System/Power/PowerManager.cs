#if __MOBILE__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		public static BatteryStatus BatteryStatus => GetBatteryStatus();

		public static EnergySaverStatus EnergySaverStatus => GetEnergySaverStatus();

		public static PowerSupplyStatus PowerSupplyStatus => GetPowerSupplyStatus();

		public static int RemainingChargePercent => GetRemainingChargePercent();
	}
}
#endif
