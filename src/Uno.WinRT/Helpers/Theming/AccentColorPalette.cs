#nullable enable

using Windows.UI;

namespace Uno.Helpers.Theming;

/// <summary>
/// Holds a set of 7 accent color variants: Accent, Light1-3, Dark1-3.
/// </summary>
internal readonly struct AccentColorPalette
{
	public Color Accent { get; }
	public Color Light1 { get; }
	public Color Light2 { get; }
	public Color Light3 { get; }
	public Color Dark1 { get; }
	public Color Dark2 { get; }
	public Color Dark3 { get; }

	public AccentColorPalette(Color accent, Color light1, Color light2, Color light3, Color dark1, Color dark2, Color dark3)
	{
		Accent = accent;
		Light1 = light1;
		Light2 = light2;
		Light3 = light3;
		Dark1 = dark1;
		Dark2 = dark2;
		Dark3 = dark3;
	}

	/// <summary>
	/// Default accent color palette matching the hardcoded blue values in SystemResources.xaml.
	/// </summary>
	public static AccentColorPalette Default { get; } = new(
		accent: Color.FromArgb(0xFF, 0x00, 0x78, 0xD7),
		light1: Color.FromArgb(0xFF, 0x42, 0x9C, 0xE3),
		light2: Color.FromArgb(0xFF, 0x76, 0xB9, 0xED),
		light3: Color.FromArgb(0xFF, 0xA6, 0xD8, 0xFF),
		dark1: Color.FromArgb(0xFF, 0x00, 0x5A, 0x9E),
		dark2: Color.FromArgb(0xFF, 0x00, 0x42, 0x75),
		dark3: Color.FromArgb(0xFF, 0x00, 0x26, 0x42)
	);

	/// <summary>
	/// Derives a full accent palette from a single accent color using the Windows 11 shade algorithm.
	/// </summary>
	/// <remarks>
	/// Honors <see cref="AccentColorHelper.NormalizeAccentColor"/>, which controls whether the accent itself is adjusted like Windows does.
	/// </remarks>
	public static AccentColorPalette FromAccentColor(Color accent) =>
		AccentPaletteGenerator.Generate(accent, AccentColorHelper.NormalizeAccentColor);
}
