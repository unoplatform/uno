// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Theme.h, commit fc2f82117

namespace Microsoft.UI.Xaml;

/// <summary>
/// Internal theme representation matching WinUI's 5-bit packed field.
/// </summary>
internal enum Theme : byte
{
	// Mask to separate High Contrast and base themes
	BaseMask = 0x03,
	// Uses initial theme, which may be Light or Dark
	None = 0x00,
	// Base themes
	Light = 0x01,
	Dark = 0x02,
	// High Contrast themes can be set in addition to Light & Dark, and
	// they take precedence.
	HighContrastMask = 0x1C,
	HighContrastNone = 0x00,
	HighContrast = 0x04,
	HighContrastWhite = 0x08,
	HighContrastBlack = 0x0C,
	HighContrastCustom = 0x10,

	// This should always be the last. If adding a new theme, add before Unused
	Unused = 0x20,
#if DEBUG
	// For debugability purposes, add extra flags so we can make sense of the base and high contrast
	// combinations. Only include in debug builds so that people don't use these in actual code.
	LigthAndHighContrastWhite = 0x09,
	LigthAndHighContrastBlack = 0x0D,
	LigthAndHighContrastCustom = 0X11,
	DarkAndHighContrastWhite = 0x0A,
	DarkAndHighContastBlack = 0x0E,
	DarkAndHighContrastCustom = 0x12,
#endif
}

// C++: static_assert(static_cast<unsigned int>(Theme::Unused) == 32u, "Theme enum is packed to only require 5 bits.");

internal static class Theming
{
	// Theme enum is stored on CDependencyObject and packed to 5 bits.
	// If a new theme needs to be added, then modifying the theme will
	// require the bitfield on CDependencyObject to be increased

	// Theme::None:                 0x00000
	// Theme::Light:                0x00001
	// Theme::Dark:                 0x00010
	// Theme::BaseMask:             0x00011
	// Theme::HighContrastMask:     0x11100
	// Theme::HighContrast:         0x00100
	// Theme::HighContrastWhite:    0x01000
	// Theme::HighContrastBlack:    0x01100
	// Theme::HighContrastCustom:   0x10000
	public static Theme GetBaseValue(Theme theme)
		=> theme & Theme.BaseMask;

	public static Theme GetHighContrastValue(Theme theme)
		=> theme & Theme.HighContrastMask;

	// Uno-specific: conversions between the public ElementTheme API surface and the internal Theme.
	public static Theme FromElementTheme(ElementTheme elementTheme) => elementTheme switch
	{
		ElementTheme.Light => Theme.Light,
		ElementTheme.Dark => Theme.Dark,
		_ => Theme.None
	};

	// Uno-specific: see FromElementTheme.
	public static ElementTheme ToElementTheme(Theme theme) => GetBaseValue(theme) switch
	{
		Theme.Light => ElementTheme.Light,
		Theme.Dark => ElementTheme.Dark,
		_ => ElementTheme.Default
	};
}
