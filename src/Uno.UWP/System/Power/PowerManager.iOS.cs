#if __IOS__
using System;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;
using UIKit;
using Foundation;

namespace Windows.System.Power
{
	public partial class PowerManager
	{
		private const string DeviceModelSimulator = "Simulator";


		private static NSObject _batteryLevelChangeSubscription;
		private static NSObject _batteryStateChangeSubscription;
		private static NSObject _powerStateChangeSubscription;

		private static readonly UIDevice _device;
		private static readonly bool _isSimulator;

		static PowerManager()
		{
			_device = UIDevice.CurrentDevice;
			_isSimulator = _device.Model?
							   .Contains(
								   DeviceModelSimulator,
								   StringComparison.InvariantCultureIgnoreCase) == true;
			_device.BatteryMonitoringEnabled = !_isSimulator;
		}

		private static void StartEnergySaverStatusMonitoring()
		{
			if (_isSimulator) return;
			TryAddNotificationSubscription(
				NSProcessInfo.PowerStateDidChangeNotification,
				ref _powerStateChangeSubscription,
				PowerStateChangeHandler);
		}

		private static void EndEnergySaverStatusMonitoring() =>
			TryRemoveNotificationSubscription(ref _powerStateChangeSubscription);

		private static void StartRemainingChargePercentMonitoring()
		{
			if (_isSimulator) return;
			TryAddNotificationSubscription(
				UIDevice.BatteryLevelDidChangeNotification,
				ref _batteryLevelChangeSubscription,
				BatteryLevelChangeHandler);
		}

		private static void EndRemainingChargePercentMonitoring() =>
			TryRemoveNotificationSubscription(ref _batteryLevelChangeSubscription);

		private static void StartPowerSupplyStatusMonitoring() =>
			StartIosBatteryStateMonitoring();

		private static void EndPowerSupplyStatusMonitoring() =>
			EndIosBatteryStateMonitoring();

		private static void StartBatteryStatusMonitoring() =>
			StartIosBatteryStateMonitoring();

		private static void EndBatteryStatusMonitoring() =>
			EndIosBatteryStateMonitoring();

		private static void StartIosBatteryStateMonitoring()
		{
			if (_isSimulator) return;
			if (_batteryStatusChanged == null &&
				_powerSupplyStatusChanged == null)
			{
				TryAddNotificationSubscription(
				UIDevice.BatteryStateDidChangeNotification,
				ref _batteryStateChangeSubscription,
				BatteryStateChangeHandler);
			}
		}

		private static void EndIosBatteryStateMonitoring()
		{
			if (_batteryStatusChanged == null &&
				_powerSupplyStatusChanged == null)
			{
				TryRemoveNotificationSubscription(ref _batteryStateChangeSubscription);
			}
		}

		private static void BatteryLevelChangeHandler(NSNotification obj) =>
			RaiseRemainingChargePercentChanged();

		private static void BatteryStateChangeHandler(NSNotification obj)
		{
			RaiseBatteryStatusChanged();
			RaisePowerSupplyStatusChanged();
		}

		private static void PowerStateChangeHandler(NSNotification obj) =>
			RaiseEnergySaverStatusChanged();

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

			return (int)Math.Round(_device.BatteryLevel * 100f);
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
