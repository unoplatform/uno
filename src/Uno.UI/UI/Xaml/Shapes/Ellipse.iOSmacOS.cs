using System;
using CoreGraphics;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Ellipse()
		{
#if __IOS__
			ClipsToBounds = true;
#endif
			SetValue(StretChProperty, Fill, Default);
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

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
