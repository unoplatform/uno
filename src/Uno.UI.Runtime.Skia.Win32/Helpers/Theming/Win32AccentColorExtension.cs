using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Windows.UI;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32AccentColorExtension : IAccentColorExtension
{
	private const string AccentRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

	public static Win32AccentColorExtension Instance { get; } = new();

	public event EventHandler? AccentColorChanged;

	private unsafe Win32AccentColorExtension()
	{
		Task.Run(() =>
		{
			try
			{
				using var hSubKeyString = new Win32Helper.NativeNulTerminatedUtf16String(AccentRegistryKey);
				HKEY hSubKey;
				var errorCode = PInvoke.RegOpenKeyEx(HKEY.HKEY_CURRENT_USER, hSubKeyString, 0, REG_SAM_FLAGS.KEY_READ, &hSubKey);
				if (errorCode != WIN32_ERROR.ERROR_SUCCESS)
				{
					this.LogError()?.Error($"{nameof(PInvoke.RegOpenKeyEx)} failed with error code: {Win32Helper.GetErrorMessage((uint)errorCode)}");
					return;
				}
				while (true)
				{
					var errorCode2 = PInvoke.RegNotifyChangeKeyValue(hSubKey, false, REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET, HANDLE.Null, false);
					if (errorCode2 != WIN32_ERROR.ERROR_SUCCESS)
					{
						this.LogError()?.Error($"{nameof(PInvoke.RegNotifyChangeKeyValue)} failed with error code: {Win32Helper.GetErrorMessage((uint)errorCode2)}");
						return;
					}

					NativeDispatcher.Main.Enqueue(() => AccentColorChanged?.Invoke(this, EventArgs.Empty));
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"An exception was thrown in {nameof(Win32AccentColorExtension)}'s notification loop", e);
				}
				throw;
			}
		});
	}

	/// <summary>
	/// Reads the accent color palette from the Windows registry.
	/// Registry key: HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent
	/// Value: AccentPalette (REG_BINARY, 32 bytes = 8 × 4-byte RGBA values)
	/// Layout: Light3[0:3], Light2[4:7], Light1[8:11], Accent[12:15], Dark1[16:19], Dark2[20:23], Dark3[24:27], unused[28:31]
	/// </summary>
	public unsafe AccentColorPalette? GetAccentColorPalette()
	{
		using var hSubKey = new Win32Helper.NativeNulTerminatedUtf16String(AccentRegistryKey);
		using var valueName = new Win32Helper.NativeNulTerminatedUtf16String("AccentPalette");

		var buffer = stackalloc byte[32];
		REG_VALUE_TYPE regValueType;
		uint bufferSize = 32;
		var errorCode = PInvoke.RegGetValue(HKEY.HKEY_CURRENT_USER, hSubKey, valueName, REG_ROUTINE_FLAGS.RRF_RT_REG_BINARY, &regValueType, buffer, &bufferSize);
		if (errorCode is not WIN32_ERROR.ERROR_SUCCESS || bufferSize < 32)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to read AccentPalette from registry: {Win32Helper.GetErrorMessage((uint)errorCode)}");
			}
			return null;
		}

		// Each entry is 4 bytes: R, G, B, A (but alpha is always 0xFF for the first 7 entries)
		return new AccentColorPalette(
			accent: Color.FromArgb(0xFF, buffer[12], buffer[13], buffer[14]),
			light1: Color.FromArgb(0xFF, buffer[8], buffer[9], buffer[10]),
			light2: Color.FromArgb(0xFF, buffer[4], buffer[5], buffer[6]),
			light3: Color.FromArgb(0xFF, buffer[0], buffer[1], buffer[2]),
			dark1: Color.FromArgb(0xFF, buffer[16], buffer[17], buffer[18]),
			dark2: Color.FromArgb(0xFF, buffer[20], buffer[21], buffer[22]),
			dark3: Color.FromArgb(0xFF, buffer[24], buffer[25], buffer[26])
		);
	}
}
