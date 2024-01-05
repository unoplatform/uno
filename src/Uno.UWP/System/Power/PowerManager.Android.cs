using System;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.OS;
using AndroidBatteryStatus = Android.OS.BatteryStatus;
using AndroidPowerManager = Android.OS.PowerManager;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;

namespace Windows.System.Power;

public partial class PowerManager
{
	private static BatteryManager _batteryManager;
	private static AndroidPowerManager _powerManager;

	private static PowerSaveModeChangeReceiver? _powerSaveModeChangeReceiver;
	private static PowerConnectionBroadcastReceiver? _powerConnectionBroadcastReceiver;
	private static BatteryChangedBroadcastReceiver? _batteryChangedBroadcastReceiver;

	[MemberNotNull(nameof(_batteryManager), nameof(_powerManager))]
	static partial void InitializePlatform()
	{
		_batteryManager = (BatteryManager)Android.App.Application.Context.GetSystemService(Context.BatteryService)!;
		_powerManager = (AndroidPowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService)!;
	}

	private static UwpBatteryStatus GetBatteryStatus()
	{
		var batteryStatusFilter = new IntentFilter(Intent.ActionBatteryChanged);
		var batteryStatusIntent = Android.App.Application.Context.RegisterReceiver(null, batteryStatusFilter);
		var status = (AndroidBatteryStatus)(batteryStatusIntent?.GetIntExtra(
			BatteryManager.ExtraStatus,
			(int)AndroidBatteryStatus.Unknown) ?? (int)AndroidBatteryStatus.Unknown);
		switch (status)
		{
			case AndroidBatteryStatus.Charging:
				return UwpBatteryStatus.Charging;
			case AndroidBatteryStatus.Discharging:
				return UwpBatteryStatus.Discharging;
			case AndroidBatteryStatus.Full:
			case AndroidBatteryStatus.NotCharging:
				return UwpBatteryStatus.Idle;
			case AndroidBatteryStatus.Unknown:
				return UwpBatteryStatus.NotPresent;
			default:
				throw new InvalidOperationException("Invalid battery status received");
		}
	}

	private static UwpEnergySaverStatus GetEnergySaverStatus() =>
		_powerManager.IsPowerSaveMode ?
			UwpEnergySaverStatus.On : UwpEnergySaverStatus.Off;

	private static UwpPowerSupplyStatus GetPowerSupplyStatus()
	{
		var batteryStatusFilter = new IntentFilter(Intent.ActionBatteryChanged);
		var batteryStatusIntent = Android.App.Application.Context.RegisterReceiver(null, batteryStatusFilter);
		var plugged = (BatteryPlugged)(batteryStatusIntent?.GetIntExtra(BatteryManager.ExtraPlugged, -1) ?? -1);
		if (plugged == BatteryPlugged.Ac || plugged == BatteryPlugged.Usb || plugged == BatteryPlugged.Usb)
		{
			return UwpPowerSupplyStatus.Adequate;
		}
		return UwpPowerSupplyStatus.NotPresent;
	}

	private static int GetRemainingChargePercent() =>
		_batteryManager.GetIntProperty((int)BatteryProperty.Capacity);

	static partial void StartEnergySaverStatus()
	{
		if (_powerSaveModeChangeReceiver == null)
		{
			_powerSaveModeChangeReceiver = new PowerSaveModeChangeReceiver();

			var filter = new IntentFilter();
			filter.AddAction(AndroidPowerManager.ActionPowerSaveModeChanged);
			Android.App.Application.Context.RegisterReceiver(_powerSaveModeChangeReceiver, filter);
		}
	}

	static partial void EndEnergySaverStatus()
	{
		if (_powerSaveModeChangeReceiver != null)
		{
			Android.App.Application.Context.UnregisterReceiver(_powerSaveModeChangeReceiver);
			_powerSaveModeChangeReceiver = null;
		}
	}

	static partial void StartBatteryStatus()
	{
		TryRegisterAndroidBatteryChangedBroadcastReceiver();
		TryRegisterAndroidPowerConnectionBroadcastReceiver();
	}

	static partial void EndBatteryStatus()
	{
		TryUnregisterAndroidBatteryChangedBroadcastReceiver();
		TryUnregisterAndroidPowerConnectionBroadcastReceiver();
	}

	static partial void StartRemainingChargePercent() =>
		TryRegisterAndroidBatteryChangedBroadcastReceiver();

	static partial void EndRemainingChargePercent() =>
		TryUnregisterAndroidBatteryChangedBroadcastReceiver();

	static partial void StartPowerSupplyStatus() =>
		TryRegisterAndroidPowerConnectionBroadcastReceiver();

	static partial void EndPowerSupplyStatus() =>
		TryUnregisterAndroidPowerConnectionBroadcastReceiver();

	private static void TryRegisterAndroidPowerConnectionBroadcastReceiver()
	{
		if (_powerConnectionBroadcastReceiver == null)
		{
			_powerConnectionBroadcastReceiver = new PowerConnectionBroadcastReceiver();

			var filter = new IntentFilter();
			filter.AddAction(Intent.ActionPowerConnected);
			filter.AddAction(Intent.ActionPowerDisconnected);
			Android.App.Application.Context.RegisterReceiver(_powerConnectionBroadcastReceiver, filter);
		}
	}

	private static void TryUnregisterAndroidPowerConnectionBroadcastReceiver()
	{
		// Two different events use this broadcast receiver.
		if (_powerSupplyStatusChanged != null ||
			_batteryStatusChanged != null)
		{
			return;
		}

		if (_powerConnectionBroadcastReceiver != null)
		{
			Android.App.Application.Context.UnregisterReceiver(_powerConnectionBroadcastReceiver);
			_powerConnectionBroadcastReceiver = null;
		}
	}

	private static void TryRegisterAndroidBatteryChangedBroadcastReceiver()
	{
		if (_batteryChangedBroadcastReceiver == null)
		{
			_batteryChangedBroadcastReceiver = new BatteryChangedBroadcastReceiver();

			var filter = new IntentFilter();
			filter.AddAction(Intent.ActionBatteryChanged);
			Android.App.Application.Context.RegisterReceiver(_batteryChangedBroadcastReceiver, filter);
		}
	}

	private static void TryUnregisterAndroidBatteryChangedBroadcastReceiver()
	{
		// Two events use this broadcast
		if (_batteryStatusChanged.Event != null ||
			_remainingChargePercentChanged.Event != null)
		{
			return;
		}

		if (_batteryChangedBroadcastReceiver != null)
		{
			Android.App.Application.Context.UnregisterReceiver(_batteryChangedBroadcastReceiver);
			_batteryChangedBroadcastReceiver = null;
		}
	}
}
