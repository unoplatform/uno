// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FrameworkTheming.h & FrameworkTheming.cpp, commit fc2f82117

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using static Uno.UI.Xaml.Core.Win32SystemColorIds;

namespace Uno.UI.Xaml.Core;

internal struct ColorAndBrushResourceInfo
{
	public string ColorKey;
	public string? BrushKey;
	public uint RgbValue;
	public bool OverrideAlpha;
}

internal sealed class FrameworkTheming
{
	public delegate void NotifyThemeChangedFunc();

	public FrameworkTheming(
		IThemingInterop themingInterop,
		NotifyThemeChangedFunc notifyThemeChangedFunc)
	{
		m_spThemingInterop = themingInterop;
		m_notifyThemeChangedFunc = notifyThemeChangedFunc;
		m_requestedTheme = Theme.None;
		m_highContrastTheme = Theme.HighContrastNone;
		m_systemTheme = Theme.Dark;
		m_accentColor = 0x00000000;
		m_disableHighContrastUpdateOnThemeChange = false;
		m_isSystemThemeChanging = false;
		m_isHighContrastChanging = false;
		m_isAppThemeChanging = false;

		m_systemTheme = m_spThemingInterop.GetSystemTheme();
	}

	public void OnThemeChanged(bool forceUpdate = false)
	{
		// TODO Uno: TraceThemeChangedStart() — MUX ETW tracing is not available in Uno.

		var oldTheme = GetTheme();
		var oldThemeSystem = m_systemTheme;
		var oldThemeHighContrast = m_highContrastTheme;
		var oldAccentColor = m_accentColor;

		m_systemTheme = m_spThemingInterop.GetSystemTheme();

		if (!m_disableHighContrastUpdateOnThemeChange)
		{
			m_highContrastTheme = m_spThemingInterop.GetSystemHighContrastTheme();
		}

		m_accentColor = m_spThemingInterop.GetSystemAccentColor();

		// Only do an update if something has actually changed, or if forced to.
		if (forceUpdate || oldTheme != GetTheme() || oldAccentColor != m_accentColor)
		{
			m_isSystemThemeChanging = oldThemeSystem != m_systemTheme;
			m_isHighContrastChanging = (oldThemeHighContrast != m_highContrastTheme);
			try
			{
				RebuildColorAndBrushResources();
				m_notifyThemeChangedFunc();
			}
			finally
			{
				// C++ uses a wil::scope_exit guard to reset the changing flags on scope exit.
				m_isSystemThemeChanging = false;
				m_isHighContrastChanging = false;
			}
		}

		// TODO Uno: TraceThemeChangedStop() — MUX ETW tracing is not available in Uno.
	}

	public void SetRequestedTheme(ApplicationTheme requestedTheme, bool doNotifyThemeChange = true)
		=> SetRequestedTheme(requestedTheme == ApplicationTheme.Light ? Theme.Light : Theme.Dark, doNotifyThemeChange);

	public void SetRequestedTheme(Theme requestedTheme, bool doNotifyThemeChange = true)
	{
		// The input theme should be a base theme.
		Debug.Assert((requestedTheme & Theme.HighContrastMask) == Theme.HighContrastNone);

		if (requestedTheme != m_requestedTheme)
		{
			var oldDefaultTheme = GetBaseTheme();

			m_requestedTheme = requestedTheme;

			// Don't notify about theme switches when we're in high contrast.
			if (doNotifyThemeChange && !HasHighContrastTheme())
			{
				// Force a theme update to get around the perf optimization
				// that tests whether the theme actually changes after refreshing
				// the system theme if we're setting a requested theme, otherwise
				// the theme will never change in response to changes to the
				// system theme (because requested theme takes precedence) and
				// we'll end up not notifying the core.
				bool forceUpdate = (m_requestedTheme != Theme.None);

				m_isAppThemeChanging = oldDefaultTheme != m_requestedTheme;
				try
				{
					OnThemeChanged(forceUpdate);
				}
				finally
				{
					// C++ uses a wil::scope_exit guard to reset the flag on scope exit.
					m_isAppThemeChanging = false;
				}
			}
		}
	}

	public bool HasRequestedTheme() => m_requestedTheme != Theme.None;

	public void UnsetRequestedTheme() => m_requestedTheme = Theme.None;

	// Uno-specific: WinUI can never drop the app override at runtime (ApplicationTheme has no None and
	// put_RequestedThemeImpl is pre-load only), but Uno's Application.SetExplicitRequestedTheme(null)
	// returns the app to follow-the-system mode. Mirror SetRequestedTheme's structure: clear the
	// override, then notify under the app-theme-changing flag. OnThemeChanged's no-change early-out
	// compares GetTheme() with the override already cleared, so the base flip caused by dropping the
	// override must be forced explicitly.
	public void ClearRequestedTheme(bool doNotifyThemeChange = true)
	{
		if (m_requestedTheme != Theme.None)
		{
			var oldDefaultTheme = GetBaseTheme();

			m_requestedTheme = Theme.None;

			// Don't notify about theme switches when we're in high contrast.
			if (doNotifyThemeChange && !HasHighContrastTheme())
			{
				bool baseThemeChanging = oldDefaultTheme != GetBaseTheme();

				m_isAppThemeChanging = baseThemeChanging;
				try
				{
					OnThemeChanged(forceUpdate: baseThemeChanging);
				}
				finally
				{
					// C++ uses a wil::scope_exit guard to reset the flag on scope exit.
					m_isAppThemeChanging = false;
				}
			}
		}
	}

	public bool HasHighContrastTheme() => m_highContrastTheme != Theme.HighContrastNone;

	// Represents the theme that the framework is using.  It is a combination
	// of the requested theme (if not set, then the system theme) and the
	// high-contrast theme.
	public Theme GetTheme()
	{
		Debug.Assert(m_systemTheme != Theme.None);
		return (m_requestedTheme != Theme.None ? m_requestedTheme : m_systemTheme) | m_highContrastTheme;
	}

	// Helpers to get the base and high-constrast components to the theme.
	public Theme GetBaseTheme() => GetTheme() & Theme.BaseMask;

	public Theme GetHighContrastTheme() => GetTheme() & Theme.HighContrastMask;

	public IReadOnlyList<ColorAndBrushResourceInfo> GetColorAndBrushResourceInfoList() => m_colorAndBrushResourceInfoList;

	public void DisableHighContrastUpdateOnThemeChange() => m_disableHighContrastUpdateOnThemeChange = true;

	// this one only takes in account app requested theme, base system theme and highcontrast theme
	public uint GetRootVisualBackground() => GetHwndBackground(Theme.None);

	// HWND's background is decided this way :
	// 1. if highcontrast theme is applied, it overrides everything all
	// 2. else if top level element (XamlRoot.Content) has a requested theme property set, pick actualtheme of that (because requestedtheme can be overriden)
	// 3. else use the base theme
	public uint GetHwndBackground(Theme xamlRootTheme)
	{
		uint backgroundColor;
		// In threshold, we set the background color to better match the app's base theme.
		// This minimizes jarring transitions such as when in light theme and doing a
		// page navigation transition, which used to show the black in the background
		// as the white page rotated out of the way.

		if (HasHighContrastTheme())
		{
			backgroundColor = m_spThemingInterop.GetSystemColor(COLOR_WINDOW);
		}
		else
		{
			Theme theme = Theme.None;
			theme = xamlRootTheme != Theme.None ? xamlRootTheme : GetBaseTheme();
			backgroundColor = (theme == Theme.Light ? 0xffffffff : 0xff000000);
		}

		return backgroundColor;
	}

	public bool IsBaseThemeChanging() => m_isSystemThemeChanging || m_isAppThemeChanging;

	public bool IsHighContrastChanging() => m_isHighContrastChanging;

	private void RebuildColorAndBrushResources()
	{
		const bool overrideAlpha = true;

		// C++: static const xstring_ptr_storage colorKeyStorage[]
		string[] colorKeyStorage =
		{
			// System color resources
			"SystemColorActiveCaptionColor",
			"SystemColorBackgroundColor",
			"SystemColorButtonFaceColor",
			"SystemColorButtonTextColor",
			"SystemColorCaptionTextColor",
			"SystemColorGrayTextColor",
			"SystemColorHighlightColor",
			"SystemColorHighlightTextColor",
			"SystemColorHotlightColor",
			"SystemColorInactiveCaptionColor",
			"SystemColorInactiveCaptionTextColor",
			"SystemColorWindowColor",
			"SystemColorWindowTextColor",
			"SystemColorDisabledTextColor",

			// Accent color resources
			"SystemColorControlAccentColor",
			"SystemAccentColor",

			// Variant accent color resources
			"SystemAccentColorDark1",
			"SystemAccentColorDark2",
			"SystemAccentColorDark3",
			"SystemAccentColorLight1",
			"SystemAccentColorLight2",
			"SystemAccentColorLight3",

			// List selection color resources.
			"SystemListAccentLowColor",
			"SystemListAccentMediumColor",
			"SystemListAccentHighColor"
		};

		// C++: static const xstring_ptr_storage brushKeyStorage[]
		string?[] brushKeyStorage =
		{
			// System color resources
			"SystemColorActiveCaptionBrush",
			"SystemColorBackgroundBrush",
			"SystemColorButtonFaceBrush",
			"SystemColorButtonTextBrush",
			"SystemColorCaptionTextBrush",
			"SystemColorGrayTextBrush",
			"SystemColorHighlightBrush",
			"SystemColorHighlightTextBrush",
			"SystemColorHotlightBrush",
			"SystemColorInactiveCaptionBrush",
			"SystemColorInactiveCaptionTextBrush",
			"SystemColorWindowBrush",
			"SystemColorWindowTextBrush",
			"SystemColorDisabledTextBrush",

			// Accent color resources
			"SystemColorControlAccentBrush",
			null,

			// Variant accent color resources
			null,
			null,
			null,
			null,
			null,
			null,

			// List selection color resources.
			null,
			null,
			null
		};
		Debug.Assert(brushKeyStorage.Length == colorKeyStorage.Length, "Array does not match number of color resources.");

		// Dynamic values.
		uint[] colorValues =
		{
			// SystemColor resources
			m_spThemingInterop.GetSystemColor(COLOR_ACTIVECAPTION),
			m_spThemingInterop.GetSystemColor(COLOR_BACKGROUND),
			m_spThemingInterop.GetSystemColor(COLOR_BTNFACE),
			m_spThemingInterop.GetSystemColor(COLOR_BTNTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_CAPTIONTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_GRAYTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_HIGHLIGHT),
			m_spThemingInterop.GetSystemColor(COLOR_HIGHLIGHTTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_HOTLIGHT),
			m_spThemingInterop.GetSystemColor(COLOR_INACTIVECAPTION),
			m_spThemingInterop.GetSystemColor(COLOR_INACTIVECAPTIONTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_WINDOW),
			m_spThemingInterop.GetSystemColor(COLOR_WINDOWTEXT),
			m_spThemingInterop.GetSystemColor(COLOR_GRAYTEXT),

			// Accent color resources
			m_accentColor,
			m_accentColor,

			// Variant accent color resources
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorDark1),
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorDark2),
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorDark3),
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorLight1),
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorLight2),
			m_spThemingInterop.GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors.SystemAccentColorLight3),

			// List selection color resources (Accent color RGB values, different Alpha-channel values).
			(0x66FFFFFF & m_accentColor),
			(0x99FFFFFF & m_accentColor),
			(0xB2FFFFFF & m_accentColor)
		};
		Debug.Assert(colorValues.Length == colorKeyStorage.Length, "Array does not match number of color resources.");

		bool[] overrideAlphaValues =
		{
			// SystemColor resources
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,
			overrideAlpha,

			// Accent color resources
			false,
			false,

			// Variant accent color resources
			false,
			false,
			false,
			false,
			false,
			false,

			// List selection color resources (Accent color RGB values, different Alpha-channel values).
			false,
			false,
			false
		};
		Debug.Assert(overrideAlphaValues.Length == colorKeyStorage.Length, "Array does not match number of color resources.");

		m_colorAndBrushResourceInfoList.Clear();
		for (var i = 0; i < colorKeyStorage.Length; ++i)
		{
			var info = new ColorAndBrushResourceInfo
			{
				ColorKey = colorKeyStorage[i],
				BrushKey = brushKeyStorage[i],
				RgbValue = colorValues[i],
				OverrideAlpha = overrideAlphaValues[i]
			};

			m_colorAndBrushResourceInfoList.Add(info);
		}
	}

	private readonly IThemingInterop m_spThemingInterop;
	private readonly NotifyThemeChangedFunc m_notifyThemeChangedFunc;

	private Theme m_requestedTheme;
	private Theme m_highContrastTheme;
	private Theme m_systemTheme;

	private uint m_accentColor;

	// High contrast isn't updated in the OnThemeChanged handler when we're in a background task.
	private bool m_disableHighContrastUpdateOnThemeChange;

	private bool m_isAppThemeChanging;
	private bool m_isSystemThemeChanging;

	private bool m_isHighContrastChanging;

	private readonly List<ColorAndBrushResourceInfo> m_colorAndBrushResourceInfoList = new();
} // class FrameworkTheming
