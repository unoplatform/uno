#nullable disable

#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		private static object _syncLock = new object();

		private static PowerSupplyStatus? _lastPowerSupplyStatus;
		private static int? _lastRemainingChargePercent;
		private static EnergySaverStatus? _lastEnergySaverStatus;
		private static BatteryStatus? _lastBatteryStatus;

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
				lock (_syncLock)
				{
					if (_powerSupplyStatusChanged == null)
					{
						StartPowerSupplyStatusMonitoring();
					}
					_powerSupplyStatusChanged += value;
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_powerSupplyStatusChanged -= value;
					if (_powerSupplyStatusChanged == null)
					{
						EndPowerSupplyStatusMonitoring();
					}
				}
			}
		}

		public static event EventHandler<object> EnergySaverStatusChanged
		{
			add
			{
				lock (_syncLock)
				{
					if (_energySaverStatusChanged == null)
					{
						StartEnergySaverStatusMonitoring();
					}

					_energySaverStatusChanged += value;
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_energySaverStatusChanged -= value;
					if (_energySaverStatusChanged == null)
					{
						EndEnergySaverStatusMonitoring();
					}
				}
			}
		}

		public static event EventHandler<object> RemainingChargePercentChanged
		{
			add
			{
				lock (_syncLock)
				{
					if (_remainingChargePercentChanged == null)
					{
						StartRemainingChargePercentMonitoring();
					}

					_remainingChargePercentChanged += value;
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_remainingChargePercentChanged -= value;
					if (_remainingChargePercentChanged == null)
					{
						EndRemainingChargePercentMonitoring();
					}
				}
			}
		}

		public static event EventHandler<object> BatteryStatusChanged
		{
			add
			{
				lock (_syncLock)
				{
					if (_batteryStatusChanged == null)
					{
						StartBatteryStatusMonitoring();
					}

					_batteryStatusChanged += value;
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_batteryStatusChanged -= value;
					if (_batteryStatusChanged == null)
					{
						EndBatteryStatusMonitoring();
					}
				}
			}
		}

		internal static void RaiseRemainingChargePercentChanged()
		{
			var currentValue = RemainingChargePercent;
			if (_lastRemainingChargePercent != currentValue)
			{
				_lastRemainingChargePercent = currentValue;
				_remainingChargePercentChanged?.Invoke(null, null);
			}
		}

		internal static void RaiseBatteryStatusChanged()
		{
			var currentValue = BatteryStatus;
			if (_lastBatteryStatus != currentValue)
			{
				_lastBatteryStatus = currentValue;
				_batteryStatusChanged?.Invoke(null, null);
			}
		}

		internal static void RaisePowerSupplyStatusChanged()
		{
			var currentValue = PowerSupplyStatus;
			if (_lastPowerSupplyStatus != currentValue)
			{
				_lastPowerSupplyStatus = currentValue;
				_powerSupplyStatusChanged?.Invoke(null, null);
			}
		}

		internal static void RaiseEnergySaverStatusChanged()
		{
			var currentValue = EnergySaverStatus;
			if (_lastEnergySaverStatus != currentValue)
			{
				_lastEnergySaverStatus = currentValue;
				_energySaverStatusChanged?.Invoke(null, null);
			}
		}
	}
}
#endif
