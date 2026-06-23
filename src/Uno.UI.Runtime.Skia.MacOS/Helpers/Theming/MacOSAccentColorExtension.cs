using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Windows.UI;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSAccentColorExtension : IAccentColorExtension
{
	private static readonly MacOSAccentColorExtension _instance = new();

	private MacOSAccentColorExtension()
	{
	}

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(IAccentColorExtension), _ => _instance);
		NativeUno.uno_set_accent_color_change_callback(&Update);
	}

	public event EventHandler? AccentColorChanged;

	public unsafe AccentColorPalette? GetAccentColorPalette()
	{
		byte r, g, b;
		NativeUno.uno_get_accent_color(&r, &g, &b);
		var accent = Color.FromArgb(0xFF, r, g, b);
		return AccentColorPalette.FromAccentColor(accent);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		// Called directly from native code; an exception escaping into the native stack is undefined
		// behavior, so swallow any thrown by AccentColorChanged subscribers (as other native callbacks do).
		try
		{
			if (typeof(MacOSAccentColorExtension).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSAccentColorExtension).Log().Trace("MacOSAccentColorExtension.AccentColorChanged");
			}

			_instance.AccentColorChanged?.Invoke(_instance, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			if (typeof(MacOSAccentColorExtension).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSAccentColorExtension).Log().Error("Exception in accent color change callback", ex);
			}
		}
	}
}
