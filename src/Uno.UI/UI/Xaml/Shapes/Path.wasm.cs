using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using static System.FormattableString;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		private readonly SvgElement _path = new SvgElement("path");

		public Path()
		{
			SvgChildren.Add(_path);

			InitCommonShapeProperties();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _path;
		}

		partial void OnDataChanged() => InvalidateMeasure();

		protected override void InvalidateShape()
		{
			switch (Data)
			{
				case GeometryData gd:
					_path.SetAttribute(
						("d", gd.Data),
						("fill-rule", gd.FillRule == FillRule.EvenOdd ? "evenodd" : "nonzero"));
					break;
				case PathGeometry pg:
					_path.SetAttribute(("d", ToStreamGeometry(pg)));
					break;
				case null:
					_path.RemoveAttribute("d");
					break;
			}
		}

		/// <summary>
		/// Transform the figures collection into a SVG Path according to :
		/// https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/d
		/// </summary>
		/// <param name="geometry"></param>
		/// <returns></returns>
		private string ToStreamGeometry(PathGeometry geometry)
		{
			IEnumerable<IFormattable> GenerateDataParts()
			{
				foreach (var figure in geometry.Figures)
				{
					yield return $"M {figure.StartPoint.X},{figure.StartPoint.Y}";

					foreach (var segment in figure.Segments)
					{
						yield return segment switch
						{
							LineSegment lineSegment =>
								$"L {lineSegment.Point.X},{lineSegment.Point.Y}",
							BezierSegment bezierSegment =>
								$"C {bezierSegment.Point1.X},{bezierSegment.Point1.Y} {bezierSegment.Point2.X},{bezierSegment.Point2.Y} {bezierSegment.Point3.X},{bezierSegment.Point3.Y}",
							QuadraticBezierSegment quadraticBezierSegment =>
								$"Q {quadraticBezierSegment.Point1.X},{quadraticBezierSegment.Point1.Y} {quadraticBezierSegment.Point2.X},{quadraticBezierSegment.Point2.Y}",
							ArcSegment arcSegment =>
								$"A {arcSegment.Size.Width} {arcSegment.Size.Height} {arcSegment.RotationAngle} {(arcSegment.IsLargeArc ? "1" : "0")} {(arcSegment.SweepDirection == SweepDirection.Clockwise ? "1" : "0")} {arcSegment.Point.X},{arcSegment.Point.Y}",
							_ => $""
						};
					}

					if (figure.IsClosed)
					{
						yield return $"Z";
					}
				}
			}

			return string.Join(" ", GenerateDataParts().Select(p=>p.ToString(null, CultureInfo.InvariantCulture)));
		}
	}
}
