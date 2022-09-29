using AndroidBatteryStatus = Android.OS.BatteryStatus;
using UwpBatteryStatus = Windows.System.Power.BatteryStatus;


namespace Windows.Devices.Power
{
	public sealed partial class BatteryReport
	{
		public int? ChargeRateInMilliwatts { get; }
		public int? DesignCapacityInMilliwattHours { get; }
		public int? FullChargeCapacityInMilliwattHours { get; }
		public int? RemainingCapacityInMilliwattHours { get; }
		public UwpBatteryStatus Status { get; }


		internal BatteryReport()
		{
			//  battery values are current for object creation

			// using info from https://developer.android.com/training/monitoring-device-state/battery-monitoring
			var ifilter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
			var batteryStatus = Android.App.Application.Context.RegisterReceiver(null, ifilter);
			if(batteryStatus is null)
			{
				Status = UwpBatteryStatus.NotPresent;
				return;
			}

			// since API 5, so we don't have to check OS level
			if (!batteryStatus.GetBooleanExtra(Android.OS.BatteryManager.ExtraPresent, false))
			{
				Status = UwpBatteryStatus.NotPresent;
				return;
			}

			Status = UwpStatusFromAndroidStatus(batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraStatus, -1));

			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
			{
				// this probably doesn't happen, but anyway we can try to set some values...

				// since API 5
				int level = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraLevel, 1);
				int scale = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraScale, 100);

				// 100*RemainingCapacityInMilliwattHours / FullChargeCapacityInMilliwattHours gives percentage of remaining battery
				RemainingCapacityInMilliwattHours = level;
				FullChargeCapacityInMilliwattHours = scale;

				return;
			}

			// now, we have API 21+, so we have more values

			// voltage is int, but in mV; we want to have voltage in Volts
			float voltage = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraVoltage, 0) / 1000;

			using var batteryManager = (Android.OS.BatteryManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.BatteryService);
			if (batteryManager is null)
			{
				Status = UwpBatteryStatus.NotPresent;
				return;
			}
			int currentMicroAmps = batteryManager.GetIntProperty((int)Android.OS.BatteryProperty.CurrentNow);   // Instantaneous battery current in microamperes, as an integer.
			int chargeCounterMicroAmpHr = batteryManager.GetIntProperty((int)Android.OS.BatteryProperty.ChargeCounter); // Battery capacity in microampere-hours, as an integer.
			// long energyCounterNanoWattHr = batteryManager.GetLongProperty((int)Android.OS.BatteryProperty.EnergyCounter); // Battery remaining energy in nanowatt-hours, as a long integer.
			int energyCounterNanoWattHr = batteryManager.GetIntProperty((int)Android.OS.BatteryProperty.EnergyCounter); // But in reality, seems like it is int not long (doc has error here)

			// Android: Instantaneous battery current in microamperes, as an integer.
			// UWP: mW
			// both: > 0 charging, < 0 discharging
			// conversion: milli = 1000 micro; and watt = volt * amper
			ChargeRateInMilliwatts = (int) (voltage * currentMicroAmps / 1000);

			// Android: Battery remaining energy in nanowatt-hours, as a long integer.
			// UWP: mWh
			// conversion: milli = 1000 micro; and micro = 1000 nano
			RemainingCapacityInMilliwattHours = (int)(energyCounterNanoWattHr / (1000 * 1000));

			// Android: Battery capacity in microampere-hours, as an integer.
			// UWP: mWh
			// conversion: milli = 1000 micro; and watt = volt * amper
			FullChargeCapacityInMilliwattHours = (int) (voltage * chargeCounterMicroAmpHr / 1000);

			// UWP doc says: Some battery controllers might return the same value as FullChargeCapacityInMilliwattHours or return no value at all.
			DesignCapacityInMilliwattHours = FullChargeCapacityInMilliwattHours;
		}
		private UwpBatteryStatus UwpStatusFromAndroidStatus(int status)
		{
			return status switch
			{
				(int)AndroidBatteryStatus.Unknown => UwpBatteryStatus.NotPresent,
				(int)AndroidBatteryStatus.Charging => UwpBatteryStatus.Charging,
				(int)AndroidBatteryStatus.Discharging => UwpBatteryStatus.Discharging,
				(int)AndroidBatteryStatus.NotCharging => UwpBatteryStatus.Idle,
				(int)AndroidBatteryStatus.Full => UwpBatteryStatus.Idle,
				// last case: either OS is changed (new values, not known while writing this code), or cannot get ExtraStatus (status == -1)
				_ => UwpBatteryStatus.NotPresent
			};
		}
	}
}
