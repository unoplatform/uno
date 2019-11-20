using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.UI;
using static Windows.UI.Xaml.Media.Stretch;

namespace Windows.UI.Xaml.Controls
{
	internal static class ImageSizeHelper
	{
		public static void MeasureSource(this Image image, Size finalSize, ref Rect child)
		{
			switch (image.Stretch)
			{
				case UniformToFill:
					var uniformToFillScale = (child.Width * finalSize.Height >= child.Height * finalSize.Width)
						? finalSize.Height / child.Height // child is flatter than parent
						: finalSize.Width / child.Width; // child is taller than parent
					child.Width *= uniformToFillScale;
					child.Height *= uniformToFillScale;
					break;

				case Uniform:
					var uniformScale = (child.Width * finalSize.Height > child.Height * finalSize.Width)
						? finalSize.Width / child.Width // child is taller than parent
						: finalSize.Height / child.Height; // child is flatter than parent
					child.Width *= uniformScale;
					child.Height *= uniformScale;
					break;

				case Fill:
					child.Width = finalSize.Width;
					child.Height = finalSize.Height;
					break;

				case None:
					break;
			}
		}

		public static void ArrangeSource(this Image image, Size finalSize, ref Rect child)
		{
			var stretch = image.Stretch;
			// In order to match UWP behaviors, in some specific cases the image must be left align
			//var isForcedLeft = (stretch == None || stretch == UniformToFill) && finalSize.Width <= child.Width;
			var isForcedLeft = false;

			double GetPositionForDimension(bool isForcedToZero, double imageDimension, double calculatedPosition)
			{
				if (isForcedToZero && !double.IsNaN(imageDimension))
				{
					return 0;
				}

				return calculatedPosition;
			}

			switch (image.HorizontalAlignment)
			{
				case HorizontalAlignment.Left:
					child.X = 0;
					break;
				case HorizontalAlignment.Right:
					child.X = GetPositionForDimension(isForcedLeft, image.Width, finalSize.Width - child.Width);
					break;
				case HorizontalAlignment.Center:
					child.X = GetPositionForDimension(isForcedLeft, image.Width, (finalSize.Width - child.Width) * 0.5f);
					break;
				case HorizontalAlignment.Stretch:
					child.X = GetPositionForDimension(isForcedLeft, -1, (finalSize.Width - child.Width) * 0.5f);
					break;
			}

			// In order to match UWP behaviors, in some specific cases the image must be top align
			//var isForcedTop = (stretch == None || stretch == UniformToFill) && finalSize.Height <= child.Height;
			var isForcedTop = false;

			switch (image.VerticalAlignment)
			{
				case VerticalAlignment.Top:
					child.Y = 0;
					break;
				case VerticalAlignment.Bottom:
					child.Y = GetPositionForDimension(isForcedTop, image.Height, finalSize.Height - child.Height);
					break;
				case VerticalAlignment.Center:
					child.Y = GetPositionForDimension(isForcedTop, image.Height, (finalSize.Height - child.Height) * 0.5f);
					break;
				case VerticalAlignment.Stretch:
					child.Y = GetPositionForDimension(isForcedTop, -1, (finalSize.Height - child.Height) * 0.5f);
					break;
			}
		}

		public static (double x, double y) BuildScale(this Image image, Size destinationSize, Size sourceSize)
		{
			return BuildScale(image.Stretch, destinationSize, sourceSize);
		}

		internal static (double x, double y) BuildScale(Stretch stretch, Size destinationSize, Size sourceSize)
		{
			if (stretch != None)
			{
				var scale = (
					x: destinationSize.Width / sourceSize.Width,
					y: destinationSize.Height / sourceSize.Height
				);

				if (double.IsInfinity(scale.x))
				{
					if (double.IsInfinity(scale.y))
					{
						return (1.0d, 1.0d);
					}

					scale.x = scale.y;
				}
				else if (double.IsInfinity(scale.y))
				{
					scale.y = scale.x;
				}

				switch (stretch)
				{
					case UniformToFill:
						var max = Math.Max(scale.x, scale.y);
						scale = (max, max);
						break;

					case Uniform:
						var min = Math.Min(scale.x, scale.y);
						scale = (min, min);
						break;
				}

				var scaleX = double.IsNaN(scale.x) || double.IsInfinity(scale.x) ? 1.0d : scale.x;
				var scaleY = double.IsNaN(scale.y) || double.IsInfinity(scale.y) ? 1.0d : scale.y;

				return (scaleX, scaleY);
			}
			else
			{
				return (1.0d, 1.0d);
			}
		}

		public static Size AdjustSize(this Image image, Size availableSize, Size measuredSize)
		{
			return AdjustSize(image.Stretch, availableSize, measuredSize);
		}

		internal static Size AdjustSize(Stretch stretch, Size availableSize, Size measuredSize)
		{
			var scale = BuildScale(stretch, availableSize, measuredSize);
			var adjustedSize = new Size(measuredSize.Width * scale.x, measuredSize.Height * scale.y);
			return adjustedSize.AtMost(availableSize);
		}
	}
}
