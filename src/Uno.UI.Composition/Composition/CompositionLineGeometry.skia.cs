#nullable enable

using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionLineGeometry : CompositionGeometry
	{
		internal override IGeometrySource2D? BuildGeometry() => new SkiaGeometrySource2D(BuildLineGeometry(Start, End));
	}
}
