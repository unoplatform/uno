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

	public string GetHighContrastSchemeName() =>
		((ISystemThemeHelperExtension)this).GetSystemTheme() == SystemTheme.Dark
			? "High Contrast Black"
			: "High Contrast White";

	public HighContrastSystemColors? GetHighContrastSystemColors()
	{
		if (!IsHighContrastEnabled())
		{
			return null;
		}

		NativeUno.uno_get_high_contrast_colors(out var colors);
		return new HighContrastSystemColors(
			ButtonFaceColor: ToColor(colors.ButtonFaceColor),
			ButtonTextColor: ToColor(colors.ButtonTextColor),
			GrayTextColor: ToColor(colors.GrayTextColor),
			HighlightColor: ToColor(colors.HighlightColor),
			HighlightTextColor: ToColor(colors.HighlightTextColor),
			HotlightColor: ToColor(colors.HotlightColor),
			WindowColor: ToColor(colors.WindowColor),
			WindowTextColor: ToColor(colors.WindowTextColor),
			ActiveCaptionColor: ToColor(colors.ActiveCaptionColor),
			BackgroundColor: ToColor(colors.BackgroundColor),
			CaptionTextColor: ToColor(colors.CaptionTextColor),
			InactiveCaptionColor: ToColor(colors.InactiveCaptionColor),
			InactiveCaptionTextColor: ToColor(colors.InactiveCaptionTextColor),
			DisabledTextColor: ToColor(colors.DisabledTextColor));
	}

	private static Color ToColor(uint argb) =>
		Color.FromArgb(
			(byte)(argb >> 24),
			(byte)(argb >> 16),
			(byte)(argb >> 8),
			(byte)argb);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		try
		{
			if (typeof(MacOSSystemThemeHelperExtension).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSSystemThemeHelperExtension).Log().Trace($"MacOSSystemThemeHelperExtension.SystemThemeChanged {((ISystemThemeHelperExtension)_instance).GetSystemTheme()}");
			}

			_instance.SystemThemeChanged?.Invoke(_instance, EventArgs.Empty);
		}
		catch (Exception e)
		{
			typeof(MacOSSystemThemeHelperExtension).Log().Error("System theme callback failed.", e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void OnHighContrastUpdate()
	{
		try
		{
			if (typeof(MacOSSystemThemeHelperExtension).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSSystemThemeHelperExtension).Log().Trace(
					$"{nameof(MacOSSystemThemeHelperExtension)}.{nameof(HighContrastChanged)} {_instance.IsHighContrastEnabled()}");
			}

			_instance.HighContrastChanged?.Invoke(_instance, EventArgs.Empty);
		}
		catch (Exception e)
		{
			typeof(MacOSSystemThemeHelperExtension).Log().Error("High contrast callback failed.", e);
		}
	}
}
