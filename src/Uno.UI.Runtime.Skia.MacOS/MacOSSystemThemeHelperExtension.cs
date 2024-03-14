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
	}

	public event EventHandler? SystemThemeChanged;

	SystemTheme ISystemThemeHelperExtension.GetSystemTheme() => (SystemTheme)NativeUno.uno_get_system_theme();

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		if (typeof(MacOSSystemThemeHelperExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSSystemThemeHelperExtension).Log().Trace($"MacOSSystemThemeHelperExtension.SystemThemeChanged {((ISystemThemeHelperExtension)_instance).GetSystemTheme()}");
		}

		_instance.SystemThemeChanged?.Invoke(_instance, EventArgs.Empty);
	}
}
