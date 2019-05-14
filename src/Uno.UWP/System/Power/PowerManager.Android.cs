#if __ANDROID__
using Android.Content;
using Android.OS;
using AndroidPowerManager = Android.OS.PowerManager;
using AndroidBatteryStatus = Android.OS.BatteryStatus;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;
using UwpEnergySaverStatus = Windows.System.Power.EnergySaverStatus;
using UwpPowerSupplyStatus = Windows.System.Power.PowerSupplyStatus;
using System;

namespace Windows.System.Power
{
    public partial class PowerManager
    {
		private static BatteryManager _batteryManager = null;
		private static AndroidPowerManager _powerManager = null;

		static PowerManager()
		{
			_batteryManager = (BatteryManager)Android.App.Application.Context.GetSystemService(Context.BatteryService);
			_powerManager = (AndroidPowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
		}

		private static UwpBatteryStatus GetBatteryStatus()
		{
			var batteryStatusFilter = new IntentFilter(Intent.ActionBatteryChanged);
			var batteryStatusIntent = Android.App.Application.Context.RegisterReceiver(null, batteryStatusFilter);
			var status = (AndroidBatteryStatus)batteryStatusIntent.GetIntExtra(BatteryManager.ExtraStatus, (int)AndroidBatteryStatus.Unknown);
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

		private static UwpPowerSupplyStatus GetPowerSupplyStatus() =>
			_batteryManager.IsCharging ?
				UwpPowerSupplyStatus.Adequate : UwpPowerSupplyStatus.NotPresent;

		private static int GetRemainingChargePercent() =>
			_batteryManager.GetIntProperty((int)BatteryProperty.Capacity);
	}
}
#endif
