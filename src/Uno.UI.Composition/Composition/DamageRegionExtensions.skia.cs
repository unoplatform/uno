#nullable enable

using System;
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
	// Reused scratch path to turn a rect into a path without allocating per call. [ThreadStatic] so it's
	// per-rendering-thread (each Skia host renders on its own thread); one per method since the two never
	// nest. Lazily created because a [ThreadStatic] field's initializer only runs on the first thread.
	[ThreadStatic]
	private static SKPath? _unionRectScratch;

	[ThreadStatic]
	private static SKPath? _clampScratch;

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

	/// <summary>Unions <paramref name="rect"/> into the damage region.</summary>
	public static void UnionRect(this SKPath region, SKRect rect)
	{
		if (rect.IsEmpty)
		{
			return;
		}

		var scratch = _unionRectScratch ??= new SKPath();
		scratch.Rewind();
		scratch.AddRect(rect);
		region.Union(scratch);
	}

	/// <summary>Clips the damage region to <paramref name="frameRect"/> (nothing outside the frame is ever presented).</summary>
	public static void ClampTo(this SKPath region, SKRect frameRect)
	{
		if (region.IsEmpty || frameRect.Contains(region.Bounds))
		{
			return;
		}

		var scratch = _clampScratch ??= new SKPath();
		scratch.Rewind();
		scratch.AddRect(frameRect);
		region.Op(scratch, SKPathOp.Intersect, region);
	}
}
