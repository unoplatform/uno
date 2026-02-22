// This file is included in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
using System;
using System.ComponentModel;
using Uno.Extensions;
using WindowsColor = Windows.UI.Color;

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI;
#else
namespace Windows.UI;
#endif

public static partial class ColorHelper
{
	/// <summary>
	/// Retrieves the display name of the specified color.
	/// </summary>
	/// <param name="color">The color to get the name for.</param>
	/// <returns>The localized display name of the color.</returns>
	public static string ToDisplayName(WindowsColor color)
	{
		var id = GetColorNameResourceId(color);
		//Uno specific
		return global::Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Uno.UI/Resources").GetString(id.ToStringInvariant());
		// WinUI uses the GetLocalizedResourceString from DXamlCore, but it is not available under Windows.UI
		//return DirectUI.DXamlCore.Current.GetLocalizedResourceString(id);
	}

	/// <summary>
	/// Converts a color string in hex format to a Windows.UI.Color.
	/// </summary>
	internal static WindowsColor ConvertColorFromHexString(string colorString)
	{
		try
		{
			colorString = colorString.Replace("#", string.Empty);

			byte a = 255;
			byte r, g, b;

			if (colorString.Length == 6)
			{
				// #RRGGBB format
				r = (byte)Convert.ToUInt32(colorString.Substring(0, 2), 16);
				g = (byte)Convert.ToUInt32(colorString.Substring(2, 2), 16);
				b = (byte)Convert.ToUInt32(colorString.Substring(4, 2), 16);
			}
			else if (colorString.Length == 8)
			{
				// #AARRGGBB format
				a = (byte)Convert.ToUInt32(colorString.Substring(0, 2), 16);
				r = (byte)Convert.ToUInt32(colorString.Substring(2, 2), 16);
				g = (byte)Convert.ToUInt32(colorString.Substring(4, 2), 16);
				b = (byte)Convert.ToUInt32(colorString.Substring(6, 2), 16);
			}
			else
			{
				return Colors.Black;
			}

			return WindowsColor.FromArgb(a, r, g, b);
		}
		catch
		{
			return Colors.Black;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static WindowsColor FromARGB(byte a, byte r, byte g, byte b)
		=> WindowsColor.FromArgb(a, r, g, b);

	/// <summary>
	/// Generates a Color structure, based on discrete Byte values for ARGB components. C# and Microsoft Visual Basic code should use Color.FromArgb instead.
	/// </summary>
	/// <param name="a">The A (transparency) component of the desired color. Range is 0-255.</param>
	/// <param name="r">The R component of the desired color. Range is 0-255.</param>
	/// <param name="g">The G component of the desired color. Range is 0-255.</param>
	/// <param name="b">The B component of the desired color. Range is 0-255.</param>
	/// <returns>The generated Color value.</returns>
	public static WindowsColor FromArgb(byte a, byte r, byte g, byte b)
		=> WindowsColor.FromArgb(a, r, g, b);

	#region Resource IDs Lookup Tables
	private static ReadOnlySpan<int> HueLimitsForSatLevel4 =>
	[
		0,
		11,
		26,
		0,
		0,
		38,
		45,
		0,
		0,
		56,
		100,
		121,
		129,
		0,
		140,
		0,
		180,
		0,
		0,
		224,
		241,
		0,
		256
	];

	private static ReadOnlySpan<int> HueLimitsForSatLevel5 =>
	[
		0,
		13,
		27,
		0,
		0,
		36,
		45,
		0,
		0,
		59,
		118,
		0,
		127,
		136,
		142,
		0,
		185,
		0,
		0,
		216,
		239,
		0,
		256
	];

	private static ReadOnlySpan<int> HueLimitsForSatLevel3 =>
	[
		0,
		8,
		0,
		0,
		39,
		46,
		0,
		0,
		0,
		71,
		120,
		0,
		131,
		144,
		0,
		0,
		163,
		0,
		177,
		211,
		249,
		0,
		256
	];

	private static ReadOnlySpan<int> HueLimitsForSatLevel2 =>
	[
		0,
		10,
		0,
		32,
		46,
		0,
		0,
		0,
		61,
		0,
		106,
		0,
		136,
		144,
		0,
		0,
		0,
		158,
		166,
		241,
		0,
		0,
		256
	];

	private static ReadOnlySpan<int> HueLimitsForSatLevel1 =>
	[
		8,
		0,
		0,
		44,
		0,
		0,
		0,
		63,
		0,
		0,
		122,
		0,
		134,
		0,
		0,
		0,
		0,
		166,
		176,
		241,
		0,
		256,
		0
	];

	private static ReadOnlySpan<int> LumLimitsForHueIndexHigh =>
	[
		170,
		170,
		170,
		155,
		170,
		170,
		170,
		170,
		170,
		115,
		170,
		170,
		170,
		170,
		170,
		170,
		170,
		170,
		150,
		150,
		170,
		140,
		165
	];

	private static ReadOnlySpan<int> LumLimitsForHueIndexLow =>
	[
		130,
		100,
		115,
		100,
		100,
		100,
		110,
		75,
		100,
		90,
		100,
		100,
		100,
		100,
		80,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100
	];

	private static ReadOnlySpan<uint> ColorNamesMid =>
	[
		5119,
		5135,
		5136,
		5137,
		5122,
		5138,
		5139,
		5140,
		5140,
		5141,
		5141,
		5142,
		5143,
		5126,
		5144,
		5129,
		5145,
		5146,
		5147,
		5148,
		5134,
		5137,
		5135
	];

	private static ReadOnlySpan<uint> ColorNamesDark =>
	[
		5137,
		5149,
		5137,
		5137,
		5137,
		5150,
		5150,
		5137,
		5151,
		5151,
		5151,
		5151,
		5152,
		5152,
		5152,
		5153,
		5153,
		5146,
		5147,
		5154,
		5155,
		5137,
		5149
	];

	private static ReadOnlySpan<uint> ColorNamesLight =>
	[
		5119,
		5120,
		5121,
		5122,
		5122,
		5123,
		5123,
		5122,
		5124,
		5125,
		5124,
		5124,
		5126,
		5127,
		5128,
		5129,
		5130,
		5131,
		5132,
		5133,
		5134,
		5122,
		5120
	];
	#endregion

	private static uint GetColorNameResourceId(WindowsColor color)
	{
		var hsl = color.ToHsl();
		double h = hsl.H * 255.0;
		double s = hsl.S * 255.0;
		double l = hsl.L * 255.0;

		if (l > 240.0)
		{
			return 5114;
		}

		if (l < 20.0)
		{
			return 5118;
		}

		if (s > 20.0)
		{
			ReadOnlySpan<int> hueLimits;

			if (s > 240.0)
			{
				hueLimits = HueLimitsForSatLevel5;
			}
			else if (s > 150.0)
			{
				hueLimits = HueLimitsForSatLevel4;
			}
			else if (s > 115.0)
			{
				hueLimits = HueLimitsForSatLevel3;
			}
			else if (s > 75.0)
			{
				hueLimits = HueLimitsForSatLevel2;
			}
			else
			{
				hueLimits = HueLimitsForSatLevel1;
			}

			var hueIndex = 0;
			while (hueIndex < 23 && hueLimits[hueIndex] <= h)
			{
				hueIndex++;
			}

			if (l > LumLimitsForHueIndexHigh[hueIndex])
			{
				return ColorNamesLight[hueIndex];
			}
			else if (l >= LumLimitsForHueIndexLow[hueIndex])
			{
				return ColorNamesMid[hueIndex];
			}
			else
			{
				return ColorNamesDark[hueIndex];
			}
		}
		else if (l <= 170.0)
		{
			return (l <= 100.0) ? (uint)5117 : 5116;
		}
		else
		{
			return 5115;
		}
	}
}
