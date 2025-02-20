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
			yield return Size.Width;
			yield return Size.Height;
			yield return RotationAngle;
			yield return IsLargeArc ? (IFormattable)$"1" : $"0";
			yield return SweepDirection == SweepDirection.Clockwise ? (IFormattable)$"1" : $"0";
			yield return Point.X;
			yield return Point.Y;
		}
	}
}
