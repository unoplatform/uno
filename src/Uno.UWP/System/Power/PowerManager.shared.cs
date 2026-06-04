using System;

namespace Windows.System.Power;

public static partial class PowerManager
{
	internal static BatteryStatus GetBatteryStatus(PowerSourceSnapshot snapshot)
	{
		if (!snapshot.IsPresent)
		{
			return BatteryStatus.NotPresent;
		}

		if (snapshot.IsCharging)
		{
			return BatteryStatus.Charging;
		}

		return snapshot.IsExternalPower
			? BatteryStatus.Idle
			: BatteryStatus.Discharging;
	}

	internal static PowerSupplyStatus GetPowerSupplyStatus(PowerSourceSnapshot snapshot) =>
		snapshot.IsExternalPower ? PowerSupplyStatus.Adequate : PowerSupplyStatus.NotPresent;

	internal static int GetRemainingChargePercent(PowerSourceSnapshot snapshot)
	{
		if (!snapshot.IsPresent || snapshot.MaxCapacity <= 0)
		{
			return 0;
		}

		var chargePercent = (int)Math.Round((double)snapshot.CurrentCapacity * 100 / snapshot.MaxCapacity);
		return Math.Clamp(chargePercent, 0, 100);
	}

	internal readonly record struct PowerSourceSnapshot(
		bool IsPresent,
		bool IsExternalPower,
		bool IsCharging,
		int CurrentCapacity,
		int MaxCapacity);
}
