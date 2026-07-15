#nullable enable
using Windows.Foundation;
using Windows.Graphics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Shapes
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

		private IGeometry? GetPath() => Data?.GetTransformedGeometry();

		private protected override void Render(IGeometry? path, double? scaleX = null, double? scaleY = null, double? renderOriginX = null,
			double? renderOriginY = null)
		{
			base.Render(path, scaleX, scaleY, renderOriginX, renderOriginY);

			_fillGeometry ??= Visual.Compositor.CreatePathGeometry();
			SpriteShape.FillGeometry = _fillGeometry;
			if (Data?.GetTransformedFilledGeometry() is IGeometrySource2D filledSource)
			{
				_fillGeometry.Path = new CompositionPath(filledSource);
			}
			else
			{
				_fillGeometry.Path = null;
			}
		}
	}
}
