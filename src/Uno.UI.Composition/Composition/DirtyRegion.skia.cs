#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates, for a single frame, the region of the surface whose visual output changed since the
/// previous presented frame, for dirty-rectangles rendering. The region is a possibly-disjoint union of
/// arbitrary shapes (not merged into a single bounding rectangle), so the gaps between far-apart changes
/// (e.g. a TextBox near the top and a ListView near the bottom) are not repainted, and non-rectangular
/// clips (rounded corners, curves) are honored. To bound cost, once too many distinct regions accumulate
/// the whole surface is repainted instead.
/// </summary>
internal sealed class DirtyRegion
{
	// Beyond this many distinct contributions in a frame, coalescing into a precise region isn't worth it;
	// repaint the whole surface instead.
	private const int MaxRegions = 64;

	private SKPath _region = new();
	private SKPath _spareUnion = new();
	private readonly SKPath _scratch = new();
	private int _count;

	/// <summary>The whole surface must be repainted this frame (e.g. resize, or too many scattered changes).</summary>
	public bool IsFullFrame { get; private set; }

	/// <summary>Nothing changed this frame and a full repaint is not required.</summary>
	public bool IsEmpty => !IsFullFrame && _count == 0;

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
		if (_count >= MaxRegions)
		{
			SetFullFrame();
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

	public void SetFullFrame()
	{
		IsFullFrame = true;
		_region.Rewind();
		_count = 0;
	}

	public void Reset()
	{
		_region.Rewind();
		_count = 0;
		IsFullFrame = false;
	}
}
