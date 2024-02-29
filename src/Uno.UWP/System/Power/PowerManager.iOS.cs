using System;
using System.Diagnostics.CodeAnalysis;
using Foundation;
using UIKit;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;

namespace Windows.System.Power;

public partial class PowerManager
{
	private const string DeviceModelSimulator = "Simulator";


	private static NSObject? _batteryLevelChangeSubscription;
	private static NSObject? _batteryStateChangeSubscription;
	private static NSObject? _powerStateChangeSubscription;

	private static UIDevice _device;
	private static bool _isSimulator;

	[MemberNotNull(nameof(_device))]
	static partial void InitializePlatform()
	{
		_device = UIDevice.CurrentDevice;
		_isSimulator = _device.Model?
						   .Contains(
							   DeviceModelSimulator,
							   StringComparison.InvariantCultureIgnoreCase) == true;
		_device.BatteryMonitoringEnabled = !_isSimulator;
	}

	static partial void StartEnergySaverStatus()
	{
		if (_isSimulator) return;
		TryAddNotificationSubscription(
			NSProcessInfo.PowerStateDidChangeNotification,
			ref _powerStateChangeSubscription,
			PowerStateChangeHandler);
	}

	static partial void EndEnergySaverStatus() =>
		TryRemoveNotificationSubscription(ref _powerStateChangeSubscription);

	static partial void StartRemainingChargePercent()
	{
		if (_isSimulator) return;
		TryAddNotificationSubscription(
			UIDevice.BatteryLevelDidChangeNotification,
			ref _batteryLevelChangeSubscription,
			BatteryLevelChangeHandler);
	}

	static partial void EndRemainingChargePercent() =>
		TryRemoveNotificationSubscription(ref _batteryLevelChangeSubscription);

	static partial void StartPowerSupplyStatus() =>
		StartIosBatteryState();

	static partial void EndPowerSupplyStatus() =>
		EndIosBatteryState();

	static partial void StartBatteryStatus() =>
		StartIosBatteryState();

	static partial void EndBatteryStatus() =>
		EndIosBatteryState();

	private static void StartIosBatteryState()
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

	private static void EndIosBatteryState()
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

	private static void TryAddNotificationSubscription(NSString notificationIdentifier, ref NSObject? subscriptionToken, Action<NSNotification> handler)
	{
		if (subscriptionToken == null)
		{
			subscriptionToken = NSNotificationCenter.DefaultCenter.AddObserver(
				notificationIdentifier,
				handler);
		}
	}

	private static void TryRemoveNotificationSubscription(ref NSObject? subscriptionToken)
	{
		subscriptionToken?.Dispose();
		subscriptionToken = null;
	}
}
