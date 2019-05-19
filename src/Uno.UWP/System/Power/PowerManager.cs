#if __MOBILE__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		private static EventHandler<object> _powerSupplyStatusChanged;
		private static EventHandler<object> _energySaverStatusChanged;
		private static EventHandler<object> _remainingChargePercentChanged;
		private static EventHandler<object> _batteryStatusChanged;

		public static BatteryStatus BatteryStatus => GetBatteryStatus();

		public static EnergySaverStatus EnergySaverStatus => GetEnergySaverStatus();

		public static PowerSupplyStatus PowerSupplyStatus => GetPowerSupplyStatus();

		public static int RemainingChargePercent => GetRemainingChargePercent();

		public static event EventHandler<object> PowerSupplyStatusChanged
		{
			add
			{
				if (_powerSupplyStatusChanged == null)
				{
					StartPowerSupplyStatusMonitoring();
				}
				_powerSupplyStatusChanged += value;
			}
			remove
			{
				_powerSupplyStatusChanged -= value;
				if (_powerSupplyStatusChanged == null)
				{
					EndPowerSupplyStatusMonitoring();
				}
			}
		}

		public static event EventHandler<object> EnergySaverStatusChanged
		{
			add
			{
				if (_energySaverStatusChanged == null)
				{
					StartEnergySaverStatusMonitoring();
				}
				_energySaverStatusChanged += value;
			}
			remove
			{
				_energySaverStatusChanged -= value;
				if (_energySaverStatusChanged == null)
				{
					EndEnergySaverStatusMonitoring();
				}
			}
		}

		public static event EventHandler<object> RemainingChargePercentChanged
		{
			add
			{
				if (_remainingChargePercentChanged == null)
				{
					StartRemainingChargePercentMonitoring();
				}
				_remainingChargePercentChanged += value;
			}
			remove
			{
				_remainingChargePercentChanged -= value;
				if (_remainingChargePercentChanged == null)
				{
					EndRemainingChargePercentMonitoring();
				}
			}
		}

		public static event EventHandler<object> BatteryStatusChanged
		{
			add
			{
				if (_batteryStatusChanged == null)
				{
					StartBatteryStatusMonitoring();
				}
				_batteryStatusChanged += value;
			}
			remove
			{
				_batteryStatusChanged -= value;
				if (_batteryStatusChanged == null)
				{
					EndBatteryStatusMonitoring();
				}
			}
		}

		internal static void RaiseRemainingChargePercentChanged() => _remainingChargePercentChanged?.Invoke(null, null);

		internal static void RaiseBatteryStatusChanged() => _batteryStatusChanged?.Invoke(null, null);

		internal static void RaisePowerSupplyStatusChanged() => _powerSupplyStatusChanged?.Invoke(null, null);

		internal static void RaiseEnergySaverStatusChanged() => _energySaverStatusChanged?.Invoke(null, null);
	}
}
#endif
