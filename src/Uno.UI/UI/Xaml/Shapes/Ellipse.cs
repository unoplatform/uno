using Windows.Foundation;
using Uno.Extensions;
using Microsoft.UI.Composition;
using System.Numerics;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
#if __SKIA__
		protected override Size MeasureOverride(Size availableSize) => MeasureRelativeShape(availableSize);

		public Ellipse()
		{
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var (_, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null);

			return finalSize;
		}

		private SkiaGeometrySource2D GetGeometry(Rect renderingArea)
		{
			var builder = new SKPathBuilder();
			builder.AddOval(new SKRect((float)renderingArea.X, (float)renderingArea.Y, (float)renderingArea.Right, (float)renderingArea.Bottom));
			var geometry = new SkiaGeometrySource2D(builder.Detach());

			return geometry;
		}
#endif

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
