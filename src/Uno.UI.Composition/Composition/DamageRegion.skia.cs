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
	// Past this many points the accumulated shape stops paying for itself, both here and later when it
	// is used as a clip, and collapses to its bounding rect. A scroll frame contributes one rect per
	// moved visual and reaches this almost immediately — which is the right answer, since the union of
	// a scrolled subtree is its scroll port.
	private const int MaxPoints = 256;
	private const int PointsPerRect = 4;

	private SKPathBuilder _rects = NewRectBuilder();
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
			_rects.Dispose();
			_rects = NewRectBuilder();
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
		if (_hasRects)
		{
			_rects.Dispose();
			_rects = NewRectBuilder();
		}

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
