using Windows.Foundation;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents a potentially non-uniform CornerRadius.
/// </summary>
/// <param name="TopLeft">Top left corner.</param>
/// <param name="TopRight">Top right corner.</param>
/// <param name="BottomRight">Bottom right corner.</param>
/// <param name="BottomLeft">Bottom left corner.</param>
internal partial record struct NonUniformCornerRadius
(
	Point TopLeft,
	Point TopRight,
	Point BottomRight,
	Point BottomLeft
)
{
	public bool IsEmpty =>
		TopLeft == Point.Zero &&
		TopRight == Point.Zero &&
		BottomRight == Point.Zero &&
		BottomLeft == Point.Zero;
}
