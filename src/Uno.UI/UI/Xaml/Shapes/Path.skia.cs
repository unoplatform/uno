#nullable enable
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	partial class Path : Shape
	{
		private CompositionPathGeometry? _fillGeometry;

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

			_fillGeometry ??= Visual.Compositor.CreatePathGeometry();
			SpriteShape.FillGeometry = _fillGeometry;
			if (Data?.GetFilledSKPath() is { } filledPath)
			{
				_fillGeometry.Path = new CompositionPath(new SkiaGeometrySource2D(filledPath));
			}
			else
			{
				_fillGeometry.Path = null;
			}
		}
	}
}
