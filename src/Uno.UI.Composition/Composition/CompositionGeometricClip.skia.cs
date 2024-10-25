#nullable enable

using System;
using SkiaSharp;
using Windows.Foundation;

namespace Windows.UI.Composition;

partial class CompositionGeometricClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		switch (Geometry)
		{
			case CompositionPathGeometry { Path.GeometrySource: SkiaGeometrySource2D geometrySource }:
				return geometrySource.Geometry.TightBounds.ToRect();

			case CompositionPathGeometry cpg:
				throw new InvalidOperationException($"Clipping with source {cpg.Path?.GeometrySource} is not supported");

			case null:
				return null;

			default:
				throw new InvalidOperationException($"Clipping with {Geometry} is not supported");
		}
	}

	internal override void Apply(SKCanvas canvas, Visual visual)
	{
		switch (Geometry)
		{
			case CompositionPathGeometry { Path.GeometrySource: SkiaGeometrySource2D geometrySource }:
				var path = geometrySource.Geometry;
				if (!TransformMatrix.IsIdentity)
				{
					var transformedPath = new SKPath();
					path.Transform(TransformMatrix.ToSKMatrix(), transformedPath);
					path = transformedPath;
				}

				canvas.ClipPath(path, antialias: true);
				break;

			case CompositionPathGeometry cpg:
				throw new InvalidOperationException($"Clipping with source {cpg.Path?.GeometrySource} is not supported");

			case null:
				// null is nop
				break;

			default:
				throw new InvalidOperationException($"Clipping with {Geometry} is not supported");
		}
	}
}
