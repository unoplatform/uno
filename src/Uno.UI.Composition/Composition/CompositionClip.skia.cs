#nullable enable
using System;
using System.Linq;
using SkiaSharp;

namespace Microsoft.UI.Composition;

partial class CompositionClip
{
	internal void Apply(SKSurface surface)
	{
		if (this is InsetClip insetClip)
		{
			surface.Canvas.ClipRect(insetClip.SKRect, SKClipOperation.Intersect, true);
		}
		else if (this is RectangleClip rectangleClip)
		{
			surface.Canvas.ClipRoundRect(rectangleClip.SKRoundRect, SKClipOperation.Intersect, true);
		}
		else if (this is CompositionGeometricClip geometricClip)
		{
			switch (geometricClip.Geometry)
			{
				case CompositionPathGeometry { Path.GeometrySource: SkiaGeometrySource2D geometrySource }:
					surface.Canvas.ClipPath(geometrySource.Geometry, antialias: true);
					break;
				case CompositionPathGeometry cpg:
					throw new InvalidOperationException($"Clipping with source {cpg.Path?.GeometrySource} is not supported");
				case null:
					// null is nop
					break;
				default:
					throw new InvalidOperationException($"Clipping with {geometricClip.Geometry} is not supported");
			}
		}
	}
}
