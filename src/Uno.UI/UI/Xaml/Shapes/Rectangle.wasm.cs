using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using System;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	partial class Rectangle
	{
		private readonly SvgElement _rectangle = new SvgElement("rect");

		public Rectangle()
		{
			SvgChildren.Add(_rectangle);

			InitCommonShapeProperties();
			OnRadiusXChangedPartial();
			OnRadiusYChangedPartial();
		}

		protected override SvgElement GetMainSvgElement()
		{
			return _rectangle;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var strokeThickness = ActualStrokeThickness;

			var childRect = new Rect(
					strokeThickness / 2,
					strokeThickness / 2,
					finalSize.Width - strokeThickness,
					finalSize.Height - strokeThickness
				)
				.AtLeast(new Size(0, 0));

			_rectangle.Arrange(childRect);
			_rectangle.SetAttribute(
				("x", childRect.X.ToStringInvariant()),
				("y", childRect.Y.ToStringInvariant()),
				("width", childRect.Width.ToStringInvariant()),
				("height", childRect.Height.ToStringInvariant())
			);

			_rectangle.Clip = new RectangleGeometry() { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };

			return finalSize;
		}

		partial void OnRadiusXChangedPartial()
		{
			_rectangle.SetAttribute("rx", RadiusX.ToStringInvariant());
		}

		partial void OnRadiusYChangedPartial()
		{
			_rectangle.SetAttribute("ry", RadiusY.ToStringInvariant());
		}
	}
}
