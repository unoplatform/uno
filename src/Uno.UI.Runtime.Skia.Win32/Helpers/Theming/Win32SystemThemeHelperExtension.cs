using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Windows.Win32.UI.Accessibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;
using Windows.UI;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32SystemThemeHelperExtension : ISystemThemeHelperExtension
{
	private const string SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

	public static Win32SystemThemeHelperExtension Instance { get; } = new();

	public event EventHandler? SystemThemeChanged;
	public event EventHandler? HighContrastChanged;

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

					NativeDispatcher.Main.Enqueue(() =>
					{
						SystemThemeChanged?.Invoke(this, EventArgs.Empty);
						// HC changes also modify registry, so we always check
						HighContrastChanged?.Invoke(this, EventArgs.Empty);
					});
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

	public unsafe bool IsHighContrastEnabled()
	{
		var hc = new HIGHCONTRASTW();
		hc.cbSize = (uint)sizeof(HIGHCONTRASTW);

		if (PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST, hc.cbSize, &hc, 0))
		{
			return (hc.dwFlags & HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON) != 0;
		}

		return false;
	}

	public unsafe string GetHighContrastSchemeName()
	{
		if (!IsHighContrastEnabled())
		{
			return "High Contrast Black";
		}

		// Determine the HC scheme by comparing system window and text colors,
		// matching the WinUI SystemThemingInterop logic.
		uint windowColor = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOW);
		uint textColor = PInvoke.GetSysColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT);

		byte windowR = (byte)(windowColor & 0xFF);
		byte windowG = (byte)((windowColor >> 8) & 0xFF);
		byte windowB = (byte)((windowColor >> 16) & 0xFF);
		byte textR = (byte)(textColor & 0xFF);
		byte textG = (byte)((textColor >> 8) & 0xFF);
		byte textB = (byte)((textColor >> 16) & 0xFF);

		bool isWhiteBg = windowR == 255 && windowG == 255 && windowB == 255;
		bool isBlackBg = windowR == 0 && windowG == 0 && windowB == 0;
		bool isWhiteText = textR == 255 && textG == 255 && textB == 255;
		bool isBlackText = textR == 0 && textG == 0 && textB == 0;

		if (isWhiteBg && isBlackText)
		{
			return "High Contrast White";
		}
		else if (isBlackBg && isWhiteText)
		{
			return "High Contrast Black";
		}
		else
		{
			return "High Contrast #1";
		}
	}

	public HighContrastSystemColors? GetHighContrastSystemColors()
	{
		if (!IsHighContrastEnabled())
		{
			return null;
		}

		return new HighContrastSystemColors(
			ButtonFaceColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_3DFACE),
			ButtonTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_BTNTEXT),
			GrayTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_GRAYTEXT),
			HighlightColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHT),
			HighlightTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHTTEXT),
			HotlightColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_HOTLIGHT),
			WindowColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_WINDOW),
			WindowTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT));
	}

	private static Color GetSystemColor(SYS_COLOR_INDEX colorIndex)
	{
		var colorRef = PInvoke.GetSysColor(colorIndex);

		return Color.FromArgb(
			255,
			(byte)(colorRef & 0xFF),
			(byte)((colorRef >> 8) & 0xFF),
			(byte)((colorRef >> 16) & 0xFF));
	}
}

