using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32SystemThemeHelperExtension : ISystemThemeHelperExtension
{
	public static Win32SystemThemeHelperExtension Instance { get; } = new();

	public event EventHandler? SystemThemeChanged;

	private unsafe Win32SystemThemeHelperExtension()
	{
		Task.Run(() =>
		{
			var hSubKeyString = Marshal.StringToHGlobalUni("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			using var hSubKeyDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, hSubKeyString);
			HKEY hSubKey;
			// We're not closing this handle since this class lasts the whole lifetime of the app.
			var errorCode = PInvoke.RegOpenKeyEx(HKEY.HKEY_CURRENT_USER, new PCWSTR((char*)hSubKeyString), 0, REG_SAM_FLAGS.KEY_READ, &hSubKey);
			if (errorCode != WIN32_ERROR.ERROR_SUCCESS)
			{
				this.Log().Log(LogLevel.Error, errorCode, static errorCode => $"{nameof(PInvoke.RegOpenKeyEx)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
				return;
			}
			while (true)
			{
				// RegNotifyChangeKeyValue will block until the theme changes
				var errorCode2 = PInvoke.RegNotifyChangeKeyValue(hSubKey, false, REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET, HANDLE.Null, false);
				if (errorCode2 != WIN32_ERROR.ERROR_SUCCESS)
				{
					this.Log().Log(LogLevel.Error, errorCode2, static errorCode => $"{nameof(PInvoke.RegNotifyChangeKeyValue)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
					return;
				}

				NativeDispatcher.Main.Enqueue(() => SystemThemeChanged?.Invoke(this, EventArgs.Empty));
			}
		});
	}

	public unsafe SystemTheme GetSystemTheme()
	{
		var hSubKey = Marshal.StringToHGlobalUni("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
		using var hSubKeyDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, hSubKey);
		var appsUseLightTheme = Marshal.StringToHGlobalUni("AppsUseLightTheme");
		using var appsUseLightThemeDisposable = new DisposableStruct<IntPtr>(Marshal.FreeHGlobal, appsUseLightTheme);

		int value = 0;
		REG_VALUE_TYPE regValueType;
		uint valueSize = (uint)Marshal.SizeOf<int>();
		var errorCode = PInvoke.RegGetValue(HKEY.HKEY_CURRENT_USER, new PCWSTR((char*)hSubKey), new PCWSTR((char*)appsUseLightTheme), REG_ROUTINE_FLAGS.RRF_RT_DWORD, &regValueType, &value, &valueSize);
		if (errorCode is not WIN32_ERROR.ERROR_SUCCESS)
		{
			this.Log().Log(LogLevel.Error, errorCode, static errorCode => $"{nameof(PInvoke.RegGetValue)} failed with error code : {Win32Helper.GetErrorMessage((uint)errorCode)}");
			return SystemTheme.Light;
		}

		return value == 1 ? SystemTheme.Light : SystemTheme.Dark;
	}
}
