using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Uno.Media
{
	internal class GeneratedStreamGeometryContext : StreamGeometryContext
	{
		StringBuilder _builder;

		public string Generated { get { return _builder.ToString() + "})"; } }

		public GeneratedStreamGeometryContext()
		{
			_builder = new StringBuilder("global::Uno.Media.GeometryHelper.Build(c =>\n{\n");
		}

		public override void ArcTo(Point Point, Size Size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat("c.ArcTo({0}, {1}, {2}, {3}, {4}, true, false);\n", Point.ToCode(), Size.ToCode(), rotationAngle.ToCode(), isLargeArc.ToCode(), sweepDirection.ToCode());
		}

		public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
		{
			_builder.AppendFormat("c.BeginFigure({0}, true, false);\n", startPoint.ToCode());
		}

		public override void BezierTo(Point Point1, Point Point2, Point Point3, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat("c.BezierTo({0}, {1}, {2}, true, false);\n", Point1.ToCode(), Point2.ToCode(), Point3.ToCode());
		}

		public override void LineTo(Point Point, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat("c.LineTo({0}, true, false);\n", Point.ToCode());
		}

		public override void PolyBezierTo(IList<Point> Points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyBezierTo is not implemented");
		}

		public override void PolyLineTo(IList<Point> Points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyLineTo is not implemented");
		}

		public override void PolyQuadraticBezierTo(IList<Point> Points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyQuadraticBezierTo is not implemented");
		}

		public override void QuadraticBezierTo(Point Point1, Point Point2, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat("c.BezierTo({0}, {1}, true, false);\n", Point1.ToCode(), Point2.ToCode());
		}

		public override void SetClosedState(bool closed)
		{
			_builder.AppendFormat("c.SetClosedState({0});\n", closed.ToString(System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant());
		}
	}

	internal static class DrawingExtensions
	{
		public static string ToCode(this Point Point)
		{
			return "new global::Windows.Foundation.Point({0}, {1})".InvariantCultureFormat(Point.X, Point.Y);
		}

		public static string ToCode(this Size Size)
		{
			return "new global::Windows.Foundation.Size({0}, {1})".InvariantCultureFormat(Size.Width, Size.Height);
		}

		public static string ToCode(this SweepDirection direction)
		{
			switch (direction)
			{
				case SweepDirection.Counterclockwise:
					return "global::Windows.UI.Xaml.Media.SweepDirection.Counterclockwise";
				case SweepDirection.Clockwise:
					return "global::Windows.UI.Xaml.Media.SweepDirection.Clockwise";
				default:
					throw new ArgumentException();
			}
		}

		public static string ToCode(this bool @bool)
		{
			return @bool ? "true" : "false";
		}

		public static string ToCode(this double @double)
		{
			return $"{@double}d";
		}
	}
}
