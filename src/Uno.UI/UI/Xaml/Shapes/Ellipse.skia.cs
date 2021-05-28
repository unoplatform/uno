using Windows.Foundation;
using Uno.Extensions;
using Windows.UI.Composition;
using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Xaml.Shapes
{
	partial class Ellipse : Shape
	{
		private ShapeVisual _rectangleVisual;

		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Ellipse()
		{
			_rectangleVisual = Visual.Compositor.CreateShapeVisual();
			Visual.Children.InsertAtBottom(_rectangleVisual);
		}

		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea.Size)
				: null);

			return shapeSize;
		}

		private SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			var geometry = new SkiaGeometrySource2D();
			geometry.Geometry.AddOval(new SKRect(0, 0, (float)finalSize.Width, (float)finalSize.Height));

			return geometry;
		}
	}
}
