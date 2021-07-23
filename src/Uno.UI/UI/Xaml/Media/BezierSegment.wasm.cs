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
			yield return Point1.X;
			yield return Point1.Y;
			yield return Point2.X;
			yield return Point2.Y;
			yield return Point3.X;
			yield return Point3.Y;
		}
	}
}
