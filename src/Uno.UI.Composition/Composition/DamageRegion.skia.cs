#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Accumulates, for a single frame, the region of the surface whose visual output changed since the
/// previous presented frame, for damage-region rendering. The region is a possibly-disjoint union of
/// arbitrary shapes (not merged into a single bounding rectangle), so the gaps between far-apart changes
/// (e.g. a TextBox near the top and a ListView near the bottom) are not repainted, and non-rectangular
/// clips (rounded corners, curves) are honored.
/// </summary>
internal sealed class DamageRegion
{
	private SKPath _region = new();
	private SKPath _spareUnion = new();
	private readonly SKPath _scratch = new();

	/// <summary>The whole surface must be repainted this frame (e.g. on resize).</summary>
	public bool IsFullFrame { get; private set; }

	/// <summary>Nothing changed this frame and a full repaint is not required.</summary>
	public bool IsEmpty => !IsFullFrame && _region.IsEmpty;

	/// <summary>The accumulated changed region as a path (valid only when not empty / not full-frame).</summary>
	public SKPath Region => _region;

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

	/// <summary>Clips the accumulated region to <paramref name="frameRect"/> (nothing outside the frame is ever presented).</summary>
	public void ClampTo(SKRect frameRect)
	{
		if (IsFullFrame || _region.IsEmpty || frameRect.Contains(_region.Bounds))
		{
			return;
		}

		_scratch.Rewind();
		_scratch.AddRect(frameRect);
		_region.Op(_scratch, SKPathOp.Intersect, _spareUnion);
		(_region, _spareUnion) = (_spareUnion, _region);
	}

	private void Union(SKPath addition)
	{
		if (_region.IsEmpty)
		{
			_region.AddPath(addition);
		}
		else
		{
			// A true geometric union keeps disjoint shapes disjoint and coalesces overlapping/adjacent ones,
			// regardless of contour winding (so the clip never develops a hole that would drop a repaint).
			// Overlapping contributions (e.g. every visible row of a scrolling list) collapse here, so the
			// path's complexity tracks the actual changed geometry, not the number of contributions.
			_region.Op(addition, SKPathOp.Union, _spareUnion);
			(_region, _spareUnion) = (_spareUnion, _region);
		}
	}

	public void SetFullFrame()
	{
		IsFullFrame = true;
		_region.Rewind();
	}

	public void Reset()
	{
		_region.Rewind();
		IsFullFrame = false;
	}
}
