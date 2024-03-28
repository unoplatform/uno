using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	partial class PolyLineSegment
	{
		internal override IEnumerable<IFormattable> ToDataStream()
		{
			// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d#lineto_path_commands

			yield return $"L";

			var points = Points;

			for (var i = 0; i < points.Count; i++)
			{
				yield return points[i].X;
				yield return points[i].Y;
			}
		}
	}
}
