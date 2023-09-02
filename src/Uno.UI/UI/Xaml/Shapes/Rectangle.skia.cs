#nullable enable

using System;
using Windows.UI.Composition;
using Windows.Foundation;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);
			var path = renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null;

			Render(path);

			return shapeSize;
		}

		private SkiaGeometrySource2D GetGeometry(Rect finalRect)
		{
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			var offset = new Vector2((float)finalRect.Left, (float)finalRect.Top);
			var size = new Vector2((float)finalRect.Width, (float)finalRect.Height);

			var geometry = radiusX is 0 || radiusY is 0
				? CompositionGeometry.BuildRectangleGeometry(offset, size)
				: CompositionGeometry.BuildRoundedRectangleGeometry(offset, size, new Vector2((float)radiusX, (float)radiusY));

			return new SkiaGeometrySource2D(geometry);
		}
	}
}
