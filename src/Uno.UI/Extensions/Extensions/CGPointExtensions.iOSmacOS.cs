using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Extensions
{
	public static class CGPointExtensions
	{
		/// <summary>
		/// Clamp CGPoint between min and max values
		/// </summary>
		public static CGPoint Clamp(this CGPoint unclampedSize, CGPoint min, CGPoint max)
		{
			return new CGPoint(
				NMath.Min(max.X, NMath.Max(unclampedSize.X, min.X)),
				NMath.Min(max.Y, NMath.Max(unclampedSize.Y, min.Y))
			);
		}

		/// <summary>
		/// Index-based access to X or Y value.
		/// </summary>
		/// <param name="point">A CGPoint</param>
		/// <param name="axisIndex">A coordinate index</param>
		/// <returns>CGPoint.X if <paramref name="axisIndex"/> =0, CGPoint.Y if <paramref name="axisIndex"/> =1</returns>
		public static nfloat GetXOrY(this CGPoint point, int axisIndex)
		{
			if (axisIndex == 0)
			{
				return point.X;
			}
			if (axisIndex == 1)
			{
				return point.Y;
			}

			throw new ArgumentOutOfRangeException(nameof(axisIndex));
		}
	}
}
