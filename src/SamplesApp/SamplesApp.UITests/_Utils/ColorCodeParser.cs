using System;
using System.Drawing;
using System.Linq;

namespace SamplesApp.UITests._Utils
{
	internal static class ColorCodeParser
	{
		public static Color Parse(string colorCode)
		{
			if (colorCode == null)
			{
				throw new ArgumentNullException(nameof(colorCode));
			}

			if (!colorCode.StartsWith("#", StringComparison.OrdinalIgnoreCase))
			{
				throw new FormatException("Color code must start with #");
			}

			byte a = 0x00;
			byte r = 0x00;
			byte g = 0x00;
			byte b = 0x00;

			colorCode = colorCode.Substring(1);

			if (colorCode.Length == 3)
			{
				a = 0xFF;
				r = Convert.ToByte(new String(colorCode[0], 2), 16);
				g = Convert.ToByte(new String(colorCode[1], 2), 16);
				b = Convert.ToByte(new String(colorCode[2], 2), 16);
			}
			else if (colorCode.Length == 4)
			{
				a = Convert.ToByte(new String(colorCode[0], 2), 16);
				r = Convert.ToByte(new String(colorCode[1], 2), 16);
				g = Convert.ToByte(new String(colorCode[2], 2), 16);
				b = Convert.ToByte(new String(colorCode[3], 2), 16);
			}
			else if (colorCode.Length == 6)
			{
				a = 0xFF;
				r = Convert.ToByte(colorCode.Substring(0, 2), 16);
				g = Convert.ToByte(colorCode.Substring(2, 2), 16);
				b = Convert.ToByte(colorCode.Substring(4, 2), 16);
			}
			else if (colorCode.Length == 8)
			{
				a = Convert.ToByte(colorCode.Substring(0, 2), 16);
				r = Convert.ToByte(colorCode.Substring(2, 2), 16);
				g = Convert.ToByte(colorCode.Substring(4, 2), 16);
				b = Convert.ToByte(colorCode.Substring(6, 2), 16);
			}
			else
			{
				throw new FormatException($"Failed to parse color code: #{colorCode}");
			}

			return System.Drawing.Color.FromArgb(a, r, g, b);
		}
	}
}
