using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Media;

namespace Windows.UI.Xaml.Media
{
	internal static class GeometryHelper
	{
		public static StreamGeometry ToStreamGeometry(this Geometry geometry)
		{
			var streamGeometry = geometry as StreamGeometry;
			if (streamGeometry == null)
			{
				streamGeometry = new StreamGeometry();

				switch (geometry)
				{
					case GeometryGroup geometryGroup:
						streamGeometry.FillRule = geometryGroup.FillRule;
						break;
					case PathGeometry pathGeometry:
						streamGeometry.FillRule = pathGeometry.FillRule;
						break;
					default:
						break;
				}

				using (StreamGeometryContext ctx = streamGeometry.Open())
				{
					ctx.Write(geometry);
				};
			}

			return streamGeometry;
		}

		public static void Write(this StreamGeometryContext ctx, Geometry geometry)
		{
			switch (geometry)
			{
				case GeometryGroup geometryGroup:
					ctx.Write(geometryGroup);
					break;
				case PathGeometry pathGeometry:
					ctx.Write(pathGeometry);
					break;
				default:
					break;
			}
		}

		public static void Write(this StreamGeometryContext ctx, GeometryGroup geometryGroup)
		{
			geometryGroup.Children?.ForEach(ctx.Write);
		}

		public static void Write(this StreamGeometryContext ctx, PathGeometry pathGeometry)
		{
			pathGeometry.Figures.ForEach(ctx.Write);
		}

		public static void Write(this StreamGeometryContext ctx, PathFigure pathFigure)
		{
			ctx.BeginFigure(pathFigure.StartPoint, pathFigure.IsFilled, pathFigure.IsClosed);
			pathFigure.Segments.ForEach(ctx.Write);
			ctx.SetClosedState(pathFigure.IsClosed);
		}

		public static void Write(this StreamGeometryContext ctx, PathSegment pathSegment)
		{
			switch (pathSegment)
			{
				case ArcSegment arc:
					ctx.ArcTo(arc.Point, arc.Size, arc.RotationAngle, arc.IsLargeArc, arc.SweepDirection, false, false);
					break;
				case BezierSegment bezier:
					ctx.BezierTo(bezier.Point1, bezier.Point2, bezier.Point3, false, false);
					break;
				case PolyBezierSegment polyBezier:
					ctx.PolyBezierTo(polyBezier.Points, false, false);
					break;
				case PolyLineSegment polyLine:
					ctx.PolyLineTo(polyLine.Points, false, false);
					break;
				case QuadraticBezierSegment quadraticBezier:
					ctx.QuadraticBezierTo(quadraticBezier.Point1, quadraticBezier.Point2, false, false);
					break;
				case PolyQuadraticBezierSegment polyQuadraticBezier:
					ctx.PolyQuadraticBezierTo(polyQuadraticBezier.Points, false, false);
					break;
				case LineSegment line:
					ctx.LineTo(line.Point, false, false);
					break;
				default:
					break;
			}
		}
	}
}
