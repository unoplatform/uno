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
		var id = GetColorNameResourceId(color);
		return DXamlCore.Current.GetLocalizedResourceString($"{id}");
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
