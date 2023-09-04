#nullable enable

#if __IOS__ || __MACOS__ || __SKIA__ || __ANDROID__ || __WASM__
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using static System.Double;
using Windows.Phone.Media.Devices;
using System.Diagnostics;

#if __IOS__
using NativePath = CoreGraphics.CGPath;
using ObjCRuntime;
using NativeSingle = System.Runtime.InteropServices.NFloat;
#elif __MACOS__
using AppKit;
using NativePath = CoreGraphics.CGPath;
using ObjCRuntime;
using NativeSingle = System.Runtime.InteropServices.NFloat;
#elif __SKIA__
using NativePath = Windows.UI.Composition.SkiaGeometrySource2D;
using NativeSingle = System.Double;

#elif __ANDROID__
using NativePath = Android.Graphics.Path;
using NativeSingle = System.Double;

#elif __WASM__
using NativePath = Windows.UI.Xaml.Shapes.Shape;
using NativeSingle = System.Double;
#endif

namespace Windows.UI.Xaml.Shapes
{
	partial class Shape
	{
		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
		}

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

				return new Size(width, height);
			}

			return new Size(feWidth, feHeight);
		}

		private protected (Size shapeSize, Rect renderingArea) ArrangeRelativeShape(Size finalSize)
		{
			var horizontal = HorizontalAlignment;
			var vertical = VerticalAlignment;
			var stretch = Stretch;
			var userMinSize = new Size(MinWidth, MinHeight);
			var userMaxSize = new Size(MaxWidth, MaxHeight);
			var userSize = new Size(Width, Height);

			var size = userSize;

			// Like for the measure, if no user size defined on a given axis, we try to stretch along this axis
			if (IsNaN(size.Width))
			{
				size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
					? finalSize.Width
					: 0;
			}
			if (IsNaN(size.Height))
			{
				size.Height = stretch == Stretch.UniformToFill || VerticalAlignment == VerticalAlignment.Stretch
					? finalSize.Height
					: 0;
			}

			// Like for the measure, in case userSize was not defined, we still have to apply the min size
			size = size
				.AtLeast(userMinSize)
				.NumberOrDefault(userMinSize);

			// The area that will be used to render the rectangle/ellipse as path
			var renderingArea = new Rect(new Point(), size);

			// Apply the stretch mode, as it might change the "shape" of a "relative shape"
			switch (stretch)
			{
				case Stretch.None:
					renderingArea.Height = renderingArea.Width = 0;
					break;

				default:
				case Stretch.Fill:
					// size is already valid ... nothing to do!
					break;

				case Stretch.Uniform when renderingArea.Width < renderingArea.Height:
					renderingArea.Height = renderingArea.Width;
					break;

				case Stretch.Uniform: // when pathArea.Width >= pathArea.Height:
					renderingArea.Width = renderingArea.Height;
					break;

				case Stretch.UniformToFill when renderingArea.Width < renderingArea.Height:
					renderingArea.Width = renderingArea.Height;
					break;

				case Stretch.UniformToFill: // when pathArea.Width >= pathArea.Height:
					renderingArea.Height = renderingArea.Width;
					break;
			}

			// The path will be injected as a Layer, so we also have to apply the horizontal and vertical alignments
			// Note: We have to make this adjustment only if the shape is overflowing the container bounds,
			//		 otherwise the alignment will be correctly applied by the container.
			(bool horizontally, bool vertically) shouldAlign;
			switch (stretch)
			{
				case Stretch.UniformToFill:
					userSize = userSize
						.NumberOrDefault(userMaxSize)
						.AtLeast(userMinSize);

					// By default we align if UniformToFill, EXCEPT if the the userSize (or max, lowered by min) is lower than the finalSize
					// For reference, it's almost equivalent to:
					// var horizontally = IsNaN(userSize.Width) || (!IsInfinity(userSize.Width) && userSize.Width > finalSize.Width) || userMinSize.Width > 0;
					// shouldAlign = (horizontally || vertically, horizontally || vertically);
					var notHorizontally = userSize.Width <= finalSize.Width;
					var notVertically = userSize.Height <= finalSize.Height;

					shouldAlign = (!notHorizontally && !notVertically, !notHorizontally && !notVertically);
					break;

				default:
					// WinUI does not adjust alignment if the shape was smaller than the finalSize
					shouldAlign = (userSize.Width > finalSize.Width, userSize.Height > finalSize.Height);
					break;
			}


			var alignmentWidth = Math.Max(size.Width, renderingArea.Width);
			var horizontalOverflow = alignmentWidth - finalSize.Width;
			if (horizontalOverflow > 0 && shouldAlign.horizontally)
			{
				switch (horizontal)
				{
					case HorizontalAlignment.Center:
						renderingArea.X -= horizontalOverflow / 2.0;
						break;

					case HorizontalAlignment.Right:
						renderingArea.X -= horizontalOverflow;
						break;
				}
			}
			var alignmentHeight = Math.Max(size.Height, renderingArea.Height);
			var verticalOverflow = alignmentHeight - finalSize.Height;
			if (verticalOverflow > 0 && shouldAlign.vertically)
			{
				switch (vertical)
				{
					case VerticalAlignment.Center:
						renderingArea.Y -= verticalOverflow / 2.0;
						break;

					case VerticalAlignment.Bottom:
						renderingArea.Y -= verticalOverflow;
						break;
				}
			}

			size = LayoutRound(size);
			renderingArea = LayoutRound(renderingArea);

			var twoHalfStrokeThickness = ActualStrokeThickness;
			var halfStrokeThickness = twoHalfStrokeThickness / 2.0;
			renderingArea.X += halfStrokeThickness;
			renderingArea.Y += halfStrokeThickness;
			renderingArea.Width -= twoHalfStrokeThickness;
			renderingArea.Height -= twoHalfStrokeThickness;

			return (size, renderingArea);
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

			// Workaround until https://github.com/unoplatform/uno/pull/13391 is merged.
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

		private protected Size ArrangeAbsoluteShape(Size finalSize, NativePath? path, FillRule fillRule = FillRule.EvenOdd)
		{
			if (path! == null!)
			{
				Render(null);
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

			GetStretchMetrics(stretch, strokeThickness, finalSize, pathBounds, out var xScale, out var yScale, out var dX, out var dY, out var stretchedSize);

#if __IOS__ || __MACOS__
			// Finally render the shape in a Layer
			var renderTransform = new CoreGraphics.CGAffineTransform(
				(nfloat)xScale, 0,
				0, (nfloat)yScale,
				(nfloat)dX, (nfloat)dY);

			var renderPath = new CoreGraphics.CGPath(path, renderTransform);

			Render(renderPath, fillRule);
#elif __SKIA__
			Render(path, xScale, yScale, dX, dY);
#elif __ANDROID__ || __WASM__
			Render(path, stretchedSize, xScale, yScale, dX, dY);
#endif

			return stretchedSize;
		}
		#endregion

		#region Helper methods

		// NOTE: The logic is mostly from https://github.com/dotnet/wpf/blob/2ff355a607d79eef5fea7796de1f29cf9ea4fbed/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shapes/Shape.cs
		// But with few adjustments to match UWP.
		internal void GetStretchMetrics(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds,
			out double xScale, out double yScale, out double dX, out double dY, out Size stretchedSize)
		{
			if (mode == Stretch.None)
			{
				xScale = 1;
				yScale = 1;
				dX = 0;
				dY = 0;
				stretchedSize = availableSize;
				return;
			}

			if (!geometryBounds.IsEmpty)
			{
				double margin = strokeThickness / 2;

				xScale = Math.Max(availableSize.Width - strokeThickness, 0);
				yScale = Math.Max(availableSize.Height - strokeThickness, 0);

				if (geometryBounds.Width > xScale * Double.Epsilon)
				{
					xScale /= geometryBounds.Width;
				}
				else
				{
					xScale = 1;
				}

				if (geometryBounds.Height > yScale * Double.Epsilon)
				{
					yScale /= geometryBounds.Height;
				}
				else
				{
					yScale = 1;
				}

				if (mode == Stretch.Uniform)
				{
					var uniformScale = Math.Min(xScale, yScale);
					xScale = yScale = uniformScale;
				}
				else if (mode == Stretch.UniformToFill)
				{
					var uniformScale = Math.Max(xScale, yScale);
					xScale = yScale = uniformScale;
				}

				dX = margin - geometryBounds.Left * xScale;
				dY = margin - geometryBounds.Top * yScale;

				stretchedSize = new Size(
					geometryBounds.Width * xScale + strokeThickness, geometryBounds.Height * yScale + strokeThickness);
			}
			else
			{
				xScale = yScale = 1;
				dX = dY = 0;
				stretchedSize = new Size(0, 0);
			}
		}


		#endregion
	}
}
#endif
