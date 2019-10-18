using System;
using System.Globalization;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;

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

		private string ToStreamGeometry(PathGeometry geometry)
		{
			StringBuilder sb = new StringBuilder();

			foreach (PathFigure figure in geometry.Figures)
			{
				sb.Append("M " + figure.StartPoint.X + " " + figure.StartPoint.Y + " ");

				foreach (PathSegment segment in figure.Segments)
				{

					if (segment.GetType() == typeof(LineSegment))
					{
						LineSegment _lineSegment = segment as LineSegment;

						sb.Append("L " + figure.StartPoint.X + " " + figure.StartPoint.Y + " ");
					}
					else if (segment.GetType() == typeof(BezierSegment))
					{
						BezierSegment _bezierSegment = segment as BezierSegment;

						sb.Append(
							 "C " +
							 _bezierSegment.Point1.X + " " + _bezierSegment.Point1.Y + " " +
							 _bezierSegment.Point2.X + " " + _bezierSegment.Point2.Y + " " +
							 _bezierSegment.Point3.X + " " + _bezierSegment.Point3.Y + " "
							 );
					}
					else if (segment.GetType() == typeof(QuadraticBezierSegment))
					{
						QuadraticBezierSegment _quadraticBezierSegment = segment as QuadraticBezierSegment;

						sb.Append(
							 "Q " +
							 _quadraticBezierSegment.Point1.X + " " + _quadraticBezierSegment.Point1.Y + " " +
							 _quadraticBezierSegment.Point2.X + " " + _quadraticBezierSegment.Point2.Y + " "
							 );
					}
					else if (segment.GetType() == typeof(ArcSegment))
					{
						ArcSegment _arcSegment = segment as ArcSegment;

						sb.Append(
							 "A " +
							 _arcSegment.Size.Width + " " + _arcSegment.Size.Height + " " +
							 _arcSegment.RotationAngle + " " +
							 (_arcSegment.IsLargeArc ? "1" : "0") + " " +
							 (_arcSegment.SweepDirection == SweepDirection.Clockwise ? "1" : "0") + " " + 
							 _arcSegment.Point.X + " " + _arcSegment.Point.Y + " "
							 );
					}
				}

				if (figure.IsClosed)
					sb.Append("Z");
			}
			return sb.ToString();
		}
	}
}
