using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class PolyQuadraticBezierSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#quadratic_b%C3%A9zier_curve
			var points = Points;

			if (points.Count % 2 != 0)
			{
				throw new InvalidOperationException("PolyQuadraticBezierSegment points must use pair points.");
			}

			yield return $"Q";

			for (var i = 0; i < points.Count; i++)
			{
				yield return points[i].X;
				yield return points[i].Y;
			}
		}
	}
}
