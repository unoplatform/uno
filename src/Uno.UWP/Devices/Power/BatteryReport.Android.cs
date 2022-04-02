using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Power
{
	public sealed partial class BatteryReport
	{
		public int? ChargeRateInMilliwatts { get; }
		public int? DesignCapacityInMilliwattHours { get; }
		public int? FullChargeCapacityInMilliwattHours { get; }
		public int? RemainingCapacityInMilliwattHours { get; }
		public System.Power.BatteryStatus Status { get; }


		internal BatteryReport()
		{
			// values are current for object creation

			var ifilter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
			var batteryStatus = Android.App.Application.Context.RegisterReceiver(null, ifilter);

			// since API 5, so we don't have to check OS level
			if (!batteryStatus.GetBooleanExtra(Android.OS.BatteryManager.ExtraPresent, false))
			{
				Status = System.Power.BatteryStatus.NotPresent;
				return;
			}

			Status = UWPstatusFromAndroStatus(batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraStatus, -1));


			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
			{
				// this probably doesn't happen, but anyway we can try set some values...

				// since API 5
				int level = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraLevel, 1);
				int scale = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraScale, 100);

				// 100*RemainingCapacityInMilliwattHours / FullChargeCapacityInMilliwattHours gives percentage of remaining battery
				RemainingCapacityInMilliwattHours = level;
				FullChargeCapacityInMilliwattHours = scale;

				return;
			}

			// now, we have API 21+, so we have more values
			int voltage = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraVoltage, 0);

			var batteryManager = (Android.OS.BatteryManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.BatteryService);
			int currentMicroAmps = batteryManager.GetIntProperty((int)Android.OS.BatteryProperty.CurrentNow);   // Instantaneous battery current in microamperes, as an integer.
			int chargeCounterMicroAmpHr = batteryManager.GetIntProperty((int)Android.OS.BatteryProperty.ChargeCounter); // Battery capacity in microampere-hours, as an integer.
			long energyCounterNanoWattHr = batteryManager.GetLongProperty((int)Android.OS.BatteryProperty.EnergyCounter); // Battery remaining energy in nanowatt-hours, as a long integer.

			// Android: Instantaneous battery current in microamperes, as an integer.
			// UWP: mW
			// both: > 0 charging, < 0 discharging
			// conversion: milli = 1000 micro; and watt = volt * amper
			ChargeRateInMilliwatts = voltage * currentMicroAmps / 1000;

			// Android: Battery remaining energy in nanowatt-hours, as a long integer.
			// UWP: mWh
			// conversion: milli = 1000 micro; and micro = 1000 nano
			RemainingCapacityInMilliwattHours = (int)(energyCounterNanoWattHr / (1000 * 1000));

			// Android: Battery capacity in microampere-hours, as an integer.
			// UWP: mWh
			// conversion: milli = 1000 micro; and watt = volt * amper
			FullChargeCapacityInMilliwattHours = voltage * chargeCounterMicroAmpHr / 1000;

			// UWP doc says: Some battery controllers might return the same value as FullChargeCapacityInMilliwattHours or return no value at all.
			DesignCapacityInMilliwattHours = FullChargeCapacityInMilliwattHours;
		}
		private Windows.System.Power.BatteryStatus UWPstatusFromAndroStatus(int status)
		{
			switch (status)
			{
				case 1: // BATTERY_STATUS_UNKNOWN
					return System.Power.BatteryStatus.Idle;
				case 2: // BATTERY_STATUS_CHARGING
					return System.Power.BatteryStatus.Charging;
				case 3: // BATTERY_STATUS_DISCHARGING
					return System.Power.BatteryStatus.Discharging;
				case 4: // BATTERY_STATUS_NOT_CHARGING
					return System.Power.BatteryStatus.Idle;
				case 5: // BATTERY_STATUS_FULL
					return System.Power.BatteryStatus.Idle;
			}

			// either OS is changed, or cannot get ExtraStatus (status == -1)
			return System.Power.BatteryStatus.NotPresent;
		}
	}
}
