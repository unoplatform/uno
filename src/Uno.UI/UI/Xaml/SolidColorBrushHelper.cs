using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml
{
	internal static class SolidColorBrushHelper
	{
		/// <summary>
		/// Takes a color code as an ARGB or RGB, or textual string from <see cref="Colors"/> string and returns a solid color brush.
		///
		/// Remark: if single digits are used to define the color, they will
		/// be duplicated (example: FFD8 will become FFFFDD88)
		/// </summary>
		public static SolidColorBrush Parse(string colorCode)
		{
			return new SolidColorBrush(Colors.Parse(colorCode));
		}

		public static SolidColorBrush Transparent => new SolidColorBrush(Colors.Transparent);
		public static SolidColorBrush AliceBlue => new SolidColorBrush(Colors.AliceBlue);
		public static SolidColorBrush AntiqueWhite => new SolidColorBrush(Colors.AntiqueWhite);
		public static SolidColorBrush Aqua => new SolidColorBrush(Colors.Aqua);
		public static SolidColorBrush Aquamarine => new SolidColorBrush(Colors.Aquamarine);
		public static SolidColorBrush Azure => new SolidColorBrush(Colors.Azure);
		public static SolidColorBrush Beige => new SolidColorBrush(Colors.Beige);
		public static SolidColorBrush Bisque => new SolidColorBrush(Colors.Bisque);
		public static SolidColorBrush Black => new SolidColorBrush(Colors.Black);
		public static SolidColorBrush BlanchedAlmond => new SolidColorBrush(Colors.BlanchedAlmond);
		public static SolidColorBrush Blue => new SolidColorBrush(Colors.Blue);
		public static SolidColorBrush BlueViolet => new SolidColorBrush(Colors.BlueViolet);
		public static SolidColorBrush Brown => new SolidColorBrush(Colors.Brown);
		public static SolidColorBrush BurlyWood => new SolidColorBrush(Colors.BurlyWood);
		public static SolidColorBrush CadetBlue => new SolidColorBrush(Colors.CadetBlue);
		public static SolidColorBrush Chartreuse => new SolidColorBrush(Colors.Chartreuse);
		public static SolidColorBrush Chocolate => new SolidColorBrush(Colors.Chocolate);
		public static SolidColorBrush Coral => new SolidColorBrush(Colors.Coral);
		public static SolidColorBrush CornflowerBlue => new SolidColorBrush(Colors.CornflowerBlue);
		public static SolidColorBrush Cornsilk => new SolidColorBrush(Colors.Cornsilk);
		public static SolidColorBrush Crimson => new SolidColorBrush(Colors.Crimson);
		public static SolidColorBrush Cyan => new SolidColorBrush(Colors.Cyan);
		public static SolidColorBrush DarkBlue => new SolidColorBrush(Colors.DarkBlue);
		public static SolidColorBrush DarkCyan => new SolidColorBrush(Colors.DarkCyan);
		public static SolidColorBrush DarkGoldenrod => new SolidColorBrush(Colors.DarkGoldenrod);
		public static SolidColorBrush DarkGray => new SolidColorBrush(Colors.DarkGray);
		public static SolidColorBrush DarkGreen => new SolidColorBrush(Colors.DarkGreen);
		public static SolidColorBrush DarkKhaki => new SolidColorBrush(Colors.DarkKhaki);
		public static SolidColorBrush DarkMagenta => new SolidColorBrush(Colors.DarkMagenta);
		public static SolidColorBrush DarkOliveGreen => new SolidColorBrush(Colors.DarkOliveGreen);
		public static SolidColorBrush DarkOrange => new SolidColorBrush(Colors.DarkOrange);
		public static SolidColorBrush DarkOrchid => new SolidColorBrush(Colors.DarkOrchid);
		public static SolidColorBrush DarkRed => new SolidColorBrush(Colors.DarkRed);
		public static SolidColorBrush DarkSalmon => new SolidColorBrush(Colors.DarkSalmon);
		public static SolidColorBrush DarkSeaGreen => new SolidColorBrush(Colors.DarkSeaGreen);
		public static SolidColorBrush DarkSlateBlue => new SolidColorBrush(Colors.DarkSlateBlue);
		public static SolidColorBrush DarkSlateGray => new SolidColorBrush(Colors.DarkSlateGray);
		public static SolidColorBrush DarkTurquoise => new SolidColorBrush(Colors.DarkTurquoise);
		public static SolidColorBrush DarkViolet => new SolidColorBrush(Colors.DarkViolet);
		public static SolidColorBrush DeepPink => new SolidColorBrush(Colors.DeepPink);
		public static SolidColorBrush DeepSkyBlue => new SolidColorBrush(Colors.DeepSkyBlue);
		public static SolidColorBrush DimGray => new SolidColorBrush(Colors.DimGray);
		public static SolidColorBrush DodgerBlue => new SolidColorBrush(Colors.DodgerBlue);
		public static SolidColorBrush Firebrick => new SolidColorBrush(Colors.Firebrick);
		public static SolidColorBrush FloralWhite => new SolidColorBrush(Colors.FloralWhite);
		public static SolidColorBrush ForestGreen => new SolidColorBrush(Colors.ForestGreen);
		public static SolidColorBrush Fuchsia => new SolidColorBrush(Colors.Fuchsia);
		public static SolidColorBrush Gainsboro => new SolidColorBrush(Colors.Gainsboro);
		public static SolidColorBrush GhostWhite => new SolidColorBrush(Colors.GhostWhite);
		public static SolidColorBrush Gold => new SolidColorBrush(Colors.Gold);
		public static SolidColorBrush Goldenrod => new SolidColorBrush(Colors.Goldenrod);
		public static SolidColorBrush Gray => new SolidColorBrush(Colors.Gray);
		public static SolidColorBrush Green => new SolidColorBrush(Colors.Green);
		public static SolidColorBrush GreenYellow => new SolidColorBrush(Colors.GreenYellow);
		public static SolidColorBrush Honeydew => new SolidColorBrush(Colors.Honeydew);
		public static SolidColorBrush HotPink => new SolidColorBrush(Colors.HotPink);
		public static SolidColorBrush IndianRed => new SolidColorBrush(Colors.IndianRed);
		public static SolidColorBrush Indigo => new SolidColorBrush(Colors.Indigo);
		public static SolidColorBrush Ivory => new SolidColorBrush(Colors.Ivory);
		public static SolidColorBrush Khaki => new SolidColorBrush(Colors.Khaki);
		public static SolidColorBrush Lavender => new SolidColorBrush(Colors.Lavender);
		public static SolidColorBrush LavenderBlush => new SolidColorBrush(Colors.LavenderBlush);
		public static SolidColorBrush LawnGreen => new SolidColorBrush(Colors.LawnGreen);
		public static SolidColorBrush LemonChiffon => new SolidColorBrush(Colors.LemonChiffon);
		public static SolidColorBrush LightBlue => new SolidColorBrush(Colors.LightBlue);
		public static SolidColorBrush LightCoral => new SolidColorBrush(Colors.LightCoral);
		public static SolidColorBrush LightCyan => new SolidColorBrush(Colors.LightCyan);
		public static SolidColorBrush LightGoldenrodYellow => new SolidColorBrush(Colors.LightGoldenrodYellow);
		public static SolidColorBrush LightGray => new SolidColorBrush(Colors.LightGray);
		public static SolidColorBrush LightGreen => new SolidColorBrush(Colors.LightGreen);
		public static SolidColorBrush LightPink => new SolidColorBrush(Colors.LightPink);
		public static SolidColorBrush LightSalmon => new SolidColorBrush(Colors.LightSalmon);
		public static SolidColorBrush LightSeaGreen => new SolidColorBrush(Colors.LightSeaGreen);
		public static SolidColorBrush LightSkyBlue => new SolidColorBrush(Colors.LightSkyBlue);
		public static SolidColorBrush LightSlateGray => new SolidColorBrush(Colors.LightSlateGray);
		public static SolidColorBrush LightSteelBlue => new SolidColorBrush(Colors.LightSteelBlue);
		public static SolidColorBrush LightYellow => new SolidColorBrush(Colors.LightYellow);
		public static SolidColorBrush Lime => new SolidColorBrush(Colors.Lime);
		public static SolidColorBrush LimeGreen => new SolidColorBrush(Colors.LimeGreen);
		public static SolidColorBrush Linen => new SolidColorBrush(Colors.Linen);
		public static SolidColorBrush Magenta => new SolidColorBrush(Colors.Magenta);
		public static SolidColorBrush Maroon => new SolidColorBrush(Colors.Maroon);
		public static SolidColorBrush MediumAquamarine => new SolidColorBrush(Colors.MediumAquamarine);
		public static SolidColorBrush MediumBlue => new SolidColorBrush(Colors.MediumBlue);
		public static SolidColorBrush MediumOrchid => new SolidColorBrush(Colors.MediumOrchid);
		public static SolidColorBrush MediumPurple => new SolidColorBrush(Colors.MediumPurple);
		public static SolidColorBrush MediumSeaGreen => new SolidColorBrush(Colors.MediumSeaGreen);
		public static SolidColorBrush MediumSlateBlue => new SolidColorBrush(Colors.MediumSlateBlue);
		public static SolidColorBrush MediumSpringGreen => new SolidColorBrush(Colors.MediumSpringGreen);
		public static SolidColorBrush MediumTurquoise => new SolidColorBrush(Colors.MediumTurquoise);
		public static SolidColorBrush MediumVioletRed => new SolidColorBrush(Colors.MediumVioletRed);
		public static SolidColorBrush MidnightBlue => new SolidColorBrush(Colors.MidnightBlue);
		public static SolidColorBrush MintCream => new SolidColorBrush(Colors.MintCream);
		public static SolidColorBrush MistyRose => new SolidColorBrush(Colors.MistyRose);
		public static SolidColorBrush Moccasin => new SolidColorBrush(Colors.Moccasin);
		public static SolidColorBrush NavajoWhite => new SolidColorBrush(Colors.NavajoWhite);
		public static SolidColorBrush Navy => new SolidColorBrush(Colors.Navy);
		public static SolidColorBrush OldLace => new SolidColorBrush(Colors.OldLace);
		public static SolidColorBrush Olive => new SolidColorBrush(Colors.Olive);
		public static SolidColorBrush OliveDrab => new SolidColorBrush(Colors.OliveDrab);
		public static SolidColorBrush Orange => new SolidColorBrush(Colors.Orange);
		public static SolidColorBrush OrangeRed => new SolidColorBrush(Colors.OrangeRed);
		public static SolidColorBrush Orchid => new SolidColorBrush(Colors.Orchid);
		public static SolidColorBrush PaleGoldenrod => new SolidColorBrush(Colors.PaleGoldenrod);
		public static SolidColorBrush PaleGreen => new SolidColorBrush(Colors.PaleGreen);
		public static SolidColorBrush PaleTurquoise => new SolidColorBrush(Colors.PaleTurquoise);
		public static SolidColorBrush PaleVioletRed => new SolidColorBrush(Colors.PaleVioletRed);
		public static SolidColorBrush PapayaWhip => new SolidColorBrush(Colors.PapayaWhip);
		public static SolidColorBrush PeachPuff => new SolidColorBrush(Colors.PeachPuff);
		public static SolidColorBrush Peru => new SolidColorBrush(Colors.Peru);
		public static SolidColorBrush Pink => new SolidColorBrush(Colors.Pink);
		public static SolidColorBrush Plum => new SolidColorBrush(Colors.Plum);
		public static SolidColorBrush PowderBlue => new SolidColorBrush(Colors.PowderBlue);
		public static SolidColorBrush Purple => new SolidColorBrush(Colors.Purple);
		public static SolidColorBrush Red => new SolidColorBrush(Colors.Red);
		public static SolidColorBrush RosyBrown => new SolidColorBrush(Colors.RosyBrown);
		public static SolidColorBrush RoyalBlue => new SolidColorBrush(Colors.RoyalBlue);
		public static SolidColorBrush SaddleBrown => new SolidColorBrush(Colors.SaddleBrown);
		public static SolidColorBrush Salmon => new SolidColorBrush(Colors.Salmon);
		public static SolidColorBrush SandyBrown => new SolidColorBrush(Colors.SandyBrown);
		public static SolidColorBrush SeaGreen => new SolidColorBrush(Colors.SeaGreen);
		public static SolidColorBrush SeaShell => new SolidColorBrush(Colors.SeaShell);
		public static SolidColorBrush Sienna => new SolidColorBrush(Colors.Sienna);
		public static SolidColorBrush Silver => new SolidColorBrush(Colors.Silver);
		public static SolidColorBrush SkyBlue => new SolidColorBrush(Colors.SkyBlue);
		public static SolidColorBrush SlateBlue => new SolidColorBrush(Colors.SlateBlue);
		public static SolidColorBrush SlateGray => new SolidColorBrush(Colors.SlateGray);
		public static SolidColorBrush Snow => new SolidColorBrush(Colors.Snow);
		public static SolidColorBrush SpringGreen => new SolidColorBrush(Colors.SpringGreen);
		public static SolidColorBrush SteelBlue => new SolidColorBrush(Colors.SteelBlue);
		public static SolidColorBrush Tan => new SolidColorBrush(Colors.Tan);
		public static SolidColorBrush Teal => new SolidColorBrush(Colors.Teal);
		public static SolidColorBrush Thistle => new SolidColorBrush(Colors.Thistle);
		public static SolidColorBrush Tomato => new SolidColorBrush(Colors.Tomato);
		public static SolidColorBrush Turquoise => new SolidColorBrush(Colors.Turquoise);
		public static SolidColorBrush Violet => new SolidColorBrush(Colors.Violet);
		public static SolidColorBrush Wheat => new SolidColorBrush(Colors.Wheat);
		public static SolidColorBrush White => new SolidColorBrush(Colors.White);
		public static SolidColorBrush WhiteSmoke => new SolidColorBrush(Colors.WhiteSmoke);
		public static SolidColorBrush Yellow => new SolidColorBrush(Colors.Yellow);
		public static SolidColorBrush YellowGreen => new SolidColorBrush(Colors.YellowGreen);
	}
}
