using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI;
using Foundation;
using Windows.Foundation;
using Stretch = Windows.UI.Xaml.Media.Stretch;
#if XAMARIN_IOS_UNIFIED
using UIKit;
using CoreAnimation;
using CoreGraphics;
using _Color = UIKit.UIColor;
using _BezierPath = UIKit.UIBezierPath;
#elif __MACOS__
using AppKit;
using CoreAnimation;
using CoreGraphics;
using _Color = AppKit.NSColor;
using _BezierPath = AppKit.NSBezierPath;
#endif

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle
	{
		//CAShapeLayer _rectangleLayer = new CAShapeLayer();
		//CAGradientLayer _gradientLayer;
		//SerialDisposable _fillSubscription = new SerialDisposable();
		//SerialDisposable _strokeSubscription = new SerialDisposable();

		public Rectangle()
		{
			ClipsToBounds = true;

			//Set default stretch value
			Stretch = Stretch.Fill;

//#if __IOS__
//			// Background color is black by default, if and only if overriding Draw(CGRect rect).
//			base.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
//#else
//			base.WantsLayer = true;
//			base.Layer.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
//#endif

//			Layer.AddSublayer(_rectangleLayer);

//			_rectangleLayer.FillColor = _Color.Clear.CGColor;
		}

		//protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		//{
		//	// Don't call base, we need to keep UIView.BackgroundColor set to transparent
		//	// because we're overriding draw.
		//}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (size, pathArea) = ArrangeRelativeShape(finalSize);

			if (pathArea.Width > 0 && pathArea.Height > 0)
			{
				var path = Math.Max(RadiusX, RadiusY) > 0
#if __IOS__
					? _BezierPath.FromRoundedRect(pathArea, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath
#else
					? _BezierPath.FromRoundedRect(area, (nfloat)RadiusX, (nfloat)RadiusY).ToCGPath()
#endif
					: _BezierPath.FromRect(pathArea).ToCGPath();

				Render(path);
			}

			return size;
		}

//		protected override Size MeasureOverride(Size availableSize)
//		{
//			// Note: This is the exact WinUI behavior for the DesiredSize:
//			//	* If available is lower than defined by user, we return 0x0
//			//	* If use the availableSize only if the Stretch mode is UniformToFill, no matter the <Horizontal|Vertical>Alignment

//			var userMinSize = new Size(MinWidth, MinHeight);
//			var userSize = new Size(Width, Height);

//			//var size = GetUserSize();

//			var stretch = Stretch;
//			var size = userSize;

//			if (double.IsNaN(size.Width))
//			{
//				size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
//					? availableSize.Width
//					: 0;
//			}
//			if (double.IsNaN(size.Height))
//			{
//				size.Height = stretch == Stretch.UniformToFill || VerticalAlignment == VerticalAlignment.Stretch
//					? availableSize.Height
//					: 0;
//			}

//			//if (Stretch == Stretch.UniformToFill)
//			//{
//			//	if (double.IsNaN(size.Width))
//			//	{
//			//		size.Width = availableSize.Width;
//			//	}
//			//	if (double.IsNaN(size.Height))
//			//	{
//			//		size.Height = availableSize.Height;
//			//	}
//			//}

//			size = size
//				.AtLeast(userMinSize)
//				.NumberOrDefault(userMinSize);

//			if (
//				// The size is not defined/valid, we cannot measure rectangle properly
//				//double.IsNaN(size.Width)
//				//|| double.IsNaN(size.Height)
//				//||
//				double.IsInfinity(size.Width)
//				|| double.IsInfinity(size.Height))

//				//// The size is greater than the available, even if it's weird, WinUI returns default in that cases
//				//|| (size.Width > availableSize.Width && size.Height > availableSize.Height))
//			{
//				// Note: This will be overriden by the Layouter that will enforce the MinSize
//				return default;
//			}
//			else
//			{
//				// The requested size is equals or lower than the available, we will be able to properly render ourself!
//				return size;
//			}
//		}

//		///// <inheritdoc />
//		//private protected override CGPath GetPath(Size userSize, Size availableSize)
//		//{
//		//	//var vertical = VerticalAlignment;
//		//	//var horizontal = HorizontalAlignment;
//		//	//var isConstrained = userSize.Width != double.NaN && userSize.Height != double.NaN;

//		//	//Size size;
//		//	//switch (Stretch)
//		//	//{
//		//	//	case Stretch.None when isConstrained:
//		//	//		size = userSize;
//		//	//		break;

//		//	//	case Stretch.None:
//		//	//		return null;

//		//	//	case Stretch.UniformToFill:
//		//	//		area = new Rect(new Point(), availableSize);
//		//	//		break;
//		//	//}

//		//	return null;
//		//}

//		protected override Size ArrangeOverride(Size finalSize)
//		{
//			//if (finalSize.Width == 0 || finalSize.Height == 0)
//			//{
//			//	return finalSize;
//			//}

//			var horizontal = HorizontalAlignment;
//			var vertical = VerticalAlignment;
//			var stretch = Stretch;
//			//var userSize = GetUserSize(); // Includes the min size
//			var userMinSize = new Size(MinWidth, MinHeight);
//			var userSize = new Size(Width, Height);

//			Size size = userSize;
//			//if (stretch == Stretch.UniformToFill)
//			//{
//			//	size = finalSize;
//			//}
//			//else
//			//{
//			//	size = GetUserSize(); // Includes the min size

//			//	if (double.IsNaN(size.Width))
//			//	{
//			//		size.Width = HorizontalAlignment == HorizontalAlignment.Stretch
//			//			? finalSize.Width
//			//			: 0;
//			//	}
//			//	if (double.IsNaN(size.Height))
//			//	{
//			//		size.Height = VerticalAlignment == VerticalAlignment.Stretch
//			//			? finalSize.Height
//			//			: 0;
//			//	}
//			//}

//			if (double.IsNaN(size.Width))
//			{
//				size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
//					? finalSize.Width
//					: 0;
//			}
//			if (double.IsNaN(size.Height))
//			{
//				size.Height = stretch == Stretch.UniformToFill || VerticalAlignment == VerticalAlignment.Stretch
//					? finalSize.Height
//					: 0;
//			}

//			size = size
//				.AtLeast(userMinSize)
//				.NumberOrDefault(userMinSize);

//			//if ((stretch == Stretch.UniformToFill || horizontal == HorizontalAlignment.Stretch) && finalSize.Width > size.Width)
//			//{
//			//	size.Width = finalSize.Width;
//			//}
//			//if ((stretch == Stretch.UniformToFill || vertical == VerticalAlignment.Stretch) && finalSize.Height > size.Height)
//			//{
//			//	size.Height = finalSize.Height;
//			//}

//			// In case of a Uniform* stretch mode, the rectangle will be a square, not matter the defined user size
//			switch (stretch)
//			{
//				case Stretch.None:
//					size = default;
//					break;

//				default:
//				case Stretch.Fill:
//					// size is already valid ... nothing to do!
//					break;

//				case Stretch.Uniform:
//					var squareSideLength = Math.Min(size.Width, size.Height);
//					size = new Size(squareSideLength, squareSideLength);
//					break;

//				case Stretch.UniformToFill:
//					squareSideLength = Math.Max(size.Width, size.Height); ;
//					size = new Size(squareSideLength, squareSideLength);
//					break;
//			}

//			// The area that will be used to render the rectangle/ellipse as path
//			var pathArea = new Rect(new Point(), size);

//			// For the Rectangle and the Ellipse, half of the StrokeThickness has to be excluded on each side of the shape.
//			var twoHalfStrokeThickness = StrokeThickness;
//			var halfStrokeThickness = twoHalfStrokeThickness / 2.0;
//			pathArea.X = halfStrokeThickness;
//			pathArea.Y = halfStrokeThickness;
//			pathArea.Width -= twoHalfStrokeThickness;
//			pathArea.Height -= twoHalfStrokeThickness;

//			// The path will be injected as a Layer, so we also have to apply the horizontal and vertical alignments
//			// Note: We have to make this adjustment only if teh shape is overflowing the container bounds,
//			//		 otherwise the alignment will be correctly applied by the container.
//			var horizontalOverflow = size.Width - finalSize.Width;
//			if (horizontalOverflow > 0
//				&& userSize.Width > finalSize.Width) // WinUI does not adjust alignment if the shape was smaller than the finalSize
//			{
//				switch (horizontal)
//				{
//					case HorizontalAlignment.Center:
//						pathArea.X -= horizontalOverflow / 2.0;
//						break;

//					case HorizontalAlignment.Right:
//						pathArea.X -= horizontalOverflow;
//						break;
//				}
//			}
//			var verticalOverflow = size.Height - finalSize.Height;
//			if (verticalOverflow > 0
//				&& userSize.Height > finalSize.Height) // WinUI does not adjust alignment if the shape was smaller than the finalSize
//			{
//				switch (vertical)
//				{
//					case VerticalAlignment.Center:
//						pathArea.Y -= verticalOverflow / 2.0;
//						break;

//					case VerticalAlignment.Bottom:
//						pathArea.Y -= verticalOverflow;
//						break;
//				}
//			}

//			var path = Math.Max(RadiusX, RadiusY) > 0
//#if __IOS__
//				? _BezierPath.FromRoundedRect(pathArea, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath
//#else
//				? _BezierPath.FromRoundedRect(area, (nfloat)RadiusX, (nfloat)RadiusY).ToCGPath()
//#endif
//				: _BezierPath.FromRect(pathArea).ToCGPath();


//			Render(path);

//			return size;
//		}

		//			var area = Bounds;

		//			switch (Stretch)
		//			{
		//				case Stretch.Fill:
		//					area = Bounds;
		//					break;
		//				case Stretch.Uniform:
		//					area = (area.Height > area.Width)
		//						? (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width))
		//						: (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height));
		//					break;
		//				case Stretch.UniformToFill:
		//					area = (area.Height > area.Width)
		//						? (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height))
		//						: (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width));
		//					break;
		//				default: // None
		//					area = CGRect.Empty;
		//					break;
		//			}

		//			area = area.Shrink((float)ActualStrokeThickness / 2);

		//			if (Math.Max(RadiusX, RadiusY) > 0)
		//			{
		//#if __IOS__
		//				_rectangleLayer.Path = _BezierPath.FromRoundedRect(area, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath;
		//#else
		//				_rectangleLayer.Path = _BezierPath.FromRoundedRect(area, (nfloat)RadiusX, (nfloat)RadiusY).ToCGPath();
		//#endif
		//			}
		//			else
		//			{
		//				_rectangleLayer.Path = _BezierPath.FromRect(area).ToCGPath();
		//			}

		//			if (_gradientLayer != null)
		//			{
		//				_gradientLayer.Frame = _rectangleLayer.Path.BoundingBox;
		//			}

		//			return base.ArrangeOverride(finalSize);

		//protected override void OnStrokeUpdated(Brush newValue)
		//{
		//	base.OnStrokeUpdated(newValue);

		//	var brush = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;

		//	_strokeSubscription.Disposable =
		//		Brush.AssignAndObserveBrush(brush, c => _rectangleLayer.StrokeColor = c);

		//	this.SetNeedsDisplay();
		//}

		//protected override void OnFillChanged(Brush newValue)
		//{
		//	base.OnFillChanged(newValue);

		//	if (_gradientLayer != null)
		//	{
		//		_gradientLayer.RemoveFromSuperLayer();
		//		_gradientLayer = null;
		//	}

		//	_rectangleLayer.FillColor = _Color.Clear.CGColor;

		//	var scbFill = newValue as SolidColorBrush;
		//	var lgbFill = newValue as LinearGradientBrush;
		//	if (scbFill != null)
		//	{
		//		_fillSubscription.Disposable =
		//			Brush.AssignAndObserveBrush(scbFill, c => _rectangleLayer.FillColor = c);
		//	}
		//	else if (lgbFill != null)
		//	{
		//		_gradientLayer = lgbFill.GetLayer(Frame.Size);
		//		Layer.InsertSublayer(_gradientLayer, 0); // We want the _gradientLayer to be below the _rectangleLayer (which contains the stroke)
		//	}

		//	InvalidateMeasure();
		//}

		//protected override void OnStrokeThicknessUpdated(double newValue)
		//{
		//	base.OnStrokeThicknessUpdated(newValue);

		//	if (newValue > 0 && newValue < ViewHelper.OnePixel)
		//	{
		//		StrokeThickness = (float)ViewHelper.OnePixel;
		//		return;
		//	}

		//	_rectangleLayer.LineWidth = (float)newValue;

		//	InvalidateMeasure();
		//}

		//protected override void OnStrokeDashArrayUpdated(DoubleCollection newValue)
		//{
		//	base.OnStrokeDashArrayUpdated(newValue);

		//	_rectangleLayer.LineDashPattern = newValue.Safe().Select(d => new NSNumber(d)).ToArray();

		//	this.SetNeedsDisplay();
		//}

		//protected override void OnStretchUpdated(Stretch newValue)
		//{
		//	base.OnStretchUpdated(newValue);

		//	InvalidateMeasure();
		//}

		partial void OnRadiusXChangedPartial()
		{
			InvalidateArrange();
		}

		partial void OnRadiusYChangedPartial()
		{
			InvalidateArrange();
		}
	}
}
