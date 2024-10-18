using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class QuadraticBezierSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#quadratic_b%C3%A9zier_curve
			yield return $"Q";
			yield return Point1.X;
			yield return Point1.Y;
			yield return Point2.X;
			yield return Point2.Y;
		}
	}
}
