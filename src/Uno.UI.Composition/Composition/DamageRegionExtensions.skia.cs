#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition;

/// <summary>
/// Helpers for accumulating a per-frame damage region into a plain <see cref="SKPath"/>. The region is a
/// possibly-disjoint union of arbitrary shapes (not merged into a single bounding rectangle), so the gaps
/// between far-apart changes (e.g. a TextBox near the top and a ListView near the bottom) are not repainted,
/// and non-rectangular clips (rounded corners, curves) are honored.
/// </summary>
internal static class DamageRegionExtensions
{
	/// <summary>Unions <paramref name="addition"/> into the damage region <paramref name="region"/>.</summary>
	public static void Union(this SKPath region, SKPath addition)
	{
		if (addition.IsEmpty)
		{
			return;
		}

		if (region.IsEmpty)
		{
			region.AddPath(addition);
		}
		else
		{
			// A true geometric union keeps disjoint shapes disjoint and coalesces overlapping/adjacent ones,
			// regardless of contour winding (so the clip never develops a hole that would drop a repaint).
			// SKPath.Op builds into an internal temp before assigning, so the result may alias the source —
			// we union in place.
			region.Op(addition, SKPathOp.Union, region);
		}
	}

	/// <summary>
	/// Unions <paramref name="rect"/> into the damage region, using <paramref name="scratch"/> to turn the
	/// rect into a path without allocating.
	/// </summary>
	public static void UnionRect(this SKPath region, SKPath scratch, SKRect rect)
	{
		if (rect.IsEmpty)
		{
			return;
		}

		scratch.Rewind();
		scratch.AddRect(rect);
		region.Union(scratch);
	}

	/// <summary>
	/// Clips the damage region to <paramref name="frameRect"/> (nothing outside the frame is ever presented),
	/// using <paramref name="scratch"/> to turn the rect into a path without allocating.
	/// </summary>
	public static void ClampTo(this SKPath region, SKPath scratch, SKRect frameRect)
	{
		if (region.IsEmpty || frameRect.Contains(region.Bounds))
		{
			return;
		}

		scratch.Rewind();
		scratch.AddRect(frameRect);
		region.Op(scratch, SKPathOp.Intersect, region);
	}
}
