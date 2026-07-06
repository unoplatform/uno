using Windows.Foundation;

namespace Windows.Graphics;

/// <summary>
/// Defines the height and wide of a surface in a two-dimensional plane.
/// </summary>
public partial struct SizeInt32
{
	// Parameter names mirror the WinAppSDK/CsWinRT metadata (enforced by the sync generator); keep as-is.
	internal SizeInt32(int _Width, int _Height)
	{
		Width = _Width;
		Height = _Height;
	}

	/// <summary>
	/// The width of a surface.
	/// </summary>
	public int Width;

	/// <summary>
	/// The height of a surface.
	/// </summary>
	public int Height;
}
