using System;
using CoreGraphics;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		public Ellipse()
		{
#if __IOS__
			ClipsToBounds = true;
#endif
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? CGPath.EllipseFromRect(renderingArea)
				: null);

			return shapeSize;
		}
	}
}
