using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class LineSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#lineto_path_commands
			yield return $"L";
			var point = Point;
			yield return point.X;
			yield return point.Y;
		}
	}
}
