namespace Windows.Graphics;

/// <summary>
/// Defines a point in a two-dimensional plane.
/// </summary>
public partial struct PointInt32
{
	// Parameter names mirror the WinAppSDK/CsWinRT metadata (enforced by the sync generator); keep as-is.
	internal PointInt32(int _X, int _Y)
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
}
