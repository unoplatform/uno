using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class BezierSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#cubic_b%C3%A9zier_curve
			yield return $"C";
			var point1 = Point1;
			yield return point1.X;
			yield return point1.Y;
			var point2 = Point2;
			yield return point2.X;
			yield return point2.Y;
			var point3 = Point3;
			yield return point3.X;
			yield return point3.Y;
		}
	}
}
