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
		NotComputed,
		// Can't be bounded more tightly than the visual's clip.
		Unbounded,
		// Bounded, but the exact area isn't known.
		BoundsOnly,
		// The exact area is known; null geometry when nothing is painted.
		Exact,
	}

	private PaintedAreaKind _paintedAreaKind;
	private Rect _paintedBounds;
	private IGeometry? _paintedGeometry;
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
	/// Bounds of what this visual paints itself, in local coordinates; an empty rect when it paints nothing.
	/// False when it can't be bounded more tightly than its clip.
	/// </summary>
	private protected bool TryGetPaintedBounds(out Rect bounds)
	{
		EnsurePaintedArea();
		bounds = _paintedBounds;
		return _paintedAreaKind != PaintedAreaKind.Unbounded;
	}

	private void EnsurePaintedArea()
	{
		if (_paintedAreaKind != PaintedAreaKind.NotComputed)
		{
			return;
		}

		_paintedBounds = default;
		if (!CanPaint())
		{
			_paintedAreaKind = PaintedAreaKind.Exact;
		}
		else if (TryBuildPaintedGeometry(out var geometry))
		{
			_paintedAreaKind = PaintedAreaKind.Exact;
			if (geometry is { IsEmpty: false })
			{
				_paintedGeometry = geometry;
				_paintedBounds = geometry.Bounds;
			}
			else
			{
				geometry?.Release();
			}
		}
		else if (PaintsWithinOwnSize)
		{
			_paintedAreaKind = PaintedAreaKind.BoundsOnly;
			_paintedBounds = new Rect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
		}
		else
		{
			_paintedAreaKind = PaintedAreaKind.Unbounded;
		}
	}

	/// <summary>
	/// The area cut out of native elements under this visual, in local coordinates and outset by the antialiasing
	/// margin at the given device scale; null when nothing is painted. False when the exact area isn't known.
	/// The result is cached until the paint is invalidated and must not be released by the caller.
	/// </summary>
	private bool TryGetNativeClipCut(float scale, out IGeometry? cut)
	{
		EnsurePaintedArea();

		cut = null;
		if (_paintedAreaKind != PaintedAreaKind.Exact)
		{
			return false;
		}

		if (_paintedGeometry is not { } painted)
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
		_paintedAreaKind = PaintedAreaKind.NotComputed;
		_paintedGeometry?.Release();
		_paintedGeometry = null;
		_nativeClipCut?.Release();
		_nativeClipCut = null;
	}
}
