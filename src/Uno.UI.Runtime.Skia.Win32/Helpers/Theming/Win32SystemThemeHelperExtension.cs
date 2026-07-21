using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Registry;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.UI;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32SystemThemeHelperExtension : ISystemThemeHelperExtension
{
	private const string SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
	private const string ControlPanelAccessibilityHighContrast = @"Control Panel\Accessibility\HighContrast";

	public static Win32SystemThemeHelperExtension Instance { get; } = new();

	public event EventHandler? SystemThemeChanged;
	public event EventHandler? HighContrastChanged;

	private Win32SystemThemeHelperExtension()
	{
		ObserveRegistryChanges(
			SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize,
			() => SystemThemeChanged?.Invoke(this, EventArgs.Empty));
		ObserveRegistryChanges(
			ControlPanelAccessibilityHighContrast,
			() => HighContrastChanged?.Invoke(this, EventArgs.Empty));
	}

	private unsafe void ObserveRegistryChanges(string subKey, Action onChanged)
	{
		_ = Task.Factory.StartNew(
			() =>
			{
				try
				{
					using var hSubKeyString = new Win32Helper.NativeNulTerminatedUtf16String(subKey);
					HKEY hSubKey;
					// We're not closing this handle since this class lasts the whole lifetime of the app.
					var errorCode = PInvoke.RegOpenKeyEx(HKEY.HKEY_CURRENT_USER, hSubKeyString, 0, REG_SAM_FLAGS.KEY_READ, &hSubKey);
					if (errorCode != WIN32_ERROR.ERROR_SUCCESS)
					{
						this.LogError()?.Error($"{nameof(PInvoke.RegOpenKeyEx)} failed for '{subKey}' with error code: {Win32Helper.GetErrorMessage((uint)errorCode)}");
						return;
					}

					while (true)
					{
						var errorCode2 = PInvoke.RegNotifyChangeKeyValue(hSubKey, false, REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET, HANDLE.Null, false);
						if (errorCode2 != WIN32_ERROR.ERROR_SUCCESS)
						{
							this.LogError()?.Error($"{nameof(PInvoke.RegNotifyChangeKeyValue)} failed for '{subKey}' with error code: {Win32Helper.GetErrorMessage((uint)errorCode2)}");
							return;
						}

						NativeDispatcher.Main.Enqueue(onChanged);
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"{nameof(Win32SystemThemeHelperExtension)} failed while observing '{subKey}'.", e);
					}
				}
			},
			CancellationToken.None,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
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
		var highContrast = new HIGHCONTRASTW
		{
			cbSize = (uint)sizeof(HIGHCONTRASTW),
		};

		return PInvoke.SystemParametersInfo(
			SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST,
			highContrast.cbSize,
			&highContrast,
			0)
			&& (highContrast.dwFlags & HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON) != 0;
	}

	public string GetHighContrastSchemeName()
	{
		var window = GetSystemColor(SYS_COLOR_INDEX.COLOR_WINDOW);
		var windowText = GetSystemColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT);

		if (IsWhite(window) && IsBlack(windowText))
		{
			return "High Contrast White";
		}

		if (IsBlack(window) && IsWhite(windowText))
		{
			return "High Contrast Black";
		}

		return "High Contrast #1";
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
			WindowTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_WINDOWTEXT),
			ActiveCaptionColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_ACTIVECAPTION),
			BackgroundColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_BACKGROUND),
			CaptionTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_CAPTIONTEXT),
			InactiveCaptionColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_INACTIVECAPTION),
			InactiveCaptionTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_INACTIVECAPTIONTEXT),
			DisabledTextColor: GetSystemColor(SYS_COLOR_INDEX.COLOR_GRAYTEXT));
	}

	private static Color GetSystemColor(SYS_COLOR_INDEX colorIndex)
	{
		var color = PInvoke.GetSysColor(colorIndex);
		return Color.FromArgb(
			255,
			(byte)(color & 0xFF),
			(byte)((color >> 8) & 0xFF),
			(byte)((color >> 16) & 0xFF));
	}

	private static bool IsWhite(Color color) =>
		(color.R == 0xFF && color.G == 0xFF && color.B == 0xFF)
		|| (color.R == 0xEB && color.G == 0xEB && color.B == 0xEB);

	private static bool IsBlack(Color color) =>
		(color.R == 0x00 && color.G == 0x00 && color.B == 0x00)
		|| (color.R == 0x10 && color.G == 0x10 && color.B == 0x10);
}
