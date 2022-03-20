#nullable enable

using System;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;
using Uno.Foundation.Logging;

namespace Windows.System.Power;

public partial class PowerManager
{
	private const string JsType = "Windows.System.Power.PowerManager";

	[Preserve]
	public static int DispatchChargingChanged()
	{
		RaiseBatteryStatusChanged();
		RaisePowerSupplyStatusChanged();
		return 0;
	}

	[Preserve]
	public static int DispatchRemainingChargePercentChanged()
	{
		RaiseRemainingChargePercentChanged();
		return 0;
	}

	[Preserve]
	public static int DispatchRemainingDischargeTimeChanged()
	{
		RaiseRemainingDischargeTimeChanged();
		return 0;
	}

	private static BatteryStatus GetBatteryStatus()
	{
		InitializeAsync(); // TODO

		var batteryStatusString = WebAssemblyRuntime.InvokeJS($"{JsType}.getBatteryStatus()");
		return Enum.TryParse<BatteryStatus>(batteryStatusString, out var batteryStatus) ?
			batteryStatus : BatteryStatus.NotPresent;
	}

	private static PowerSupplyStatus GetPowerSupplyStatus()
	{
		InitializeAsync(); // TODO

		var powerSupplyStatusString = WebAssemblyRuntime.InvokeJS($"{JsType}.getPowerSupplyStatus()");
		return Enum.TryParse<PowerSupplyStatus>(powerSupplyStatusString, out var powerSupplyStatus) ?
			powerSupplyStatus : PowerSupplyStatus.NotPresent;
	}

	private static int GetRemainingChargePercent()
	{
		InitializeAsync(); // TODO

		var remainingChargeString = WebAssemblyRuntime.InvokeJS($"{JsType}.getRemainingChargePercent()");
		if (double.TryParse(remainingChargeString, out var remainingCharge) &&
			!double.IsNaN(remainingCharge))
		{
			return (int)(remainingCharge * 100);
		}

		return 100;
	}

	private static TimeSpan GetRemainingDischargeTime()
	{
		InitializeAsync(); // TODO

		var remainingDischargeTimeString = WebAssemblyRuntime.InvokeJS($"{JsType}.getRemainingDischargeTime()");
		if (double.TryParse(remainingDischargeTimeString, out var remainingDischargeTimeInSeconds) &&
			!double.IsNaN(remainingDischargeTimeInSeconds) &&
			remainingDischargeTimeInSeconds >= 0)
		{
			return TimeSpan.FromSeconds(remainingDischargeTimeInSeconds);
		}

		return TimeSpan.MaxValue;
	}

	private static async Task InitializeAsync()
	{
		try
		{
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.initializeAsync()");
		}
		catch (Exception ex)
		{
			if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
			{
				typeof(PowerManager).Log().LogError("Could not initialize PowerManager", ex);
			}
		}
	}
}
