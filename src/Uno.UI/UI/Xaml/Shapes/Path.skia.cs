#nullable enable
using System;
using Uno.Media;
using Windows.Foundation;
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

		private SkiaGeometrySource2D? GetPath() => GetSkiaGeometry(Data);

		private SKPath? GetSKPath(Geometry? geometry)
		{
			switch (geometry)
			{
				case PathGeometry pg:
					return ToSKPath(pg);
				case LineGeometry lg:
					return CompositionGeometry.BuildLineGeometry(lg.StartPoint.ToVector2(), lg.EndPoint.ToVector2());
				case RectangleGeometry rg:
					return CompositionGeometry.BuildRectangleGeometry(
						offset: new Vector2((float)rg.Rect.X, (float)rg.Rect.Y),
						size: new Vector2((float)rg.Rect.Width, (float)rg.Rect.Height));
				case GeometryGroup group:
					return ToSKPath(@group);
				case StreamGeometry sg:
					return sg.GetGeometrySource2D().Geometry;
				case EllipseGeometry eg:
					return CompositionGeometry.BuildEllipseGeometry(eg.Center.ToVector2(), new Vector2((float)eg.RadiusX, (float)eg.RadiusY));
			}

			if (geometry != null)
			{
				throw new NotSupportedException($"Geometry {geometry} is not supported");
			}

			return null;
		}

		private SkiaGeometrySource2D? GetSkiaGeometry(Geometry? geometry)
		{
			if (geometry is StreamGeometry sg)
			{
				// Avoid allocating SkiaGeometrySource2D if we have one already.
				return sg.GetGeometrySource2D();
			}
			else if (GetSKPath(geometry) is { } path)
			{
				return new SkiaGeometrySource2D(path);
			}

			return null;
		}

		private SKPath ToSKPath(PathGeometry geometry)
		{
			var path = new SKPath();

			foreach (PathFigure figure in geometry.Figures)
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

			path.FillType = geometry.FillRule.ToSkiaFillType();

			return path;
		}

		private SKPath ToSKPath(GeometryGroup geometryGroup)
		{
			var path = new SKPath();

			foreach (var geometry in geometryGroup.Children)
			{
				var geometryPath = GetSKPath(geometry);
				path.AddPath(geometryPath);
			}

			path.FillType = geometryGroup.FillRule.ToSkiaFillType();
			return path;
		}
	}
}
