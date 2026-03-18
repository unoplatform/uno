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
	/// Computes a full accent color palette from a single accent color by blending
	/// lighter and darker shades via linear RGB interpolation.
	/// </summary>
	/// <remarks>
	/// Light shades are blended toward white, dark shades toward black.
	/// Factors were reverse-engineered from Windows default blue (#0078D7) and its palette.
	/// </remarks>
	public static AccentColorPalette FromAccentColor(Color accent)
	{
		return new AccentColorPalette(
			accent: accent,
			light1: Lerp(accent, Colors.White, 0.26),
			light2: Lerp(accent, Colors.White, 0.46),
			light3: Lerp(accent, Colors.White, 0.65),
			dark1: Lerp(accent, Colors.Black, 0.26),
			dark2: Lerp(accent, Colors.Black, 0.46),
			dark3: Lerp(accent, Colors.Black, 0.65)
		);
	}

	private static Color Lerp(Color from, Color to, double factor)
	{
		return Color.FromArgb(
			0xFF,
			(byte)(from.R + (to.R - from.R) * factor),
			(byte)(from.G + (to.G - from.G) * factor),
			(byte)(from.B + (to.B - from.B) * factor)
		);
	}
}
