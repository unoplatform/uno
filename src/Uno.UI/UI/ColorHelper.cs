using System;
using System.ComponentModel;
using System.Linq;
using DirectUI;
using WindowsColor = Windows/*Intentional space for WinUI upgrade tool*/.UI.Color;

namespace Microsoft.UI;

public static partial class ColorHelper
{
	/// <summary>
	/// Retrieves the display name of the specified color.
	/// </summary>
	/// <param name="color">The color to get the name for.</param>
	/// <returns>The localized display name of the color.</returns>
	public static string ToDisplayName(WindowsColor color)
	{
		//uno specific

		var closestKnownColor = Enum.GetValues<KnownColors>()
			.OrderBy(c => ColorDistance(new WindowsColor((uint)c), color))
			.First();

		var resourceId = closestKnownColor.ToString();
		var resource = DXamlCore.Current.GetLocalizedResourceString(resourceId);

		if (string.IsNullOrEmpty(resource))
		{
			return closestKnownColor.ToString();
		}

		return resource;
	}

	//uno specific
	static double ColorDistance(WindowsColor color1, WindowsColor color2)
	{
		var r1 = (int)(color1.R);
		var g1 = (int)(color1.G);
		var b1 = (int)(color1.B);
		var r2 = (int)(color2.R);
		var g2 = (int)(color2.G);
		var b2 = (int)(color2.B);

		return Math.Sqrt(Math.Pow(r1 - r2, 2) + Math.Pow(g1 - g2, 2) + Math.Pow(b1 - b2, 2));
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
}
