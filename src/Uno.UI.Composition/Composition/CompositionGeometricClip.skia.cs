#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class CompositionGeometricClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
	{
		if (Geometry is not null)
		{
			var geometry = Geometry.BuildGeometry();

			if (geometry is SkiaGeometrySource2D skiaGeometrySource)
			{
				return skiaGeometrySource.Geometry.TightBounds.ToRect();
			}
			else
			{
				throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
			}
		}

		return null;
	}

	internal override IGeometry? GetClipPath(Visual visual)
	{
		if (Geometry is not null)
		{
			var geometry = Geometry.BuildGeometry();

			if (geometry is SkiaGeometrySource2D geometrySource)
			{
				IGeometry path = geometrySource;
				if (!TransformMatrix.IsIdentity)
				{
					path = path.Transform(TransformMatrix);
				}

				return path;
			}
			else
			{
				throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
			}
		}

		return null;
	}
}
