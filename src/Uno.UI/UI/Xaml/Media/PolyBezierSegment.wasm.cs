using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class PolyBezierSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#cubic_b%C3%A9zier_curve
			var points = Points;

			if (points.Count % 3 != 0)
			{
				throw new InvalidOperationException("PolyBezierSegment points must use triplet points.");
			}

			yield return $"C";

			for (var i = 0; i < points.Count; i++)
			{
				yield return points[i].X;
				yield return points[i].Y;
			}
		}
	}
}
