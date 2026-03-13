// MUX Reference Theme.h, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml;

/// <summary>
/// Internal theme representation matching WinUI's 5-bit packed field.
/// </summary>
internal enum Theme : byte
{
	// Mask to separate High Contrast and base themes
	BaseMask                    = 0x03,
	// Uses initial theme, which may be Light or Dark
	None                        = 0x00,
	// Base themes
	Light                       = 0x01,
	Dark                        = 0x02,
	// High Contrast themes can be set in addition to Light & Dark, and
	// they take precedence.
	HighContrastMask            = 0x1C,
	HighContrastNone            = 0x00,
	HighContrast                = 0x04,
	HighContrastWhite           = 0x08,
	HighContrastBlack           = 0x0C,
	HighContrastCustom          = 0x10,

	// This should always be the last. If adding a new theme, add before Unused
	Unused = 0x20,
}

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
		=> (Theme)((byte)theme & 0x03);

	public static Theme FromElementTheme(ElementTheme elementTheme) => elementTheme switch
	{
		ElementTheme.Light => Theme.Light,
		ElementTheme.Dark => Theme.Dark,
		_ => Theme.None
	};

	public static ElementTheme ToElementTheme(Theme theme) => GetBaseValue(theme) switch
	{
		Theme.Light => ElementTheme.Light,
		Theme.Dark => ElementTheme.Dark,
		_ => ElementTheme.Default
	};
}
