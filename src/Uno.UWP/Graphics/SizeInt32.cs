using Windows.Foundation;

namespace Windows.Graphics;

/// <summary>
/// Defines the height and wide of a surface in a two-dimensional plane.
/// </summary>
public partial struct SizeInt32
{
	internal SizeInt32(int width, int height)
	{
		Width = width;
		Height = height;
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
