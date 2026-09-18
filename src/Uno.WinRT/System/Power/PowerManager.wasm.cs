#nullable enable


using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

using NativeMethods = __Windows.__System.Power.PowerManager.NativeMethods;

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

		NativeMethods.StartChargingChange();
		_isChargingChangedSubscribed = true;
	}

	private static void EndChargingChange()
	{
		if (_powerSupplyStatusChanged.IsActive || _batteryStatusChanged.IsActive)
		{
			// One of the related events is still active.
			return;
		}

		NativeMethods.EndChargingChange();
		_isChargingChangedSubscribed = false;
	}

	static partial void StartRemainingChargePercent() => NativeMethods.StartRemainingChargePercent();

	static partial void EndRemainingChargePercent() => NativeMethods.EndRemainingChargePercent();

	static partial void StartRemainingDischargeTime() => NativeMethods.StartRemainingDischargeTime();

	static partial void EndRemainingDischargedTime() => NativeMethods.EndRemainingDischargedTime();

	[JSExport]
	internal static int DispatchChargingChanged()
	{
		RaiseBatteryStatusChanged();
		RaisePowerSupplyStatusChanged();
		return 0;
	}

	[JSExport]
	internal static void DispatchRemainingChargePercentChanged() => RaiseRemainingChargePercentChanged();

	[JSExport]
	internal static void DispatchRemainingDischargeTimeChanged() => RaiseRemainingDischargeTimeChanged();

	private static BatteryStatus GetBatteryStatus()
	{
		EnsureInitialized();

		var batteryStatusString = NativeMethods.GetBatteryStatus();
		return Enum.TryParse<BatteryStatus>(batteryStatusString, out var batteryStatus) ?
			batteryStatus : Power.BatteryStatus.NotPresent;
	}

	private static PowerSupplyStatus GetPowerSupplyStatus()
	{
		EnsureInitialized();

		var powerSupplyStatusString = NativeMethods.GetPowerSupplyStatus();
		return Enum.TryParse<PowerSupplyStatus>(powerSupplyStatusString, out var powerSupplyStatus) ?
			powerSupplyStatus : Power.PowerSupplyStatus.NotPresent;
	}

	private static int GetRemainingChargePercent()
	{
		EnsureInitialized();

		var remainingCharge = NativeMethods.GetRemainingChargePercent();
		if (!double.IsNaN(remainingCharge))
		{
			return (int)(remainingCharge * 100);
		}

		return 100;
	}

	private static TimeSpan GetRemainingDischargeTime()
	{
		EnsureInitialized();

		var remainingDischargeTimeInSeconds = NativeMethods.GetRemainingDischargeTime();
		if (!double.IsNaN(remainingDischargeTimeInSeconds) &&
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
			await NativeMethods.InitializeAsync();
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
