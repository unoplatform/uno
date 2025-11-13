using System;

namespace Windows.Graphics;

/// <summary>
/// Defines the size and location of a rectangular surface.
/// </summary>
public partial struct RectInt32
{
	internal RectInt32(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	/// <summary>
	/// The X coordinate of the top-left corner of the rectangle.
	/// </summary>
	public int X;

	/// <summary>
	/// The Y coordinate of the top-left corner of the rectangle.
	/// </summary>
	public int Y;

	/// <summary>
	/// The width of a rectangle.
	/// </summary>
	public int Width;

	/// <summary>
	/// The height of a rectangle.
	/// </summary>
	public int Height;

	public override bool Equals(object obj) => obj is RectInt32 @int && X == @int.X && Y == @int.Y && Width == @int.Width && Height == @int.Height;

	public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

	public static bool operator ==(RectInt32 left, RectInt32 right) => left.Equals(right);

	public static bool operator !=(RectInt32 left, RectInt32 right) => !(left == right);

	internal bool Contains(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Height;

	internal bool Contains(PointInt32 point) => Contains(point.X, point.Y);
}
