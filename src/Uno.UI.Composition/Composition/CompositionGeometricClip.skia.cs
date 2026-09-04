#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

partial class CompositionGeometricClip
{
	private protected override Rect? GetBoundsCore(Visual visual)
		=> Geometry?.BuildGeometry() is IGeometry geometry ? geometry.Bounds : (Rect?)null;

	internal override IGeometry? GetClipPath(Visual visual)
	{
		if (Geometry?.BuildGeometry() is IGeometry path)
		{
			// BuildGeometry hands out the geometry object's own cached instance, so take a reference on it rather
			// than passing the borrow along — Transform already returns one.
			if (TransformMatrix.IsIdentity)
			{
				path.AddRef();
				return path;
			}

			return path.Transform(TransformMatrix);
		}

		return null;
	}
}
