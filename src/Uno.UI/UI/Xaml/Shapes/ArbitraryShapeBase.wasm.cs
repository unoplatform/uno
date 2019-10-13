using System;
using System.Diagnostics;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using System.Numerics;

namespace Windows.UI.Xaml.Shapes
{
	partial class ArbitraryShapeBase
	{
#pragma warning disable CS0067, CS0649
		private double _scaleX;
		private double _scaleY;
#pragma warning restore CS0067, CS0649

		private IDisposable BuildDrawableLayer()
		{
			return Disposable.Empty;
		}

		private Size GetActualSize() => Size.Empty;

		protected override Size MeasureOverride(Size availableSize)
		{
			// We make sure to invoke native methods while not in the visual tree
			// (For instance getBBox will fail on FF)
			if (Parent == null)
			{
				return new Size();
			}

			var measurements = GetMeasurements(availableSize);
			var desiredSize = measurements.desiredSize;

			var s = base.MeasureOverride(availableSize);

			return desiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			// We make sure to invoke native methods while not in the visual tree
			// (For instance getBBox will fail on FF)
			if (Parent == null)
			{
				return new Size();
			}

			var measurements = GetMeasurements(finalSize);

			var scale = Matrix3x2.CreateScale((float)measurements.scaleX, (float)measurements.scaleY);
			var translate = Matrix3x2.CreateTranslation((float)measurements.translateX, (float)measurements.translateY);
			var matrix = translate * scale;

			foreach (FrameworkElement child in GetChildren())
			{
				child.SetNativeTransform(matrix);
			}

			return finalSize;
		}

		private (Size desiredSize, double translateX, double translateY, double scaleX, double scaleY) GetMeasurements(Size availableSize)
		{
			// Get the content size using Bounding Box on it.
			var contentBBox = GetBBoxOfChildrenWithStrokeThickness();

			if (Stretch == Stretch.None)
			{
				// No scaling, just output the bounding box unchanged
				return (new Size(contentBBox.Right, contentBBox.Bottom), 0, 0, 1, 1);
			}

			var contentAspectRatio = contentBBox.Width / contentBBox.Height;

			//  Calculate the control size
			var calculatedWidth = LimitWithUserSize(availableSize.Width, Width, contentBBox.Width);
			var calculatedHeight = LimitWithUserSize(availableSize.Height, Height, calculatedWidth / contentAspectRatio);

			var translateX = contentBBox.X * -1;
			var translateY = contentBBox.Y * -1;

			if (Stretch == Stretch.Fill)
			{
				// Full scaling without keeping aspect ratio
				var sx = calculatedWidth / contentBBox.Width;
				var sy = calculatedHeight / contentBBox.Height;
				return (new Size(calculatedWidth, calculatedHeight), translateX, translateY, sx, sy);
			}

			// Check if the content wider than the control surface
			var calculatedAspectRatio = calculatedWidth / calculatedHeight;
			var isContentWiderThanControl = contentAspectRatio > calculatedAspectRatio;

			var width = calculatedWidth;
			var height = calculatedHeight;
			var scaleX = calculatedWidth / contentBBox.Width;
			var scaleY = calculatedHeight / contentBBox.Height;

			if (Stretch == Stretch.Uniform)
			{
				if (isContentWiderThanControl)
				{
					height = width / contentAspectRatio;
					scaleY = scaleX;
				}
				else
				{
					width = height * contentAspectRatio;
					scaleX = scaleY;
				}
			}
			else if (Stretch == Stretch.UniformToFill)
			{
				if (isContentWiderThanControl)
				{
					width = height * contentAspectRatio;
					scaleX = scaleY;
				}
				else
				{
					height = width / contentAspectRatio;
					scaleY = scaleX;
				}
			}
			else
			{
				throw new InvalidOperationException("Unknown stretch mode.");
			}

			var desiredSize = new Size(width, height);

			var measurements = (desiredSize, translateX, translateY, scaleX, scaleY);

			return measurements;
		}

		private Rect GetBBoxOfChildrenWithStrokeThickness()
		{
			var bbox = Rect.Empty;

			foreach (FrameworkElement child in GetChildren())
			{
				var childRect = GetBBoxWithStrokeThickness(child);
				if (bbox == Rect.Empty)
				{
					bbox = childRect;
				}
				else
				{
					bbox.Union(childRect);
				}
			}

			return bbox;
		}

		private Rect GetBBoxWithStrokeThickness(FrameworkElement element)
		{
			var bbox = element.GetBBox();
			if (Stroke == null || StrokeThickness < double.Epsilon)
			{
				return bbox;
			}

			var halfStrokeThickness = StrokeThickness / 2;

			var x = Math.Min(bbox.X, bbox.Left - halfStrokeThickness);
			var y = Math.Min(bbox.Y, bbox.Top - halfStrokeThickness);
			var width = bbox.Right + halfStrokeThickness - x;
			var height = bbox.Bottom + halfStrokeThickness - y;

			var bBoxWithStrokeThickness = new Rect(x, y, width, height);

			return bBoxWithStrokeThickness;
		}
	}
}
