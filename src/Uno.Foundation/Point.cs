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

namespace Windows.Foundation;

[DebuggerDisplay("{DebugDisplay,nq}")]
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

	internal Point WithX(double x) => new Point(x, Y);

	internal Point WithY(double y) => new Point(X, y);

	public override int GetHashCode()
		=> X.GetHashCode() ^ Y.GetHashCode();

	public override bool Equals(object obj)
		=> obj is Point other && Equals(this, other);

	public bool Equals(Point value) // Even if not in public doc, this is public on UWP
		=> Equals(this, value);

	private static bool Equals(Point left, Point right)
		=> left.X == right.X && left.Y == right.Y;

	public static bool operator ==(Point left, Point right)
		=> Equals(left, right);

	public static bool operator !=(Point left, Point right)
		=> !Equals(left, right);

	public static Point operator +(Point p1, Point p2)
		=> new Point(p1.X + p2.X, p1.Y + p2.Y);

	public static Point operator -(Point p1, Point p2)
		=> new Point(p1.X - p2.X, p1.Y - p2.Y);

	public static Point operator -(Point a)
		=> new Point(-a.X, -a.Y);

	public static implicit operator Point(string point)
	{
		if (string.IsNullOrEmpty(point))
		{
			// Marker to enable null-comparison if the string comparer
			// has been called with null.
			return new Point(double.NaN, double.NaN);
		}
		else
		{
			var parts = point
				.Split(new[] { ',' })
				.Select(value => double.Parse(value, CultureInfo.InvariantCulture))
				.ToArray();

			return new Point(parts[0], parts[1]);
		}
	}

	public override string ToString()
		=> "[{0}, {1}]".InvariantCultureFormat(X, Y);

	internal string ToDebugString()
		=> FormattableString.Invariant($"{X:F2},{Y:F2}");

	private string DebugDisplay => $"{X:f1},{Y:f1}";
}
