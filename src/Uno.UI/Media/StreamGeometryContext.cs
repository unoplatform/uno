using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Uno.Media
{
	public abstract class StreamGeometryContext : IDisposable
	{
		public abstract void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin);

		public abstract void BeginFigure(Point startPoint, bool isFilled);

		public abstract void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin);

		public abstract void LineTo(Point point, bool isStroked, bool isSmoothJoin);

		public abstract void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

		public abstract void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

		public abstract void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin);

		public abstract void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin);

		public abstract void SetClosedState(bool closed);

		public virtual void Dispose()
		{
		}
	}
}
