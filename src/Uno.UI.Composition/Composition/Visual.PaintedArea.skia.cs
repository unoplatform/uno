#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

// What this visual's own content paints (children excluded), in its local coordinates, at two precisions: cheap
// bounds for damage tracking, and the exact area for the native-element clip, which cuts native elements only where
// XAML paints over them. Both describe the same painting; a visual overriding one should override the other.
public partial class Visual
{
	// Antialiasing partly paints the pixels along the painted edges, and hosts turn the native clip into a region by
	// pixel centers: without a margin a thin ring at a fractional offset could lose every pixel to the native element.
	private const float NativeClipAntialiasingMargin = 1f;

	private bool _hasPaintedGeometry;
	private bool _isPaintedGeometryKnown;
	private IGeometry? _paintedGeometry;
	private float _nativeClipCutScale;
	private IGeometry? _nativeClipCut;

	/// <summary>
	/// Bounds of what this visual paints itself, in local coordinates; an empty rect when it paints nothing.
	/// False when it can't be bounded more tightly than its clip.
	/// </summary>
	private protected virtual bool TryGetPaintedBounds(out Rect bounds)
	{
		bounds = default;
		if (!CanPaint())
		{
			return true;
		}

		if (PaintsWithinOwnSize)
		{
			bounds = new Rect(0, 0, Math.Max(0f, Size.X), Math.Max(0f, Size.Y));
			return true;
		}

		return false;
	}

	/// <summary>
	/// Builds the exact area this visual paints itself, in local coordinates: null when it paints nothing. False when
	/// it isn't known more tightly than <see cref="TryGetPaintedBounds"/>. Translucent paint counts as painted.
	/// Only called once per paint invalidation, so it may build geometry; the caller owns the result.
	/// </summary>
	private protected virtual bool TryBuildPaintedGeometry(out IGeometry? geometry)
	{
		geometry = null;
		return false;
	}

	/// <summary>
	/// The area cut out of native elements under this visual, in local coordinates and outset by the antialiasing
	/// margin at the given device scale; null when nothing is painted. False when the exact area isn't known.
	/// The result is cached until the paint is invalidated and must not be released by the caller.
	/// </summary>
	private bool TryGetNativeClipCut(float scale, out IGeometry? cut)
	{
		if (!_hasPaintedGeometry)
		{
			_hasPaintedGeometry = true;
			_isPaintedGeometryKnown = TryBuildPaintedGeometry(out _paintedGeometry);
		}

		cut = null;
		if (!_isPaintedGeometryKnown)
		{
			return false;
		}

		if (_paintedGeometry is not { IsEmpty: false } painted)
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
		_hasPaintedGeometry = false;
		_isPaintedGeometryKnown = false;
		_paintedGeometry?.Release();
		_paintedGeometry = null;
		_nativeClipCut?.Release();
		_nativeClipCut = null;
	}
}
