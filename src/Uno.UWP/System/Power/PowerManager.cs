#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.Foundation;
using Uno.Helpers;

namespace Windows.System.Power;

/// <summary>
/// Provides access to information about a device's battery and power supply status.
/// </summary>
public static partial class PowerManager
{
	private static object _syncLock = new object();

	private static PowerSupplyStatus? _lastPowerSupplyStatus;
	private static int? _lastRemainingChargePercent;
	private static BatteryStatus? _lastBatteryStatus;
#if __WASM__
	private static TimeSpan _lastRemainingDischargeTime;
#endif
#if !__WASM__
	private static EnergySaverStatus? _lastEnergySaverStatus;
#endif

	private readonly static StartStopEventWrapper<EventHandler<object?>> _powerSupplyStatusChanged;
	private readonly static StartStopEventWrapper<EventHandler<object?>> _remainingChargePercentChanged;
	private readonly static StartStopEventWrapper<EventHandler<object?>> _batteryStatusChanged;
#if __WASM__
	private readonly static StartStopEventWrapper<EventHandler<object?>> _remainingDischargeTimeChanged;
#endif
#if !__WASM__
	private readonly static StartStopEventWrapper<EventHandler<object>> _energySaverStatusChanged;
#endif

	static PowerManager()
	{
		_powerSupplyStatusChanged = new(() => StartPowerSupplyStatus(), () => EndPowerSupplyStatus(), _syncLock);
		_remainingChargePercentChanged = new(() => StartRemainingChargePercent(), () => EndRemainingChargePercent(), _syncLock);
		_batteryStatusChanged = new(() => StartBatteryStatus(), () => EndBatteryStatus(), _syncLock);
#if __WASM__
		_remainingDischargeTimeChanged = new(() => StartRemainingDischargeTime(), () => EndRemainingDischargedTime(), _syncLock);
#endif
#if !__WASM__
		_energySaverStatusChanged = new(() => StartEnergySaverStatus(), () => EndEnergySaverStatus(), _syncLock);
#endif

		InitializePlatform();
	}

	/// <summary>
	/// Gets the device's battery status.
	/// </summary>
	public static BatteryStatus BatteryStatus => GetBatteryStatus();

	/// <summary>
	/// Gets the device's power supply status.
	/// </summary>
	public static PowerSupplyStatus PowerSupplyStatus => GetPowerSupplyStatus();

	/// <summary>
	/// Gets the total percentage of charge remaining from all batteries connected to the device.
	/// </summary>
	public static int RemainingChargePercent => GetRemainingChargePercent();

#if __WASM__
	/// <summary>
	/// Gets the total runtime remaining from all batteries connected to the device.
	/// </summary>
	public static TimeSpan RemainingDischargeTime => GetRemainingDischargeTime();
#endif

#if !__WASM__ // Cannot detect energy saver status on WASM yet.
	/// <summary>
	/// Gets the devices's battery saver status, indicating when to save energy.
	/// </summary>
	public static EnergySaverStatus EnergySaverStatus => GetEnergySaverStatus();
#endif

	public static event EventHandler<object?> PowerSupplyStatusChanged
	{
		add => _powerSupplyStatusChanged.AddHandler(value);
		remove => _powerSupplyStatusChanged.RemoveHandler(value);
	}

	public static event EventHandler<object?> RemainingChargePercentChanged
	{
		add => _remainingChargePercentChanged.AddHandler(value);
		remove => _remainingChargePercentChanged.RemoveHandler(value);
	}

	public static event EventHandler<object?> BatteryStatusChanged
	{
		add => _batteryStatusChanged.AddHandler(value);
		remove => _batteryStatusChanged.RemoveHandler(value);
	}

#if __WASM__
	public static event EventHandler<object?> RemainingDischargeTimeChanged
	{
		add => _remainingDischargeTimeChanged.AddHandler(value);
		remove => _remainingDischargeTimeChanged.RemoveHandler(value);
	}
#endif

#if !__WASM__
	public static event EventHandler<object> EnergySaverStatusChanged
	{
		add => _energySaverStatusChanged.AddHandler(value);
		remove => _energySaverStatusChanged.RemoveHandler(value);
	}
#endif

	internal static void RaiseRemainingChargePercentChanged()
	{
		var currentValue = RemainingChargePercent;
		if (_lastRemainingChargePercent != currentValue)
		{
			_lastRemainingChargePercent = currentValue;
			_remainingChargePercentChanged.Event?.Invoke(null, null);
		}
	}

	internal static void RaiseBatteryStatusChanged()
	{
		var currentValue = BatteryStatus;
		if (_lastBatteryStatus != currentValue)
		{
			_lastBatteryStatus = currentValue;
			_batteryStatusChanged.Event?.Invoke(null, null);
		}
	}

	internal static void RaisePowerSupplyStatusChanged()
	{
		var currentValue = PowerSupplyStatus;
		if (_lastPowerSupplyStatus != currentValue)
		{
			_lastPowerSupplyStatus = currentValue;
			_powerSupplyStatusChanged.Event?.Invoke(null, null);
		}
	}

#if __WASM__
	internal static void RaiseRemainingDischargeTimeChanged()
	{
		var currentValue = RemainingDischargeTime;
		if (_lastRemainingDischargeTime != currentValue)
		{
			_lastRemainingDischargeTime = currentValue;
			_remainingDischargeTimeChanged.Event?.Invoke(null, null);
		}
	}
#endif

#if !__WASM__
	internal static void RaiseEnergySaverStatusChanged()
	{
		var currentValue = EnergySaverStatus;
		if (_lastEnergySaverStatus != currentValue)
		{
			_lastEnergySaverStatus = currentValue;
			_energySaverStatusChanged.Event?.Invoke(null, null);
		}
	}
#endif

	static partial void InitializePlatform();

	static partial void StartPowerSupplyStatus();

	static partial void EndPowerSupplyStatus();

	static partial void StartRemainingChargePercent();

	static partial void EndRemainingChargePercent();

	static partial void StartBatteryStatus();

	static partial void EndBatteryStatus();

#if __WASM__
	static partial void StartRemainingDischargeTime();

	static partial void EndRemainingDischargedTime();
#endif

#if !__WASM__
	static partial void StartEnergySaverStatus();

	static partial void EndEnergySaverStatus();
#endif
}
#endif
