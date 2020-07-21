using Windows.Foundation;
using Uno.Extensions;
using Windows.UI.Composition;
using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Xaml.Shapes
{
	partial class Ellipse
	{
		private ShapeVisual _rectangleVisual;

		public Ellipse()
		{
			_rectangleVisual = Visual.Compositor.CreateShapeVisual();
			Visual.Children.InsertAtBottom(_rectangleVisual);
		}

		internal override SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			var geometry = new SkiaGeometrySource2D();
			geometry.Geometry.AddOval(new SKRect(0, 0, (float)finalSize.Width, (float)finalSize.Height));

			return geometry;
		}
	}
}
