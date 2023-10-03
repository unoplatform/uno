#nullable enable

#if __APPLE_UIKIT__ || __ANDROID__
using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using static System.Double;
using System.Diagnostics;

#if __APPLE_UIKIT__
using NativePath = CoreGraphics.CGPath;
using ObjCRuntime;
using NativeSingle = System.Runtime.InteropServices.NFloat;
#elif __ANDROID__
using NativePath = Android.Graphics.Path;
using NativeSingle = System.Double;
#endif

namespace Microsoft.UI.Xaml.Shapes;

partial class Shape
{
	private protected Size ArrangeAbsoluteShape(Size finalSize, NativePath? path, FillRule fillRule = FillRule.EvenOdd)
	{
		if (path! == null!)
		{
			Render(null);
			return default;
		}

		var horizontal = HorizontalAlignment;
		var vertical = VerticalAlignment;
		var stretch = Stretch;
		var userSize = GetUserSizes();
		var strokeThickness = StrokeThickness;
		var halfStrokeThickness = strokeThickness / 2.0;
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

		// Compute the final size of the Shape and the render properties
		Size size;
		(double x, double y) renderOrigin, renderScale, renderOverflow;
		switch (stretch)
		{
			default:
			case Stretch.None:
				var pathNaturalSize = new Size(pathBounds.Right + halfStrokeThickness, pathBounds.Bottom + halfStrokeThickness);
				var (userMinSize, userMaxSize) = GetMinMax(userSize);

				var clampedSize = pathNaturalSize.AtMost(userMaxSize).AtLeast(userMinSize); // The size defined on the Shape has priority over the size of the geometry itself!
				renderScale = (1, 1);
				renderOrigin = (0, 0);
				renderOverflow = (clampedSize.Width - finalSize.Width, clampedSize.Height - finalSize.Height); // We do not add halfStrokeThickness: The stroke is allowed to flow out of container for None

				// Stretch none forces top/left alignment in the parent.
				size = finalSize;
				break;

			case Stretch.Fill:
				size = ComputeSizeLowerThanBounds(userSize, finalSize);
				renderScale = ComputeScaleFactors(pathSize, ref size, strokeThickness);
				renderOrigin = (halfStrokeThickness - pathBounds.X * renderScale.x, halfStrokeThickness - pathBounds.Y * renderScale.y);
				renderOverflow = (size.Width - finalSize.Width, size.Height - finalSize.Height);
				break;

			case Stretch.Uniform:
#if !IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED
				var boundsAdjustements = AdjustRenderingBounds(userSize, ref finalSize, horizontal, vertical);
#endif
				var defaultSize = size = ComputeSizeLowerThanBounds(userSize, finalSize);

				// This set of rules are a complete non sense as we should normally just use userSize.min.width and userSize.min.height,
				// but they are actually reproducing a bug of WinUI where the MinWidth and MinHeight are sometimes
				// constrained by the layout slot ...
				// Note: This is only a replication of what we observed in the UI tests, and might have some flaw in the logic.
				//		 Especially, we expect that the max vs. min applied on Width vs. Height is probably drove by the aspect ratio.
				var minSizeForScale = default(Size);
				if (userSize.min.hasWidth && userSize.min.hasHeight)
				{
					var min = Math.Min(userSize.min.width, userSize.min.height);
					minSizeForScale = new Size(0, min);
				}
				else if (userSize.min.hasWidth && this is Path)
				{
					var min = Math.Min(finalSize.Width, finalSize.Height).AtMost(userSize.min.width);
					minSizeForScale = new Size(min, min);
				}
				else if (userSize.min.hasWidth)
				{
					var max = Math.Max(finalSize.Width, finalSize.Height).AtMost(userSize.min.width);
					minSizeForScale = new Size(max, max);
				}
				else if (userSize.min.hasHeight)
				{
					var min = Math.Min(finalSize.Width, finalSize.Height).AtMost(userSize.min.height);
					minSizeForScale = new Size(min, min);
				}

				do
				{
					renderScale = ComputeScaleFactors(pathSize, ref size, strokeThickness);

					if (renderScale.x >= renderScale.y)
					{
						renderScale.x = renderScale.y;
						size.Width = pathSize.Width * renderScale.x + strokeThickness;
					}
					else
					{
						renderScale.y = renderScale.x;
						size.Height = pathSize.Height * renderScale.y + strokeThickness;
					}

					// Make sure that the  current scale does permit us to respect user's min size.
					// If not we we scale up the target render size to make sure to fit requirements and restart the scale computation.
					// Note: We need to re-invoke the ComputeScaleFactors in order to properly apply StrokeThickness
					if (userSize.min.hasWidth && size.Width < minSizeForScale.Width)
					{
						var adjustmentScale = minSizeForScale.Width / size.Width;
						defaultSize = size = defaultSize.Multiply(adjustmentScale);
						renderScale.x = MinValue; // Make sure to restart computation
					}
					else if (userSize.min.hasHeight && size.Height < minSizeForScale.Height)
					{
						var adjustmentScale = minSizeForScale.Height / size.Height;
						defaultSize = size = defaultSize.Multiply(adjustmentScale);
						renderScale.y = MinValue; // Make sure to restart computation
					}
				} while (renderScale.y != renderScale.x);

				renderOrigin = (halfStrokeThickness - pathBounds.X * renderScale.x, halfStrokeThickness - pathBounds.Y * renderScale.y);
				renderOverflow = (size.Width - finalSize.Width, size.Height - finalSize.Height);

#if !IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED
				AdjustRenderingOffsets(boundsAdjustements, ref renderOrigin, renderOverflow, horizontal, vertical);
#endif
				break;

			case Stretch.UniformToFill:
				size = GetMinMax(userSize).min.AtLeast(finalSize);
				renderScale = ComputeScaleFactors(pathSize, ref size, strokeThickness);
				var unScaledSize = size;
				if (renderScale.x < renderScale.y)
				{
					renderScale.x = renderScale.y;
					size.Width = pathSize.Width * renderScale.x + strokeThickness;
				}
				else
				{
					renderScale.y = renderScale.x;
					size.Height = pathSize.Height * renderScale.y + strokeThickness;
				}

				renderOrigin = (halfStrokeThickness - pathBounds.X * renderScale.x, halfStrokeThickness - pathBounds.Y * renderScale.y);
				// Reproduces a bug of WinUI where it's the size without the stretch that is being used to compute the alignments below
				renderOverflow = (
					userSize.hasWidth ? unScaledSize.Width - finalSize.Width : size.Width - finalSize.Width,
					userSize.hasHeight ? unScaledSize.Height - finalSize.Height : size.Height - finalSize.Height
				);
				break;
		}

		// As the Shape is rendered as a Layer which does not take in consideration alignment (when size is larger than finalSize),
		// compute the offset to apply to the rendering layer.
		var renderCenteredByDefault = stretch != Stretch.None;
		if (renderOverflow.x > 0
			&& (!userSize.hasWidth || userSize.width > finalSize.Width)
			&& (!userSize.max.hasWidth || userSize.max.width > finalSize.Width)) // WinUI does not adjust alignment if the shape was smaller than the finalSize
		{
			switch (horizontal)
			{
				case HorizontalAlignment.Center:
					renderOrigin.x -= renderOverflow.x / 2.0;
					break;

				case HorizontalAlignment.Right:
					renderOrigin.x -= renderOverflow.x;
					break;
			}
		}
		else if (renderCenteredByDefault && renderOverflow.x < 0 && horizontal == HorizontalAlignment.Stretch)
		{
			// It might happen that even stretched, the shape does not use all the finalSize width,
			// in that case it's centered by WinUI.
			renderOrigin.x -= renderOverflow.x / 2.0;
		}

		if (renderOverflow.y > 0
			&& (!userSize.hasHeight || userSize.height > finalSize.Height)
			&& (!userSize.max.hasHeight || userSize.max.height > finalSize.Height)) // WinUI does not adjust alignment if the shape was smaller than the finalSize
		{
			switch (vertical)
			{
				case VerticalAlignment.Center:
					renderOrigin.y -= renderOverflow.y / 2.0;
					break;

				case VerticalAlignment.Bottom:
					renderOrigin.y -= renderOverflow.y;
					break;
			}
		}
		else if (renderCenteredByDefault && renderOverflow.y < 0 && vertical == VerticalAlignment.Stretch)
		{
			// It might happen that even stretched, the shape does not use all the finalSize height,
			// in that case it's centered by WinUI.
			renderOrigin.y -= renderOverflow.y / 2.0;
		}

#if __APPLE_UIKIT__
		// Finally render the shape in a Layer
		var renderTransform = new CoreGraphics.CGAffineTransform(
			(nfloat)renderScale.x, 0,
			0, (nfloat)renderScale.y,
			(nfloat)renderOrigin.x, (nfloat)renderOrigin.y);

		var renderPath = new CoreGraphics.CGPath(path, renderTransform);

		Render(renderPath, fillRule);
#if __APPLE_UIKIT__
		// If the Shape does not have size defined, and natural size of the geometry is lower than the finalSize,
		// then we don't clip the shape!
		ClipsToBounds = stretch != Stretch.None
			|| userSize.hasWidth || userSize.max.hasWidth || userSize.hasHeight || userSize.max.hasHeight
			|| pathSize.Width > finalSize.Width || pathSize.Height > finalSize.Height;
#endif
#elif __ANDROID__
		Render(path, size, renderScale.x, renderScale.y, renderOrigin.x, renderOrigin.y);
#endif

		return size;
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
	#region Helper methods

	private
		(
			double width, bool hasWidth, double height, bool hasHeight,
			(double width, bool hasWidth, double height, bool hasHeight) min,
			(double width, bool hasWidth, double height, bool hasHeight) max
		)
		GetUserSizes()
	{
		var width = Width;
		var height = Height;
		var minWidth = MinWidth;
		var minHeight = MinHeight;
		var maxWidth = MaxWidth.AtLeast(minWidth); // UWP is applying "min" after "max", so if "min" > "max", "min" wins
		var maxHeight = MaxHeight.AtLeast(minHeight);

		return (
			width, !IsNaN(width), height, !IsNaN(height),
			(minWidth, IsFinite(minWidth) && minWidth > 0, minHeight, IsFinite(minHeight) && minHeight > 0),
			(maxWidth, IsFinite(maxWidth), maxHeight, IsFinite(maxHeight)));
	}

	// This replicates the behavior of LayoutHelper.GetMinMax() without reading again all DependencyProperties
	private (Size min, Size max) GetMinMax(
		(
			double width, bool hasWidth, double height, bool hasHeight,
			(double width, bool hasWidth, double height, bool hasHeight) min,
			(double width, bool hasWidth, double height, bool hasHeight) max
		)
		userSize)
	{
		var size = new Size(userSize.width, userSize.height);
		var minSize = new Size(userSize.min.width, userSize.min.height); ;
		var maxSize = new Size(userSize.max.width, userSize.max.height); ;

		minSize = size
			.NumberOrDefault(new Size(0, 0))
			.AtMost(maxSize)
			.AtLeast(minSize); // UWP is applying "min" after "max", so if "min" > "max", "min" wins

		maxSize = size
			.NumberOrDefault(new Size(PositiveInfinity, PositiveInfinity))
			.AtMost(maxSize)
			.AtLeast(minSize); // UWP is applying "min" after "max", so if "min" > "max", "min" wins

		return (minSize, maxSize);
	}

	private static Size ComputeSizeLowerThanBounds(
		(
			double width, bool hasWidth, double height, bool hasHeight,
			(double width, bool hasWidth, double height, bool hasHeight) min,
			(double width, bool hasWidth, double height, bool hasHeight) max
		) userSize,
		Size finalSize)
	{
		return new Size(
			userSize.hasWidth
				? userSize.width.AtMost(userSize.max.width)
				: userSize.max.width.AtMost(finalSize.Width).FiniteOrDefault(finalSize.Width),
			userSize.hasHeight
				? userSize.height.AtMost(userSize.max.height)
				: userSize.max.height.AtMost(finalSize.Height).FiniteOrDefault(finalSize.Height));
	}

	private static (float x, float y) ComputeScaleFactors(Size geometrySize, ref Size renderSize, double strokeThickness)
	{
		float x, y;
		if (geometrySize.Width < double.Epsilon)
		{
			x = 1;
			renderSize.Width = strokeThickness;
		}
		else if (double.IsInfinity(renderSize.Width))
		{
			x = 1;
			renderSize.Width = geometrySize.Width;
		}
		else
		{
			var renderWidthWithStrokeThickness = Math.Max(renderSize.Width - strokeThickness, 0);
			x = (float)(renderWidthWithStrokeThickness / geometrySize.Width);
		}
		if (geometrySize.Height < double.Epsilon)
		{
			y = 1;
			renderSize.Height = strokeThickness;
		}
		else if (double.IsInfinity(renderSize.Height))
		{
			y = 1;
			renderSize.Height = geometrySize.Height;
		}
		else
		{
			var renderHeightWithStrokeThickness = Math.Max(renderSize.Height - strokeThickness, 0);
			y = (float)(renderHeightWithStrokeThickness / geometrySize.Height);
		}

		return (x, y);
	}

#if !IS_DESIRED_SMALLER_THAN_CONSTRAINTS_ALLOWED
	private (bool, Size parentFinalSize, Size finalSize) AdjustRenderingBounds(
		(
			double width, bool hasWidth, double height, bool hasHeight,
			(double width, bool hasWidth, double height, bool hasHeight) min,
			(double width, bool hasWidth, double height, bool hasHeight) max
		) userSize,
		ref Size finalSize,
		HorizontalAlignment horizontal,
		VerticalAlignment vertical)
	{
		// Unlike WinUI, on Uno the layouter does not allow a FrameworkElement to return a size smaller than the [Min]<Width|Height> in its Measure
		// (it will forcefully apply the size and the min size to the value returned by the Measure before storing it in the DesiredSize).
		// But when stretch is Uniform, if for instance the geometry is 100x100, the (min) size is 200x300 and the available size is 200x200,
		// then on WinUI the resulting shape (and its DesiredSize) will be 200x200, which is obviously smaller than the (min) size!
		// So here it's a workaround that will detect that specific case (isBeingStretchedByParent),
		// and then applies an offset to the rendering origin to compensate the wrong size used by the parent for alignments.

		var parentFinalSize = finalSize;
		var availableWhenStretchedForSize = _realDesiredSize;
		var availableWhenStretchedForMin = DesiredSize; // We use the DesiredSize since it's the actual "available" size in that case.

		var isForcefullyStretchedByParent = false;
		if (parentFinalSize.Width > availableWhenStretchedForMin.Width
			&& userSize.min.hasWidth && userSize.min.width == parentFinalSize.Width)
		{
			isForcefullyStretchedByParent = true;
			finalSize.Width = availableWhenStretchedForMin.Width;
		}
		else if (
			// It's not expected to be stretched but parent is trying to stretch us ...
			horizontal != HorizontalAlignment.Stretch && parentFinalSize.Width > availableWhenStretchedForSize.Width
			// ... and we do have a Width defined on us which is larger that the measured size.
			&& userSize.hasWidth && userSize.width > availableWhenStretchedForSize.Width
		)
		{
			isForcefullyStretchedByParent = true;
			finalSize.Width = availableWhenStretchedForSize.Width;
		}

		if (parentFinalSize.Height > availableWhenStretchedForMin.Height
			&& userSize.min.hasHeight && userSize.min.height == parentFinalSize.Height)
		{
			isForcefullyStretchedByParent = true;
			finalSize.Height = availableWhenStretchedForMin.Height;
		}
		else if (
			vertical != VerticalAlignment.Stretch && parentFinalSize.Height > availableWhenStretchedForSize.Height
			&& userSize.hasHeight && userSize.height > availableWhenStretchedForSize.Height)
		{
			isForcefullyStretchedByParent = true;
			finalSize.Height = availableWhenStretchedForSize.Height;
		}

		return (isForcefullyStretchedByParent, parentFinalSize, finalSize);
	}

	private static void AdjustRenderingOffsets(
		(bool isForcefullyStretchedByParent, Size parentFinalSize, Size finalSize) boundsAdjustements,
		ref (double x, double y) renderOrigin,
		(double x, double y) renderOverflow,
		HorizontalAlignment horizontal,
		VerticalAlignment vertical)
	{
		var (isForcefullyStretchedByParent, parentFinalSize, finalSize) = boundsAdjustements;
		if (isForcefullyStretchedByParent)
		{
			// The parent will use the (min) size to align this Shape, so here we offset the renderOrigin by the opposite value that is going to be applied.
			// Notes about the renderOverflow:
			//		* As the parent aligns us using the wrong size, we have to apply it by ourselves for all alignments
			//		* For alignment=Stretch, it will be applied by the standard code path below, so don't apply it here.
			var overflowCorrection = (x: parentFinalSize.Width - finalSize.Width, y: parentFinalSize.Height - finalSize.Height);
			if (overflowCorrection.x > 0)
			{
				switch (horizontal)
				{
					case HorizontalAlignment.Center when renderOverflow.x < 0: renderOrigin.x += (overflowCorrection.x - renderOverflow.x) / 2.0; break;
					case HorizontalAlignment.Right when renderOverflow.x < 0: renderOrigin.x += (overflowCorrection.x - renderOverflow.x); break;
					case HorizontalAlignment.Center: renderOrigin.x += overflowCorrection.x / 2.0; break;
					case HorizontalAlignment.Right: renderOrigin.x += overflowCorrection.x; break;
				}
			}

			if (overflowCorrection.y > 0)
			{
				switch (vertical)
				{
					case VerticalAlignment.Center when renderOverflow.y < 0: renderOrigin.y += (overflowCorrection.y - renderOverflow.y) / 2.0; break;
					case VerticalAlignment.Bottom when renderOverflow.y < 0: renderOrigin.y += (overflowCorrection.y - renderOverflow.y); break;
					case VerticalAlignment.Center: renderOrigin.y += overflowCorrection.y / 2.0; break;
					case VerticalAlignment.Bottom: renderOrigin.y += overflowCorrection.y; break;
				}
			}
		}
	}
#endif

	#endregion

}
#endif
