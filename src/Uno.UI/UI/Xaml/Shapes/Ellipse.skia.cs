using Windows.Foundation;
using Uno.Extensions;
using Microsoft.UI.Composition;
using System.Numerics;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Shapes
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
				? GetGeometry(renderingArea)
				: null);

			return shapeSize;
		}

		private SkiaGeometrySource2D GetGeometry(Rect renderingArea)
		{
			var geometry = new SkiaGeometrySource2D();
			geometry.Geometry.AddOval(new SKRect((float)renderingArea.X, (float)renderingArea.Y, (float)renderingArea.Right, (float)renderingArea.Bottom));

			return geometry;
		}
	}
}
