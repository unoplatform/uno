using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Extensions
{
	internal static class RectExtensions
	{
		public static Point GetCenter(this Rect rect) => new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
	}
}
