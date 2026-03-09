// MUX Reference Theme.h, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml;

/// <summary>
/// Internal theme representation matching WinUI's 5-bit packed field.
/// </summary>
internal enum Theme : byte
{
	None = 0,
	Light = 1,
	Dark = 2,
}

internal static class Theming
{
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
