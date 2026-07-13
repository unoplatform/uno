#nullable enable

using System.Numerics;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Entry point for a pluggable 2D drawing backend. The default implementation is backed by SkiaSharp
/// (<see cref="SkiaDrawingBackend"/>); a host or experiment can supply an alternative via
/// <see cref="DrawingBackend.Register"/>.
/// </summary>
/// <remarks>
/// This is the resource-factory half of the abstraction: it manufactures the stateful handles
/// (geometry today; images, typefaces and shaders later) that cross the backend boundary. Transient draw
/// configuration (paint) is passed inline on the drawing-session verbs instead of being manufactured here.
/// </remarks>
internal interface IDrawingBackend
{
	/// <summary>Creates a builder used to construct an <see cref="IGeometry"/>.</summary>
	IPathBuilder CreatePathBuilder();

	/// <summary>Creates a linear-gradient shader in the current coordinate space.</summary>
	IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>Creates a color filter that multiplies alpha by <paramref name="opacity"/>, or null when it would be a no-op.</summary>
	IColorFilter? CreateOpacityColorFilter(float opacity);
}
