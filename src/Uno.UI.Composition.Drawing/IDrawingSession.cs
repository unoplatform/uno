#nullable enable

using System.ComponentModel;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Immediate-mode, stateful drawing surface — the canvas-verb half of the pluggable-backend abstraction.
/// Each verb takes exactly the paint inputs it honors, and geometry is passed as an <see cref="IGeometry"/> handle.
/// Retained-mode concerns (display-list recording/replay) are intentionally a backend implementation detail, not on this interface.
/// </summary>
public interface IDrawingSession
{
	/// <summary>The current total transform, from the drawing origin to the current coordinate space.</summary>
	Matrix4x4 TotalMatrix { get; }

	/// <summary>
	/// The backend's live, directly-drawable native surface (e.g. a SkiaSharp <c>SKCanvas</c>), or <c>null</c> when
	/// the backend records neutral commands and exposes none. Type-erased; a consumer type-checks it before drawing.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	object? NativeSurface { get; }

	/// <summary>
	/// The backend factory that owns this session. Mint textures/resources through it so they are native to THIS
	/// session's backend (a foreign texture is not accepted); prefer it over a process-global factory.
	/// </summary>
	IDrawingFactory Factory { get; }

	void SetMatrix(in Matrix4x4 matrix);

	void Concat(in Matrix4x4 matrix);

	void Translate(float dx, float dy);

	void Scale(float sx, float sy);

	/// <summary>Pushes the current state and returns the count to restore back to.</summary>
	int Save();

	int SaveCount { get; }

	void Restore();

	void RestoreToCount(int count);

	/// <summary>Begins a plain offscreen layer.</summary>
	void SaveLayer(bool antialias = false);

	/// <summary>Begins an offscreen layer whose content is transformed by <paramref name="colorFilter"/> on restore.</summary>
	void SaveLayer(IColorFilter colorFilter, bool antialias = false);

	/// <summary>Begins an offscreen layer composited back with <paramref name="blendMode"/> on restore.</summary>
	void SaveLayer(BlendMode blendMode, bool antialias = false);

	/// <summary>
	/// Begins an offscreen layer whose content is transformed by <paramref name="filter"/> when the matching
	/// <see cref="Restore"/> composites it back (e.g. a drop shadow derived from the drawn content).
	/// </summary>
	void SaveLayer(IEffectFilter filter);

	void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void Clear(Color color);

	/// <summary>Fills <paramref name="rect"/> with a solid <paramref name="color"/> (bake any opacity into its alpha).</summary>
	void DrawRect(in Rect rect, Color color, bool antialias = false);

	/// <summary>Fills <paramref name="rect"/> with <paramref name="shader"/> (which carries its own alpha).</summary>
	void DrawRect(in Rect rect, IShader shader, bool antialias = false);

	/// <summary>
	/// Fills a rounded rectangle with a solid <paramref name="color"/>; <paramref name="radii"/> are the per-corner
	/// radii in (TopLeft, TopRight, BottomRight, BottomLeft) order.
	/// </summary>
	void DrawRoundedRect(in Rect rect, Vector4 radii, Color color, bool antialias = false);

	/// <summary>
	/// Fills the annulus between an outer and inner rounded rectangle with a solid <paramref name="color"/> — a
	/// rounded border in one shape. Per-corner radii in (TopLeft, TopRight, BottomRight, BottomLeft) order.
	/// </summary>
	void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color, bool antialias = false);

	/// <summary>Fills <paramref name="geometry"/> with a solid <paramref name="color"/> (bake any opacity into its alpha).</summary>
	void DrawPath(IGeometry geometry, Color color, bool antialias = false);

	/// <summary>
	/// Draws <paramref name="silhouette"/> as a soft shadow: coverage blurred by (<paramref name="sigmaX"/>,
	/// <paramref name="sigmaY"/>) device pixels and filled with <paramref name="color"/>; <paramref name="additive"/>
	/// sums overlapping contributions.
	/// </summary>
	void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive, bool antialias = false);

	/// <summary>Strokes the outline of <paramref name="geometry"/>.</summary>
	void StrokePath(IGeometry geometry, Color color, float strokeWidth, bool antialias = false);

	void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias = false);

	/// <summary>Draws <paramref name="texture"/> with its top-left at (<paramref name="x"/>, <paramref name="y"/>), modulated by <paramref name="opacity"/>.</summary>
	void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false);

	/// <summary>Draws <paramref name="texture"/> with a <paramref name="colorFilter"/> applied (e.g. a monochrome tint).</summary>
	void DrawImage(ITexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false);

	/// <summary>
	/// Draws <paramref name="texture"/> stretched into <paramref name="destination"/> as a nine-slice: the
	/// <paramref name="centerSlice"/> rectangle (in image pixels) defines the fixed corners / stretchable
	/// edges and center. When <paramref name="centerHollow"/> is true the center slice is not drawn.
	/// </summary>
	void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false);

	/// <summary>
	/// Applies <paramref name="filter"/> to the current surface content as an effect-brush backdrop, modulated
	/// by <paramref name="opacity"/>. Mirrors WinUI effect-brush paint semantics.
	/// </summary>
	void DrawEffectBackdrop(IEffectFilter filter, float opacity);
}
