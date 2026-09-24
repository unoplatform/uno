#nullable enable

using System;
using Windows.UI;

namespace Uno.Helpers.Theming;

/// <summary>
/// Derives accent shades the way Windows 11 does (uxtheme's palette_formula and AccentColorUtils),
/// so platforms exposing a single accent color produce the values UISettings.GetColorValue returns on Windows.
/// </summary>
internal static class AccentPaletteGenerator
{
	// Windows-tuned palettes for the Settings swatches: Accent, Light1-3, Dark1-3.
	private static readonly int[] _knownPalettes =
	[
		0xFFB900, 0xFFC20D, 0xFFD52A, 0xFFE845, 0xE19D00, 0x9B5D00, 0x5C2100,
		0xE74856, 0xEB5C68, 0xF38A91, 0xFBB4B7, 0xE32938, 0xA6161E, 0x650D0F,
		0x0078D4, 0x0091F8, 0x4CC2FF, 0x99EBFF, 0x0067C0, 0x003E92, 0x001A68,
		0x0099BC, 0x00B8DF, 0x30E5FF, 0x7AF7FF, 0x0085A6, 0x005573, 0x002A45,
		0x7A7574, 0x8E8988, 0xBDB9B8, 0xE7E5E4, 0x696363, 0x403B3A, 0x1B1515,
		0x767676, 0x8B8B8B, 0xBBBBBA, 0xE6E6E6, 0x646464, 0x3B3B3B, 0x151515,
		0xFF8C00, 0xFF9910, 0xFFB634, 0xFFD155, 0xE37700, 0xA14600, 0x651900,
		0xE81123, 0xEF2733, 0xF46762, 0xFB9D8B, 0xD20E1E, 0x9E0912, 0x6F0306,
		0x0063B1, 0x007FDC, 0x40BDFF, 0x9CEBFF, 0x0055A1, 0x00337C, 0x00145A,
		0x2D7D9A, 0x3598B7, 0x6FC4D5, 0xA7E8ED, 0x266C88, 0x17455F, 0x08213A,
		0x5D5A58, 0x767370, 0xAEACAA, 0xE2E1DE, 0x504D4B, 0x302D2C, 0x141010,
		0x4C4A48, 0x686562, 0xA6A5A1, 0xDFDEDC, 0x413F3D, 0x272524, 0x100D0D,
		0xF7630C, 0xF8741D, 0xFB9A44, 0xFEBD68, 0xE05307, 0xA13105, 0x691202,
		0xEA005E, 0xFF0A6F, 0xFF529F, 0xFF95CA, 0xD30050, 0x9E002F, 0x6D0011,
		0x8E8CD8, 0x9F9CDE, 0xC5C1EC, 0xE8E3F8, 0x6D6BCD, 0x36349B, 0x1A1956,
		0x00B7C3, 0x00D5E1, 0x29F7FF, 0x69FCFF, 0x009FAA, 0x006770, 0x00343B,
		0x68768A, 0x7C8A9C, 0xADBBC5, 0xD9E6EA, 0x586579, 0x343C51, 0x13172D,
		0x69797E, 0x7C8E92, 0xADBCBF, 0xD8E7E8, 0x59676D, 0x353F45, 0x131920,
		0xCA5010, 0xE46012, 0xF09346, 0xF5C07C, 0xB6440E, 0x872808, 0x5C0E03,
		0xC30052, 0xEA0065, 0xFF459D, 0xFF97D1, 0xB10046, 0x860029, 0x5F000F,
		0x6B69D6, 0x817DDC, 0xB5ADEB, 0xE4D8F8, 0x4F4DCE, 0x2C2B9E, 0x13136A,
		0x038387, 0x04ADB2, 0x21F6FA, 0x7CFBFC, 0x037276, 0x024B4E, 0x01282A,
		0x515C6B, 0x657486, 0xA0AEB7, 0xD7E2E4, 0x454E5E, 0x292F40, 0x0F1224,
		0x4A5459, 0x616F75, 0x9EABAE, 0xD6E0E1, 0x3F484D, 0x252B32, 0x0D1118,
		0xDA3B01, 0xF44801, 0xFE7E34, 0xFEB16C, 0xC53201, 0x931E01, 0x660B00,
		0xE3008C, 0xFF04A4, 0xFF4FCB, 0xFF94EE, 0xCD007B, 0x990053, 0x69002E,
		0x8764B8, 0x9979C3, 0xC3A8DC, 0xE9D4F2, 0x744FAC, 0x4B3279, 0x241251,
		0x00B294, 0x00D3B1, 0x21FFE0, 0x67FFEE, 0x009B7E, 0x00664A, 0x00361B,
		0x567C73, 0x66948A, 0x9ABEB8, 0xC5E7E4, 0x496B62, 0x2B443A, 0x0F2015,
		0x647C64, 0x779177, 0xA8BEA7, 0xD3E7D2, 0x556B55, 0x324332, 0x121E12,
		0xEF6950, 0xF1795F, 0xF7A083, 0xFCC2A3, 0xEC492D, 0xB12210, 0x670E09,
		0xBF0077, 0xE60094, 0xFF43C8, 0xFF97EE, 0xAD0069, 0x830048, 0x5C0029,
		0x744DA9, 0x8963B8, 0xBA9CD4, 0xE6CEF0, 0x654199, 0x402775, 0x1F0E54,
		0x018574, 0x01B09C, 0x1AFDE9, 0x77FEF7, 0x017463, 0x014E3A, 0x002A15,
		0x486860, 0x5B837A, 0x94B5AF, 0xC7E4E1, 0x3D5A52, 0x243930, 0x0D1B11,
		0x525E54, 0x69786B, 0xA3AFA5, 0xD7E2D9, 0x465147, 0x29312A, 0x0F150F,
		0xD13438, 0xD84B4C, 0xE8807A, 0xF7B1A5, 0xBE2B2E, 0x8D1A1C, 0x61090A,
		0xC239B3, 0xCB4FBF, 0xE183D9, 0xF4B2F1, 0xAE30A0, 0x7F1D75, 0x540A4D,
		0xB146C2, 0xBD5BCB, 0xD88DE1, 0xF1BBF4, 0x9E3AB0, 0x6F2382, 0x460D5A,
		0x00CC6A, 0x00E775, 0x26FF8E, 0x5FFFA5, 0x00B25A, 0x007635, 0x003F13,
		0x498205, 0x61A907, 0x99F618, 0xC1F96C, 0x3E7204, 0x254B03, 0x0D2801,
		0x847545, 0x9D8D52, 0xC2B986, 0xE9E5B0, 0x74633B, 0x4E3B23, 0x2B150C,
		0xFF4343, 0xFF5653, 0xFF8279, 0xFFAB9B, 0xFF1F1F, 0xC90000, 0x7C0000,
		0x9A0089, 0xC800B3, 0xFF35EE, 0xFF97FA, 0x8C007C, 0x6B005E, 0x4C0042,
		0x881798, 0xAB1DBE, 0xD95BE6, 0xEFACF2, 0x7B148B, 0x5B0C6D, 0x3F0451,
		0x10893E, 0x14AE4C, 0x3AE872, 0x89F1A5, 0x0E7835, 0x08521F, 0x032E0B,
		0x107C10, 0x19A115, 0x45E532, 0x95EF81, 0x0E6D0E, 0x084B08, 0x032B03,
		0x7E735F, 0x948971, 0xBFB8A3, 0xE8E5CE, 0x6D6251, 0x473A30, 0x241511,
	];

	private static readonly float[] _grayOffsets = [0.17f, 0.10f, 0.05f, 0f, -0.05f, -0.10f, -0.17f];

	/// <param name="accent">The accent color.</param>
	/// <param name="normalize">
	/// When true, the accent itself is adjusted like Windows does for a user-picked color (lightness clamping).
	/// When false, the accent is preserved and only the shades are derived.
	/// </param>
	public static AccentColorPalette Generate(Color accent, bool normalize)
	{
		var rgb = (accent.R << 16) | (accent.G << 8) | accent.B;
		if (TryGetKnownPalette(rgb, out var known))
		{
			return known;
		}

		var (h, s, l) = RgbToHslSingle(accent.R, accent.G, accent.B);
		var palette = 0.25f > l || (1.0 - l) < 0.25 || 0.15f > s
			? GetGrayPalette(h, s, l)
			: GetFormulaPalette(accent.R, accent.G, accent.B, clampLightness: true);

		if (!normalize && palette[0] != rgb)
		{
			// Windows would have adjusted the accent: keep it, and derive the shades around it with the same Lab math.
			palette = GetFormulaPalette(accent.R, accent.G, accent.B, clampLightness: false);
			palette[0] = rgb;
		}

		return ToPalette(palette, 0);
	}

	private static bool TryGetKnownPalette(int rgb, out AccentColorPalette palette)
	{
		for (var i = 0; i < _knownPalettes.Length; i += 7)
		{
			if (_knownPalettes[i] == rgb)
			{
				palette = ToPalette(_knownPalettes, i);
				return true;
			}
		}

		palette = default;
		return false;
	}

	private static AccentColorPalette ToPalette(int[] p, int start) =>
		new(ToColor(p[start]), ToColor(p[start + 1]), ToColor(p[start + 2]), ToColor(p[start + 3]), ToColor(p[start + 4]), ToColor(p[start + 5]), ToColor(p[start + 6]));

	private static Color ToColor(int rgb) => Color.FromArgb(0xFF, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

	private static int Pack(int r, int g, int b) => (r << 16) | (g << 8) | b;

	#region Grays, blacks and whites (AccentColorUtils, single precision)

	private const float Epsilon = 2.3841858E-07f;
	private const float OneSixth = 1f / 6f;
	private const float OneThird = 1f / 3f;
	private const float TwoThirds = 2f / 3f;

	private static int[] GetGrayPalette(float h, float s, float l)
	{
		if (0.25f > l)
		{
			l = 0.25f;
		}
		else if ((1.0 - l) < 0.25)
		{
			l = 0.75f;
		}

		var spread = Math.Abs(0.5f - l);
		spread += spread;
		spread += spread;

		var offsets = new float[7];
		for (var i = 0; i < 7; i++)
		{
			offsets[i] = spread * _grayOffsets[i];
		}

		// Shift the whole ramp back into [0, 1] when an end overflows.
		var shift = offsets[0] + l;
		shift = 1f < shift ? shift - 1f : Math.Min(0f, offsets[6] + l);

		// Offsets run Light3 to Dark3; the result is ordered Accent, Light1-3, Dark1-3.
		var ramp = new int[7];
		for (var i = 0; i < 7; i++)
		{
			var value = (l + offsets[i]) - shift;
			var capped = 1f > value ? value : 1f;
			ramp[i] = HslToRgbSingle(h, s, capped > 0f ? capped : 0f);
		}

		return [ramp[3], ramp[2], ramp[1], ramp[0], ramp[4], ramp[5], ramp[6]];
	}

	private static (float H, float S, float L) RgbToHslSingle(byte red, byte green, byte blue)
	{
		float r = red / 255f, g = green / 255f, b = blue / 255f;
		var min = Math.Min(Math.Min(r, g), b);
		var max = Math.Max(Math.Max(r, g), b);
		var sum = min + max;
		var delta = max - min;
		var l = sum * 0.5f;
		if (Math.Abs(delta) < Epsilon)
		{
			return (0f, 0f, l);
		}

		var s = l <= 0.5f ? delta / sum : delta / (2f - sum);
		var deltaB = (max - b) * OneSixth / delta;
		var deltaG = (max - g) * OneSixth / delta;
		float h;
		if (Math.Abs(r - max) < Epsilon)
		{
			h = deltaB - deltaG;
		}
		else
		{
			var deltaR = (max - r) * OneSixth / delta;
			h = Math.Abs(g - max) < Epsilon ? (OneThird + deltaR) - deltaB : (deltaG + TwoThirds) - deltaR;
		}

		if (0f > h)
		{
			h += 1f;
		}
		else if (h > 1f)
		{
			h -= 1f;
		}

		return (h, s, l);
	}

	private static int HslToRgbSingle(float h, float s, float l)
	{
		float r, g, b;
		if (Math.Abs(s) < Epsilon)
		{
			r = g = b = l;
		}
		else
		{
			var v2 = 0.5f >= l ? (s + 1f) * l : (s + l) - s * l;
			var v1 = (l + l) - v2;
			r = HueToChannel(v1, v2, h + OneThird);
			g = HueToChannel(v1, v2, h);
			b = HueToChannel(v1, v2, h - OneThird);
		}

		// Windows truncates here rather than rounding.
		return Pack((byte)(int)(r * 255f), (byte)(int)(g * 255f), (byte)(int)(b * 255f));
	}

	private static float HueToChannel(float v1, float v2, float hue)
	{
		if (0f > hue)
		{
			hue += 1f;
		}

		if (hue > 1f)
		{
			hue -= 1f;
		}

		if (OneSixth > hue)
		{
			return (v2 - v1) * hue * 6f + v1;
		}

		if (0.5f > hue)
		{
			return v2;
		}

		if (TwoThirds > hue)
		{
			return (v2 - v1) * (TwoThirds - hue) * 6f + v1;
		}

		return v1;
	}

	#endregion

	#region Lab formula (palette_formula, double precision)

	private const double WhiteX = 95.047;
	private const double WhiteY = 100.0;
	private const double WhiteZ = 108.883;

	private static int[] GetFormulaPalette(byte red, byte green, byte blue, bool clampLightness)
	{
		var (x, y, z) = RgbToXyz(red, green, blue);
		var (l, a, b) = XyzToLab(x, y, z);
		if (clampLightness)
		{
			if (l < 49.0)
			{
				l = 49.0;
			}
			else if (l > 50.0)
			{
				l = 50.0;
			}
		}

		var accent = LabToRgb(l, a, b);
		var dark = LabToRgb(0.0, a, b);
		var light = LabToRgb(100.0, a, b);

		return
		[
			Pack(accent.R, accent.G, accent.B),
			SaturateMatch(accent, Lerp(accent, light, 0.16)),
			SaturateMatch(accent, Lerp(accent, light, 0.58)),
			SaturateMatch(accent, Lerp(accent, light, 0.82)),
			SaturateMatch(accent, Lerp(dark, accent, 0.78)),
			SaturateMatch(accent, Lerp(dark, accent, 0.5)),
			SaturateMatch(accent, Lerp(dark, accent, 0.18)),
		];
	}

	private static (int R, int G, int B) Lerp((int R, int G, int B) from, (int R, int G, int B) to, double t) =>
		(LerpChannel(from.R, to.R, t), LerpChannel(from.G, to.G, t), LerpChannel(from.B, to.B, t));

	private static int LerpChannel(int from, int to, double t) => (int)(from * (1.0 - t) + to * t + 0.5);

	// Raises the shade's HSL saturation to at least the accent's.
	private static int SaturateMatch((int R, int G, int B) accent, (int R, int G, int B) shade)
	{
		var accentSaturation = RgbToHslDouble(accent).S;
		var (h, s, l) = RgbToHslDouble(shade);
		if (accentSaturation > s)
		{
			s = accentSaturation;
		}

		return HslToRgbDouble(h, s, l);
	}

	private static (double H, double S, double L) RgbToHslDouble((int R, int G, int B) color)
	{
		double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
		var max = Math.Max(Math.Max(r, g), b);
		var min = Math.Min(Math.Min(r, g), b);
		var l = (max + min) * 0.5;
		if (max == min)
		{
			return (0.0, 0.0, l);
		}

		var delta = max - min;
		var s = delta / (l > 0.5 ? 2.0 - max - min : min + max);
		double h;
		if (max == r)
		{
			h = ((g - b) / delta + (g < b ? 6.0 : 0.0)) / 6.0;
		}
		else if (max == g)
		{
			h = ((b - r) / delta + 2.0) / 6.0;
		}
		else
		{
			h = ((r - g) / delta + 4.0) / 6.0;
		}

		return (h, s, l);
	}

	private static int HslToRgbDouble(double hue, double saturation, double lightness)
	{
		var h = hue % 1.0;
		if (hue < 0)
		{
			h += 1.0;
		}

		h = h < 0 ? 0 : Math.Min(1.0, h);
		var s = saturation < 0 ? 0 : Math.Min(1.0, saturation);
		var l = lightness < 0 ? 0 : Math.Min(1.0, lightness);

		var q = l <= 0.5 ? (s + 1.0) * l : (l + s) - l * s;
		if (q <= 0)
		{
			return 0;
		}

		var h6 = h * 6.0;
		var sextant = (int)Math.Floor(h6);
		var p = l + l - q;
		var fraction = h6 - sextant;
		var vsf = (q - p) / q * q * fraction;
		var mid1 = q - vsf;
		var mid2 = vsf + p;

		var (r, g, b) = sextant switch
		{
			0 => (q, mid2, p),
			1 => (mid1, q, p),
			2 => (p, q, mid2),
			3 => (p, mid1, q),
			4 => (mid2, p, q),
			5 => (q, p, mid1),
			_ => (0.0, 0.0, 0.0),
		};

		return Pack((int)(r * 255.0 + 0.5), (int)(g * 255.0 + 0.5), (int)(b * 255.0 + 0.5));
	}

	private static double ToLinear(byte channel)
	{
		var v = channel / 255.0;
		return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
	}

	private static (double X, double Y, double Z) RgbToXyz(byte red, byte green, byte blue)
	{
		double r = ToLinear(red), g = ToLinear(green), b = ToLinear(blue);
		return (
			(r * 0.4124 + g * 0.3576 + b * 0.1805) * 100.0,
			(g * 0.7152 + r * 0.2126 + b * 0.0722) * 100.0,
			(r * 0.0193 + g * 0.1192 + b * 0.9505) * 100.0);
	}

	private static double LabF(double t) =>
		t > 0.008856 ? Math.Pow(t, 0.3333333333333333) : t * 7.787 + 0.13793103448275862;

	private static double LabFInverse(double t) =>
		t > 0.20689655172413793 ? Math.Pow(t, 3.0) : (t - 0.13793103448275862) * 0.12841854934601665;

	private static (double L, double A, double B) XyzToLab(double x, double y, double z)
	{
		double fx = LabF(x / WhiteX), fy = LabF(y / WhiteY), fz = LabF(z / WhiteZ);
		return (fy * 116.0 - 16.0, (fx - fy) * 500.0, (fy - fz) * 200.0);
	}

	private static (int R, int G, int B) LabToRgb(double l, double a, double b)
	{
		var fy = (l + 16.0) / 116.0;
		var fx = a / 500.0 + fy;
		var fz = fy - b / 200.0;
		var x = LabFInverse(fx) * WhiteX / 100.0;
		var y = LabFInverse(fy) * WhiteY / 100.0;
		var z = LabFInverse(fz) * WhiteZ / 100.0;

		return (
			ToSrgbByte(x * 3.2406 - y * 1.5372 - z * 0.4986),
			ToSrgbByte(y * 1.8758 - x * 0.9689 + z * 0.0415),
			ToSrgbByte(x * 0.0557 - y * 0.204 + z * 1.057));
	}

	private static int ToSrgbByte(double linear)
	{
		var v = linear > 0.0031308 ? 1.055 * Math.Pow(linear, 0.4166666666666667) - 0.055 : linear * 12.92;
		return (int)(Math.Max(0.0, Math.Min(1.0, v)) * 255.0 + 0.5);
	}

	#endregion
}
