using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Numerics;
using System.Linq;
using static System.Math;

#if XAMARIN_IOS_UNIFIED
using UIKit;
using Path = UIKit.UIBezierPath;
#elif __MACOS__
using AppKit;
using Path = AppKit.NSBezierPath;
using CoreGraphics;
#elif XAMARIN_ANDROID
using Android.Graphics.Drawables.Shapes;
using Path = Android.Graphics.Path;
using Uno.UI;
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

		public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
		{
#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS || __MACOS__
			bezierPath.MoveTo(startPoint);
#elif XAMARIN_ANDROID
			var physicalStartPoint = LogicalToPhysicalNoRounding(startPoint);
			bezierPath.MoveTo((float)physicalStartPoint.X, (float)physicalStartPoint.Y);
#endif

			_points.Add(startPoint);
		}

		public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
		{
#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
			bezierPath.AddLineTo(point);
#elif __MACOS__
			bezierPath.LineTo(point);
#elif XAMARIN_ANDROID
			var physicalPoint = LogicalToPhysicalNoRounding(point);
			bezierPath.LineTo((float)physicalPoint.X, (float)physicalPoint.Y);
#endif

			_points.Add(point);
		}

		public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
		{
#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
			bezierPath.AddCurveToPoint(point3, point1, point2);
#elif __MACOS__
			bezierPath.CurveTo(point3, point1, point2);
#elif XAMARIN_ANDROID
			var physicalPoint1 = LogicalToPhysicalNoRounding(point1);
			var physicalPoint2 = LogicalToPhysicalNoRounding(point2);
			var physicalPoint3 = LogicalToPhysicalNoRounding(point3);
			bezierPath.CubicTo((float)physicalPoint1.X, (float)physicalPoint1.Y, (float)physicalPoint2.X, (float)physicalPoint2.Y, (float)physicalPoint3.X, (float)physicalPoint3.Y);
#endif
			_points.Add(point3);
		}

		public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
		{
#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
			bezierPath.AddQuadCurveToPoint(point2, point1);
#elif __MACOS__
			// Convert a Quadratic Curve to cubic curve to draw it.
			// https://stackoverflow.com/a/52569210/1771254
			var startPoint = bezierPath.CurrentPoint;
			var endPoint = point1;

			var controlPoint1 = new CGPoint(startPoint.X + ((point2.X - startPoint.X) * 2.0 / 3.0),  startPoint.Y + (point2.Y - startPoint.Y) * 2.0 / 3.0);
			var controlPoint2 = new CGPoint(endPoint.X + ((point2.X - endPoint.X) * 2.0 / 3.0), endPoint.Y + (point2.Y - endPoint.Y) * 2.0 / 3.0);
			bezierPath.CurveTo(point1, controlPoint1, controlPoint2);
#elif XAMARIN_ANDROID
			var physicalPoint1 = LogicalToPhysicalNoRounding(point1);
			var physicalPoint2 = LogicalToPhysicalNoRounding(point2);
			bezierPath.QuadTo((float)physicalPoint1.X, (float)physicalPoint1.Y, (float)physicalPoint2.X, (float)physicalPoint2.Y);
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

#if XAMARIN_IOS_UNIFIED || XAMARIN_IOS
			bezierPath.AddArc(
				center, 
				(nfloat)radius, 
				(nfloat)startAngle, 
				(nfloat)endAngle, 
				sweepDirection == SweepDirection.Clockwise
			);

#elif __MACOS__
			bezierPath.AppendPathWithArc(center,
										 (nfloat)radius,
										 (nfloat)startAngle,
										 (nfloat)endAngle,
										 sweepDirection == SweepDirection.Clockwise);
#elif XAMARIN_ANDROID
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
				circle.LogicalToPhysicalPixels().ToRectF(),
				(float)startAngle,
				(float)sweepAngle
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
#if XAMARIN_IOS || XAMARIN_IOS_UNIFIED
					bezierPath.ClosePath();
#elif XAMARIN_ANDROID
					bezierPath.Close();
#endif
				}
			}
		}

#if XAMARIN_ANDROID
		private static Point LogicalToPhysicalNoRounding(Point point)
		{
			return new Point(point.X * ViewHelper.Scale, point.Y * ViewHelper.Scale);
		}
#endif

		public override void Dispose()
		{
			_owner.Close(bezierPath);
		}
	}
}
