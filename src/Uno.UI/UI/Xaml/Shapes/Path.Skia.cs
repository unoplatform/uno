using System;
using Uno.Disposables;
using Uno.Media;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path
	{
		internal override SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			switch (Data)
			{
				case PathGeometry pg:
					return ToGeometrySource2D(pg);
				case StreamGeometry sg:
					return sg.GetGeometrySource2D();
			}

			throw new NotSupportedException($"Geometry {Data} is not supported");
		}

		private SkiaGeometrySource2D ToGeometrySource2D(PathGeometry geometry)
		{
			var skiaGeometry = new SkiaGeometrySource2D();

			foreach (PathFigure figure in geometry.Figures)
			{
				skiaGeometry.Geometry.MoveTo((float)figure.StartPoint.X, (float)figure.StartPoint.Y);

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						skiaGeometry.Geometry.LineTo((float)lineSegment.Point.X, (float)lineSegment.Point.Y);
					}
					else if (segment is BezierSegment bezierSegment)
					{
						skiaGeometry.Geometry.CubicTo(
							 (float)bezierSegment.Point1.X, (float)bezierSegment.Point1.Y,
							 (float)bezierSegment.Point2.X, (float)bezierSegment.Point2.Y,
							 (float)bezierSegment.Point3.X, (float)bezierSegment.Point3.Y);
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						skiaGeometry.Geometry.QuadTo(
							 (float)quadraticBezierSegment.Point1.X, (float)quadraticBezierSegment.Point1.Y,
							 (float)quadraticBezierSegment.Point2.X, (float)quadraticBezierSegment.Point2.Y);
					}
					else if (segment is ArcSegment arcSegment)
					{
						skiaGeometry.Geometry.ArcTo(
							 (float)arcSegment.Size.Width, (float)arcSegment.Size.Height,
							 (float)arcSegment.RotationAngle,
							 arcSegment.IsLargeArc ? SkiaSharp.SKPathArcSize.Large : SkiaSharp.SKPathArcSize.Small,
							 (arcSegment.SweepDirection == SweepDirection.Clockwise ? SkiaSharp.SKPathDirection.Clockwise : SkiaSharp.SKPathDirection.CounterClockwise),
							 (float)arcSegment.Point.X, (float)arcSegment.Point.Y);
					}
				}

				if (figure.IsClosed)
				{
					skiaGeometry.Geometry.Close();
				}
			}

			return skiaGeometry;
		}

	}
}
