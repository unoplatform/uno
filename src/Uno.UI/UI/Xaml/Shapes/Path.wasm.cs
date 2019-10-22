using System;
using System.Collections.Generic;
using System.Globalization;
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

		partial void OnDataChanged()
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
			List<string> pathlist = new List<string>();

			foreach (PathFigure figure in geometry.Figures)
			{
				pathlist.Add("M " + figure.StartPoint.X + "," + figure.StartPoint.Y);

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						pathlist.Add("L " + lineSegment.Point.X + "," + lineSegment.Point.Y);
					}
					else if (segment is BezierSegment bezierSegment)
					{
						pathlist.Add(
							"C " +
							 bezierSegment.Point1.X + "," + bezierSegment.Point1.Y + " " +
							 bezierSegment.Point2.X + "," + bezierSegment.Point2.Y + " " +
							 bezierSegment.Point3.X + "," + bezierSegment.Point3.Y);
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						pathlist.Add(
							 "Q " +
							 quadraticBezierSegment.Point1.X + "," + quadraticBezierSegment.Point1.Y + " " +
							 quadraticBezierSegment.Point2.X + "," + quadraticBezierSegment.Point2.Y);
					}
					else if (segment is ArcSegment arcSegment)
					{
						pathlist.Add(
							 "A " +
							 arcSegment.Size.Width + " " + arcSegment.Size.Height + " " +
							 arcSegment.RotationAngle + " " +
							 (arcSegment.IsLargeArc ? "1" : "0") + " " +
							 (arcSegment.SweepDirection == SweepDirection.Clockwise ? "1" : "0") + " " +
							 arcSegment.Point.X + "," + arcSegment.Point.Y);
					}
				}

				if (figure.IsClosed)
					pathlist.Add("Z");
			}
			return FormattableString.Invariant($"{string.Join(" ", pathlist.ToArray())}");
		}
	}
}
