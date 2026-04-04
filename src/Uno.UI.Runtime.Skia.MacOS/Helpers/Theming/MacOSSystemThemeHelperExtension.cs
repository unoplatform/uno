using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSSystemThemeHelperExtension : ISystemThemeHelperExtension
{
	private static readonly MacOSSystemThemeHelperExtension _instance = new();

	private MacOSSystemThemeHelperExtension()
	{
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), _ => _instance);
		NativeUno.uno_set_system_theme_change_callback(&Update);
		NativeUno.uno_set_high_contrast_change_callback(&OnHighContrastUpdate);
	}

	public event EventHandler? SystemThemeChanged;
	public event EventHandler? HighContrastChanged;

	SystemTheme ISystemThemeHelperExtension.GetSystemTheme() => (SystemTheme)NativeUno.uno_get_system_theme();

	public bool IsHighContrastEnabled() => NativeUno.uno_get_high_contrast();

	public string GetHighContrastSchemeName()
	{
		// macOS doesn't differentiate between HC Black/White/Custom.
		// "Increase Contrast" is essentially the only mode.
		return "High Contrast Black";
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		if (typeof(MacOSSystemThemeHelperExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSSystemThemeHelperExtension).Log().Trace($"MacOSSystemThemeHelperExtension.SystemThemeChanged {((ISystemThemeHelperExtension)_instance).GetSystemTheme()}");
		}

		_instance.SystemThemeChanged?.Invoke(_instance, EventArgs.Empty);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void OnHighContrastUpdate()
	{
		if (typeof(MacOSSystemThemeHelperExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSSystemThemeHelperExtension).Log().Trace($"MacOSSystemThemeHelperExtension.HighContrastChanged IsHighContrast={_instance.IsHighContrastEnabled()}");
		}

		_instance.HighContrastChanged?.Invoke(_instance, EventArgs.Empty);
	}
}

