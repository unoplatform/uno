#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

// What this visual's own content paints (children excluded), in its local coordinates, cached until the paint is
// invalidated. Damage tracking needs only its rect bounds, on every paint invalidation; the native-element clip needs
// its exact geometry, so native elements are only cut out where XAML actually paints over them. The two are computed
// separately: the exact geometry can cost far more than its bounds (a union over every shape of a ShapeVisual).
public partial class Visual
{
	// Antialiasing partly paints the pixels along the painted edges, and hosts turn the native clip into a region by
	// pixel centers: without a margin a thin ring at a fractional offset could lose every pixel to the native element.
	private const float NativeClipAntialiasingMargin = 1f;

	/// <summary>
	/// The rect bounds of what a visual paints itself. Null: it can't be bounded more tightly than its clip; an empty
	/// rect: it paints nothing.
	/// </summary>
	private readonly record struct PaintedBounds(Rect? Bounds);

	/// <summary>
	/// The exact area a visual paints itself, owned by the visual. Null geometry: it paints nothing, or the area isn't
	/// known when not <paramref name="IsKnown"/>.
	/// </summary>
	private readonly record struct PaintedGeometry(bool IsKnown, IGeometry? Geometry);

	private PaintedBounds? _paintedBounds;
	private PaintedGeometry? _paintedGeometry;
	private float _nativeClipCutScale;
	private IGeometry? _nativeClipCut;

	/// <summary>
	/// Builds the exact area this visual paints itself, in local coordinates: null when it paints nothing. False when
	/// it isn't known. Translucent paint counts as painted. Only called when <see cref="CanPaint"/>, once per paint
	/// invalidation; the caller owns the result.
	/// </summary>
	private protected virtual bool TryBuildPaintedGeometry(out IGeometry? geometry)
	{
		geometry = null;
		return false;
	}

	/// <summary>
	/// Builds the rect bounds of what <see cref="TryBuildPaintedGeometry"/> builds, without building it: an empty rect
	/// when nothing is painted. False when this visual has no cheaper way than the exact geometry. Only called when
	/// <see cref="CanPaint"/> and the exact geometry isn't already built.
	/// </summary>
	private protected virtual bool TryBuildPaintedBounds(out Rect bounds)
	{
		bounds = default;
		return false;
	}

	/// <summary>
	/// The exact area this visual paints itself, in local coordinates; null when it paints nothing. False when it isn't
	/// known. Cached until the paint is invalidated; the caller must not release it.
	/// </summary>
	private bool TryGetPaintedGeometry(out IGeometry? geometry)
	{
		if (_paintedGeometry is not { } cached)
		{
			if (!CanPaint())
			{
				cached = new(IsKnown: true, null);
			}
			else if (TryBuildPaintedGeometry(out var built))
			{
				if (built is { IsEmpty: true })
				{
					built.Release();
					built = null;
				}
				cached = new(IsKnown: true, built);
			}
			else
			{
				cached = new(IsKnown: false, null);
			}
			_paintedGeometry = cached;
		}

		geometry = cached.Geometry;
		return cached.IsKnown;
	}

	/// <summary>
	/// Bounds of what this visual paints itself, in local coordinates; an empty rect when it paints nothing. False when
	/// it can't be bounded more tightly than its clip. Cached until the paint is invalidated.
	/// </summary>
	private protected bool TryGetPaintedBounds(out Rect bounds)
	{
		if (_paintedBounds is not { } cached)
		{
			cached = new(BuildPaintedBounds());
			_paintedBounds = cached;
		}

		bounds = cached.Bounds ?? default;
		return cached.Bounds is not null;
	}

	private Rect? BuildPaintedBounds()
	{
		if (!CanPaint())
		{
			return default(Rect);
		}

		// Already built, the exact geometry gives the tightest bounds for free; otherwise build the bounds alone when
		// the visual can, and only fall back to building the geometry when it can't.
		if (_paintedGeometry is not { IsKnown: true } && TryBuildPaintedBounds(out var bounds))
		{
			return bounds;
		}

		if (TryGetPaintedGeometry(out var geometry))
		{
			return geometry?.Bounds ?? default(Rect);
		}

		return PaintsWithinOwnSize ? new Rect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y)) : (Rect?)null;
	}

	/// <summary>
	/// The area cut out of native elements under this visual, in local coordinates and outset by the antialiasing
	/// margin at the given device scale; null when nothing is painted. False when the exact area isn't known.
	/// The result is cached until the paint is invalidated and must not be released by the caller.
	/// </summary>
	private bool TryGetNativeClipCut(float scale, out IGeometry? cut)
	{
		cut = null;
		if (_paintedBounds is { Bounds: { } bounds } && IsRectEmpty(bounds))
		{
			return true;
		}

		if (!TryGetPaintedGeometry(out var painted))
		{
			return false;
		}

		if (painted is null)
		{
			return true;
		}

		if (_nativeClipCut is null || _nativeClipCutScale != scale)
		{
			_nativeClipCut?.Release();
			// Outset by one device pixel whatever the scale this visual is drawn at.
			using var fringe = painted.GetStrokeFillGeometry(new StrokeStyle
			{
				Thickness = 2 * NativeClipAntialiasingMargin / Math.Max(scale, float.Epsilon),
				StartCap = StrokeCap.Round,
				EndCap = StrokeCap.Round,
				LineJoin = StrokeJoin.Round,
			});
			_nativeClipCut = painted.Combine(fringe, GeometryCombineMode.Union);
			_nativeClipCutScale = scale;
		}

		cut = _nativeClipCut;
		return true;
	}

	private void InvalidatePaintedArea()
	{
		_paintedGeometry?.Geometry?.Release();
		_paintedGeometry = null;
		_paintedBounds = null;
		_nativeClipCut?.Release();
		_nativeClipCut = null;
	}
}
