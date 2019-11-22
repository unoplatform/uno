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
			var horizontalAlignment = image.HorizontalAlignment;

			if (stretch == None || stretch == UniformToFill)
			{
				// In order to match UWP behaviors, in some specific cases the image must be left align
				if (!double.IsNaN(image.Width) && finalSize.Width <= child.Width)
				{
					horizontalAlignment = HorizontalAlignment.Left;
				}
			}

			if(horizontalAlignment == HorizontalAlignment.Stretch && child.Width < finalSize.Width)
			{
				// In order to match UWP behaviors, image is centered when smaller than available size
				// when the "Stretch" alignment is used.
				horizontalAlignment = HorizontalAlignment.Center;
			}

			switch (horizontalAlignment)
			{
				case HorizontalAlignment.Left:
				case HorizontalAlignment.Stretch:
					child.X = 0;
					break;
				case HorizontalAlignment.Right:
					child.X = finalSize.Width - child.Width;
					break;
				case HorizontalAlignment.Center:
					child.X = (finalSize.Width - child.Width) * 0.5f;
					break;
			}

			var verticalAlignment = image.VerticalAlignment;

			if (stretch == None || stretch == UniformToFill)
			{
				// In order to match UWP behaviors, in some specific cases the image must be top align
				if (!double.IsNaN(image.Height) && finalSize.Height <= child.Height)
				{
					verticalAlignment = VerticalAlignment.Top;
				}
			}

			if (verticalAlignment == VerticalAlignment.Stretch && child.Height < finalSize.Height)
			{
				// In order to match UWP behaviors, image is centered when smaller than available size
				// when the "Stretch" alignment is used.
				verticalAlignment = VerticalAlignment.Center;
			}

			switch (verticalAlignment)
			{
				case VerticalAlignment.Top:
				case VerticalAlignment.Stretch:
					child.Y = 0;
					break;
				case VerticalAlignment.Bottom:
					child.Y = finalSize.Height - child.Height;
					break;
				case VerticalAlignment.Center:
					child.Y = (finalSize.Height - child.Height) * 0.5f;
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

				var scaleX = double.IsNaN(scale.x) ? 1.0d : scale.x;
				var scaleY = double.IsNaN(scale.y) ? 1.0d : scale.y;

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
