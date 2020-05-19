using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

// Do not remove or change for the WinUI conversion tool (space is required).
using Color = global::Windows .UI.Color;

namespace Windows.UI
{
	public static partial class Colors
	{
		private static Dictionary<string, Color> _colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
		
		public static Color FromARGB(byte a, byte r, byte g, byte b)
		{
			return Color.FromArgb(a, r, g, b);
		}

		public static Color FromInteger(int color)
		{
			return FromARGB(
				(byte)((color & 0xFF000000) >> 24),
				(byte)((color & 0x00FF0000) >> 16),
				(byte)((color & 0x0000FF00) >> 8),
				(byte)((color & 0x000000FF))
			);
		}

		/// <summary>
		/// Parses a string representing a color 
		/// </summary>
		/// <param name="colorCode"></param>
		/// <returns></returns>
		public static Color Parse(string colorCode)
		{
			if(colorCode?.StartsWith("#") ?? false)
			{
				return FromARGB(colorCode);
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(colorCode))
				{
					Color color;

					if (!_colorMap.TryGetValue(colorCode, out color))
					{
						var field = typeof(Colors).GetField(colorCode);

						if (field != null)
						{
							_colorMap[colorCode] = color = (Color)field.GetValue(null);
						}
						else
						{
							throw new InvalidOperationException($"The color {colorCode} is unknown");
						}
					}

					return color;
				}
				else
				{
					throw new InvalidOperationException($"Cannot parse an empty color string");
				}
			}
		}

		/// <summary>
		/// Takes a color code as an ARGB, RGB, #ARGB, #RGB string and returns a color. 
		/// 
		/// Remark: if single digits are used to define the color, they will
		/// be duplicated (example: FFD8 will become FFFFDD88)
		/// </summary>
		/// <param name="colorCode"></param>
		/// <returns></returns>
		public static Color FromARGB(string colorCode)
		{
			byte a, r, b, g;
			a = r = g = b = new byte();

			colorCode = colorCode.TrimStart(new char[] { '#' });

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
				a = 0xFF;
				r = 0xFF;
				g = 0x0;
				b = 0x0;
			}
			return Color.FromArgb(a, r, g, b);
		}

		public static readonly Color Transparent = FromInteger(0x00FFFFFF);
		public static readonly Color AliceBlue = FromInteger(unchecked((int)0xFFF0F8FF));
		public static readonly Color AntiqueWhite = FromInteger(unchecked((int)0xFFFAEBD7));
		public static readonly Color Aqua = FromInteger(unchecked((int)0xFF00FFFF));
		public static readonly Color Aquamarine = FromInteger(unchecked((int)0xFF7FFFD4));
		public static readonly Color Azure = FromInteger(unchecked((int)0xFFF0FFFF));
		public static readonly Color Beige = FromInteger(unchecked((int)0xFFF5F5DC));
		public static readonly Color Bisque = FromInteger(unchecked(unchecked((int)0xFFFFE4C4)));
		public static readonly Color Black = FromInteger(unchecked((int)0xFF000000));
		public static readonly Color BlanchedAlmond = FromInteger(unchecked((int)0xFFFFEBCD));
		public static readonly Color Blue = FromInteger(unchecked((int)0xFF0000FF));
		public static readonly Color BlueViolet = FromInteger(unchecked((int)0xFF8A2BE2));
		public static readonly Color Brown = FromInteger(unchecked((int)0xFFA52A2A));
		public static readonly Color BurlyWood = FromInteger(unchecked((int)0xFFDEB887));
		public static readonly Color CadetBlue = FromInteger(unchecked((int)0xFF5F9EA0));
		public static readonly Color Chartreuse = FromInteger(unchecked((int)0xFF7FFF00));
		public static readonly Color Chocolate = FromInteger(unchecked((int)0xFFD2691E));
		public static readonly Color Coral = FromInteger(unchecked((int)0xFFFF7F50));
		public static readonly Color CornflowerBlue = FromInteger(unchecked((int)0xFF6495ED));
		public static readonly Color Cornsilk = FromInteger(unchecked((int)0xFFFFF8DC));
		public static readonly Color Crimson = FromInteger(unchecked((int)0xFFDC143C));
		public static readonly Color Cyan = FromInteger(unchecked((int)0xFF00FFFF));
		public static readonly Color DarkBlue = FromInteger(unchecked((int)0xFF00008B));
		public static readonly Color DarkCyan = FromInteger(unchecked((int)0xFF008B8B));
		public static readonly Color DarkGoldenrod = FromInteger(unchecked((int)0xFFB8860B));
		public static readonly Color DarkGray = FromInteger(unchecked((int)0xFFA9A9A9));
		public static readonly Color DarkGreen = FromInteger(unchecked((int)0xFF006400));
		public static readonly Color DarkKhaki = FromInteger(unchecked((int)0xFFBDB76B));
		public static readonly Color DarkMagenta = FromInteger(unchecked((int)0xFF8B008B));
		public static readonly Color DarkOliveGreen = FromInteger(unchecked((int)0xFF556B2F));
		public static readonly Color DarkOrange = FromInteger(unchecked((int)0xFFFF8C00));
		public static readonly Color DarkOrchid = FromInteger(unchecked((int)0xFF9932CC));
		public static readonly Color DarkRed = FromInteger(unchecked((int)0xFF8B0000));
		public static readonly Color DarkSalmon = FromInteger(unchecked((int)0xFFE9967A));
		public static readonly Color DarkSeaGreen = FromInteger(unchecked((int)0xFF8FBC8B));
		public static readonly Color DarkSlateBlue = FromInteger(unchecked((int)0xFF483D8B));
		public static readonly Color DarkSlateGray = FromInteger(unchecked((int)0xFF2F4F4F));
		public static readonly Color DarkTurquoise = FromInteger(unchecked((int)0xFF00CED1));
		public static readonly Color DarkViolet = FromInteger(unchecked((int)0xFF9400D3));
		public static readonly Color DeepPink = FromInteger(unchecked((int)0xFFFF1493));
		public static readonly Color DeepSkyBlue = FromInteger(unchecked((int)0xFF00BFFF));
		public static readonly Color DimGray = FromInteger(unchecked((int)0xFF696969));
		public static readonly Color DodgerBlue = FromInteger(unchecked((int)0xFF1E90FF));
		public static readonly Color Firebrick = FromInteger(unchecked((int)0xFFB22222));
		public static readonly Color FloralWhite = FromInteger(unchecked((int)0xFFFFFAF0));
		public static readonly Color ForestGreen = FromInteger(unchecked((int)0xFF228B22));
		public static readonly Color Fuchsia = FromInteger(unchecked((int)0xFFFF00FF));
		public static readonly Color Gainsboro = FromInteger(unchecked((int)0xFFDCDCDC));
		public static readonly Color GhostWhite = FromInteger(unchecked((int)0xFFF8F8FF));
		public static readonly Color Gold = FromInteger(unchecked((int)0xFFFFD700));
		public static readonly Color Goldenrod = FromInteger(unchecked((int)0xFFDAA520));
		public static readonly Color Gray = FromInteger(unchecked((int)0xFF808080));
		public static readonly Color Green = FromInteger(unchecked((int)0xFF008000));
		public static readonly Color GreenYellow = FromInteger(unchecked((int)0xFFADFF2F));
		public static readonly Color Honeydew = FromInteger(unchecked((int)0xFFF0FFF0));
		public static readonly Color HotPink = FromInteger(unchecked((int)0xFFFF69B4));
		public static readonly Color IndianRed = FromInteger(unchecked((int)0xFFCD5C5C));
		public static readonly Color Indigo = FromInteger(unchecked((int)0xFF4B0082));
		public static readonly Color Ivory = FromInteger(unchecked((int)0xFFFFFFF0));
		public static readonly Color Khaki = FromInteger(unchecked((int)0xFFF0E68C));
		public static readonly Color Lavender = FromInteger(unchecked((int)0xFFE6E6FA));
		public static readonly Color LavenderBlush = FromInteger(unchecked((int)0xFFFFF0F5));
		public static readonly Color LawnGreen = FromInteger(unchecked((int)0xFF7CFC00));
		public static readonly Color LemonChiffon = FromInteger(unchecked((int)0xFFFFFACD));
		public static readonly Color LightBlue = FromInteger(unchecked((int)0xFFADD8E6));
		public static readonly Color LightCoral = FromInteger(unchecked((int)0xFFF08080));
		public static readonly Color LightCyan = FromInteger(unchecked((int)0xFFE0FFFF));
		public static readonly Color LightGoldenrodYellow = FromInteger(unchecked((int)0xFFFAFAD2));
		public static readonly Color LightGray = FromInteger(unchecked((int)0xFFD3D3D3));
		public static readonly Color LightGreen = FromInteger(unchecked((int)0xFF90EE90));
		public static readonly Color LightPink = FromInteger(unchecked((int)0xFFFFB6C1));
		public static readonly Color LightSalmon = FromInteger(unchecked((int)0xFFFFA07A));
		public static readonly Color LightSeaGreen = FromInteger(unchecked((int)0xFF20B2AA));
		public static readonly Color LightSkyBlue = FromInteger(unchecked((int)0xFF87CEFA));
		public static readonly Color LightSlateGray = FromInteger(unchecked((int)0xFF778899));
		public static readonly Color LightSteelBlue = FromInteger(unchecked((int)0xFFB0C4DE));
		public static readonly Color LightYellow = FromInteger(unchecked((int)0xFFFFFFE0));
		public static readonly Color Lime = FromInteger(unchecked((int)0xFF00FF00));
		public static readonly Color LimeGreen = FromInteger(unchecked((int)0xFF32CD32));
		public static readonly Color Linen = FromInteger(unchecked((int)0xFFFAF0E6));
		public static readonly Color Magenta = FromInteger(unchecked((int)0xFFFF00FF));
		public static readonly Color Maroon = FromInteger(unchecked((int)0xFF800000));
		public static readonly Color MediumAquamarine = FromInteger(unchecked((int)0xFF66CDAA));
		public static readonly Color MediumBlue = FromInteger(unchecked((int)0xFF0000CD));
		public static readonly Color MediumOrchid = FromInteger(unchecked((int)0xFFBA55D3));
		public static readonly Color MediumPurple = FromInteger(unchecked((int)0xFF9370DB));
		public static readonly Color MediumSeaGreen = FromInteger(unchecked((int)0xFF3CB371));
		public static readonly Color MediumSlateBlue = FromInteger(unchecked((int)0xFF7B68EE));
		public static readonly Color MediumSpringGreen = FromInteger(unchecked((int)0xFF00FA9A));
		public static readonly Color MediumTurquoise = FromInteger(unchecked((int)0xFF48D1CC));
		public static readonly Color MediumVioletRed = FromInteger(unchecked((int)0xFFC71585));
		public static readonly Color MidnightBlue = FromInteger(unchecked((int)0xFF191970));
		public static readonly Color MintCream = FromInteger(unchecked((int)0xFFF5FFFA));
		public static readonly Color MistyRose = FromInteger(unchecked((int)0xFFFFE4E1));
		public static readonly Color Moccasin = FromInteger(unchecked((int)0xFFFFE4B5));
		public static readonly Color NavajoWhite = FromInteger(unchecked((int)0xFFFFDEAD));
		public static readonly Color Navy = FromInteger(unchecked((int)0xFF000080));
		public static readonly Color OldLace = FromInteger(unchecked((int)0xFFFDF5E6));
		public static readonly Color Olive = FromInteger(unchecked((int)0xFF808000));
		public static readonly Color OliveDrab = FromInteger(unchecked((int)0xFF6B8E23));
		public static readonly Color Orange = FromInteger(unchecked((int)0xFFFFA500));
		public static readonly Color OrangeRed = FromInteger(unchecked((int)0xFFFF4500));
		public static readonly Color Orchid = FromInteger(unchecked((int)0xFFDA70D6));
		public static readonly Color PaleGoldenrod = FromInteger(unchecked((int)0xFFEEE8AA));
		public static readonly Color PaleGreen = FromInteger(unchecked((int)0xFF98FB98));
		public static readonly Color PaleTurquoise = FromInteger(unchecked((int)0xFFAFEEEE));
		public static readonly Color PaleVioletRed = FromInteger(unchecked((int)0xFFDB7093));
		public static readonly Color PapayaWhip = FromInteger(unchecked((int)0xFFFFEFD5));
		public static readonly Color PeachPuff = FromInteger(unchecked((int)0xFFFFDAB9));
		public static readonly Color Peru = FromInteger(unchecked((int)0xFFCD853F));
		public static readonly Color Pink = FromInteger(unchecked((int)0xFFFFC0CB));
		public static readonly Color Plum = FromInteger(unchecked((int)0xFFDDA0DD));
		public static readonly Color PowderBlue = FromInteger(unchecked((int)0xFFB0E0E6));
		public static readonly Color Purple = FromInteger(unchecked((int)0xFF800080));
		public static readonly Color Red = FromInteger(unchecked((int)0xFFFF0000));
		public static readonly Color RosyBrown = FromInteger(unchecked((int)0xFFBC8F8F));
		public static readonly Color RoyalBlue = FromInteger(unchecked((int)0xFF4169E1));
		public static readonly Color SaddleBrown = FromInteger(unchecked((int)0xFF8B4513));
		public static readonly Color Salmon = FromInteger(unchecked((int)0xFFFA8072));
		public static readonly Color SandyBrown = FromInteger(unchecked((int)0xFFF4A460));
		public static readonly Color SeaGreen = FromInteger(unchecked((int)0xFF2E8B57));
		public static readonly Color SeaShell = FromInteger(unchecked((int)0xFFFFF5EE));
		public static readonly Color Sienna = FromInteger(unchecked((int)0xFFA0522D));
		public static readonly Color Silver = FromInteger(unchecked((int)0xFFC0C0C0));
		public static readonly Color SkyBlue = FromInteger(unchecked((int)0xFF87CEEB));
		public static readonly Color SlateBlue = FromInteger(unchecked((int)0xFF6A5ACD));
		public static readonly Color SlateGray = FromInteger(unchecked((int)0xFF708090));
		public static readonly Color Snow = FromInteger(unchecked((int)0xFFFFFAFA));
		public static readonly Color SpringGreen = FromInteger(unchecked((int)0xFF00FF7F));
		public static readonly Color SteelBlue = FromInteger(unchecked((int)0xFF4682B4));
		public static readonly Color Tan = FromInteger(unchecked((int)0xFFD2B48C));
		public static readonly Color Teal = FromInteger(unchecked((int)0xFF008080));
		public static readonly Color Thistle = FromInteger(unchecked((int)0xFFD8BFD8));
		public static readonly Color Tomato = FromInteger(unchecked((int)0xFFFF6347));
		public static readonly Color Turquoise = FromInteger(unchecked((int)0xFF40E0D0));
		public static readonly Color Violet = FromInteger(unchecked((int)0xFFEE82EE));
		public static readonly Color Wheat = FromInteger(unchecked((int)0xFFF5DEB3));
		public static readonly Color White = FromInteger(unchecked((int)0xFFFFFFFF));
		public static readonly Color WhiteSmoke = FromInteger(unchecked((int)0xFFF5F5F5));
		public static readonly Color Yellow = FromInteger(unchecked((int)0xFFFFFF00));
		public static readonly Color YellowGreen = FromInteger(unchecked((int)0xFF9ACD32));
	}
}
