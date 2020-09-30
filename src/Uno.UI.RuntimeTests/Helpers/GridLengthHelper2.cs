using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	class GridLengthHelper2
	{
		public static GridLength Auto =>
#if NETFX_CORE && !HAS_UNO_WINUI
			GridLength.Auto;
#else
			GridLengthHelper.Auto;
#endif

		internal static GridLength FromValueAndType(double value, GridUnitType pixel) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			new GridLength(value, pixel);
#else
			GridLengthHelper.FromValueAndType(value, pixel);
#endif

		internal static bool GetIsStar(GridLength sizeHint) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			sizeHint.IsStar;
#else
			GridLengthHelper.GetIsStar(sizeHint);
#endif

		internal static bool GetIsAbsolute(GridLength sizeHint) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			sizeHint.IsAbsolute;
#else
			GridLengthHelper.GetIsAbsolute(sizeHint);
#endif

		internal static bool GetIsAuto(GridLength sizeHint) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			sizeHint.IsAuto;
#else
			GridLengthHelper.GetIsAuto(sizeHint);
#endif
	}
}
