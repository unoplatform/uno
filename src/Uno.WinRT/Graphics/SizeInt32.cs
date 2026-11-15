using System;
using Windows.Foundation;

namespace Windows.Graphics;

/// <summary>
/// Defines the height and wide of a surface in a two-dimensional plane.
/// </summary>
public partial struct SizeInt32 : IEquatable<SizeInt32>
{
	// Parameter names mirror the WinAppSDK/CsWinRT metadata (enforced by the sync generator); keep as-is.
	public SizeInt32(int _Width, int _Height)
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

	public bool Equals(SizeInt32 other) => Width == other.Width && Height == other.Height;

	public override bool Equals(object obj) => obj is SizeInt32 other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Width, Height);

	public static bool operator ==(SizeInt32 left, SizeInt32 right) => left.Equals(right);

	public static bool operator !=(SizeInt32 left, SizeInt32 right) => !left.Equals(right);
}
