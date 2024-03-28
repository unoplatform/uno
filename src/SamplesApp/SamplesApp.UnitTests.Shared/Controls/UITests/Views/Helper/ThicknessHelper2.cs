using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Samples.Helper
{
	class ThicknessHelper2
	{
		public static Thickness FromLengths(double left, double top, double right, double bottom) =>
			new Thickness(left, top, right, bottom);

		public static Thickness FromUniformLength(double uniformLength) =>
			new Thickness(uniformLength);
	}
}
