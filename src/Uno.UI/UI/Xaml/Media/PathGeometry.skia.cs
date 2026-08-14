using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

using Rect = Windows.Foundation.Rect;


namespace Microsoft.UI.Xaml.Media
{
	partial class PathGeometry
	{
		private protected override Rect ComputeBounds()
		{
			var geometry = GetGeometry();
			if (geometry is null || geometry.IsEmpty)
			{
				return default;
			}

			var rect = geometry.Bounds;
			return Transform is { } transform ? transform.TransformBounds(rect) : rect;
		}

		internal override IGeometry GetGeometry() => BuildGeometry(false);

		internal override IGeometry GetFilledGeometry() => BuildGeometry(true);

		private IGeometry BuildGeometry(bool skipUnfilled)
		{
			var builder = GeometryFactory.Current.CreatePathBuilder();
			builder.FillRule = FillRule == FillRule.EvenOdd ? GeometryFillRule.EvenOdd : GeometryFillRule.NonZero;

			foreach (PathFigure figure in Figures)
			{
				if (skipUnfilled && !figure.IsFilled)
				{
					continue;
				}

				builder.MoveTo(new Vector2((float)figure.StartPoint.X, (float)figure.StartPoint.Y));

				foreach (PathSegment segment in figure.Segments)
				{
					if (segment is LineSegment lineSegment)
					{
						builder.LineTo(new Vector2((float)lineSegment.Point.X, (float)lineSegment.Point.Y));
					}
					else if (segment is PolyLineSegment polyLineSegment)
					{
						foreach (var point in polyLineSegment.Points)
						{
							builder.LineTo(new Vector2((float)point.X, (float)point.Y));
						}
					}
					else if (segment is BezierSegment bezierSegment)
					{
						builder.CubicTo(
							new Vector2((float)bezierSegment.Point1.X, (float)bezierSegment.Point1.Y),
							new Vector2((float)bezierSegment.Point2.X, (float)bezierSegment.Point2.Y),
							new Vector2((float)bezierSegment.Point3.X, (float)bezierSegment.Point3.Y));
					}
					else if (segment is PolyBezierSegment polyBezierSegment)
					{
						for (var i = 0; i < polyBezierSegment.Points.Count - 2; i += 3)
						{
							builder.CubicTo(
								new Vector2((float)polyBezierSegment.Points[i].X, (float)polyBezierSegment.Points[i].Y),
								new Vector2((float)polyBezierSegment.Points[i + 1].X, (float)polyBezierSegment.Points[i + 1].Y),
								new Vector2((float)polyBezierSegment.Points[i + 2].X, (float)polyBezierSegment.Points[i + 2].Y));
						}
					}
					else if (segment is QuadraticBezierSegment quadraticBezierSegment)
					{
						builder.QuadraticTo(
							new Vector2((float)quadraticBezierSegment.Point1.X, (float)quadraticBezierSegment.Point1.Y),
							new Vector2((float)quadraticBezierSegment.Point2.X, (float)quadraticBezierSegment.Point2.Y));
					}
					else if (segment is PolyQuadraticBezierSegment polyQuadraticBezierSegment)
					{
						for (var i = 0; i < polyQuadraticBezierSegment.Points.Count - 1; i += 2)
						{
							builder.QuadraticTo(
								new Vector2((float)polyQuadraticBezierSegment.Points[i].X, (float)polyQuadraticBezierSegment.Points[i].Y),
								new Vector2((float)polyQuadraticBezierSegment.Points[i + 1].X, (float)polyQuadraticBezierSegment.Points[i + 1].Y));
						}
					}
					else if (segment is ArcSegment arcSegment)
					{
						builder.ArcTo(
							new Vector2((float)arcSegment.Size.Width, (float)arcSegment.Size.Height),
							(float)arcSegment.RotationAngle,
							arcSegment.IsLargeArc,
							arcSegment.SweepDirection == SweepDirection.Clockwise,
							new Vector2((float)arcSegment.Point.X, (float)arcSegment.Point.Y));
					}
				}

				if (figure.IsClosed)
				{
					builder.Close();
				}
			}

			return builder.Build();
		}
	}
}
