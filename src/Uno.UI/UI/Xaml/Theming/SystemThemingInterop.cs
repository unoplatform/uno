// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SystemThemingInterop.h & SystemThemingInterop.cpp, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
using Uno.Helpers.Theming;
using Windows.UI;
using Windows.UI.ViewManagement;
using static Uno.UI.Xaml.Core.Win32SystemColorIds;

namespace Uno.UI.Xaml.Core;

// Uno-specific: this implementation bridges WinUI's IThemingInterop contract over Uno's existing
// per-platform OS detection (Uno.Helpers.Theming.SystemThemeHelper and
// WinRTFeatureConfiguration.Accessibility). WinUI's static test hooks (OverrideSystemTheme,
// OverrideHighContrast, OverrideAccentColor, RemoveThemingOverrides —
// SystemThemingInterop.cpp:305-328; SystemThemingInterop.h:18-23 additionally declares an
// unimplemented OverrideVariantAccentColor) map onto SystemThemeHelper.SystemThemeOverride and
// WinRTFeatureConfiguration.Accessibility.HighContrast respectively; an accent-color override
// hook is not available in Uno.
internal sealed class SystemThemingInterop : IThemingInterop
{
	// C++ light/dark detection (based on text color), SystemThemingInterop.cpp:25-31:
	//   // It's possible to get a little more accurate than this via (30*r) + (59*g) + (11*b) >= 12750,
	//   // but there's a lot of code around windows that uses this simplified check so we're going to stick with that.
	//   return ((5 * color.G + 2 * color.R + color.B) > 8 * 128);

	// Returns either ThemeLight or ThemeDark.
	public Theme GetSystemTheme()
	{
		// WinUI (SystemThemingInterop.cpp:33-119) resolves the OS light/dark state in this order:
		// 1. The test override (s_systemThemeOverride), when one has been set, such as during test runs.
		// 2. On the Windows.Core device family, IUISettings3::GetColorValue(UIColorType_Background)
		//    classified through ColorCheckHelper_IsLightThemeColor (quoted above).
		// 3. //On certain platforms (for e.g. Hololens), Shell API to query for the default theme doesn't exist.
		//    //We (XAML & MRT), for consistency, have agreed to query the reg key: SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme and SystemUsesLightTheme
		//    //until we have a (Shell) converged way to query the theme value on all platforms
		//    HKCU\...\Personalize\AppsUseLightTheme (0 => Dark, else Light), then the machine-wide
		//    HKLM\...\Personalize\SystemUsesLightTheme, and finally
		//    IUISettings3::GetColorValue(UIColorType_Background) (white => Light, else Dark).
		// In Uno the equivalent per-platform OS detection — including the test override
		// (SystemThemeHelper.SystemThemeOverride) — lives in Uno.Helpers.Theming.SystemThemeHelper.
		return SystemThemeHelper.SystemTheme == SystemTheme.Light ? Theme.Light : Theme.Dark;
	}

	// Returns ThemeHighContrastNone if off, correct high-contrast
	// theme otherwise.
	public Theme GetSystemHighContrastTheme()
	{
#if !__SKIA__
		return Theme.HighContrastNone;
#else
		if (!SystemThemeHelper.IsHighContrast)
		{
			return Theme.HighContrastNone;
		}

		if (SystemThemeHelper.HighContrastSystemColors is { } colors)
		{
			if (IsWhite(colors.WindowColor) && IsBlack(colors.WindowTextColor))
			{
				return Theme.HighContrastWhite;
			}

			if (IsBlack(colors.WindowColor) && IsWhite(colors.WindowTextColor))
			{
				return Theme.HighContrastBlack;
			}

			return Theme.HighContrastCustom;
		}

		return SystemThemeHelper.HighContrastSchemeName switch
		{
			"High Contrast White" => Theme.HighContrastWhite,
			"High Contrast Black" => Theme.HighContrastBlack,
			"High Contrast #1" => Theme.HighContrastCustom,
			"High Contrast #2" => Theme.HighContrastCustom,
			_ => Theme.HighContrast,
		};
#endif
	}

	public uint GetSystemColor(int colorId)
	{
		// WinUI (SystemThemingInterop.cpp:179-203) returns the live OS system-color palette —
		// ConvertFromABGRToARGB(GetSysColor(colorId) | 0xFF000000) — with a test-override palette
		// (s_sysColorPaletteOverride) taking precedence.
		if (SystemThemeHelper.HighContrastSystemColors is not { } colors)
		{
			return GetFallbackSystemColor(colorId);
		}

		var color = colorId switch
		{
			COLOR_ACTIVECAPTION => colors.ActiveCaptionColor,
			COLOR_BACKGROUND => colors.BackgroundColor,
			COLOR_BTNFACE => colors.ButtonFaceColor,
			COLOR_BTNTEXT => colors.ButtonTextColor,
			COLOR_CAPTIONTEXT => colors.CaptionTextColor,
			COLOR_GRAYTEXT => colors.GrayTextColor,
			COLOR_HIGHLIGHT => colors.HighlightColor,
			COLOR_HIGHLIGHTTEXT => colors.HighlightTextColor,
			COLOR_HOTLIGHT => colors.HotlightColor,
			COLOR_INACTIVECAPTION => colors.InactiveCaptionColor,
			COLOR_INACTIVECAPTIONTEXT => colors.InactiveCaptionTextColor,
			COLOR_WINDOW => colors.WindowColor,
			COLOR_WINDOWTEXT => colors.WindowTextColor,
			_ => Colors.Fuchsia,
		};

		return ConvertColorToIntValue(color);
	}

	private uint GetFallbackSystemColor(int colorId)
	{
		var isWhite = GetSystemHighContrastTheme() == Theme.HighContrastWhite;
		var window = isWhite ? Colors.White : Colors.Black;
		var windowText = isWhite ? Colors.Black : Colors.White;
		var highlight = isWhite ? Colors.Black : Color.FromArgb(255, 26, 235, 255);
		var highlightText = isWhite ? Colors.White : Colors.Black;

		return ConvertColorToIntValue(colorId switch
		{
			COLOR_ACTIVECAPTION => window,
			COLOR_BACKGROUND => window,
			COLOR_BTNFACE => window,
			COLOR_BTNTEXT => windowText,
			COLOR_CAPTIONTEXT => windowText,
			COLOR_GRAYTEXT => isWhite ? Color.FromArgb(255, 109, 109, 109) : Color.FromArgb(255, 63, 242, 63),
			COLOR_HIGHLIGHT => highlight,
			COLOR_HIGHLIGHTTEXT => highlightText,
			COLOR_HOTLIGHT => isWhite ? Colors.Blue : Colors.Yellow,
			COLOR_INACTIVECAPTION => window,
			COLOR_INACTIVECAPTIONTEXT => windowText,
			COLOR_WINDOW => window,
			COLOR_WINDOWTEXT => windowText,
			_ => Colors.Fuchsia,
		});
	}

	public uint GetSystemAccentColor()
	{
		// WinUI first honors the test override (s_accentColorOverride) before delegating.
		return GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColor);
	}

	public uint GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors colorIndex)
	{
		// We expect that every SKU should support IUISettings3->GetColorValue(UIColorType_Accent), since that's a public API that 3rd party apps can
		// also use to query the accent color.

		if (!m_initializedUISettings3)
		{
			// Uno-specific: WinUI obtains the UISettings through FxCallbacks::DXamlCore_GetUISettings;
			// Uno's Windows.UI.ViewManagement.UISettings serves the equivalent accent palette.
			m_spUISettings3 = new UISettings();

			m_initializedUISettings3 = true;
		}

		if (m_spUISettings3 is not null)
		{
			var uiColorType = colorIndex switch
			{
				IThemingInterop.VariantAccentColors.SystemAccentColorDark1 => UIColorType.AccentDark1,
				IThemingInterop.VariantAccentColors.SystemAccentColorDark2 => UIColorType.AccentDark2,
				IThemingInterop.VariantAccentColors.SystemAccentColorDark3 => UIColorType.AccentDark3,
				IThemingInterop.VariantAccentColors.SystemAccentColorLight1 => UIColorType.AccentLight1,
				IThemingInterop.VariantAccentColors.SystemAccentColorLight2 => UIColorType.AccentLight2,
				IThemingInterop.VariantAccentColors.SystemAccentColorLight3 => UIColorType.AccentLight3,
				_ => UIColorType.Accent,
			};

			var accentColor = m_spUISettings3.GetColorValue(uiColorType);
			return ConvertColorToIntValue(accentColor);
		}

		// If IUISettings3 is not available, we have a default color of fuchsia to make it hopefully-obvious that we need to fix something.
		return 0xFFFF00FF;
	}

	private bool m_initializedUISettings3;
	private UISettings? m_spUISettings3;

	private static uint ConvertColorToIntValue(Color color)
	{
		return (uint)(color.A << 24 | color.R << 16 | color.G << 8 | color.B);
	}

#if __SKIA__
	private static bool IsWhite(Color color) =>
		(color.R == 0xFF && color.G == 0xFF && color.B == 0xFF)
		|| (color.R == 0xEB && color.G == 0xEB && color.B == 0xEB);

	private static bool IsBlack(Color color) =>
		(color.R == 0x00 && color.G == 0x00 && color.B == 0x00)
		|| (color.R == 0x10 && color.G == 0x10 && color.B == 0x10);
#endif
} // class SystemThemingInterop
