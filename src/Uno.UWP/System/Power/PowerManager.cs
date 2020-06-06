#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Helpers;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		private static object _syncLock = new object();

		private static PowerSupplyStatus? _lastPowerSupplyStatus;
		private static int? _lastRemainingChargePercent;
		private static EnergySaverStatus? _lastEnergySaverStatus;
		private static BatteryStatus? _lastBatteryStatus;

		private static StartStopEventWrapper<EventHandler<object>> _powerSupplyStatusChangedWrapper;
		private static StartStopEventWrapper<EventHandler<object>> _energySaverStatusChangedWrapper;
		private static StartStopEventWrapper<EventHandler<object>> _remainingChargePercentChangedWrapper;
		private static StartStopEventWrapper<EventHandler<object>> _batteryStatusChangedWrapper;

		static PowerManager()
		{
			_powerSupplyStatusChangedWrapper = new StartStopEventWrapper<EventHandler<object>>(
				() => StartPowerSupplyStatusMonitoring(),
				() => StopPowerSupplyStatusMonitoring(),
				_syncLock);
			_energySaverStatusChangedWrapper = new StartStopEventWrapper<EventHandler<object>>(
				() => StartEnergySaverStatusMonitoring(),
				() => StopEnergySaverStatusMonitoring(),
				_syncLock);
			_remainingChargePercentChangedWrapper = new StartStopEventWrapper<EventHandler<object>>(
				() => StartRemainingChargePercentMonitoring(),
				() => StopRemainingChargePercentMonitoring(),
				_syncLock);
			_batteryStatusChangedWrapper = new StartStopEventWrapper<EventHandler<object>>(
				() => StartBatteryStatusMonitoring(),
				() => StopBatteryStatusMonitoring(),
				_syncLock);

			InitializePlatform();
		}

		static partial void InitializePlatform();

		public static BatteryStatus BatteryStatus => GetBatteryStatus();

		public static EnergySaverStatus EnergySaverStatus => GetEnergySaverStatus();

		public static PowerSupplyStatus PowerSupplyStatus => GetPowerSupplyStatus();

		public static int RemainingChargePercent => GetRemainingChargePercent();

		public static event EventHandler<object> PowerSupplyStatusChanged
		{
			add => _powerSupplyStatusChangedWrapper.AddHandler(value);
			remove => _powerSupplyStatusChangedWrapper.RemoveHandler(value);
		}

		public static event EventHandler<object> EnergySaverStatusChanged
		{
			add => _energySaverStatusChangedWrapper.AddHandler(value);
			remove => _energySaverStatusChangedWrapper.RemoveHandler(value);
		}

		public static event EventHandler<object> RemainingChargePercentChanged
		{
			add => _remainingChargePercentChangedWrapper.AddHandler(value);
			remove => _remainingChargePercentChangedWrapper.RemoveHandler(value);
		}

		public static event EventHandler<object> BatteryStatusChanged
		{
			add => _batteryStatusChangedWrapper.AddHandler(value);
			remove => _batteryStatusChangedWrapper.RemoveHandler(value);
		}

		internal static void RaiseRemainingChargePercentChanged()
		{
			var currentValue = RemainingChargePercent;
			if (_lastRemainingChargePercent != currentValue)
			{
				_lastRemainingChargePercent = currentValue;
				_remainingChargePercentChangedWrapper.Event?.Invoke(null, null);
			}
		}

		internal static void RaiseBatteryStatusChanged()
		{
			var currentValue = BatteryStatus;
			if (_lastBatteryStatus != currentValue)
			{
				_lastBatteryStatus = currentValue;
				_batteryStatusChangedWrapper.Event?.Invoke(null, null);
			}
		}

		internal static void RaisePowerSupplyStatusChanged()
		{
			var currentValue = PowerSupplyStatus;
			if (_lastPowerSupplyStatus != currentValue)
			{
				_lastPowerSupplyStatus = currentValue;
				_powerSupplyStatusChangedWrapper.Event?.Invoke(null, null);
			}
		}

		internal static void RaiseEnergySaverStatusChanged()
		{
			var currentValue = EnergySaverStatus;
			if (_lastEnergySaverStatus != currentValue)
			{
				_lastEnergySaverStatus = currentValue;
				_energySaverStatusChangedWrapper.Event?.Invoke(null, null);
			}
		}
	}
}
#endif
