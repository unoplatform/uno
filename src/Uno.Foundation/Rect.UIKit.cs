using CoreGraphics;

namespace Windows.Foundation;

public partial struct Rect
{
	/// <summary>
	/// Converts this <see cref="Rect"/> to a <see cref="CGRect"/>.
	/// </summary>
	public CGRect ToCGRect() => new(X, Y, Width, Height);

	public static implicit operator Rect(CGRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

	public static implicit operator CGRect(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
