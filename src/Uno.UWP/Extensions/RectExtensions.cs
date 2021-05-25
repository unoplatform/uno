using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Uno.Extensions
{
	public static class RectExtensions
	{
		/// <summary>
		/// Creates a transformed <see cref="Rect"/> using a <see cref="Matrix3x2"/>.
		/// </summary>
		/// <param name="rect">The rectangle to transform</param>
		/// <param name="m">The matrix to use to transform the <paramref name="rect"/></param>
		/// <returns>A new rectangle</returns>
		internal static Rect Transform(this Rect rect, Matrix3x2 m)
			=> Matrix3x2Extensions.Transform(m, rect);

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

		internal static Rect OffsetRect(this Rect rect, double dx, double dy)
		{
			rect.X += dx;
			rect.Y += dy;

			return rect;
		}

		internal static Rect OffsetRect(this Rect rect, Point offset) => rect.OffsetRect(offset.X, offset.Y);

		internal static bool IsIntersecting(this Rect rect, Rect other)
		{
			rect.Intersect(other);
			return !rect.IsEmpty;
		}
	}
}
