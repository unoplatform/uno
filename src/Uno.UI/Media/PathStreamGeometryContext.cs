using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Numerics;
using System.Linq;
using static System.Math;
using Uno.Extensions;

#if __IOS__
using UIKit;
using Path = UIKit.UIBezierPath;
using ObjCRuntime;
#elif __MACOS__
using AppKit;
using Path = AppKit.NSBezierPath;
using CoreGraphics;
using ObjCRuntime;
#elif __ANDROID__
using Android.Graphics.Drawables.Shapes;
using Path = Android.Graphics.Path;
using Uno.UI;
#elif __SKIA__
using Path = Windows.UI.Composition.SkiaGeometrySource2D;
#else
using Path = System.Object;
#endif

namespace Uno.Media
{
	class PathStreamGeometryContext : StreamGeometryContext
	{
		private readonly List<Point> _points = new List<Point>();
		private readonly StreamGeometry _owner;
		private Path bezierPath = new Path();

		internal PathStreamGeometryContext(StreamGeometry owner)
		{
			_owner = owner;
		}

		public override void BeginFigure(Point startPoint, bool isFilled)
		{
#if __IOS__ || __MACOS__
			bezierPath.MoveTo(startPoint);
#elif __ANDROID__
			bezierPath.MoveTo((float)startPoint.X, (float)startPoint.Y);
#elif __SKIA__
			bezierPath.Geometry.MoveTo(new SkiaSharp.SKPoint((float)startPoint.X, (float)startPoint.Y));
#endif

			_points.Add(startPoint);
		}

		public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
		{
#if __IOS__
			bezierPath.AddLineTo(point);
#elif __MACOS__
			bezierPath.LineTo(point);
#elif __ANDROID__
			bezierPath.LineTo((float)point.X, (float)point.Y);
#elif __SKIA__
			bezierPath.Geometry.LineTo((float)point.X, (float)point.Y);
#endif

			_points.Add(point);
		}

		public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
		{
#if __IOS__
			bezierPath.AddCurveToPoint(point3, point1, point2);
#elif __MACOS__
			bezierPath.CurveTo(point3, point1, point2);
#elif __ANDROID__
			bezierPath.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
#elif __SKIA__
			bezierPath.Geometry.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
#endif
			_points.Add(point3);
		}

		public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
		{
#if __IOS__
			bezierPath.AddQuadCurveToPoint(point2, point1);
#elif __MACOS__
			// Convert a Quadratic Curve to cubic curve to draw it.
			// https://stackoverflow.com/a/52569210/1771254
			var startPoint = bezierPath.CurrentPoint;
			var endPoint = point1;

			var controlPoint1 = new CGPoint(startPoint.X + ((point2.X - startPoint.X) * 2.0 / 3.0), startPoint.Y + (point2.Y - startPoint.Y) * 2.0 / 3.0);
			var controlPoint2 = new CGPoint(endPoint.X + ((point2.X - endPoint.X) * 2.0 / 3.0), endPoint.Y + (point2.Y - endPoint.Y) * 2.0 / 3.0);
			bezierPath.CurveTo(point1, controlPoint1, controlPoint2);
#elif __ANDROID__
			bezierPath.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
#elif __SKIA__
			bezierPath.Geometry.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
#endif

			_points.Add(point2);
		}

		public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
		{
			if (size.Width != size.Height)
			{
				throw new NotImplementedException("The arc must be based on a circle, not an ellipse.");
			}

			var startPoint = _points.Last();
			var endPoint = point;
			var radius = size.Width;
			var sign = isLargeArc != (sweepDirection == SweepDirection.Clockwise);
			var center = CenterFromPointsAndRadius(startPoint, endPoint, radius, sign);
			var startAngle = Atan2(startPoint.Y - center.Y, startPoint.X - center.X);
			var endAngle = Atan2(endPoint.Y - center.Y, endPoint.X - center.X);
			var circle = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);

#if __IOS__
			bezierPath.AddArc(
				center,
				(nfloat)radius,
				(nfloat)startAngle,
				(nfloat)endAngle,
				sweepDirection == SweepDirection.Clockwise
			);

#elif __MACOS__
			//Ugly workaround. check if all vars are defined
			if (!double.IsNaN(radius) && !double.IsNaN(startAngle) && !double.IsNaN(endAngle))
			{
				//Convert to degrees in a 0 =< x =< 360 deg range
				startAngle = MathEx.ToDegreeNormalized(startAngle);
				endAngle = MathEx.ToDegreeNormalized(endAngle);
				bezierPath.AppendPathWithArc(center,
										 (nfloat)radius,
										 (nfloat)startAngle,
										 (nfloat)endAngle);

				//Move to startPoint. To prevent segment being drawn to the startPoint from the end of the arc
				bezierPath.MoveTo(startPoint);
			}
#elif __ANDROID__
			var sweepAngle = endAngle - startAngle;

			// Convert to degrees
			startAngle = startAngle * (180 / PI);
			sweepAngle = sweepAngle * (180 / PI);

			// Invert y-axis
			startAngle = (startAngle + 360) % 360;
			sweepAngle = (sweepAngle + 360) % 360;

			// Apply direction
			if (sweepDirection == SweepDirection.Counterclockwise)
			{
				sweepAngle -= 360;
			}

			bezierPath.ArcTo(
				circle.ToRectF(),
				(float)startAngle,
				(float)sweepAngle
			);
#elif __SKIA__
			var sweepAngle = endAngle - startAngle;

			// Convert to degrees
			startAngle = startAngle * (180 / PI);
			sweepAngle = sweepAngle * (180 / PI);

			// Invert y-axis
			startAngle = (startAngle + 360) % 360;
			sweepAngle = (sweepAngle + 360) % 360;

			// Apply direction
			if (sweepDirection == SweepDirection.Counterclockwise)
			{
				sweepAngle -= 360;
			}

			bezierPath.Geometry.ArcTo(
				new SkiaSharp.SKRect((float)circle.Left, (float)circle.Top, (float)circle.Right, (float)circle.Bottom),
				(float)startAngle,
				(float)sweepAngle,
				false
			);
#endif

			_points.Add(point);
		}

		private static Point CenterFromPointsAndRadius(Point point1, Point point2, double radius, bool sign)
		{
			// Find the center of a circle from 2 points and a radius
			// http://mathforum.org/library/drmath/view/53027.html

			var x1 = point1.X;
			var y1 = point1.Y;
			var x2 = point2.X;
			var y2 = point2.Y;

			var q = Sqrt(Pow(x2 - x1, 2) + Pow(y1 - y2, 2));

			var y3 = (y1 + y2) / 2;
			var x3 = (x1 + x2) / 2;

			var x = sign
				? x3 + Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (y1 - y2) / q
				: x3 - Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (y1 - y2) / q;

			var y = sign
				? y3 + Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (x2 - x1) / q
				: y3 - Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (x2 - x1) / q;

			return new Point(x, y);
		}

		public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			foreach (var point in points)
			{
				LineTo(point, isStroked, isSmoothJoin);
			}
		}

		public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException();
		}

		public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException();
		}

		public override void SetClosedState(bool closed)
		{
			if (bezierPath != null)
			{
				if (closed)
				{
#if __IOS__ || __MACOS__
					bezierPath.ClosePath();
#elif __ANDROID__
					bezierPath.Close();
#elif __SKIA__
					bezierPath.Geometry.Close();
#elif __WASM__
					// TODO: In most cases, the path is handled by the browser.
					// But it might still be possible to hit this code path on Wasm?
					// This needs to be revisited.
#elif IS_UNIT_TESTS
					// Empty on unit tests.
#else
					throw new NotSupportedException("SetClosedState is not supported on this platform.");
#endif
				}
			}
		}

		public override void Dispose()
		{
			_owner.Close(bezierPath);
		}
	}
}
