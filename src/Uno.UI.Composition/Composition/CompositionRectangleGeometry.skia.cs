#nullable enable

using SkiaSharp;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	public partial class CompositionRectangleGeometry : CompositionGeometry
	{
		internal override IGeometrySource2D? BuildGeometry()
			=> new SkiaGeometrySource2D(BuildRectangleGeometry(Offset, Size));
	}
}
