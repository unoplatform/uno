#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition;

internal static class DamageRegionExtensions
{
	public static void Union(this SKPath region, SKPath addition)
	{
		if (addition.IsEmpty)
		{
			return;
		}

		if (region.IsEmpty)
		{
			addition.Transform(SKMatrix.Identity, region);
		}
		else
		{
			region.Op(addition, SKPathOp.Union, region);
		}
	}

	public static void UnionRect(this SKPath region, SKRect rect)
	{
		if (rect.IsEmpty)
		{
			return;
		}

		using var scratch = Microsoft.UI.Composition.SkiaExtensions.CreateRectPath(rect);
		region.Union(scratch);
	}

	public static void ClampTo(this SKPath region, SKRect frameRect)
	{
		if (region.IsEmpty || frameRect.Contains(region.Bounds))
		{
			return;
		}

		using var scratch = Microsoft.UI.Composition.SkiaExtensions.CreateRectPath(frameRect);
		region.Op(scratch, SKPathOp.Intersect, region);
	}
}
