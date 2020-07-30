using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class ShapeVisual
	{
		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			foreach(var shape in Shapes)
			{
				shape.Render(surface, info);
			}
		}
	}
}
