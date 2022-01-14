using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
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
				case LineGeometry lineGeometry:
					ctx.Write(lineGeometry);
					break;
				case RectangleGeometry rectangleGeometry:
					ctx.Write(rectangleGeometry);
					break;
				case EllipseGeometry ellipseGeometry:
					ctx.Write(ellipseGeometry);
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

		public static void Write(this StreamGeometryContext ctx, LineGeometry lineGeometry)
		{
			ctx.BeginFigure(lineGeometry.StartPoint, false);
			ctx.LineTo(lineGeometry.EndPoint, true, false);
			ctx.SetClosedState(true);
		}

		public static void Write(this StreamGeometryContext ctx, RectangleGeometry rectangleGeometry)
		{
			var rect = rectangleGeometry.Rect;

			var topLeft = rect.Location;
			var topRight = new Point(rect.Right, rect.Top);
			var bottomLeft = new Point(rect.Left, rect.Bottom);
			var bottomRight = new Point(rect.Right, rect.Bottom);

			ctx.BeginFigure(topLeft, true);
			ctx.LineTo(topRight, true, false);
			ctx.LineTo(bottomRight, true, false);
			ctx.LineTo(bottomLeft, true, false);
			ctx.LineTo(topLeft, true, false);

			ctx.SetClosedState(true);
		}

		public static void Write(this StreamGeometryContext ctx, EllipseGeometry ellipseGeometry)
		{
			// TODO - for some reason the following code is crashing on iOS.
			// https://github.com/unoplatform/uno/issues/6849

			//var cx = ellipseGeometry.Center.X;
			//var cy = ellipseGeometry.Center.Y;
			//var rx = ellipseGeometry.RadiusX;
			//var ry = ellipseGeometry.RadiusY;

			//ctx.BeginFigure(new Point(cx, cy - ry), true, true);
			//ctx.ArcTo(new Point(cx, cy + ry), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true, false);
			//ctx.ArcTo(new Point(cx, cy - ry), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true, false);
			//ctx.SetClosedState(true);
		}

		public static void Write(this StreamGeometryContext ctx, PathFigure pathFigure)
		{
			ctx.BeginFigure(pathFigure.StartPoint, pathFigure.IsFilled);
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
