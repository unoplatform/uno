#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates the region that must be repainted this frame.
/// </summary>
/// <remarks>
/// Rect contributions — every visual that only moved, which during a scroll is the whole subtree —
/// are appended to a builder under the nonzero fill rule, where overlapping same-direction contours
/// already clip to their union. Running a path boolean per contribution instead would be
/// O(visuals) boolean ops against a monotonically growing path.
/// </remarks>
internal sealed class DamageRegion : IDisposable
{
	// The accumulated region is handed to SKCanvas.ClipPath, and a many-contour clip can cost a
	// GPU-side mask allocation per frame — on a tiled mobile GPU that is enough to exhaust the
	// driver's mappable memory. Keep the contour count low and collapse to the bounding rect beyond
	// it. During a scroll that costs nothing: every moved visual is inside the scroll port, so the
	// union is the scroll port either way.
	private const int MaxPoints = 32;
	private const int PointsPerRect = 4;

	private readonly SKPathBuilder _rects = NewRectBuilder();
	private readonly SKPath _exact = new();

	private SKRect _rectBounds;
	private int _rectPoints;
	private bool _hasRects;

	private static SKPathBuilder NewRectBuilder() => new() { FillType = SKPathFillType.Winding };

	internal bool IsEmpty => !_hasRects && _exact.IsEmpty;

	internal void UnionRect(SKRect rect)
	{
		if (rect.IsEmpty)
		{
			return;
		}

		if (_hasRects && _rectPoints >= MaxPoints)
		{
			var collapsed = SKRect.Union(_rectBounds, rect);
			_rects.Reset();
			_rects.FillType = SKPathFillType.Winding;
			_rects.AddRect(collapsed);
			_rectBounds = collapsed;
			_rectPoints = PointsPerRect;
			return;
		}

		_rects.AddRect(rect);
		_rectBounds = _hasRects ? SKRect.Union(_rectBounds, rect) : rect;
		_rectPoints += PointsPerRect;
		_hasRects = true;
	}

	/// <summary>Adds a contribution whose exact geometry matters (a repainted visual, not a moved one).</summary>
	internal void Union(SKPath addition)
	{
		if (addition.IsEmpty)
		{
			return;
		}

		// A real boolean here: an arbitrary path's contour directions are unknown, so appending it to
		// the nonzero builder could cancel against an overlapping contour and under-damage.
		if (_exact.IsEmpty)
		{
			addition.Transform(SKMatrix.Identity, _exact);
		}
		else
		{
			_exact.Op(addition, SKPathOp.Union, _exact);
		}
	}

	internal void Reset()
	{
		// Reset, never dispose-and-recreate: the builder is reusable, and churning a native object per
		// frame is exactly the pressure this type exists to avoid.
		_rects.Reset();
		_rects.FillType = SKPathFillType.Winding;
		_exact.Reset();
		_rectBounds = default;
		_rectPoints = 0;
		_hasRects = false;
	}

	/// <summary>
	/// Writes the accumulated region into <paramref name="destination"/>, clamped to
	/// <paramref name="clampTo"/>, and resets this region for the next frame.
	/// </summary>
	internal void SnapshotAndReset(SKPath destination, SKRect clampTo)
	{
		destination.Reset();

		if (_hasRects)
		{
			using var rects = _rects.Detach();
			rects.Transform(SKMatrix.Identity, destination);

			if (!_exact.IsEmpty)
			{
				destination.Op(_exact, SKPathOp.Union, destination);
			}
		}
		else if (!_exact.IsEmpty)
		{
			_exact.Transform(SKMatrix.Identity, destination);
		}

		if (!destination.IsEmpty && !clampTo.Contains(destination.Bounds))
		{
			using var frame = Microsoft.UI.Composition.SkiaExtensions.CreateRectPath(clampTo);
			destination.Op(frame, SKPathOp.Intersect, destination);
		}

		Reset();
	}

	public void Dispose()
	{
		_rects.Dispose();
		_exact.Dispose();
	}
}
