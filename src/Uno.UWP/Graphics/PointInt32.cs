namespace Windows.Graphics;

/// <summary>
/// Defines a point in a two-dimensional plane.
/// </summary>
public partial struct PointInt32
{
	internal PointInt32(int x, int y)
	{
		X = x;
		Y = y;
	}

	/// <summary>
	/// The X coordinate value of a point.
	/// </summary>
	public int X;

	/// <summary>
	/// The Y coordinate value of a point.
	/// </summary>
	public int Y;
}
