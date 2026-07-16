#nullable enable

using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Immediate-mode, stateful drawing surface — the canvas-verb half of the pluggable-backend abstraction
/// (Track 1). It mirrors the drawing surface the composition layer draws against today. Each verb takes
/// exactly the paint inputs it honors (rather than one combined paint struct), and geometry is passed as
/// an <see cref="IGeometry"/> handle.
/// </summary>
/// <remarks>
/// Retained-mode concerns (display-list recording/replay, e.g. SkiaSharp's SKPicture) are intentionally
/// NOT on this interface — they are an implementation detail of the backend behind it.
/// </remarks>
internal interface IDrawingSession
{
	/// <summary>The current total transform, from the drawing origin to the current coordinate space.</summary>
	Matrix4x4 TotalMatrix { get; }

	void SetMatrix(in Matrix4x4 matrix);

	void Concat(in Matrix4x4 matrix);

	void Translate(float dx, float dy);

	void Scale(float sx, float sy);

	/// <summary>Pushes the current state and returns the count to restore back to.</summary>
	int Save();

	int SaveCount { get; }

	void Restore();

	void RestoreToCount(int count);

	/// <summary>Begins an offscreen layer, optionally bounded and with a compositing paint applied on restore.</summary>
	void SaveLayer(Rect? bounds = null, bool antialias = false, float opacity = 1f, IColorFilter? colorFilter = null, BlendMode blendMode = BlendMode.SrcOver);

	/// <summary>
	/// Begins an offscreen layer whose content is transformed by <paramref name="filter"/> when the matching
	/// <see cref="Restore"/> composites it back (e.g. a drop shadow derived from the drawn content).
	/// </summary>
	void SaveLayer(IEffectFilter filter);

	void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false);

	void Clear(Color color);

	/// <summary>Fills <paramref name="rect"/> with a solid <paramref name="color"/> or, if given, a <paramref name="shader"/>.</summary>
	void DrawRect(in Rect rect, Color color, bool antialias = false, float opacity = 1f, IShader? shader = null, IMaskFilter? maskFilter = null, BlendMode blendMode = BlendMode.SrcOver);

	/// <summary>Fills <paramref name="geometry"/> with a solid <paramref name="color"/> or, if given, a <paramref name="shader"/>.</summary>
	void DrawPath(IGeometry geometry, Color color, bool antialias = false, float opacity = 1f, IShader? shader = null, IMaskFilter? maskFilter = null, BlendMode blendMode = BlendMode.SrcOver);

	/// <summary>Strokes the outline of <paramref name="geometry"/>.</summary>
	void StrokePath(IGeometry geometry, Color color, float strokeWidth, bool antialias = false);

	void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth, bool antialias = false);

	/// <summary>Draws <paramref name="image"/> with its top-left at (<paramref name="x"/>, <paramref name="y"/>) in the current coordinate space.</summary>
	void DrawImage(IImage image, float x, float y, ImageSampling sampling, bool antialias = false, float opacity = 1f, IColorFilter? colorFilter = null, BlendMode blendMode = BlendMode.SrcOver);

	/// <summary>
	/// Draws <paramref name="image"/> stretched into <paramref name="destination"/> as a nine-slice: the
	/// <paramref name="centerSlice"/> rectangle (in image pixels) defines the fixed corners / stretchable
	/// edges and center. When <paramref name="centerHollow"/> is true the center slice is not drawn.
	/// </summary>
	void DrawImageNineSlice(IImage image, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false, IColorFilter? colorFilter = null);

	/// <summary>
	/// Applies <paramref name="filter"/> to the current surface content as an effect-brush backdrop:
	/// a transparent offscreen layer whose backdrop is the filtered content, optionally modulated by
	/// <paramref name="opacity"/>. Mirrors the WinUI effect-brush paint semantics.
	/// </summary>
	void DrawEffectBackdrop(IEffectFilter filter, float opacity);
}
