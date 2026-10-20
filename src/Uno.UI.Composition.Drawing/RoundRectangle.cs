#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A rectangle with (possibly non-uniform) per-corner radii. Corner radii are (x, y) pairs, in the
/// same order Skia uses: top-left, top-right, bottom-right, bottom-left.
/// </summary>
public readonly record struct RoundRectangle
{
	public Rect Rect { get; init; }
	public Vector2 TopLeft { get; init; }
	public Vector2 TopRight { get; init; }
	public Vector2 BottomRight { get; init; }
	public Vector2 BottomLeft { get; init; }
}
