#nullable enable


namespace Windows.System.Power;

partial class PowerManager
{
	private const string JsType = "Windows.System.Power.PowerManager";

	private static bool _isInitialized;


	static partial void StartPowerSupplyStatus() => StartChargingChanged();

	static partial void StopPowerSupplyStatus() => EndChargingChanged();

	private static void StartChargingChanged()
	{

	}

	static partial void StartRemainingChargePercent()



	static partial void EndRemainingChargePercent();

	static partial void StartBatteryStatus();

	static partial void EndBatteryStatus();

	static partial void StartRemainingDischargeTime()
	{

	}

	static partial void EndRemainingDischargedTime()
	{
	}

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
		EnsureInitialized();

		var batteryStatusString = WebAssemblyRuntime.InvokeJS($"{JsType}.getBatteryStatus()");
		return Enum.TryParse<BatteryStatus>(batteryStatusString, out var batteryStatus) ?
			batteryStatus : Power.BatteryStatus.NotPresent;
	}

	private static PowerSupplyStatus GetPowerSupplyStatus()
	{
		EnsureInitialized();

		var powerSupplyStatusString = WebAssemblyRuntime.InvokeJS($"{JsType}.getPowerSupplyStatus()");
		return Enum.TryParse<PowerSupplyStatus>(powerSupplyStatusString, out var powerSupplyStatus) ?
			powerSupplyStatus : Power.PowerSupplyStatus.NotPresent;
	}

	private static int GetRemainingChargePercent()
	{
		EnsureInitialized();

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
		EnsureInitialized();

		var remainingDischargeTimeString = WebAssemblyRuntime.InvokeJS($"{JsType}.getRemainingDischargeTime()");
		if (double.TryParse(remainingDischargeTimeString, out var remainingDischargeTimeInSeconds) &&
			!double.IsNaN(remainingDischargeTimeInSeconds) &&
			remainingDischargeTimeInSeconds >= 0)
		{
			return TimeSpan.FromSeconds(remainingDischargeTimeInSeconds);
		}

		return TimeSpan.MaxValue;
	}

	private static void EnsureInitialized()
	{
		if (!_isInitialized)
		{
			throw new InvalidOperationException("You must call the InitializeAsync method of PowerManager before using any of its properties.");
		}
	}
}
