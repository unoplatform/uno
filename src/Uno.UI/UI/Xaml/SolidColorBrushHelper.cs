using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;

#if XAMARIN_IOS_UNIFIED
using GenericColor = UIKit.UIColor;
using UIKit;
#elif XAMARIN_IOS
using GenericColor = MonoTouch.UIKit.UIColor;
using MonoTouch.UIKit;
#elif XAMARIN_ANDROID
using GenericColor = Android.Graphics.Color;
#elif NETFX_CORE
using Windows.UI.Xaml.Media;
using GenericColor = Windows.UI.Color;
using Windows.UI;
#elif __MACOS__
using GenericColor = Windows.UI.Color;
#else
using GenericColor = System.Drawing.Color;
#endif

namespace Windows.UI.Xaml
{
	public static class SolidColorBrushHelper
	{
#if XAMARIN_IOS
		public static SolidColorBrush FromARGB(byte a, byte r, byte g, byte b)
		{
			return new SolidColorBrush(UIColor.FromRGBA(r, g, b, a));
		}
#elif XAMARIN_ANDROID
		public static SolidColorBrush FromARGB(byte a, byte r, byte g, byte b)
		{
			return new SolidColorBrush(Android.Graphics.Color.Argb(a, r, g, b));
		}
#else
		public static SolidColorBrush FromARGB(byte a, byte r, byte g, byte b)
		{
			return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
		}
#endif

		/// <summary>
		/// Takes a color code as an ARGB or RGB string and returns a solid color brush. 
		/// 
		/// Remark: if single digits are used to define the color, they will
		/// be duplicated (example: FFD8 will become FFFFDD88)
		/// </summary>
		public static SolidColorBrush FromARGB(string colorCode)
		{
			return new SolidColorBrush(Colors.Parse(colorCode));
		}

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

		/// <summary>
		/// Takes a standard color name (E.G. White) and returns a solid color brush
		/// </summary>
		public static SolidColorBrush FromName(string colorName)
		{
			// I know, I don't like it either.  Need to check for perf.
			switch (colorName)
			{
				case "Transparent":
					return Transparent;
				case "AliceBlue":
					return AliceBlue;
				case "AntiqueWhite":
					return AntiqueWhite;
				case "Aqua":
					return Aqua;
				case "Aquamarine":
					return Aquamarine;
				case "Azure":
					return Azure;
				case "Beige":
					return Beige;
				case "Bisque":
					return Bisque;
				case "Black":
					return Black;
				case "BlanchedAlmond":
					return BlanchedAlmond;
				case "Blue":
					return Blue;
				case "BlueViolet":
					return BlueViolet;
				case "Brown":
					return Brown;
				case "BurlyWood":
					return BurlyWood;
				case "CadetBlue":
					return CadetBlue;
				case "Chartreuse":
					return Chartreuse;
				case "Chocolate":
					return Chocolate;
				case "Coral":
					return Coral;
				case "CornflowerBlue":
					return CornflowerBlue;
				case "Cornsilk":
					return Cornsilk;
				case "Crimson":
					return Crimson;
				case "Cyan":
					return Cyan;
				case "DarkBlue":
					return DarkBlue;
				case "DarkCyan":
					return DarkCyan;
                case "DarkGoldenrod":
				case "DarkGoldenRod":
					return DarkGoldenrod;
                case "DarkGray":
					return DarkGray;
                case "DarkGreen":
					return DarkGreen;
                case "DarkKhaki":
					return DarkKhaki;
				case "DarkMagenta":
					return DarkMagenta;
				case "DarkOliveGreen":
					return DarkOliveGreen;
				case "DarkOrange":
					return DarkOrange;
				case "DarkOrchid":
					return DarkOrchid;
				case "DarkRed":
					return DarkRed;
				case "DarkSalmon":
					return DarkSalmon;
				case "DarkSeaGreen":
					return DarkSeaGreen;
				case "DarkSlateBlue":
					return DarkSlateBlue;
				case "DarkSlateGray":
					return DarkSlateGray;
				case "DarkTurquoise":
					return DarkTurquoise;
				case "DarkViolet":
					return DarkViolet;
				case "DeepPink":
					return DeepPink;
				case "DeepSkyBlue":
					return DeepSkyBlue;
				case "DimGray":
					return DimGray;
				case "DodgerBlue":
					return DodgerBlue;
				case "Firebrick":
					return Firebrick;
				case "FloralWhite":
					return FloralWhite;
				case "ForestGreen":
					return ForestGreen;
				case "Fuchsia":
					return Fuchsia;
				case "Gainsboro":
					return Gainsboro;
				case "GhostWhite":
					return GhostWhite;
				case "Gold":
					return Gold;
				case "Goldenrod":
				case "GoldenRod":
					return Goldenrod;
				case "Gray":
					return Gray;
				case "Green":
					return Green;
				case "GreenYellow":
					return GreenYellow;
				case "Honeydew":
					return Honeydew;
				case "HotPink":
					return HotPink;
				case "IndianRed":
					return IndianRed;
				case "Indigo":
					return Indigo;
				case "Ivory":
					return Ivory;
				case "Khaki":
					return Khaki;
				case "Lavender":
					return Lavender;
				case "LavenderBlush":
					return LavenderBlush;
				case "LawnGreen":
					return LawnGreen;
				case "LemonChiffon":
					return LemonChiffon;
				case "LightBlue":
					return LightBlue;
				case "LightCoral":
					return LightCoral;
				case "LightCyan":
					return LightCyan;
				case "LightGoldenrodYellow":
				case "LightGoldenRodYellow":
					return LightGoldenrodYellow;
				case "LightGray":
					return LightGray;
				case "LightGreen":
					return LightGreen;
				case "LightPink":
					return LightPink;
				case "LightSalmon":
					return LightSalmon;
				case "LightSeaGreen":
					return LightSeaGreen;
				case "LightSkyBlue":
					return LightSkyBlue;
				case "LightSlateGray":
					return LightSlateGray;
                case "LightSteelBlue":
					return LightSteelBlue;
                case "LightYellow":
					return LightYellow;
                case "Lime":
					return Lime;
				case "LimeGreen":
					return LimeGreen;
				case "Linen":
					return Linen;
				case "Magenta":
					return Magenta;
				case "Maroon":
					return Maroon;
				case "MediumAquamarine":
					return MediumAquamarine;
				case "MediumBlue":
					return MediumBlue;
				case "MediumOrchid":
					return MediumOrchid;
				case "MediumPurple":
					return MediumPurple;
				case "MediumSeaGreen":
					return MediumSeaGreen;
				case "MediumSlateBlue":
					return MediumSlateBlue;
				case "MediumSpringGreen":
					return MediumSpringGreen;
				case "MediumTurquoise":
					return MediumTurquoise;
				case "MediumVioletRed":
					return MediumVioletRed;
				case "MidnightBlue":
					return MidnightBlue;
				case "MintCream":
					return MintCream;
				case "MistyRose":
					return MistyRose;
				case "Moccasin":
					return Moccasin;
				case "NavajoWhite":
					return NavajoWhite;
				case "Navy":
					return Navy;
				case "OldLace":
					return OldLace;
				case "Olive":
					return Olive;
				case "OliveDrab":
					return OliveDrab;
				case "Orange":
					return Orange;
				case "OrangeRed":
					return OrangeRed;
				case "Orchid":
					return Orchid;
				case "PaleGoldenrod":
				case "PaleGoldenRod":
					return PaleGoldenrod;
				case "PaleGreen":
					return PaleGreen;
				case "PaleTurquoise":
					return PaleTurquoise;
				case "PaleVioletRed":
					return PaleVioletRed;
				case "PapayaWhip":
					return PapayaWhip;
				case "PeachPuff":
					return PeachPuff;
				case "Peru":
					return Peru;
				case "Pink":
					return Pink;
				case "Plum":
					return Plum;
				case "PowderBlue":
					return PowderBlue;
				case "Purple":
					return Purple;
				case "Red":
					return Red;
				case "RosyBrown":
					return RosyBrown;
				case "RoyalBlue":
					return RoyalBlue;
				case "SaddleBrown":
					return SaddleBrown;
				case "Salmon":
					return Salmon;
				case "SandyBrown":
					return SandyBrown;
				case "SeaGreen":
					return SeaGreen;
				case "SeaShell":
					return SeaShell;
				case "Sienna":
					return Sienna;
				case "Silver":
					return Silver;
				case "SkyBlue":
					return SkyBlue;
				case "SlateBlue":
					return SlateBlue;
				case "SlateGray":
					return SlateGray;
				case "Snow":
					return Snow;
				case "SpringGreen":
					return SpringGreen;
				case "SteelBlue":
					return SteelBlue;
				case "Tan":
					return Tan;
				case "Teal":
					return Teal;
				case "Thistle":
					return Thistle;
				case "Tomato":
					return Tomato;
				case "Turquoise":
					return Turquoise;
				case "Violet":
					return Violet;
				case "Wheat":
					return Wheat;
				case "White":
					return White;
				case "WhiteSmoke":
					return WhiteSmoke;
				case "Yellow":
					return Yellow;
				case "YellowGreen":
					return YellowGreen;
				default:
					throw new ArgumentOutOfRangeException("There is no default Brush with the name " + colorName);
			};
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
