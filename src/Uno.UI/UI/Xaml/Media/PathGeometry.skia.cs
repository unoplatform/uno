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
				path.MoveTo((float)figure.StartPoint.X, (float)figure.StartPoint.Y);

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						path.LineTo((float)lineSegment.Point.X, (float)lineSegment.Point.Y);
					}
					else if (segment is BezierSegment bezierSegment)
					{
						path.CubicTo(
							 (float)bezierSegment.Point1.X, (float)bezierSegment.Point1.Y,
							 (float)bezierSegment.Point2.X, (float)bezierSegment.Point2.Y,
							 (float)bezierSegment.Point3.X, (float)bezierSegment.Point3.Y);
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						path.QuadTo(
							 (float)quadraticBezierSegment.Point1.X, (float)quadraticBezierSegment.Point1.Y,
							 (float)quadraticBezierSegment.Point2.X, (float)quadraticBezierSegment.Point2.Y);
					}
					else if (segment is ArcSegment arcSegment)
					{
						path.ArcTo(
							 (float)arcSegment.Size.Width, (float)arcSegment.Size.Height,
							 (float)arcSegment.RotationAngle,
							 arcSegment.IsLargeArc ? SkiaSharp.SKPathArcSize.Large : SkiaSharp.SKPathArcSize.Small,
							 (arcSegment.SweepDirection == SweepDirection.Clockwise ? SkiaSharp.SKPathDirection.Clockwise : SkiaSharp.SKPathDirection.CounterClockwise),
							 (float)arcSegment.Point.X, (float)arcSegment.Point.Y);
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
