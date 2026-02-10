using SkiaSharp;
using Uno.UI.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media
{
	partial class PathGeometry
	{
		internal override SKPath GetSKPath() => GetSKPath(false);

		internal override SKPath GetFilledSKPath() => GetSKPath(true);

		private SKPath GetSKPath(bool skipUnfilled)
		{
			var path = new SKPath();

			foreach (PathFigure figure in Figures)
			{
				if (skipUnfilled && !figure.IsFilled)
				{
					continue;
				}

				path.MoveTo((float)figure.StartPoint.X, (float)figure.StartPoint.Y);

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						path.LineTo((float)lineSegment.Point.X, (float)lineSegment.Point.Y);
					}
					else if (segment is PolyLineSegment polyLineSegment)
					{
						foreach (var point in polyLineSegment.Points)
						{
							path.LineTo((float)point.X, (float)point.Y);
						}
					}
					else if (segment is BezierSegment bezierSegment)
					{
						path.CubicTo(
							 (float)bezierSegment.Point1.X, (float)bezierSegment.Point1.Y,
							 (float)bezierSegment.Point2.X, (float)bezierSegment.Point2.Y,
							 (float)bezierSegment.Point3.X, (float)bezierSegment.Point3.Y);
					}
					else if (segment is PolyBezierSegment polyBezierSegment)
					{
						for (var i = 0; i < polyBezierSegment.Points.Count; i += 3)
						{
							path.CubicTo(
								 (float)polyBezierSegment.Points[i].X, (float)polyBezierSegment.Points[i].Y,
								 (float)polyBezierSegment.Points[i + 1].X, (float)polyBezierSegment.Points[i + 1].Y,
								 (float)polyBezierSegment.Points[i + 2].X, (float)polyBezierSegment.Points[i + 2].Y);
						}
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						path.QuadTo(
							 (float)quadraticBezierSegment.Point1.X, (float)quadraticBezierSegment.Point1.Y,
							 (float)quadraticBezierSegment.Point2.X, (float)quadraticBezierSegment.Point2.Y);
					}
					else if (segment is PolyQuadraticBezierSegment polyQuadraticBezierSegment)
					{
						for (var i = 0; i < polyQuadraticBezierSegment.Points.Count; i += 2)
						{
							path.QuadTo(
								 (float)polyQuadraticBezierSegment.Points[i].X, (float)polyQuadraticBezierSegment.Points[i].Y,
								 (float)polyQuadraticBezierSegment.Points[i + 1].X, (float)polyQuadraticBezierSegment.Points[i + 1].Y);
						}
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
