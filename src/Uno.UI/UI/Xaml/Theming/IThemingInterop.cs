// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThemingInterop.h, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Core;

internal interface IThemingInterop
{
	public enum VariantAccentColors
	{
		SystemAccentColor,
		SystemAccentColorDark1,
		SystemAccentColorDark2,
		SystemAccentColorDark3,
		SystemAccentColorLight1,
		SystemAccentColorLight2,
		SystemAccentColorLight3
	}

	// Returns either ThemeLight or ThemeDark.
	Theme GetSystemTheme();

	// Returns ThemeHighContrastNone if off, correct high-contrast
	// theme otherwise.
	Theme GetSystemHighContrastTheme();

	uint GetSystemColor(int colorId);

	uint GetSystemAccentColor();

	uint GetSystemVariantAccentColor(VariantAccentColors colorIndex);
} // class IThemingInterop

// Uno-specific: the winuser.h COLOR_* system color indices that WinUI passes to
// IThemingInterop::GetSystemColor (FrameworkTheming.cpp / SystemThemingInterop.cpp reference
// them directly from the Win32 headers).
internal static class Win32SystemColorIds
{
	public const int COLOR_BACKGROUND = 1;
	public const int COLOR_ACTIVECAPTION = 2;
	public const int COLOR_INACTIVECAPTION = 3;
	public const int COLOR_WINDOW = 5;
	public const int COLOR_WINDOWTEXT = 8;
	public const int COLOR_CAPTIONTEXT = 9;
	public const int COLOR_HIGHLIGHT = 13;
	public const int COLOR_HIGHLIGHTTEXT = 14;
	public const int COLOR_BTNFACE = 15;
	public const int COLOR_GRAYTEXT = 17;
	public const int COLOR_BTNTEXT = 18;
	public const int COLOR_INACTIVECAPTIONTEXT = 19;
	public const int COLOR_HOTLIGHT = 26;
}
