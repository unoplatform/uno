using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.UI;
using static Windows.UI.Xaml.Media.Stretch;

namespace Windows.UI.Xaml.Controls
{
	internal static class ImageSizeHelper
	{
		public static Size MeasureSource(this Image image, Size finalSize, Size imageSize)
		{
			switch (image.Stretch)
			{
				case UniformToFill:
					{
						var childAspectRatio = imageSize.AspectRatio();
						var finalAspectRatio = finalSize.AspectRatio();
						if (childAspectRatio <= finalAspectRatio)
						{
							// Child wider than parent, so we're using the width to fill
							// It's also the default mode if aspect ratios are the same
							imageSize.Width = finalSize.Width;
							imageSize.Height = finalSize.Width / childAspectRatio;
						}
						else
						{
							// child is taller than parent, so where' using the height to fill
							imageSize.Width = finalSize.Height * childAspectRatio;
							imageSize.Height = finalSize.Height;
						}

						break;
					}

				case Uniform:
					{
						var childAspectRatio = imageSize.AspectRatio();
						var finalAspectRatio = finalSize.AspectRatio();
						if (childAspectRatio <= finalAspectRatio)
						{
							// Child wider than parent, so we're using the height to fill
							imageSize.Width = finalSize.Height * childAspectRatio;
							imageSize.Height = finalSize.Height;
						}
						else
						{
							// child is taller than parent, so where' using the width to fill
							imageSize.Width = finalSize.Width;
							imageSize.Height = finalSize.Width / childAspectRatio;
						}

						break;
					}

				case Fill:
					{
						imageSize = finalSize;
						break;
					}

					// In case of None, there's no adjustment to make to the size of the image
			}

			return imageSize;
		}

#if !__SKIA__
		public static Rect ArrangeSource(this Image image, Size finalSize, Size containerSize)
		{
			var child = new Rect(default, containerSize);

			var stretch = image.Stretch;
			var horizontalAlignment = image.HorizontalAlignment;

			if (stretch == None || stretch == UniformToFill)
			{
				// In order to match UWP behaviors, in some specific cases the image must be left align
				if (finalSize.Width <= child.Width && horizontalAlignment == HorizontalAlignment.Stretch)
				{
					horizontalAlignment = HorizontalAlignment.Left;
				}
			}

			if (horizontalAlignment == HorizontalAlignment.Stretch && child.Width < finalSize.Width)
			{
				// In order to match UWP behaviors, image is centered when smaller than available size
				// when the "Stretch" alignment is used.
				horizontalAlignment = HorizontalAlignment.Center;
			}

			switch (horizontalAlignment)
			{
				case HorizontalAlignment.Left:
					child.X = 0;
					break;
				case HorizontalAlignment.Right:
					child.X = finalSize.Width - child.Width;
					break;
				case HorizontalAlignment.Stretch:
				case HorizontalAlignment.Center:
					child.X = (finalSize.Width - child.Width) * 0.5f;
					break;
			}

			var verticalAlignment = image.VerticalAlignment;

			if (stretch == None || stretch == UniformToFill)
			{
				// In order to match UWP behaviors, in some specific cases the image must be top align
				if (finalSize.Height <= child.Height && verticalAlignment == VerticalAlignment.Stretch)
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
					child.Y = 0;
					break;
				case VerticalAlignment.Bottom:
					child.Y = finalSize.Height - child.Height;
					break;
				case VerticalAlignment.Stretch:
				case VerticalAlignment.Center:
					child.Y = (finalSize.Height - child.Height) * 0.5f;
					break;
			}

			return child;
		}
#endif

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
			return AdjustSize(image.Stretch, image.ApplySizeConstraints(availableSize), measuredSize);
		}

		internal static Size AdjustSize(Stretch stretch, Size availableSize, Size measuredSize)
		{
			var scale = BuildScale(stretch, availableSize, measuredSize);
			var adjustedSize = new Size(measuredSize.Width * scale.x, measuredSize.Height * scale.y);
			return adjustedSize.AtMost(availableSize);
		}
	}
}
