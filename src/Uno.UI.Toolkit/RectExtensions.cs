using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Uno.UI.Toolkit.Extensions
{
	internal static class RectExtensions
	{
#if !HAS_UNO
		/// <summary>
		/// Gets the center of the rectangle.
		/// </summary>
		/// <param name="rect">A rectangle.</param>
		/// <returns>The center of the rectangle.</returns>
		public static Point GetCenter(this Rect rect)
			=> new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);

		/// <summary>
		/// Gets the center of the rectangle.
		/// </summary>
		/// <param name="rect">A rectangle.</param>
		/// <returns>The center of the rectangle.</returns>
		public static Point GetLocation(this Rect rect)
			=> new(rect.X, rect.Y);

		/// <summary>
		/// Returns the orientation of the rectangle.
		/// </summary>
		/// <param name="rect">A rectangle.</param>
		/// <returns>Portrait, Landscape, or None (if the rectangle has an exact 1:1 ratio)</returns>
		public static DisplayOrientations GetOrientation(this Rect rect)
		{
			if (rect.Height > rect.Width)
			{
				return DisplayOrientations.Portrait;
			}
			else if (rect.Width > rect.Height)
			{
				return DisplayOrientations.Landscape;
			}
			else
			{
				return DisplayOrientations.None;
			}
		}
#endif
	}
}
