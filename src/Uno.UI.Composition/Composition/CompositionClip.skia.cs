#nullable enable
using System;
using System.Linq;
using SkiaSharp;

namespace Microsoft.UI.Composition;

partial class CompositionClip
{
	internal void Apply(SKCanvas canvas, Visual visual)
	{
		if (this is InsetClip insetClip)
		{
			var rect = new SKRect(
				insetClip.LeftInset,
				insetClip.TopInset,
				visual.Size.X - insetClip.RightInset,
				visual.Size.Y - insetClip.BottomInset);
			canvas.ClipRect(rect, SKClipOperation.Intersect, true);
		}
		else if (this is RectangleClip rectangleClip)
		{
			canvas.ClipRoundRect(rectangleClip.SKRoundRect, SKClipOperation.Intersect, true);
		}
		else if (this is CompositionGeometricClip geometricClip)
		{
			switch (geometricClip.Geometry)
			{
				case CompositionPathGeometry { Path.GeometrySource: SkiaGeometrySource2D geometrySource }:
					canvas.ClipPath(geometrySource.Geometry, antialias: true);
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
