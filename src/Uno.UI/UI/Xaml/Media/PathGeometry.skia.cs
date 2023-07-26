using SkiaSharp;
using Uno.UI.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
	partial class PathGeometry
	{
		internal override SKPath GetSKPath()
		{
			var path = new SKPath();

			foreach (PathFigure figure in Figures)
			{
				var startPoint = figure.StartPoint;
				path.MoveTo((float)startPoint.X, (float)startPoint.Y);

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						var point = lineSegment.Point;
						path.LineTo((float)point.X, (float)point.Y);
					}
					else if (segment is BezierSegment bezierSegment)
					{
						var point1 = bezierSegment.Point1;
						var point2 = bezierSegment.Point2;
						var point3 = bezierSegment.Point3;
						path.CubicTo(
							 (float)point1.X, (float)point1.Y,
							 (float)point2.X, (float)point2.Y,
							 (float)point3.X, (float)point3.Y);
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						var point1 = quadraticBezierSegment.Point1;
						var point2 = quadraticBezierSegment.Point2;
						path.QuadTo(
							 (float)point1.X, (float)point1.Y,
							 (float)point2.X, (float)point2.Y);
					}
					else if (segment is ArcSegment arcSegment)
					{
						var size = arcSegment.Size;
						var point = arcSegment.Point;
						path.ArcTo(
							 (float)size.Width, (float)size.Height,
							 (float)arcSegment.RotationAngle,
							 arcSegment.IsLargeArc ? SkiaSharp.SKPathArcSize.Large : SkiaSharp.SKPathArcSize.Small,
							 (arcSegment.SweepDirection == SweepDirection.Clockwise ? SkiaSharp.SKPathDirection.Clockwise : SkiaSharp.SKPathDirection.CounterClockwise),
							 (float)point.X, (float)point.Y);
					}
				}

				if (figure.IsClosed)
				{
					path.Close();
				}
			}

			path.FillType = FillRule.ToSkiaFillType();

			return path;
		}
	}
}
