using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using UIKit;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;

namespace Windows.System.Power;

public partial class PowerManager
{
	private const string DeviceModelSimulator = "Simulator";
#if __MACCATALYST__
	private const string IsPresentKey = "Is Present";
	private const string IsChargingKey = "Is Charging";
	private const string IsChargedKey = "Is Charged";
	private const string CurrentCapacityKey = "Current Capacity";
	private const string MaxCapacityKey = "Max Capacity";
	private const string PowerSourceStateKey = "Power Source State";
	private const string AcPowerValue = "AC Power";
#endif


	private static NSObject _batteryLevelChangeSubscription;
	private static NSObject _batteryStateChangeSubscription;
	private static NSObject _powerStateChangeSubscription;

	private static UIDevice _device;
	private static bool _isSimulator;

	static partial void InitializePlatform()
	{
		_device = UIDevice.CurrentDevice;
		_isSimulator = _device.Model?
						   .Contains(
							   DeviceModelSimulator,
							   StringComparison.InvariantCultureIgnoreCase) == true;
#if !__MACCATALYST__
		_device.BatteryMonitoringEnabled = !_isSimulator;
#endif
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
#if __MACCATALYST__
		var powerSourceState = GetMacCatalystPowerSourceState();
		if (!powerSourceState.IsPresent)
		{
			return UwpBatteryStatus.NotPresent;
		}

		if (powerSourceState.IsCharging)
		{
			return UwpBatteryStatus.Charging;
		}

		if (powerSourceState.IsCharged)
		{
			return UwpBatteryStatus.Idle;
		}

		return UwpBatteryStatus.Discharging;
#else
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
#endif
	}

	private static UwpEnergySaverStatus GetEnergySaverStatus() =>
		NSProcessInfo.ProcessInfo.LowPowerModeEnabled ?
			UwpEnergySaverStatus.On : UwpEnergySaverStatus.Off;

	private static UwpPowerSupplyStatus GetPowerSupplyStatus()
	{
#if __MACCATALYST__
		var powerSourceState = GetMacCatalystPowerSourceState();
		return powerSourceState.PowerSourceState == AcPowerValue ?
			UwpPowerSupplyStatus.Adequate : UwpPowerSupplyStatus.NotPresent;
#else
		return _device.BatteryState == UIDeviceBatteryState.Charging ?
			UwpPowerSupplyStatus.Adequate : UwpPowerSupplyStatus.NotPresent;
#endif
	}

	private static int GetRemainingChargePercent()
	{
#if __MACCATALYST__
		var powerSourceState = GetMacCatalystPowerSourceState();
		if (!powerSourceState.IsPresent)
		{
			return 0;
		}

		if (powerSourceState.MaxCapacity > 0)
		{
			return (int)Math.Round(powerSourceState.CurrentCapacity / (double)powerSourceState.MaxCapacity * 100d);
		}

		return powerSourceState.CurrentCapacity;
#else
		if (_isSimulator) return 100;
		if (_device.BatteryLevel < 0.0f) return 0;

		return (int)Math.Round(_device.BatteryLevel * 100f);
#endif
	}

#if __MACCATALYST__
	private static MacCatalystPowerSourceState GetMacCatalystPowerSourceState()
	{
		var powerSourcesInfo = IOPSCopyPowerSourcesInfo();
		if (powerSourcesInfo == IntPtr.Zero)
		{
			return default;
		}

		try
		{
			var powerSourcesList = IOPSCopyPowerSourcesList(powerSourcesInfo);
			if (powerSourcesList == IntPtr.Zero)
			{
				return default;
			}

			try
			{
				var array = Runtime.GetNSObject<NSArray>(powerSourcesList);
				if (array == null || array.Count == 0)
				{
					return default;
				}

				for (nuint i = 0; i < array.Count; i++)
				{
					var powerSource = array.GetItem<NSObject>(i);
					var descriptionHandle = IOPSGetPowerSourceDescription(powerSourcesInfo, powerSource.Handle);
					var description = Runtime.GetNSObject<NSDictionary>(descriptionHandle);
					if (description == null)
					{
						continue;
					}

					if (description[new NSString(IsPresentKey)] is NSNumber isPresentNumber
						&& !isPresentNumber.BoolValue)
					{
						continue;
					}

					return new MacCatalystPowerSourceState
					{
						IsPresent = true,
						IsCharging = (description[new NSString(IsChargingKey)] as NSNumber)?.BoolValue == true,
						IsCharged = (description[new NSString(IsChargedKey)] as NSNumber)?.BoolValue == true,
						CurrentCapacity = (description[new NSString(CurrentCapacityKey)] as NSNumber)?.Int32Value ?? 0,
						MaxCapacity = (description[new NSString(MaxCapacityKey)] as NSNumber)?.Int32Value ?? 0,
						PowerSourceState = (description[new NSString(PowerSourceStateKey)] as NSString)?.ToString()
					};
				}
			}
			finally
			{
				CFRelease(powerSourcesList);
			}
		}
		finally
		{
			CFRelease(powerSourcesInfo);
		}

		return default;
	}

	private struct MacCatalystPowerSourceState
	{
		public bool IsPresent { get; set; }
		public bool IsCharging { get; set; }
		public bool IsCharged { get; set; }
		public int CurrentCapacity { get; set; }
		public int MaxCapacity { get; set; }
		public string PowerSourceState { get; set; }
	}

	[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
	private static extern IntPtr IOPSCopyPowerSourcesInfo();

	[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
	private static extern IntPtr IOPSCopyPowerSourcesList(IntPtr blob);

	[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
	private static extern IntPtr IOPSGetPowerSourceDescription(IntPtr blob, IntPtr ps);

	[DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	private static extern void CFRelease(IntPtr cfTypeRef);
#endif

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
