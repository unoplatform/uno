using Uno.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Globalization;

#if __APPLE_UIKIT__
using CoreGraphics;
#endif

namespace Windows.Foundation;

[DebuggerDisplay("{DebugDisplay,nq}")]
[Uno.Foundation.Internals.Bindable]
public partial struct Point
{
	// These are public in WinUI (with the underscore!), but we don't want to expose it for now at least.
	private float _x;
	private float _y;

	public Point(float x, float y)
	{
		_x = x;
		_y = y;
	}

	public Point(double x, double y)
		: this((float)x, (float)y)
	{
	}

	internal static Point Zero => new Point(0, 0);

	public double X
	{
		get => _x;
		set => _x = (float)value;
	}

	public double Y
	{
		get => _y;
		set => _y = (float)value;
	}

	internal Point WithX(double x) => new Point(x, Y);

	internal Point WithY(double y) => new Point(X, y);

	public override int GetHashCode()
		=> X.GetHashCode() ^ Y.GetHashCode();

	public override bool Equals(object o)
		=> o is Point other && Equals(this, other);

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

	public override string ToString()
		=> "[{0}, {1}]".InvariantCultureFormat(X, Y);

	internal string ToDebugString()
		=> FormattableString.Invariant($"{X:F2},{Y:F2}");

	private string DebugDisplay => $"{X:f1},{Y:f1}";
}
