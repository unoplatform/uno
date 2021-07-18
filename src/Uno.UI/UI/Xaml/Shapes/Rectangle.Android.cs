using Windows.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;
using Android.Graphics;
using Uno.UI;
using System;
using Rect = Windows.Foundation.Rect;
using Android.Views;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		static Rectangle()
		{
			StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Rectangle()
		{
		}
			
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Android.Graphics.Path path;

			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				path = new Android.Graphics.Path();

			//Android's path rendering logic rounds values down to the nearest int, make sure we round up here instead using the ViewHelper scaling logic. However we only want to round the height and width, not the frame offsets.
			var physicalRenderingArea = renderingArea.LogicalToPhysicalPixels();
			if (FrameRoundingAdjustment is { } fra)
			{
				physicalRenderingArea.Height += fra.Height;
				physicalRenderingArea.Width += fra.Width;
			}

			var logicalRenderingArea = physicalRenderingArea.PhysicalToLogicalPixels();
			logicalRenderingArea.X = renderingArea.X;
			logicalRenderingArea.Y = renderingArea.Y;

			path.AddRoundRect(logicalRenderingArea.ToRectF(), (float)RadiusX, (float)RadiusY, Android.Graphics.Path.Direction.Cw);
			}
			else
			{
				path = null;
			}

			Render(path);

			return shapeSize;
		}
	}
}
