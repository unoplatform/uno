#nullable enable


using System;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;
using Uno.Foundation.Logging;

namespace Windows.System.Power;

partial class PowerManager
{
	private static bool _isInitialized;
	private static bool _isChargingChangedSubscribed;

	static partial void StartPowerSupplyStatus() => StartChargingChange();

	static partial void EndPowerSupplyStatus() => EndChargingChange();

	static partial void StartBatteryStatus() => StartChargingChange();

	static partial void EndBatteryStatus() => EndChargingChange();

	private static void StartChargingChange()
	{
		if (_isChargingChangedSubscribed)
		{
			return;
		}

		WebAssemblyRuntime.InvokeJS($"{JsType}.startChargingChange()");
		_isChargingChangedSubscribed = true;
	}

	private static void EndChargingChange()
	{
		if (_powerSupplyStatusChanged.IsActive || _batteryStatusChanged.IsActive)
		{
			// One of the related events is still active.
			return;
		}

		WebAssemblyRuntime.InvokeJS($"{JsType}.endChargingChange()");
		_isChargingChangedSubscribed = false;
	}

	static partial void StartRemainingChargePercent() => WebAssemblyRuntime.InvokeJS($"{JsType}.startRemainingChargePercentChange()");

	static partial void EndRemainingChargePercent() => WebAssemblyRuntime.InvokeJS($"{JsType}.endRemainingChargePercentChange()");

	static partial void StartRemainingDischargeTime() => WebAssemblyRuntime.InvokeJS($"{JsType}.startRemainingDischargeTimeChange()");

	static partial void EndRemainingDischargedTime() => WebAssemblyRuntime.InvokeJS($"{JsType}.endRemainingDischargeTimeChange()");

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

	private static async Task<bool> InitializePlatformAsync()
	{
		try
		{
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.initializeAsync()");
			_isInitialized = true;
			return true;
		}
		catch (Exception ex)
		{
			if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
			{
				typeof(PowerManager).Log().LogError("Could not initialize PowerManager", ex);
			}
			return false;
		}
	}
}
