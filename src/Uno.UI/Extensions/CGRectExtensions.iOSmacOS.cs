using CoreGraphics;
using System;
using System.Drawing;
using ObjCRuntime;

namespace Uno.UI.Extensions
{
	public static class CGRectExtensions
	{
		public static CGRect IncrementX(this CGRect thisCGRect, nfloat delta)
		{
			thisCGRect.X += delta;
			return thisCGRect;
		}

		public static CGRect IncrementY(this CGRect thisCGRect, nfloat delta)
		{
			thisCGRect.Y += delta;
			return thisCGRect;
		}

		public static CGRect SetX(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.X = value;
			return thisCGRect;
		}

		public static CGRect SetY(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Y = value;
			return thisCGRect;
		}

		public static CGRect SetBottom(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Height = value - thisCGRect.Y;
			return thisCGRect;
		}

		public static CGRect SetRight(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Width = value - thisCGRect.X;
			return thisCGRect;
		}

		public static CGRect SetRightRespectWidth(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.X = value - thisCGRect.Width;
			return thisCGRect;
		}

		public static CGRect SetHorizontalCenter(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.X = value - (thisCGRect.Width / 2);
			return thisCGRect;
		}

		public static CGRect SetVerticalCenter(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Y = value - (thisCGRect.Height / 2);
			return thisCGRect;
		}

		public static CGRect Shrink(this CGRect thisCGRect, nfloat numberOfPixels)
		{
			return Shrink(thisCGRect, numberOfPixels, numberOfPixels, numberOfPixels, numberOfPixels);
		}

		public static CGRect Shrink(this CGRect rect, Windows.UI.Xaml.Thickness thickness)
		{
			rect.X += (nfloat)thickness.Left;
			rect.Y += (nfloat)thickness.Top;
			rect.Width -= (nfloat)(thickness.Left + thickness.Right);
			rect.Height -= (nfloat)(thickness.Top + thickness.Bottom);

			return rect;
		}

		public static CGRect Shrink(this CGRect thisCGRect, nfloat left, nfloat top, nfloat right, nfloat bottom)
		{
			thisCGRect.X += left;
			thisCGRect.Y += top;
			thisCGRect.Width -= (left + right);
			thisCGRect.Height -= (top + bottom);
			return thisCGRect;
		}

		public static CGRect IncrementHeight(this CGRect thisCGRect, nfloat delta)
		{
			thisCGRect.Height += delta;
			return thisCGRect;
		}

		public static CGRect IncrementWidth(this CGRect thisCGRect, nfloat delta)
		{

			thisCGRect.Width += delta;
			return thisCGRect;
		}

		public static CGRect SetWidth(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Width = value;
			return thisCGRect;
		}

		public static CGRect SetHeight(this CGRect thisCGRect, nfloat value)
		{
			thisCGRect.Height = value;
			return thisCGRect;
		}

		public static CGRect Copy(this CGRect thisCGRect)
		{
			return new CGRect(thisCGRect.X, thisCGRect.Y, thisCGRect.Width, thisCGRect.Height);
		}

		public static CGRect ResetPosition(this CGRect thisCGRect)
		{
			return new CGRect(0, 0, thisCGRect.Width, thisCGRect.Height);
		}

		public static bool AreSizesDifferent(this CGRect cgRectOne, CGRect cgRectTwo)
		{
			return NMath.Abs(cgRectOne.Width - cgRectTwo.Width) > nfloat.Epsilon
				|| NMath.Abs(cgRectOne.Height - cgRectTwo.Height) > nfloat.Epsilon;
		}

		/// <summary>
		/// Index-based access to X or Y value.
		/// </summary>
		/// <param name="rect">A CGRect</param>
		/// <param name="axisIndex">A coordinate axis index</param>
		/// <returns>CGRect.X if <paramref name="axisIndex"/> =0, CGRect.Y if <paramref name="axisIndex"/> =1</returns>
		public static nfloat GetXOrY(this CGRect rect, int axisIndex)
		{
			if (axisIndex == 0)
			{
				return rect.X;
			}
			if (axisIndex == 1)
			{
				return rect.Y;
			}

			throw new ArgumentOutOfRangeException(nameof(axisIndex));
		}

		/// <summary>
		/// Index-based access to Width and Height.
		/// </summary>
		/// <param name="rect">A CGRect</param>
		/// <param name="axisIndex">A coordinate axis index</param>
		/// <returns>CGRect.Width if <paramref name="axisIndex"/> =0, CGRect.Height if <paramref name="axisIndex"/> =1</returns>
		public static nfloat GetWidthOrHeight(this CGRect rect, int axisIndex)
		{
			if (axisIndex == 0)
			{
				return rect.Width;
			}
			if (axisIndex == 1)
			{
				return rect.Height;
			}

			throw new ArgumentOutOfRangeException(nameof(axisIndex));
		}

		/// <summary>
		/// Index-based creation of a CGRect with modified X or Y
		/// </summary>
		/// <param name="rect">A CGRect</param>
		/// <param name="axisIndex">A coordinate axis index</param>
		/// <param name="newXYValue">The new value for X or Y</param>
		/// <returns>A CGRect with modified X if <paramref name="axisIndex"/> =0, or modified Y if <paramref name="axisIndex"/> =1</returns>
		public static CGRect SetXOrY(this CGRect rect, int axisIndex, nfloat newXYValue)
		{
			if (axisIndex == 0)
			{
				rect.X = newXYValue;
			}
			else if (axisIndex == 1)
			{
				rect.Y = newXYValue;
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(axisIndex));
			}
			return rect;
		}
	}
}
