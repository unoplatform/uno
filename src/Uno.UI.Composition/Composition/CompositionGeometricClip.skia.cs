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
			return TransformMatrix.IsIdentity ? path : path.Transform(TransformMatrix);
		}

		return null;
	}
}
