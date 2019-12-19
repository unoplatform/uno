using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Globalization;
#if XAMARIN_IOS
using CoreGraphics;
#endif

namespace Windows.Foundation
{
	[DebuggerDisplay("{X},{Y}")]
	public partial struct Point
	{
		public Point(double x, double y)
		{
			X = x;
			Y = y;
		}

		internal static Point Zero => new Point(0, 0);

		public double X { get; set; }
		public double Y { get; set; }

		public override string ToString()
		{
			return "[{0}, {1}]".InvariantCultureFormat(X, Y);
		}

		public static bool operator ==(Point left, Point right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Point left, Point right)
		{
			return !left.Equals(right);
		}

		public static Point operator +(Point p1, Point p2)
		{
			return new Point(p1.X + p2.X, p1.Y + p2.Y);
		}

		public static Point operator -(Point p1, Point p2)
		{
			return new Point(p1.X - p2.X, p1.Y - p2.Y);
		}

		public static implicit operator Point(string point)
		{
			var parts = point
				.Split(new[] { ',' })
				.Select(value => double.Parse(value, CultureInfo.InvariantCulture))
				.ToArray();

			return new Point(parts[0], parts[1]);
		}

		public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

		public override bool Equals(object obj)
		{
			if (obj is Point)
			{
				var point = (Point)obj;

				return X == point.X && Y == point.Y;
			}

			return false;
		}
	}
}
