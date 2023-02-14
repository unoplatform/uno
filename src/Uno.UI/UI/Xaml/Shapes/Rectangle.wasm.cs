using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Wasm;
using Uno.Extensions;
using System;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
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

			Uno.UI.Xaml.WindowManagerInterop.SetSvgElementRect(_rectangle.HtmlId, childRect);

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
