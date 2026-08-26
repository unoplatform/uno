#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates the region that must be repainted this frame.
/// </summary>
/// <remarks>
/// Rect contributions — every visual that only moved, which during a scroll is the whole subtree — are
/// appended to one builder under the nonzero fill rule, where overlapping same-direction contours already read
/// as their union. Running a path boolean per contribution would be O(visuals) booleans against a
/// monotonically growing path.
/// </remarks>
internal sealed class DamageRegion : IDisposable
{
	// The region ends up in SKCanvas.ClipPath, where a many-contour clip costs a mask the GPU has to allocate
	// every frame. Past this many contours, fuse everything regardless of what that over-damages.
	private const int MaxRects = 32;

	// Finished rect contours. The one still being grown is _open, which only joins them on snapshot.
	private readonly SKPathBuilder _rects = new() { FillType = SKPathFillType.Winding };
	private readonly SKPath _exact = new();

	private SKRect _open;
	private SKRect _allBounds;
	private int _rectCount;

	internal bool IsEmpty => _rectCount == 0 && _exact.IsEmpty;

	internal void UnionRect(SKRect rect)
	{
		// Not SKRect.IsEmpty: that is only true for an all-zero rect, so a zero-area or inverted one would reach
		// the bookkeeping below, where SKRect.Union drops it while SKPathBuilder.AddRect would normalize and
		// keep it — leaving the bounds no longer covering the contours.
		rect = rect.Standardized;
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		if (_rectCount == 0)
		{
			_open = _allBounds = rect;
			_rectCount = 1;
			return;
		}

		_allBounds = SKRect.Union(_allBounds, rect);

		// Growing the open rect keeps the clip closer to a single analytic rect, but it also damages everything
		// between that rect and this contribution. Only worth it while it costs no more than this contribution
		// already does — true of a scroll, where visuals arrive in tree order and land in the same port, and
		// false for damage elsewhere, which opens its own rect rather than fusing the two.
		var grown = SKRect.Union(_open, rect);
		if (Area(grown) <= Area(_open) + Area(rect))
		{
			_open = grown;
			return;
		}

		if (_rectCount >= MaxRects)
		{
			_rects.Reset();
			_open = _allBounds;
			_rectCount = 1;
			return;
		}

		_rects.AddRect(_open);
		_open = rect;
		_rectCount++;
	}

	/// <summary>Adds a contribution whose exact geometry matters (a repainted visual, not a moved one).</summary>
	internal void Union(SKPath addition)
	{
		if (addition.IsEmpty)
		{
			return;
		}

		// A real boolean here: an arbitrary path's contour directions are unknown, so appending it to the
		// nonzero builder could cancel against an overlapping contour and under-damage.
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
		// Reset, never dispose-and-recreate: the builder is reusable, and churning a native object per frame is
		// exactly the pressure this type exists to avoid. Reset() restores the Winding fill type.
		_rects.Reset();
		_exact.Reset();
		_open = default;
		_allBounds = default;
		_rectCount = 0;
	}

	/// <summary>
	/// Writes the accumulated region into <paramref name="destination"/>, clamped to <paramref name="clampTo"/>,
	/// and resets this region for the next frame.
	/// </summary>
	internal void SnapshotAndReset(SKPath destination, SKRect clampTo)
	{
		destination.Reset();

		// Detach() already emptied the builder, so a throw between here and Reset() would leave the region
		// claiming rects it no longer holds. Reset unconditionally to keep the next frame sane.
		try
		{
			if (_rectCount > 0)
			{
				_rects.AddRect(_open);
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
		}
		finally
		{
			Reset();
		}
	}

	// Frame-sized rects square to more than float can hold precisely.
	private static double Area(SKRect rect) => (double)rect.Width * rect.Height;

	public void Dispose()
	{
		_rects.Dispose();
		_exact.Dispose();
	}
}
