#nullable enable
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Path : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private SkiaGeometrySource2D? GetPath() => Data?.GetGeometrySource2D();

		private protected override void Render(SkiaGeometrySource2D? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null,
			double? renderOriginY = null)
		{
			base.Render(path, scaleX, scaleY, renderOriginX, renderOriginY);

			if (Data?.GetUnfilledSKPath() is { } negativePath)
			{
				SpriteShape.NegativeFillGeometry = Visual.Compositor.CreatePathGeometry(new CompositionPath(new SkiaGeometrySource2D(negativePath)));
			}
			else
			{
				SpriteShape.NegativeFillGeometry = null;
			}
		}
	}
}
