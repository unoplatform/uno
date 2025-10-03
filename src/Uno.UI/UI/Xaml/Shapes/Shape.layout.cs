#nullable enable

#if __APPLE_UIKIT__ || __SKIA__ || __ANDROID__ || __WASM__
using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using static System.Double;
using Windows.Phone.Media.Devices;
using System.Diagnostics;

#if __APPLE_UIKIT__
using NativePath = CoreGraphics.CGPath;
using ObjCRuntime;
using NativeSingle = System.Runtime.InteropServices.NFloat;
#elif __SKIA__
using NativePath = Microsoft.UI.Composition.SkiaGeometrySource2D;
using NativeSingle = System.Double;

#elif __ANDROID__
using NativePath = Android.Graphics.Path;
using NativeSingle = System.Double;

#elif __WASM__
using NativePath = Microsoft.UI.Xaml.Shapes.Shape;
using NativeSingle = System.Double;
#endif

namespace Microsoft.UI.Xaml.Shapes
{
	partial class Shape
	{
		#region Measure / Arrange should be shared using Geometry instead of CGPath
		private protected Size MeasureRelativeShape(Size availableSize)
		{
			var stretch = Stretch;

			var feWidth = Width switch
			{
				var w when double.IsNaN(w) => MinWidth,
				var w => w
			};

			var feHeight = Height switch
			{
				var h when double.IsNaN(h) => MinHeight,
				var h => h
			};

			if (stretch == Stretch.UniformToFill)
			{
				var width = availableSize.Width;
				var height = availableSize.Height;

				if (double.IsInfinity(width) && double.IsInfinity(height))
				{
					return new Size(feWidth, feHeight);
				}
				else if (double.IsInfinity(width) || double.IsInfinity(height))
				{
					width = Math.Min(width, height);
				}
				else
				{
					width = Math.Max(width, height);
				}

				// Yes, it's intentional for this to be width, width.
				// This is the same as WinUI is doing.
				// The reasoning for this is if you have some code like:
				//
				// <Grid Width="150" Height="150" BorderBrush="Green" BorderThickness="1">
				//     <Ellipse Width="300" Stretch="UniformToFill"
				//              HorizontalAlignment="Left" VerticalAlignment="Bottom" Fill="Red" />
				// </Grid >
				//
				// In order to get "VerticalAlignment" working as expected, we need
				// to force the height to 300
				return new Size(width, width);
			}

			return new Size(feWidth, feHeight);
		}

#if !IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED
		// The desired size before it has been changed to apply the [Min]<Width|Height>
		private Size _realDesiredSize;
#endif

		private protected Size MeasureAbsoluteShape(Size availableSize, NativePath? path)
		{
			if (path! == null!)
			{
				return default;
			}

			var stretch = Stretch;
			var stroke = Stroke;
			var strokeThickness = stroke is null ? DefaultStrokeThicknessWhenNoStrokeDefined : StrokeThickness;
			var pathBounds = GetPathBoundingBox(path); // The BoundingBox shouldn't include the control points.
			var pathSize = (Size)pathBounds.Size;

			if (NativeSingle.IsInfinity(pathBounds.Right) || NativeSingle.IsInfinity(pathBounds.Bottom))
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
				}

				return default;
			}

			// Workaround. This should be happening by FrameworkElement.
			availableSize = availableSize.AtLeast(this.GetMinMax().min);

			// Compute the final size of the Shape and the render properties
			Size size;
			switch (stretch)
			{
				default:
				case Stretch.None:
					// If stretch is None, we have to keep the origin defined by the absolute coordinates of the path:
					//
					// This means that if you draw a line from 50,50 to 100,100 (so it's not starting at 0, 0),
					// with a 'None' stretch mode you will have:
					//    ------
					//    |    |
					//    |    |
					//    |  \ |
					//    |   \|
					//    ------
					//
					// while with another Stretch mode you will have:
					//    ------
					//    |\   |
					//    | \  |
					//    |  \ |
					//    |   \|
					//    ------
					//
					// So for measure when None we includes that origin in the path size.
					//
					// Also, as the path does not have any notion of stroke thickness, we have to include it for the measure phase.
					// Note: The logic would say to include the full StrokeThickness as it will "overflow" half on booth side of the path,
					//		 but WinUI does include only the half of it.
					var halfStrokeThickness = strokeThickness / 2;
					size = new Size(pathBounds.Right + halfStrokeThickness, pathBounds.Bottom + halfStrokeThickness);
					break;

				case Stretch.Fill:
					size = new Size(
						availableSize.Width == PositiveInfinity ? pathBounds.Width + strokeThickness : Math.Max(availableSize.Width, strokeThickness),
						availableSize.Height == PositiveInfinity ? pathBounds.Height + strokeThickness : Math.Max(availableSize.Height, strokeThickness)
						);
					break;

				case Stretch.Uniform:
					// For Stretch.Uniform, if available size is infinity (both Width and Height), we just return the path size plus stroke thickness.
					if (availableSize.Width == PositiveInfinity && availableSize.Height == PositiveInfinity)
					{
						size = new Size(pathSize.Width + strokeThickness, pathSize.Height + strokeThickness);
					}
					else if (availableSize.Width <= strokeThickness || availableSize.Height <= strokeThickness)
					{
						size = new Size(strokeThickness, strokeThickness);
					}
					else
					{
						if ((pathSize.Width / (availableSize.Width - strokeThickness)) > pathSize.Height / (availableSize.Height - strokeThickness))
						{
							var width = availableSize.Width;
							var height = ((width - strokeThickness) / pathSize.AspectRatio()) + strokeThickness;
							size = new Size(width, height);
						}
						else
						{
							var height = availableSize.Height;
							var width = ((height - strokeThickness) * pathSize.AspectRatio()) + strokeThickness;
							size = new Size(width, height);
						}
					}

					break;

				case Stretch.UniformToFill:
					// For Stretch.UniformToFill, if available size is infinity (both Width and Height), we just return the path size plus stroke thickness.
					if (availableSize.Width == PositiveInfinity && availableSize.Height == PositiveInfinity)
					{
						size = new Size(pathSize.Width + strokeThickness, pathSize.Height + strokeThickness);
					}
					else if (availableSize.Width <= strokeThickness && availableSize.Height <= strokeThickness)
					{
						size = new Size(strokeThickness, strokeThickness);
					}
					else
					{
						var availableSizeWithoutStroke = new Size(Math.Max(0, availableSize.Width - strokeThickness), Math.Max(0, availableSize.Height - strokeThickness));

						if (availableSize.Height == double.PositiveInfinity ||
							(availableSize.Height == 0 && availableSize.Width != double.PositiveInfinity) ||
							(availableSize.Width != double.PositiveInfinity && (pathSize.Width / availableSizeWithoutStroke.Width) <= pathSize.Height / availableSizeWithoutStroke.Height))
						{
							var width = availableSize.Width;
							var height = (availableSizeWithoutStroke.Width / pathSize.AspectRatio()) + strokeThickness;
							size = new Size(Math.Max(width, strokeThickness), Math.Max(height, strokeThickness));
						}
						else
						{
							var height = availableSize.Height;
							var width = (availableSizeWithoutStroke.Height * pathSize.AspectRatio()) + StrokeThickness;
							size = new Size(Math.Max(width, StrokeThickness), Math.Max(height, StrokeThickness));
						}
					}

					break;
			}

#if !IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED
			_realDesiredSize = size;
#endif

			return size;
		}
		#endregion
	}
}
#endif
