using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Shapes
{
	partial class Rectangle
	{
		public Rectangle() : base("rect")
		{
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			UpdateRender();
			_mainSvgElement.SetAttribute(
				("rx", RadiusX.ToStringInvariant()),
				("ry", RadiusY.ToStringInvariant())
			);
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Uno.UI.Xaml.WindowManagerInterop.SetSvgElementRect(_mainSvgElement.HtmlId, renderingArea);

			_mainSvgElement.Clip = new RectangleGeometry() { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };

			return shapeSize;
		}
	}
}
