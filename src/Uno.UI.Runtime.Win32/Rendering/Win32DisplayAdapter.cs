#nullable enable

using System;
using System.Globalization;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.Runtime.Win32;

/// <summary>
/// The adapter driving a window's display, as Windows reports it, for contexts whose API can't report a LUID. The ids
/// are 0 for an adapter that isn't on PCI, such as an indirect display.
/// </summary>
internal readonly record struct Win32DisplayAdapter(uint VendorId, uint DeviceId, string Name)
{
	internal static Win32DisplayAdapter? Override { get; set; }

	internal static unsafe Win32DisplayAdapter? FromWindow(HWND hwnd)
	{
		if (Override is { } adapterOverride)
		{
			return adapterOverride;
		}

		var monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
		var monitorInfo = new MONITORINFOEXW { monitorInfo = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFOEXW) } };
		if (!PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&monitorInfo))
		{
			return null;
		}

		var monitorName = monitorInfo.szDevice.ToString();
		var device = new DISPLAY_DEVICEW { cb = (uint)sizeof(DISPLAY_DEVICEW) };
		for (uint i = 0; PInvoke.EnumDisplayDevices(null, i, ref device, 0); i++)
		{
			if (device.DeviceName.ToString() == monitorName)
			{
				return Parse(device.DeviceID.ToString(), device.DeviceString.ToString());
			}
		}

		return null;
	}

	internal static Win32DisplayAdapter Parse(string deviceId, string name)
		=> new(ReadId(deviceId, "VEN_"), ReadId(deviceId, "DEV_"), name);

	/// <summary>
	/// Whether a renderer reporting these values runs on this adapter. Ids decide when both sides have them, and a
	/// renderer that can only be told apart by name has to name this adapter.
	/// </summary>
	internal bool Matches(uint vendorId, uint deviceId, string? name)
		=> vendorId != 0 && VendorId != 0
			? vendorId == VendorId && (deviceId == 0 || DeviceId == 0 || deviceId == DeviceId)
			: Name is { Length: > 0 } && name?.StartsWith(Name, StringComparison.OrdinalIgnoreCase) == true;

	private static uint ReadId(string deviceId, string prefix)
	{
		var start = deviceId.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) + prefix.Length;
		return start >= prefix.Length
			&& start + 4 <= deviceId.Length
			&& uint.TryParse(deviceId.AsSpan(start, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)
				? id
				: 0;
	}
}
