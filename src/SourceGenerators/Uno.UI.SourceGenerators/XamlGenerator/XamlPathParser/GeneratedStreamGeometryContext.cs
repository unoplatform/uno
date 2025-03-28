using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Globalization;

namespace Uno.Media
{
	internal class GeneratedStreamGeometryContext : StreamGeometryContext
	{
		StringBuilder _builder;

		private static readonly string CRLF = Environment.NewLine;

		internal FillRule FillRule { get; set; }

		public string Generate() => $"{_builder}}}, {FillRule.ToCode()})";

		public GeneratedStreamGeometryContext()
		{
			_builder = new StringBuilder("global::Uno.Media.GeometryHelper.Build(c =>" + CRLF + "{" + CRLF);
		}

		public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.ArcTo({0}, {1}, {2}, {3}, {4}, true, false);" + CRLF, point.ToCode(), size.ToCode(), rotationAngle.ToCode(), isLargeArc.ToCode(), sweepDirection.ToCode());
		}

		public override void BeginFigure(Point startPoint, bool isFilled)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.BeginFigure({0}, true);" + CRLF, startPoint.ToCode());
		}

		public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.BezierTo({0}, {1}, {2}, true, false);" + CRLF, point1.ToCode(), point2.ToCode(), point3.ToCode());
		}

		public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.LineTo({0}, true, false);" + CRLF, point.ToCode());
		}

		public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyBezierTo is not implemented");
		}

		public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyLineTo is not implemented");
		}

		public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException("PolyQuadraticBezierTo is not implemented");
		}

		public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.BezierTo({0}, {1}, true, false);" + CRLF, point1.ToCode(), point2.ToCode());
		}

		public override void SetClosedState(bool closed)
		{
			_builder.AppendFormat(CultureInfo.InvariantCulture, "c.SetClosedState({0});" + CRLF, closed.ToString(System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant());
		}
	}

	internal static class DrawingExtensions
	{
		public static string ToCode(this Point point)
		{
			return "new global::Windows.Foundation.Point({0}, {1})".InvariantCultureFormat(point.X, point.Y);
		}

		public static string ToCode(this Size size)
		{
			return "new global::Windows.Foundation.Size({0}, {1})".InvariantCultureFormat(size.Width, size.Height);
		}

		public static string ToCode(this FillRule fillRule)
		{
			return fillRule == FillRule.EvenOdd
				? "global::Windows.UI.Xaml.Media.FillRule.EvenOdd"
				: "global::Windows.UI.Xaml.Media.FillRule.Nonzero";
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
