#nullable enable

using System.ComponentModel;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Immediate-mode, stateful drawing surface — the canvas-verb half of the pluggable-backend abstraction.
/// Each verb takes exactly the paint inputs it honors, and geometry is passed as an <see cref="IGeometry"/> handle.
/// Retained-mode concerns (display-list recording/replay) are intentionally a backend implementation detail, not on this interface.
/// </summary>
/// <summary>One placed instance of a shared geometry: the outline plus where to draw it.</summary>
public readonly record struct PathInstance(IGeometry Geometry, Vector2 Offset);

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
	void SaveLayer();

	/// <summary>Begins an offscreen layer whose content is transformed by <paramref name="colorFilter"/> on restore.</summary>
	void SaveLayer(IColorFilter colorFilter);

	/// <summary>Begins an offscreen layer composited back as a <c>DstIn</c> alpha mask on restore — the drawn
	/// content keeps only the pixels covered by a subsequently-drawn mask's alpha. (This is the only blend the
	/// layer path needs; general blend/composite modes belong to the effect and color-filter seams.)</summary>
	void SaveLayerMask();

	/// <summary>
	/// Begins an offscreen layer whose content is transformed by <paramref name="filter"/> when the matching
	/// <see cref="Restore"/> composites it back (e.g. a drop shadow derived from the drawn content).
	/// </summary>
	void SaveLayer(IEffectFilter filter);

	void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect);

	void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect);

	void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect);

	void Clear(Color color);

	/// <summary>Fills <paramref name="rect"/> with a solid <paramref name="color"/> (bake any opacity into its alpha).</summary>
	void DrawRect(in Rect rect, Color color);

	/// <summary>Fills <paramref name="rect"/> with <paramref name="shader"/> (which carries its own alpha).</summary>
	void DrawRect(in Rect rect, IShader shader);

	/// <summary>
	/// Fills a rounded rectangle with a solid <paramref name="color"/>; <paramref name="radii"/> are the per-corner
	/// radii in (TopLeft, TopRight, BottomRight, BottomLeft) order.
	/// </summary>
	void DrawRoundedRect(in Rect rect, Vector4 radii, Color color);

	/// <summary>
	/// Fills the annulus between an outer and inner rounded rectangle with a solid <paramref name="color"/> — a
	/// rounded border in one shape. Per-corner radii in (TopLeft, TopRight, BottomRight, BottomLeft) order.
	/// </summary>
	void DrawRoundedRectBorder(in Rect outer, Vector4 outerRadii, in Rect inner, Vector4 innerRadii, Color color);

	/// <summary>Fills <paramref name="geometry"/> with a solid <paramref name="color"/> (bake any opacity into its alpha).</summary>
	void DrawPath(IGeometry geometry, Color color);

	/// <summary>
	/// Fills <paramref name="geometry"/> translated by <paramref name="offset"/>, without disturbing the transform
	/// stack. Lets a caller place MANY instances of one geometry — a glyph reused across a run, a repeated icon —
	/// so the instances can share a single cached geometry, and so a backend is free to coalesce a consecutive run
	/// of them into one draw. The default routes through Save/Translate/Restore, which is correct but costs four
	/// session calls per instance; override it where that matters.
	/// <para>
	/// CONTRACT: <paramref name="geometry"/> must outlive the session and any recording made from it — it is
	/// referenced, not copied. That is the point: a snapshot per instance would reintroduce exactly the
	/// duplication instancing removes. Pass geometry owned by a cache (immutable, not disposed by the caller),
	/// never a transient built for one draw.
	/// </para>
	/// </summary>
	void DrawPath(IGeometry geometry, Color color, Vector2 offset)
	{
		Save();
		Translate(offset.X, offset.Y);
		DrawPath(geometry, color);
		Restore();
	}

	/// <summary>
	/// Fills a whole run of placed geometries in ONE call, all in <paramref name="color"/>. Handing the backend the
	/// entire run — rather than one call per instance — is what lets it merge them into a single draw; a lazy
	/// per-call batch would need an end-of-run signal this interface has no place to put (the session is not
	/// disposable). Same sharing contract as the single-instance overload: the geometries are referenced, not
	/// copied, so they must outlive the session and any recording made from it.
	/// </summary>
	void DrawPaths(ReadOnlySpan<PathInstance> instances, Color color)
	{
		foreach (var i in instances)
		{
			DrawPath(i.Geometry, color, i.Offset);
		}
	}

	/// <summary>
	/// Draws <paramref name="silhouette"/> as a soft shadow: coverage blurred by (<paramref name="sigmaX"/>,
	/// <paramref name="sigmaY"/>) device pixels and filled with <paramref name="color"/>; <paramref name="additive"/>
	/// sums overlapping contributions.
	/// </summary>
	void DrawShadow(IGeometry silhouette, Color color, float sigmaX, float sigmaY, bool additive);

	/// <summary>Strokes the outline of <paramref name="geometry"/>.</summary>
	void StrokePath(IGeometry geometry, Color color, float strokeWidth);

	void DrawLine(Vector2 p0, Vector2 p1, Color color, float strokeWidth);

	/// <summary>Draws <paramref name="texture"/> with its top-left at (<paramref name="x"/>, <paramref name="y"/>), modulated by <paramref name="opacity"/>.</summary>
	void DrawImage(ITexture texture, float x, float y, float opacity = 1f);

	/// <summary>Draws <paramref name="texture"/> with a <paramref name="colorFilter"/> applied (e.g. a monochrome tint).</summary>
	void DrawImage(ITexture texture, float x, float y, IColorFilter colorFilter);

	/// <summary>
	/// Draws <paramref name="texture"/> stretched into <paramref name="destination"/> as a nine-slice: the
	/// <paramref name="centerSlice"/> rectangle (in image pixels) defines the fixed corners / stretchable
	/// edges and center. When <paramref name="centerHollow"/> is true the center slice is not drawn.
	/// </summary>
	void DrawImageNineSlice(ITexture texture, in Rect centerSlice, in Rect destination, bool centerHollow);

	/// <summary>
	/// Applies <paramref name="filter"/> to the current surface content as an effect-brush backdrop, modulated
	/// by <paramref name="opacity"/>. Mirrors WinUI effect-brush paint semantics.
	/// </summary>
	void DrawEffectBackdrop(IEffectFilter filter, float opacity);
}
