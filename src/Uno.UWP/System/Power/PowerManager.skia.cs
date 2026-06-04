#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Uno.Foundation.Logging;

namespace Windows.System.Power;

public static partial class PowerManager
{
	private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
	private const string IOKitFramework = "/System/Library/Frameworks/IOKit.framework/IOKit";
	private const string ObjectiveCFramework = "/usr/lib/libobjc.A.dylib";
	private const uint Utf8Encoding = 0x08000100;
	private const int CfNumberIntType = 9;

	private static readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

	private static Timer? _statusPollingTimer;
	private static bool _isMacOS;

	static partial void InitializePlatform() =>
		_isMacOS = OperatingSystem.IsMacOS();

	internal static bool IsPlatformSupported() => _isMacOS;

	static partial void StartPowerSupplyStatus() =>
		TryStartStatusPolling(nameof(PowerSupplyStatusChanged));

	static partial void EndPowerSupplyStatus() =>
		TryStopStatusPolling();

	static partial void StartRemainingChargePercent() =>
		TryStartStatusPolling(nameof(RemainingChargePercentChanged));

	static partial void EndRemainingChargePercent() =>
		TryStopStatusPolling();

	static partial void StartBatteryStatus() =>
		TryStartStatusPolling(nameof(BatteryStatusChanged));

	static partial void EndBatteryStatus() =>
		TryStopStatusPolling();

	static partial void StartEnergySaverStatus() =>
		TryStartStatusPolling(nameof(EnergySaverStatusChanged));

	static partial void EndEnergySaverStatus() =>
		TryStopStatusPolling();

	private static BatteryStatus GetBatteryStatus()
	{
		EnsurePlatformSupported(nameof(BatteryStatus));
		return GetBatteryStatus(GetPowerSourceSnapshot());
	}

	private static EnergySaverStatus GetEnergySaverStatus()
	{
		EnsurePlatformSupported(nameof(EnergySaverStatus));
		return IsLowPowerModeEnabled() ? EnergySaverStatus.On : EnergySaverStatus.Off;
	}

	private static PowerSupplyStatus GetPowerSupplyStatus()
	{
		EnsurePlatformSupported(nameof(PowerSupplyStatus));
		return GetPowerSupplyStatus(GetPowerSourceSnapshot());
	}

	private static int GetRemainingChargePercent()
	{
		EnsurePlatformSupported(nameof(RemainingChargePercent));
		return GetRemainingChargePercent(GetPowerSourceSnapshot());
	}

	private static void EnsurePlatformSupported(string memberName)
	{
		if (!_isMacOS)
		{
			throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException("Windows.System.Power.PowerManager", memberName);
		}
	}

	private static void TryStartStatusPolling(string eventName)
	{
		if (!_isMacOS)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Power.PowerManager", $"event {eventName}");
			return;
		}

		lock (_syncLock)
		{
			if (_statusPollingTimer is not null)
			{
				return;
			}

			SeedLastKnownValues();
			_statusPollingTimer = new Timer(static _ => PollStatuses(), null, _pollingInterval, _pollingInterval);
		}
	}

	private static void TryStopStatusPolling()
	{
		if (!_isMacOS)
		{
			return;
		}

		lock (_syncLock)
		{
			if (_batteryStatusChanged.IsActive ||
				_powerSupplyStatusChanged.IsActive ||
				_remainingChargePercentChanged.IsActive ||
				_energySaverStatusChanged.IsActive)
			{
				return;
			}

			_statusPollingTimer?.Dispose();
			_statusPollingTimer = null;
		}
	}

	private static void SeedLastKnownValues()
	{
		var snapshot = GetPowerSourceSnapshot();
		_lastBatteryStatus = GetBatteryStatus(snapshot);
		_lastPowerSupplyStatus = GetPowerSupplyStatus(snapshot);
		_lastRemainingChargePercent = GetRemainingChargePercent(snapshot);
		_lastEnergySaverStatus = IsLowPowerModeEnabled() ? EnergySaverStatus.On : EnergySaverStatus.Off;
	}

	private static void PollStatuses()
	{
		lock (_syncLock)
		{
			if (_statusPollingTimer is null)
			{
				return;
			}

			try
			{
				if (_batteryStatusChanged.IsActive)
				{
					RaiseBatteryStatusChanged();
				}

				if (_powerSupplyStatusChanged.IsActive)
				{
					RaisePowerSupplyStatusChanged();
				}

				if (_remainingChargePercentChanged.IsActive)
				{
					RaiseRemainingChargePercentChanged();
				}

				if (_energySaverStatusChanged.IsActive)
				{
					RaiseEnergySaverStatusChanged();
				}
			}
			catch (Exception exception)
			{
				if (typeof(PowerManager).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(PowerManager).Log().LogWarning("Failed to poll power status changes on macOS", exception);
				}
			}
		}
	}

	private static PowerSourceSnapshot GetPowerSourceSnapshot()
	{
		IntPtr powerSourcesInfo = IntPtr.Zero;
		IntPtr powerSourcesList = IntPtr.Zero;

		try
		{
			powerSourcesInfo = IOPSCopyPowerSourcesInfo();
			if (powerSourcesInfo == IntPtr.Zero)
			{
				return default;
			}

			powerSourcesList = IOPSCopyPowerSourcesList(powerSourcesInfo);
			if (powerSourcesList == IntPtr.Zero)
			{
				return default;
			}

			var totalCurrentCapacity = 0;
			var totalMaxCapacity = 0;
			var isPresent = false;
			var isCharging = false;
			var isExternalPower = false;

			var count = CFArrayGetCount(powerSourcesList);
			for (nint index = 0; index < count; index++)
			{
				var powerSource = CFArrayGetValueAtIndex(powerSourcesList, index);
				if (powerSource == IntPtr.Zero)
				{
					continue;
				}

				var description = IOPSGetPowerSourceDescription(powerSourcesInfo, powerSource);
				if (description == IntPtr.Zero || !GetBooleanValue(description, "Is Present"))
				{
					continue;
				}

				isPresent = true;
				totalCurrentCapacity += GetInt32Value(description, "Current Capacity");
				totalMaxCapacity += GetInt32Value(description, "Max Capacity");
				isCharging |= GetBooleanValue(description, "Is Charging");
				isExternalPower |= string.Equals(GetStringValue(description, "Power Source State"), "AC Power", StringComparison.Ordinal);
			}

			return new(isPresent, isExternalPower, isCharging, totalCurrentCapacity, totalMaxCapacity);
		}
		finally
		{
			if (powerSourcesList != IntPtr.Zero)
			{
				CFRelease(powerSourcesList);
			}

			if (powerSourcesInfo != IntPtr.Zero)
			{
				CFRelease(powerSourcesInfo);
			}
		}
	}

	private static bool IsLowPowerModeEnabled()
	{
		var processInfoClass = objc_getClass("NSProcessInfo");
		if (processInfoClass == IntPtr.Zero)
		{
			return false;
		}

		var processInfo = IntPtr_objc_msgSend(processInfoClass, sel_registerName("processInfo"));
		if (processInfo == IntPtr.Zero)
		{
			return false;
		}

		var isLowPowerModeEnabled = sel_registerName("isLowPowerModeEnabled");
		if (byte_objc_msgSend_IntPtr(processInfo, sel_registerName("respondsToSelector:"), isLowPowerModeEnabled) == 0)
		{
			return false;
		}

		return byte_objc_msgSend(processInfo, isLowPowerModeEnabled) != 0;
	}

	private static bool GetBooleanValue(IntPtr dictionary, string key)
	{
		var value = GetDictionaryValue(dictionary, key);
		return value != IntPtr.Zero && CFBooleanGetValue(value);
	}

	private static int GetInt32Value(IntPtr dictionary, string key)
	{
		var value = GetDictionaryValue(dictionary, key);
		return value != IntPtr.Zero && CFNumberGetValue(value, CfNumberIntType, out var result)
			? result
			: 0;
	}

	private static string? GetStringValue(IntPtr dictionary, string key)
	{
		var value = GetDictionaryValue(dictionary, key);
		return value == IntPtr.Zero ? null : CFStringToString(value);
	}

	private static IntPtr GetDictionaryValue(IntPtr dictionary, string key)
	{
		var keyHandle = CFStringCreateWithCString(IntPtr.Zero, key, Utf8Encoding);
		if (keyHandle == IntPtr.Zero)
		{
			return IntPtr.Zero;
		}

		try
		{
			return CFDictionaryGetValue(dictionary, keyHandle);
		}
		finally
		{
			CFRelease(keyHandle);
		}
	}

	private static string? CFStringToString(IntPtr value)
	{
		var length = CFStringGetLength(value);
		if (length == 0)
		{
			return string.Empty;
		}

		var bufferSize = CFStringGetMaximumSizeForEncoding(length, Utf8Encoding) + 1;
		var buffer = new byte[(int)bufferSize];
		if (!CFStringGetCString(value, buffer, buffer.Length, Utf8Encoding))
		{
			return null;
		}

		var nullTerminatorIndex = Array.IndexOf(buffer, (byte)0);
		var count = nullTerminatorIndex >= 0 ? nullTerminatorIndex : buffer.Length;
		return Encoding.UTF8.GetString(buffer, 0, count);
	}

	[DllImport(CoreFoundationFramework)]
	private static extern void CFRelease(IntPtr cf);

	[DllImport(CoreFoundationFramework)]
	private static extern nint CFArrayGetCount(IntPtr array);

	[DllImport(CoreFoundationFramework)]
	private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

	[DllImport(CoreFoundationFramework)]
	private static extern IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

	[DllImport(CoreFoundationFramework)]
	private static extern bool CFBooleanGetValue(IntPtr boolean);

	[DllImport(CoreFoundationFramework)]
	private static extern bool CFNumberGetValue(IntPtr number, int type, out int value);

	[DllImport(CoreFoundationFramework)]
	private static extern nint CFStringGetLength(IntPtr handle);

	[DllImport(CoreFoundationFramework)]
	private static extern nint CFStringGetMaximumSizeForEncoding(nint length, uint encoding);

	[DllImport(CoreFoundationFramework)]
	private static extern bool CFStringGetCString(IntPtr handle, byte[] buffer, nint bufferSize, uint encoding);

	[DllImport(CoreFoundationFramework)]
	private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

	[DllImport(IOKitFramework)]
	private static extern IntPtr IOPSCopyPowerSourcesInfo();

	[DllImport(IOKitFramework)]
	private static extern IntPtr IOPSCopyPowerSourcesList(IntPtr blob);

	[DllImport(IOKitFramework)]
	private static extern IntPtr IOPSGetPowerSourceDescription(IntPtr blob, IntPtr powerSource);

	[DllImport(ObjectiveCFramework, EntryPoint = "objc_getClass")]
	private static extern IntPtr objc_getClass(string name);

	[DllImport(ObjectiveCFramework, EntryPoint = "sel_registerName")]
	private static extern IntPtr sel_registerName(string name);

	[DllImport(ObjectiveCFramework, EntryPoint = "objc_msgSend")]
	private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

	[DllImport(ObjectiveCFramework, EntryPoint = "objc_msgSend")]
	private static extern byte byte_objc_msgSend(IntPtr receiver, IntPtr selector);

	[DllImport(ObjectiveCFramework, EntryPoint = "objc_msgSend")]
	private static extern byte byte_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);
}
