#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;
using UIKit;
using Foundation;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		private const string DeviceModelSimulator = "iPhone Simulator";

		private static EventHandler<object> _remainingChargePercentChanged;
		private static EventHandler<object> _energySaverStatusChanged;
		private static EventHandler<object> _batteryStatusChanged;
		private static EventHandler<object> _powerSupplyStatusChanged;

		private static NSObject _batteryLevelChangeSubscription;
		private static NSObject _batteryStateChangeSubscription;
		private static NSObject _powerStateChangeSubscription;

		private static readonly UIDevice _device;
		private static readonly bool _isSimulator;

		static PowerManager()
		{
			_device = UIDevice.CurrentDevice;
			_isSimulator = _device.Model == DeviceModelSimulator;
			_device.BatteryMonitoringEnabled = !_isSimulator;
		}

		public static event EventHandler<object> RemainingChargePercentChanged
		{
			add
			{
				if (!_isSimulator)
				{
					TryAddNotificationSubscription(
						UIDevice.BatteryLevelDidChangeNotification,
						ref _batteryLevelChangeSubscription,
						BatteryLevelChangeHandler);
				}
				_remainingChargePercentChanged += value;
			}
			remove
			{
				_remainingChargePercentChanged -= value;
				if (_remainingChargePercentChanged == null)
				{
					TryRemoveNotificationSubscription(ref _batteryLevelChangeSubscription);
				}
			}
		}

		public static event EventHandler<object> EnergySaverStatusChanged
		{
			add
			{
				if (!_isSimulator)
				{
					TryAddNotificationSubscription(
						NSProcessInfo.PowerStateDidChangeNotification,
						ref _powerStateChangeSubscription,
						PowerStateChangeHandler);
				}
				_energySaverStatusChanged += value;
			}
			remove
			{
				_energySaverStatusChanged -= value;
				if (_energySaverStatusChanged == null)
				{
					TryRemoveNotificationSubscription(ref _powerStateChangeSubscription);
				}
			}
		}

		public static event EventHandler<object> BatteryStatusChanged
		{
			add
			{
				if (!_isSimulator)
				{
					TryAddNotificationSubscription(
						UIDevice.BatteryStateDidChangeNotification,
						ref _batteryStateChangeSubscription,
						BatteryStateChangeHandler);
				}
				_batteryStatusChanged += value;
			}
			remove
			{
				_batteryStatusChanged -= value;
				if (_batteryStatusChanged == null &&
					_powerSupplyStatusChanged == null)
				{
					TryRemoveNotificationSubscription(ref _batteryStateChangeSubscription);
				}
			}
		}

		public static event EventHandler<object> PowerSupplyStatusChanged
		{
			add
			{
				if (!_isSimulator)
				{
					TryAddNotificationSubscription(
						UIDevice.BatteryStateDidChangeNotification,
						ref _batteryStateChangeSubscription,
						BatteryStateChangeHandler);
				}
				_powerSupplyStatusChanged += value;
			}
			remove
			{
				_powerSupplyStatusChanged -= value;
				if (_batteryStatusChanged == null &&
					_powerSupplyStatusChanged == null)
				{
					TryRemoveNotificationSubscription(ref _batteryStateChangeSubscription);
				}
			}
		}

		private static void BatteryLevelChangeHandler(NSNotification obj) =>
			_remainingChargePercentChanged?.Invoke(null, null);

		private static void BatteryStateChangeHandler(NSNotification obj)
		{
			_batteryStatusChanged?.Invoke(null, null);
			_powerSupplyStatusChanged?.Invoke(null, null);
		}

		private static void PowerStateChangeHandler(NSNotification obj) =>
			_energySaverStatusChanged?.Invoke(null, null);

		private static UwpBatteryStatus GetBatteryStatus()
		{
			switch (_device.BatteryState)
			{
				case UIDeviceBatteryState.Unknown:
					return UwpBatteryStatus.NotPresent;
				case UIDeviceBatteryState.Unplugged:
					return UwpBatteryStatus.Discharging;
				case UIDeviceBatteryState.Charging:
					return UwpBatteryStatus.Charging;
				case UIDeviceBatteryState.Full:
					return UwpBatteryStatus.Idle;
				default:
					throw new InvalidOperationException("Invalid battery status received");
			}
		}

		private static UwpEnergySaverStatus GetEnergySaverStatus() =>
			NSProcessInfo.ProcessInfo.LowPowerModeEnabled ?
				UwpEnergySaverStatus.On : UwpEnergySaverStatus.Off;

		private static UwpPowerSupplyStatus GetPowerSupplyStatus() =>
			_device.BatteryState == UIDeviceBatteryState.Charging ?
				UwpPowerSupplyStatus.Adequate : UwpPowerSupplyStatus.NotPresent;

		private static int GetRemainingChargePercent()
		{
			if (_isSimulator) return 100;
			if (_device.BatteryLevel < 0.0f) return 0;

			return (int)(_device.BatteryLevel * 100f);
		}

		private static void TryAddNotificationSubscription(NSString notificationIdentifier, ref NSObject subscriptionToken, Action<NSNotification> handler)
		{
			if (subscriptionToken == null)
			{
				subscriptionToken = NSNotificationCenter.DefaultCenter.AddObserver(
					notificationIdentifier,
					handler);
			}
		}

		private static void TryRemoveNotificationSubscription(ref NSObject subscriptionToken)
		{
			subscriptionToken?.Dispose();
			subscriptionToken = null;
		}
	}
}
#endif
