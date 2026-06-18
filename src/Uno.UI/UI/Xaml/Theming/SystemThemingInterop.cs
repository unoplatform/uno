// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SystemThemingInterop.h & SystemThemingInterop.cpp, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
using Uno.Helpers.Theming;
using Windows.UI;
using Windows.UI.ViewManagement;

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
		var highContrastTheme = Theme.HighContrastNone;

		// WinUI (SystemThemingInterop.cpp:121-177) checks SystemParametersInfoW(SPI_GETHIGHCONTRAST)
		// and, when high contrast is on, classifies the variant from the OS WINDOW/WINDOWTEXT colors:
		//   // Redstone Bug #6417331: Xbox uses video-safe black (0x101010) and white (0xEBEBEB) when in High Contrast
		//   white background (0xFFFFFF, or Xbox 0xEBEBEB) + black foreground => Theme::HighContrastWhite
		//   black background (0x000000, or Xbox 0x101010) + white foreground => Theme::HighContrastBlack
		//   anything else                                                    => Theme::HighContrastCustom
		// (The test-override path additionally classifies 0xFFEEEEEE/0xFF111111 as HighContrastCustom and
		// falls back to the generic Theme::HighContrast for unrecognized palettes.)
		// TODO Uno: OS high-contrast variant detection on Skia is a documented follow-up; until then the
		// app-global flag maps to the generic Theme.HighContrast.
		if (SystemThemeHelper.IsHighContrast)
		{
			highContrastTheme = Theme.HighContrast;
		}

		return highContrastTheme;
	}

	public uint GetSystemColor(int colorId)
	{
		// WinUI (SystemThemingInterop.cpp:179-203) returns the live OS system-color palette —
		// ConvertFromABGRToARGB(GetSysColor(colorId) | 0xFF000000) — with a test-override palette
		// (s_sysColorPaletteOverride) taking precedence.
		// TODO Uno: there is no OS system-color palette abstraction yet (part of the high-contrast
		// follow-up above). Until then, default to the same easily findable color value WinUI uses
		// for entries not in its override palette, so brushes referencing unmapped system colors can
		// be spotted.
		return 0xFFAABBCC;
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
} // class SystemThemingInterop
