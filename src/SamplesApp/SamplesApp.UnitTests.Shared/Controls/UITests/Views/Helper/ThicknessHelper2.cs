using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.Samples.Helper
{
	class ThicknessHelper2
	{
		public static Thickness FromLengths(double left, double top, double right, double bottom) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			new Thickness(left, top, right, bottom);
#else
			ThicknessHelper.FromLengths(left, top, right, bottom);
#endif

		public static Thickness FromUniformLength(double uniformLength) =>
#if NETFX_CORE && !HAS_UNO_WINUI
			new Thickness(uniformLength);
#else
			ThicknessHelper.FromUniformLength(uniformLength);
#endif
	}
}
