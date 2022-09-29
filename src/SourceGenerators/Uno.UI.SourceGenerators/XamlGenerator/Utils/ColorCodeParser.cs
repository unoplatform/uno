using System;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static class ColorCodeParser
	{
		public static string ParseColorCode(string colorCode)
		{
			if (colorCode == null)
			{
				throw new ArgumentNullException(nameof(colorCode));
			}

			if (!colorCode.StartsWith("#"))
			{
				throw new FormatException("Color code must start with #");
			}

			byte a;
			byte r;
			byte g;
			byte b;

			if (colorCode.Length == 4)
			{
				a = 0xFF;
				r = Convert.ToByte(new string(colorCode[1], 2), 16);
				g = Convert.ToByte(new string(colorCode[2], 2), 16);
				b = Convert.ToByte(new string(colorCode[3], 2), 16);
			}
			else if (colorCode.Length == 5)
			{
				a = Convert.ToByte(new string(colorCode[1], 2), 16);
				r = Convert.ToByte(new string(colorCode[2], 2), 16);
				g = Convert.ToByte(new string(colorCode[3], 2), 16);
				b = Convert.ToByte(new string(colorCode[4], 2), 16);
			}
			else if (colorCode.Length == 7)
			{
				a = 0xFF;
				r = Convert.ToByte(colorCode.Substring(1, 2), 16);
				g = Convert.ToByte(colorCode.Substring(3, 2), 16);
				b = Convert.ToByte(colorCode.Substring(5, 2), 16);
			}
			else if (colorCode.Length == 9)
			{
				a = Convert.ToByte(colorCode.Substring(1, 2), 16);
				r = Convert.ToByte(colorCode.Substring(3, 2), 16);
				g = Convert.ToByte(colorCode.Substring(5, 2), 16);
				b = Convert.ToByte(colorCode.Substring(7, 2), 16);
			}
			else
			{
				throw new FormatException($"Failed to parse color code: #{colorCode}");
			}

			return $"{a}, {r}, {g}, {b}";
		}
	}
}
