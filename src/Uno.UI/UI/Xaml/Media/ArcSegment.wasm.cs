using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class ArcSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#elliptical_arc_curve

			yield return $"A";
			var size = Size;
			yield return size.Width;
			yield return size.Height;
			yield return RotationAngle;
			yield return IsLargeArc ? (IFormattable)$"1" : $"0";
			yield return SweepDirection == SweepDirection.Clockwise ? (IFormattable)$"1" : $"0";
			var point = Point;
			yield return point.X;
			yield return point.Y;
		}
	}
}
