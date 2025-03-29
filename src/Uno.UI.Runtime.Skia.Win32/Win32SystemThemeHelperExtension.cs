using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32SystemThemeHelperExtension : ISystemThemeHelperExtension
{
	private const string SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

	public static Win32SystemThemeHelperExtension Instance { get; } = new();

	public event EventHandler? SystemThemeChanged;

	private unsafe Win32SystemThemeHelperExtension()
	{
		Task.Run(() =>
		{
			try
			{
				using var hSubKeyString = new Win32Helper.NativeNulTerminatedUtf16String(SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize);
				HKEY hSubKey;
				// We're not closing this handle since this class lasts the whole lifetime of the app.
				var errorCode = PInvoke.RegOpenKeyEx(HKEY.HKEY_CURRENT_USER, hSubKeyString, 0, REG_SAM_FLAGS.KEY_READ, &hSubKey);
				if (errorCode != WIN32_ERROR.ERROR_SUCCESS)
				{
					this.LogError()?.Error($"{nameof(PInvoke.RegOpenKeyEx)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
					return;
				}
				while (true)
				{
					// RegNotifyChangeKeyValue will block until the theme changes
					var errorCode2 = PInvoke.RegNotifyChangeKeyValue(hSubKey, false, REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET, HANDLE.Null, false);
					if (errorCode2 != WIN32_ERROR.ERROR_SUCCESS)
					{
						this.LogError()?.Error($"{nameof(PInvoke.RegNotifyChangeKeyValue)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
						return;
					}

					NativeDispatcher.Main.Enqueue(() => SystemThemeChanged?.Invoke(this, EventArgs.Empty));
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"An exception was thrown in {nameof(Win32SystemThemeHelperExtension)}'s notification loop", e);
				}
				throw;
			}
		});
	}

	public unsafe SystemTheme GetSystemTheme()
	{
		using var hSubKey = new Win32Helper.NativeNulTerminatedUtf16String(SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize);
		using var appsUseLightTheme = new Win32Helper.NativeNulTerminatedUtf16String("AppsUseLightTheme");

		int value = 0;
		REG_VALUE_TYPE regValueType;
		uint valueSize = (uint)Marshal.SizeOf<int>();
		var errorCode = PInvoke.RegGetValue(HKEY.HKEY_CURRENT_USER, hSubKey, appsUseLightTheme, REG_ROUTINE_FLAGS.RRF_RT_DWORD, &regValueType, &value, &valueSize);
		if (errorCode is not WIN32_ERROR.ERROR_SUCCESS)
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegGetValue)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
			return SystemTheme.Light;
		}

		return value == 1 ? SystemTheme.Light : SystemTheme.Dark;
	}
}
