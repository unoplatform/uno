using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Xaml;

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

		internal static Rect InflateBy(this Rect left, Thickness right)
		{
			var newWidth = right.Left + left.Width + right.Right;
			var newHeight = right.Top + left.Height + right.Bottom;

			// The origin is always following the left/top
			var newX = left.X - right.Left;
			var newY = left.Y - right.Top;

			return new Rect(newX, newY, Math.Max(newWidth, 0d), Math.Max(newHeight, 0d));
		}

		internal static Rect DeflateBy(this Rect left, Thickness right)
			=> left.InflateBy(new Thickness(-right.Left, -right.Top, -right.Right, -right.Bottom));
#endif
	}
}
