using Windows.Foundation;
using Android.Graphics;
using Uno.UI;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize) => base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetPath(renderingArea)
				: null);

			return shapeSize;
		}

		private Android.Graphics.Path GetPath(Rect availableSize)
		{
			var output = new Android.Graphics.Path();

			//Android's path rendering logic rounds values down to the nearest int, make sure we round up here instead using the ViewHelper scaling logic
			var physicalRenderingArea = availableSize.LogicalToPhysicalPixels();
			if (FrameRoundingAdjustment is { } fra)
			{
				physicalRenderingArea.Height += fra.Height;
				physicalRenderingArea.Width += fra.Width;
			}

			var logicalRenderingArea = physicalRenderingArea.PhysicalToLogicalPixels();
			logicalRenderingArea.X = availableSize.X;
			logicalRenderingArea.Y = availableSize.Y;

			output.AddOval(
				logicalRenderingArea.ToRectF(),
				Android.Graphics.Path.Direction.Cw);

			return output;
		}
	}
}
