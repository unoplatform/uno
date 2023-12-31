#nullable enable

using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public partial class CompositionEllipseGeometry : CompositionGeometry
	{
		internal override IGeometrySource2D? BuildGeometry()
			=> new SkiaGeometrySource2D(BuildEllipseGeometry(Center, Radius));
	}
}
