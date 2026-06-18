#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates, for a single frame, the region of the surface whose visual output changed since the
/// previous presented frame, for damage-region rendering. The region is a possibly-disjoint union of
/// arbitrary shapes (not merged into a single bounding rectangle), so the gaps between far-apart changes
/// (e.g. a TextBox near the top and a ListView near the bottom) are not repainted, and non-rectangular
/// clips (rounded corners, curves) are honored. To bound cost, once too many distinct regions accumulate
/// the whole surface is repainted instead.
/// </summary>
internal sealed class DamageRegion
{
	// Beyond this many distinct contributions in a frame, a precise per-region clip isn't worth its cost.
	// Instead of repainting the whole surface, collapse to the bounding box of everything accumulated so far
	// (one cheap rectangular clip) and keep extending that box. This is correct (a superset of the exact
	// region) and far tighter than a full-surface repaint — e.g. a scroll, where every visible row moves and
	// contributes its own region, stays clipped to the scrolled viewport instead of the whole window.
	private const int MaxRegions = 64;

	private SKPath _region = new();
	private SKPath _spareUnion = new();
	private readonly SKPath _scratch = new();
	private int _count;
	private bool _coalescedToBounds;

	/// <summary>The whole surface must be repainted this frame (e.g. resize, or too many scattered changes).</summary>
	public bool IsFullFrame { get; private set; }

	/// <summary>Nothing changed this frame and a full repaint is not required.</summary>
	public bool IsEmpty => !IsFullFrame && _count == 0;

	/// <summary>Diagnostic: the number of distinct contributions accumulated this frame (capped at <see cref="MaxRegions"/>).</summary>
	public int RegionCount => _count;

	/// <summary>The bounding box of the changed region (valid only when not empty / not full-frame).</summary>
	public SKRect Bounds => _region.Bounds;

	/// <summary>
	/// The accumulated changed region as a path (valid only when not empty / not full-frame). When it is a
	/// single rectangle, <see cref="IsRect"/> returns it so the consumer can take a cheaper rectangular path.
	/// </summary>
	public SKPath Region => _region;

	/// <summary>True (and outputs the rectangle) when the whole region is a single axis-aligned rectangle.</summary>
	public bool IsRect(out SKRect rect)
	{
		rect = _region.Bounds; // for a rectangular path, Bounds is exactly the rectangle
		return _region.IsRect;
	}

	public void AddRect(SKRect rect)
	{
		if (IsFullFrame || rect.IsEmpty)
		{
			return;
		}

		_scratch.Rewind();
		_scratch.AddRect(rect);
		Union(_scratch);
	}

	public void AddPath(SKPath region)
	{
		if (IsFullFrame || region.IsEmpty)
		{
			return;
		}

		Union(region);
	}

	private void Union(SKPath addition)
	{
		if (_coalescedToBounds)
		{
			// Already a single bounding rect; just extend it to include the new contribution (stays one rect).
			ExtendBounds(addition.Bounds);
			return;
		}

		if (_count >= MaxRegions)
		{
			// Too many distinct contributions to keep clipping precisely. Collapse everything accumulated so
			// far to its bounding box and switch to extending that box from here on.
			var bounds = SKRect.Union(_region.Bounds, addition.Bounds);
			_region.Rewind();
			_region.AddRect(bounds);
			_coalescedToBounds = true;
			_count = 1;
			return;
		}

		if (_count == 0)
		{
			_region.AddPath(addition);
		}
		else
		{
			// A true geometric union keeps disjoint shapes disjoint and coalesces overlapping/adjacent ones,
			// regardless of contour winding (so the clip never develops a hole that would drop a repaint).
			_region.Op(addition, SKPathOp.Union, _spareUnion);
			(_region, _spareUnion) = (_spareUnion, _region);
		}

		_count++;
	}

	private void ExtendBounds(SKRect addition)
	{
		var bounds = SKRect.Union(_region.Bounds, addition);
		_region.Rewind();
		_region.AddRect(bounds);
	}

	public void SetFullFrame()
	{
		IsFullFrame = true;
		_region.Rewind();
		_count = 0;
		_coalescedToBounds = false;
	}

	public void Reset()
	{
		_region.Rewind();
		_count = 0;
		IsFullFrame = false;
		_coalescedToBounds = false;
	}
}
