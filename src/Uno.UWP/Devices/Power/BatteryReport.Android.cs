
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
			// values are current for object creation

			var ifilter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
			var batteryStatus = Android.App.Application.Context.RegisterReceiver(null, ifilter);

			// since API 5, so we don't have to check OS level
			if (!batteryStatus.GetBooleanExtra(Android.OS.BatteryManager.ExtraPresent, false))
			{
				Status = UwpBatteryStatus.NotPresent;
				return;
			}

			Status = UwpStatusFromAndroidStatus(batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraStatus, -1));


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
		private UwpBatteryStatus UwpStatusFromAndroidStatus(int status)
		{
			switch (status)
			{
				case (int)AndroidBatteryStatus.Unknown: 
					return UwpBatteryStatus.NotPresent;
				case (int)AndroidBatteryStatus.Charging: 
					return UwpBatteryStatus.Charging;
				case (int)AndroidBatteryStatus.Discharging: 
					return UwpBatteryStatus.Discharging;
				case (int)AndroidBatteryStatus.NotCharging: 
					return UwpBatteryStatus.Idle;
				case (int)AndroidBatteryStatus.Full: 
					return UwpBatteryStatus.Idle;
			}

			// either OS is changed (new values, not known while writing this code), or cannot get ExtraStatus (status == -1)
			return UwpBatteryStatus.NotPresent;
		}
	}
}
