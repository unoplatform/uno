#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

// What this visual's own content paints (children excluded), in its local coordinates. It is computed once per paint
// invalidation: damage tracking uses its bounds (rects are far cheaper to clip to than paths), and the native-element
// clip uses its exact geometry, so native elements are only cut out where XAML actually paints over them.
public partial class Visual
{
	// Antialiasing partly paints the pixels along the painted edges, and hosts turn the native clip into a region by
	// pixel centers: without a margin a thin ring at a fractional offset could lose every pixel to the native element.
	private const float NativeClipAntialiasingMargin = 1f;

	private enum PaintedAreaKind : byte
	{
		// Can't be bounded more tightly than the visual's clip.
		Unbounded,
		// Bounded, but the exact area isn't known.
		BoundsOnly,
		// The exact area is known; null geometry when nothing is painted.
		Exact,
	}

	// The geometry stays owned by the visual: callers must not release it.
	private readonly record struct PaintedArea(PaintedAreaKind Kind, Rect Bounds, IGeometry? Geometry);

	private PaintedArea? _paintedArea;
	private float _nativeClipCutScale;
	private IGeometry? _nativeClipCut;

	/// <summary>
	/// Builds the exact area this visual paints itself, in local coordinates: null when it paints nothing. False when
	/// it isn't known, in which case the visual is bounded by its Size when it <see cref="PaintsWithinOwnSize"/>.
	/// Translucent paint counts as painted. Only called when <see cref="CanPaint"/>, once per paint invalidation;
	/// the caller owns the result.
	/// </summary>
	private protected virtual bool TryBuildPaintedGeometry(out IGeometry? geometry)
	{
		geometry = null;
		return false;
	}

	/// <summary>
	/// What this visual paints itself, from the cache or computed now; cached until the paint is invalidated.
	/// </summary>
	private PaintedArea GetPaintedArea()
	{
		if (_paintedArea is { } cached)
		{
			return cached;
		}

		PaintedArea area;
		if (!CanPaint())
		{
			area = new(PaintedAreaKind.Exact, default, null);
		}
		else if (TryBuildPaintedGeometry(out var geometry))
		{
			if (geometry is { IsEmpty: true })
			{
				geometry.Release();
				geometry = null;
			}

			area = new(PaintedAreaKind.Exact, geometry?.Bounds ?? default, geometry);
		}
		else if (PaintsWithinOwnSize)
		{
			area = new(PaintedAreaKind.BoundsOnly, new Rect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y)), null);
		}
		else
		{
			area = new(PaintedAreaKind.Unbounded, default, null);
		}

		_paintedArea = area;
		return area;
	}

	/// <summary>
	/// Bounds of what this visual paints itself, in local coordinates; an empty rect when it paints nothing.
	/// False when it can't be bounded more tightly than its clip.
	/// </summary>
	private protected bool TryGetPaintedBounds(out Rect bounds)
	{
		var area = GetPaintedArea();
		bounds = area.Bounds;
		return area.Kind != PaintedAreaKind.Unbounded;
	}

	/// <summary>
	/// The area cut out of native elements under this visual, in local coordinates and outset by the antialiasing
	/// margin at the given device scale; null when nothing is painted. False when the exact area isn't known.
	/// The result is cached until the paint is invalidated and must not be released by the caller.
	/// </summary>
	private bool TryGetNativeClipCut(float scale, out IGeometry? cut)
	{
		var area = GetPaintedArea();

		cut = null;
		if (area.Kind != PaintedAreaKind.Exact)
		{
			return false;
		}

		if (area.Geometry is not { } painted)
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
		_paintedArea?.Geometry?.Release();
		_paintedArea = null;
		_nativeClipCut?.Release();
		_nativeClipCut = null;
	}
}
