#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition;

internal static class DamageRegionExtensions
{
	[ThreadStatic]
	private static SKPath? _unionRectScratch;

	[ThreadStatic]
	private static SKPath? _clampScratch;

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
			global::Microsoft.UI.Composition.SkiaGeometrySource2D.CountedOp(region, addition, SKPathOp.Union, region);
		}
	}

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

	public static void ClampTo(this SKPath region, SKRect frameRect)
	{
		if (region.IsEmpty || frameRect.Contains(region.Bounds))
		{
			return;
		}

		var scratch = _clampScratch ??= new SKPath();
		scratch.Rewind();
		scratch.AddRect(frameRect);
		global::Microsoft.UI.Composition.SkiaGeometrySource2D.CountedOp(region, scratch, SKPathOp.Intersect, region);
	}
}
