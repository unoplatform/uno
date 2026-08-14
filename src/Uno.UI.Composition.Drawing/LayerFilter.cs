#nullable enable

using System.Numerics;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A neutral, value-type description of the filter a layer applies (via <see cref="IDrawingSession.SaveLayer(in
/// LayerFilter)"/> or <see cref="IDrawingSession.DrawEffectBackdrop(in LayerFilter, float)"/>): a gaussian blur
/// (<see cref="SigmaX"/>/<see cref="SigmaY"/>, <see cref="ClampEdge"/>), optionally shifted by <see cref="Offset"/>
/// and alpha-tinted by <see cref="Tint"/> — which together express a drop shadow.
/// </summary>
/// <remarks>
/// Pure parameters, no device resource — a backend reads the fields directly and builds its blur (no downcast).
/// This replaces the opaque, backend-realized <c>IEffectFilter</c>: an effect graph is evaluated into drawing-session
/// calls, so the only "filter" a layer ever needs is this blur/shadow.
/// </remarks>
public readonly record struct LayerFilter(float SigmaX, float SigmaY, bool ClampEdge, Vector2 Offset, Color? Tint)
{
	/// <summary>A plain gaussian blur (no offset/tint).</summary>
	public static LayerFilter Blur(float sigmaX, float sigmaY, bool clampEdge)
		=> new(sigmaX, sigmaY, clampEdge, Vector2.Zero, null);

	/// <summary>A drop shadow: blur the content, offset it by (<paramref name="dx"/>, <paramref name="dy"/>), tint by <paramref name="color"/>.</summary>
	public static LayerFilter DropShadow(float dx, float dy, float sigmaX, float sigmaY, Color color)
		=> new(sigmaX, sigmaY, false, new Vector2(dx, dy), color);
}
