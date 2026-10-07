using System;
using System.Runtime.InteropServices;

namespace Microsoft.UI.Xaml
{
	public sealed partial class GridLengthHelper
	{
		public static GridLength Auto { get; } = new GridLength(1.0, GridUnitType.Auto);

		internal static GridLength OneStar { get; } = new GridLength(1.0, GridUnitType.Star);

		public static GridLength FromPixels(double pixels) => FromValueAndType(pixels, GridUnitType.Pixel);

		public static GridLength FromValueAndType(double value, GridUnitType type) => new GridLength(value, type);

		public static bool GetIsAbsolute(GridLength target) => target.IsAbsolute;

		public static bool GetIsAuto(GridLength target) => target.IsAuto;

		public static bool GetIsStar(GridLength target) => target.IsStar;

		public static bool Equals(GridLength target, GridLength value) => target.Equals(value);
	}
}
