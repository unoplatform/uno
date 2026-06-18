using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSAccentColorExtension : IAccentColorExtension
{
	private static readonly MacOSAccentColorExtension _instance = new();

	public event EventHandler? AccentColorChanged;

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(IAccentColorExtension), _ => _instance);
		NativeUno.uno_set_accent_color_change_callback(&Update);
	}

	public Color GetAccentColor()
	{
		var argb = NativeUno.uno_get_accent_color();
		return Color.FromArgb(
			(byte)((argb >> 24) & 0xFF),
			(byte)((argb >> 16) & 0xFF),
			(byte)((argb >> 8) & 0xFF),
			(byte)(argb & 0xFF));
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		if (typeof(MacOSAccentColorExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSAccentColorExtension).Log().Trace($"MacOSAccentColorExtension.AccentColorChanged {_instance.GetAccentColor()}");
		}

		_instance.AccentColorChanged?.Invoke(_instance, EventArgs.Empty);
	}
}
