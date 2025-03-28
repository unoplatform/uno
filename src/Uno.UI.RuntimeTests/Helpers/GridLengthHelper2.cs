using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	class GridLengthHelper2
	{
		public static GridLength Auto =>
			GridLength.Auto;

		internal static GridLength FromValueAndType(double value, GridUnitType pixel) =>
			new GridLength(value, pixel);

		internal static bool GetIsStar(GridLength sizeHint) =>
			sizeHint.IsStar;

		internal static bool GetIsAbsolute(GridLength sizeHint) =>
			sizeHint.IsAbsolute;

		internal static bool GetIsAuto(GridLength sizeHint) =>
			sizeHint.IsAuto;
	}
}
