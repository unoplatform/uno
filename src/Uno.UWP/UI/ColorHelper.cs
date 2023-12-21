using System;
using System.ComponentModel;
using System.Linq;
using Uno;
using Color = global::Windows/*Intentional space for WinUI upgrade tool*/.UI.Color;
using KnownColors = global::Windows/*Intentional space for WinUI upgrade tool*/.UI.KnownColors;

namespace Windows.UI
{
	public static partial class ColorHelper
	{
		// Uno Doc: this is a simple implementation that doesn't use localized strings
		public static string ToDisplayName(Color color)
		{
			// IFCPTR_RETURN(returnValue);
			//
			// int colorNameResourceId = 0;
			// IFC_RETURN(ColorHelper_ToDisplayNameId(color, &colorNameResourceId));
			// IFC_RETURN(DXamlCore::GetCurrent()->GetLocalizedResourceString(colorNameResourceId, returnValue));
			//
			// return S_OK;

			return _toDisplayColor(color);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Color FromARGB(byte a, byte r, byte g, byte b)
			=> Color.FromArgb(a, r, g, b);

		public static Color FromArgb(byte a, byte r, byte g, byte b)
			=> Color.FromArgb(a, r, g, b);

		// Uno Specific
		private static double ColorDistance(Color color1, Color color2)
		{
			// https://stackoverflow.com/a/8796867
			var a1 = (double)color1.A / 255;
			var r1 = (double)color1.R / 255 * a1;
			var g1 = (double)color1.G / 255 * a1;
			var b1 = (double)color1.B / 255 * a1;
			var a2 = (double)color2.A / 255;
			var r2 = (double)color2.R / 255 * a2;
			var g2 = (double)color2.G / 255 * a2;
			var b2 = (double)color2.B / 255 * a2;

			return Math.Max((r1 - r2) * (r1 - r2), (r1 - r2 + a2 - a1) * (r1 - r2 + a2 - a1)) +
				Math.Max((g1 - g2) * (g1 - g2), (g1 - g2 + a2 - a1) * (g1 - g2 + a2 - a1)) +
				Math.Max((b1 - b2) * (b1 - b2), (b1 - b2 + a2 - a1) * (b1 - b2 + a2 - a1));
		}

		// Uno Specific
		private static Func<Color, string> _toDisplayColor =
			Funcs.CreateMemoized((Color color) => Enum.GetName(Enum.GetValues(typeof(KnownColors)).Cast<KnownColors>().MinBy(c => ColorDistance(new Color((uint)c), color))!));
	}
}
