#nullable enable
using System;
using Uno.Disposables;
using Uno.Media;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.UI.Xaml.Media;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private SkiaGeometrySource2D GetPath() => GetSkiaGeometry(Data);

		private SkiaGeometrySource2D GetSkiaGeometry(Geometry geometry)
		{
			switch (geometry)
			{
				case PathGeometry pg:
					return ToGeometrySource2D(pg);
				case LineGeometry lg:
					return new SkiaGeometrySource2D(CompositionGeometry.BuildLineGeometry(lg.StartPoint.ToVector2(), lg.EndPoint.ToVector2()));
				case RectangleGeometry rg:
					return new SkiaGeometrySource2D(CompositionGeometry.BuildRectangleGeometry(
						offset: new Vector2((float)rg.Rect.X, (float)rg.Rect.Y),
						size: new Vector2((float)rg.Rect.Width, (float)rg.Rect.Height)));
				case GeometryGroup group:
					return ToGeometrySource2D(@group);
				case StreamGeometry sg:
					return sg.GetGeometrySource2D();
				case EllipseGeometry eg:
					return new SkiaGeometrySource2D(CompositionGeometry.BuildEllipseGeometry(eg.Center.ToVector2(), new Vector2((float)eg.RadiusX, (float)eg.RadiusY)));
			}

			if (geometry != null)
			{
				throw new NotSupportedException($"Geometry {geometry} is not supported");
			}

			return null;
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

			skiaGeometry.Geometry.FillType = geometry.FillRule.ToSkiaFillType();

			return skiaGeometry;
		}

		private SkiaGeometrySource2D ToGeometrySource2D(GeometryGroup geometryGroup)
		{
			var path = new SKPath();

			foreach (var geometry in geometryGroup.Children)
			{
				// TODO(minor): GetSkiaGeometry will allocate an unneeded SkiaGeometrySource2D. We only need the SKPath
				// Consider refactoring to eliminate the extra allocation.
				var geometryPath = GetSkiaGeometry(geometry);
				path.AddPath(geometryPath.Geometry);
			}

			path.FillType = geometryGroup.FillRule.ToSkiaFillType();

			return new SkiaGeometrySource2D(path);
		}
	}
}
