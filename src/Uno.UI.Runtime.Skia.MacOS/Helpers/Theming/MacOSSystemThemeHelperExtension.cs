using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI;

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
		// macOS "Increase Contrast" doesn't have named HC schemes like Windows.
		// Infer from the current system theme (dark appearance → HC Black, light → HC White).
		var theme = (SystemTheme)NativeUno.uno_get_system_theme();
		return theme == SystemTheme.Dark ? "High Contrast Black" : "High Contrast White";
	}

	public HighContrastSystemColors? GetHighContrastSystemColors()
	{
		NativeUno.uno_get_high_contrast_colors(out var native);
		return new HighContrastSystemColors(
			ButtonFaceColor: ArgbToColor(native.ButtonFaceColor),
			ButtonTextColor: ArgbToColor(native.ButtonTextColor),
			GrayTextColor: ArgbToColor(native.GrayTextColor),
			HighlightColor: ArgbToColor(native.HighlightColor),
			HighlightTextColor: ArgbToColor(native.HighlightTextColor),
			HotlightColor: ArgbToColor(native.HotlightColor),
			WindowColor: ArgbToColor(native.WindowColor),
			WindowTextColor: ArgbToColor(native.WindowTextColor),
			ActiveCaptionColor: ArgbToColor(native.ActiveCaptionColor),
			BackgroundColor: ArgbToColor(native.BackgroundColor),
			CaptionTextColor: ArgbToColor(native.CaptionTextColor),
			InactiveCaptionColor: ArgbToColor(native.InactiveCaptionColor),
			InactiveCaptionTextColor: ArgbToColor(native.InactiveCaptionTextColor),
			DisabledTextColor: ArgbToColor(native.DisabledTextColor)
		);
	}

	private static Color ArgbToColor(uint argb) =>
		Color.FromArgb(
			(byte)((argb >> 24) & 0xFF),
			(byte)((argb >> 16) & 0xFF),
			(byte)((argb >> 8) & 0xFF),
			(byte)(argb & 0xFF));

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
