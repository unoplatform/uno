using System.Numerics;
using Windows.Foundation;

#if IS_UNO_COMPOSITION
namespace Uno.UI.Composition;
#else
namespace Windows.UI.Xaml;
#endif

/// <summary>
/// Represents a potentially non-uniform CornerRadius.
/// </summary>
/// <param name="TopLeft">Top left corner.</param>
/// <param name="TopRight">Top right corner.</param>
/// <param name="BottomRight">Bottom right corner.</param>
/// <param name="BottomLeft">Bottom left corner.</param>
internal partial record struct NonUniformCornerRadius
(
	Vector2 TopLeft,
	Vector2 TopRight,
	Vector2 BottomRight,
	Vector2 BottomLeft
)
{
	public bool IsEmpty =>
		TopLeft == Vector2.Zero &&
		TopRight == Vector2.Zero &&
		BottomRight == Vector2.Zero &&
		BottomLeft == Vector2.Zero;
}
