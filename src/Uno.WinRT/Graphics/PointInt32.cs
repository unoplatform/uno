using System;

namespace Windows.Graphics;

/// <summary>
/// Defines a point in a two-dimensional plane.
/// </summary>
public partial struct PointInt32 : IEquatable<PointInt32>
{
	// Parameter names mirror the WinAppSDK/CsWinRT metadata (enforced by the sync generator); keep as-is.
	public PointInt32(int _X, int _Y)
	{
		X = _X;
		Y = _Y;
	}

	/// <summary>
	/// The X coordinate value of a point.
	/// </summary>
	public int X;

	/// <summary>
	/// The Y coordinate value of a point.
	/// </summary>
	public int Y;

	public bool Equals(PointInt32 other) => X == other.X && Y == other.Y;

	public override bool Equals(object obj) => obj is PointInt32 other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(X, Y);

	public static bool operator ==(PointInt32 left, PointInt32 right) => left.Equals(right);

	public static bool operator !=(PointInt32 left, PointInt32 right) => !left.Equals(right);
}
