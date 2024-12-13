using Uno.Extensions;
using System;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Shapes
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

			return finalSize;
		}

		private protected override string GetBBoxCacheKeyImpl() =>
#if DEBUG
			throw new InvalidOperationException("Rectangle doesnt use GetBBox. Should the impl change in the future, add key-gen and invalidation mechanism");
#else
			null;
#endif
	}
}
