using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using CoreAnimation;
using CoreGraphics;
using UIKit;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	partial class Shape
	{
		// Drawing scale
		//private float _scaleX;
		//private float _scaleY;

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			RefreshShape();
		}

		//internal override void OnLayoutUpdated()
		//{
		//	base.OnLayoutUpdated();
		//	RefreshShape();
		//}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			RefreshShape();
		}

		//private protected abstract CGPath GetPath(Size userSize, Size availableSize);
		//private protected abstract bool ShouldPreserveOrigin { get; }

		private CALayer _shapeLayer;

		private protected void Render(CGPath path)
		{
			//if (!IsLoaded)
			//{
			//	// For safety purpose! This method should invoked only from ArrangeOverride 
			//	return;
			//}

			// Remove the old layer if any
			_shapeLayer?.RemoveFromSuperLayer();

			// Well ... nothing to do !
			if (path == null)
			{
				_shapeLayer = null;
				return;
			}

			_shapeLayer = CreateLayer(path);
			Layer.AddSublayer(_shapeLayer);
		}

		//protected override Size MeasureOverride(Size availableSize)
		//{
		//	var userMinSize = this.GetMinSize();
		//	var userSize = new Size(Width, Height).AtLeast(userMinSize).NumberOrDefault(userMinSize);
		//	var path = GetPath(userSize, availableSize);
		//	if (path == null)
		//	{
		//		return default;
		//	}

		//	var pathBounds = path.BoundingBox;
		//	var pathSize = (Windows.Foundation.Size)pathBounds.Size;
		//	if (pathSize == default)
		//	{
		//		return default;
		//	}

		//	// For shapes that has an offset from the origin, if the stretch mode is not None we remove this offset.
		//	// cf. remarks of ShouldPreserveOrigin XML doc.
		//	if (ShouldPreserveOrigin)
		//	{
		//		// On iOS 11, the origin (X, Y) of bounds could be infinite, leading to strange results.
		//		if (!nfloat.IsInfinity(pathBounds.X))
		//		{
		//			pathSize.Width += pathBounds.X;
		//		}
		//		if (!nfloat.IsInfinity(pathBounds.Y))
		//		{
		//			pathSize.Height += pathBounds.Y;
		//		}
		//	}

		//	//var availableWidth = availableSize.Width;
		//	//var availableHeight = availableSize.Height;
		//	//var userWidth = this.Width;
		//	//var userHeight = this.Height;

		//	// As weird as it seems, it's how WinUI behaves for the measure!
		//	// Note: .5 so if thickness is 1, we have 1, but if .8 we have 0 ... like WinUI
		//	//var halfStrokeThickness = Math.Floor((ActualStrokeThickness + .5f) / 2.0);
		//	var halfStrokeThickness = GetHalfStrokeThickness();
		//	pathSize = pathSize.Add(new Size(halfStrokeThickness, halfStrokeThickness));

		//	//// For safety, we make sure to remove NaN (but not infinity at this point)
		//	//availableSize = availableSize.NumberOrDefault(pathSize);

		//	//var size = this.ApplySizeConstraints(availableSize)
		//	//	.NumberOrDefault(pathSize)
		//	//	.Add(new Size(halfStrokeThickness, halfStrokeThickness));

		//	//var controlWidth = availableWidth <= 0 ? userWidth : availableWidth;
		//	//var controlHeight = availableHeight <= 0 ? userHeight : availableHeight;

		//	// Default values
		//	//var calculatedWidth = LimitWithUserSize(controlWidth, userWidth, pathWidth);
		//	//var calculatedHeight = LimitWithUserSize(controlHeight, userHeight, pathHeight);

		//	//var strokeThickness = this.ActualStrokeThickness;
		//	//var strokeThicknessF = (float)strokeThickness;

		//	//// At this point 'path<Width|Height>' might be 0, especially for vertical / horizontal Line
		//	//_scaleX = pathWidth == 0 ? 1 : (nfloat)size.Width / pathWidth;
		//	//_scaleY = pathHeight == 0 ? 1 : (nfloat)size.Height / pathHeight;

		//	//Make sure that we have a valid scale if both of them are not set
		//	//if (double.IsInfinity((double)_scaleX)
		//	//	&& double.IsInfinity((double)_scaleY))
		//	//{
		//	//	_scaleX = 1;
		//	//	_scaleY = 1;
		//	//}

		//	// Here we will override some of the default values
		//	switch (Stretch)
		//	{
		//		case Stretch.None:
		//			_scaleX = 1;
		//			_scaleY = 1;
		//			break;

		//		case Stretch.Fill:
		//			(_scaleX, _scaleY) = GetScale(pathSize, availableSize);
		//			break;

		//		case Stretch.Uniform:
		//		{
		//			var scale = GetScale(pathSize, availableSize);
		//			_scaleX = _scaleY = Math.Min(scale.x, scale.y);
		//			break;
		//		}
		//		case Stretch.UniformToFill:
		//		{
		//			var scale = GetScale(pathSize, availableSize);
		//			_scaleX = _scaleY = Math.Max(scale.x, scale.y);
		//			break;
		//		}
		//	}

		//	//calculatedWidth += strokeThickness;
		//	//calculatedHeight += strokeThickness;

		//	return new Size(pathSize.Width * _scaleX, pathSize.Height * _scaleY);
		//}

		//private CGRect GetActualSize() => Bounds;

		//private IDisposable BuildDrawableLayer()
		//{
		//	if (Bounds == CGRect.Empty)
		//	{
		//		return Disposable.Empty;
		//	}

		//	var newLayer = CreateLayerOrDefault();
		//	if (newLayer == null)
		//	{
		//		return Disposable.Empty;
		//	}

		//	Layer.AddSublayer(newLayer);
		//	return Disposable.Create(() => newLayer.RemoveFromSuperLayer());
		//}


		private protected Size MeasureRelativeShape(Size availableSize)
		{
			var stretch = Stretch;
			var userMinSize = new Size(MinWidth, MinHeight);
			var userSize = new Size(Width, Height);

			var size = userSize;

			// If no user size defined on a given axis, we try to stretch along this axis
			if (double.IsNaN(size.Width))
			{
				size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
					? availableSize.Width
					: 0;
			}
			if (double.IsNaN(size.Height))
			{
				size.Height = stretch == Stretch.UniformToFill || VerticalAlignment == VerticalAlignment.Stretch
					? availableSize.Height
					: 0;
			}

			// In case userSize was not defined, we still have to apply the min size
			size = size
				.AtLeast(userMinSize)
				.NumberOrDefault(userMinSize);

			if (double.IsInfinity(size.Width) || double.IsInfinity(size.Height))
			{
				// The size is invalid (the userSize was not defined and we were not able to stretch), just hide the shape.
				// Note: This will be overriden by the Layouter that will enforce the MinSize
				return default;
			}
			else
			{
				return size;
			}
		}
		private protected (Size shapeSize, Rect pathArea) ArrangeRelativeShape(Size finalSize)
		{
			var horizontal = HorizontalAlignment;
			var vertical = VerticalAlignment;
			var stretch = Stretch;
			var userMinSize = new Size(MinWidth, MinHeight);
			var userSize = new Size(Width, Height);

			var size = userSize;

			// Like for the measure, if no user size defined on a given axis, we try to stretch along this axis
			if (double.IsNaN(size.Width))
			{
				size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
					? finalSize.Width
					: 0;
			}
			if (double.IsNaN(size.Height))
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
			var pathArea = new Rect(new Point(), size);

			// Apply the stretch mode, as it might change the "shape" of a "relative shape"
			switch (stretch)
			{
				case Stretch.None:
					pathArea.Height = pathArea.Width = 0;
					break;

				default:
				case Stretch.Fill:
					// size is already valid ... nothing to do!
					break;

				case Stretch.Uniform when pathArea.Width < pathArea.Height:
					pathArea.Height = pathArea.Width;
					break;

				case Stretch.Uniform: // when pathArea.Width > pathArea.Height:
					pathArea.Width = pathArea.Height;
					break;

				case Stretch.UniformToFill when pathArea.Width < pathArea.Height:
					pathArea.Width = pathArea.Height;
					break;

				case Stretch.UniformToFill: // when pathArea.Width > pathArea.Height:
					pathArea.Height = pathArea.Width;
					break;
			}

			// For the Rectangle and the Ellipse, half of the StrokeThickness has to be excluded on each side of the shape.
			var twoHalfStrokeThickness = StrokeThickness;
			var halfStrokeThickness = twoHalfStrokeThickness / 2.0;
			pathArea.X += halfStrokeThickness;
			pathArea.Y += halfStrokeThickness;
			pathArea.Width -= twoHalfStrokeThickness;
			pathArea.Height -= twoHalfStrokeThickness;

			// The path will be injected as a Layer, so we also have to apply the horizontal and vertical alignments
			// Note: We have to make this adjustment only if the shape is overflowing the container bounds,
			//		 otherwise the alignment will be correctly applied by the container.
			var horizontalOverflow = size.Width - finalSize.Width;
			if (horizontalOverflow > 0
				&& userSize.Width > finalSize.Width) // WinUI does not adjust alignment if the shape was smaller than the finalSize
			{
				switch (horizontal)
				{
					case HorizontalAlignment.Center:
						pathArea.X -= horizontalOverflow / 2.0;
						break;

					case HorizontalAlignment.Right:
						pathArea.X -= horizontalOverflow;
						break;
				}
			}
			var verticalOverflow = size.Height - finalSize.Height;
			if (verticalOverflow > 0
				&& userSize.Height > finalSize.Height) // WinUI does not adjust alignment if the shape was smaller than the finalSize
			{
				switch (vertical)
				{
					case VerticalAlignment.Center:
						pathArea.Y -= verticalOverflow / 2.0;
						break;

					case VerticalAlignment.Bottom:
						pathArea.Y -= verticalOverflow;
						break;
				}
			}

			return (size, pathArea);
		}


		private protected Size MeasureAbsoluteShape(Size availableSize, CGPath path)
		{
			if (path == null)
			{
				return default;
			}

			var pathBounds = path.BoundingBox;
			var pathSize = (Size)pathBounds.Size;
			var userMinSize = new Size(MinWidth, MinHeight);
			var userSize = new Size(Width, Height);
			var stretch = Stretch;

			if (stretch == Stretch.None)
			{
				var halfStrokeThickness = GetHalfStrokeThickness();
				pathSize.Width += halfStrokeThickness;
				pathSize.Height += halfStrokeThickness;

				if (!nfloat.IsInfinity(pathBounds.X))
				{
					pathSize.Width += pathBounds.X;
				}
				if (!nfloat.IsInfinity(pathBounds.Y))
				{
					pathSize.Height += pathBounds.Y;
				}
			}

			var size = userSize; // The size defined on the Shape has priority over the size of the path!

			// If no user size defined on a given axis, we either use the size of the path or we try to stretch along this axis.
			//if (double.IsNaN(size.Width))
			//{
			//	size.Width = stretch == Stretch.UniformToFill || HorizontalAlignment == HorizontalAlignment.Stretch
			//		? Math.Max(availableSize.Width, pathSize.Width)
			//		: pathSize.Width;
			//}
			//if (double.IsNaN(size.Height))
			//{
			//	size.Height = stretch == Stretch.UniformToFill || VerticalAlignment == VerticalAlignment.Stretch
			//		? Math.Max(availableSize.Height, pathSize.Height)
			//		: pathSize.Height;
			//}
			if (double.IsNaN(size.Width))
			{
				size.Width = stretch == Stretch.None
					? pathSize.Width
					: Math.Max(availableSize.Width, pathSize.Width);
			}
			if (double.IsNaN(size.Height))
			{
				size.Height = stretch == Stretch.None
					? pathSize.Height
					: Math.Max(availableSize.Height, pathSize.Height);
			}

			// In case userSize was not defined, we still have to apply the min size
			size = size
				.AtLeast(userMinSize)
				.NumberOrDefault(userMinSize);

			//if (pathSize == default)
			//{
			//	return default;
			//}

			//// On iOS 11, the origin (X, Y) of bounds could be infinite, leading to strange results.
			//if (nfloat.IsInfinity(bounds.X))
			//{
			//	bounds.X = 0;
			//}

			//if (nfloat.IsInfinity(bounds.Y))
			//{
			//	bounds.Y = 0;
			//}

			//var pathWidth = bounds.Width;
			//var pathHeight = bounds.Height;
			//if (pathWidth == 0 && pathHeight == 0)
			//{
			//	return default;
			//}

			//// For shapes that has an offset from the origin, if the stretch mode is not None we remove this offset.
			//// cf. remarks of ShouldPreserveOrigin XML doc.
			//if (Stretch == Stretch.None)
			//{
			//	if (!nfloat.IsInfinity(pathBounds.X))
			//	{
			//		size.Width += pathBounds.X;
			//	}
			//	if (!nfloat.IsInfinity(pathBounds.Y))
			//	{
			//		size.Height += pathBounds.Y;
			//	}
			//}

			//var availableWidth = availableSize.Width;
			//var availableHeight = availableSize.Height;
			//var userWidth = this.Width;
			//var userHeight = this.Height;

			//switch (stretch)
			//{
			//	case Stretch.None:
			//		var halfStrokeThickness = GetHalfStrokeThickness();
			//		size.Width += halfStrokeThickness;
			//		size.Height += halfStrokeThickness;
			//		break;
			//}

			//// As weird as it seems, it's how WinUI behaves for the measure!
			//// Note: .5 so if thickness is 1, we have 1, but if .8 we have 0 ... like WinUI
			////var halfStrokeThickness = Math.Floor((ActualStrokeThickness + .5f) / 2.0);
			//var halfStrokeThickness = GetHalfStrokeThickness();
			//size.Width += halfStrokeThickness;
			//size.Height += halfStrokeThickness;

			//// For safety, we make sure to remove NaN (but not infinity at this point)
			//availableSize = availableSize.NumberOrDefault(pathSize);

			//var size = this.ApplySizeConstraints(availableSize)
			//	.NumberOrDefault(pathSize)
			//	.Add(new Size(halfStrokeThickness, halfStrokeThickness));

			//var controlWidth = availableWidth <= 0 ? userWidth : availableWidth;
			//var controlHeight = availableHeight <= 0 ? userHeight : availableHeight;

			// Default values
			//var calculatedWidth = LimitWithUserSize(controlWidth, userWidth, pathWidth);
			//var calculatedHeight = LimitWithUserSize(controlHeight, userHeight, pathHeight);

			//var strokeThickness = this.ActualStrokeThickness;
			//var strokeThicknessF = (float)strokeThickness;

			//// At this point 'path<Width|Height>' might be 0, especially for vertical / horizontal Line
			//_scaleX = pathWidth == 0 ? 1 : (nfloat)size.Width / pathWidth;
			//_scaleY = pathHeight == 0 ? 1 : (nfloat)size.Height / pathHeight;

			//Make sure that we have a valid scale if both of them are not set
			//if (double.IsInfinity((double)_scaleX)
			//	&& double.IsInfinity((double)_scaleY))
			//{
			//	_scaleX = 1;
			//	_scaleY = 1;
			//}

			// Here we will override some of the default values

			// Compute the scale factor to apply if the path is smaller than the availableSize and we have to stretch.
			//if (size.Width < availableSize.Width || size.Height < availableSize.Height)
			{
				//double scaleX, scaleY;
				switch (stretch)
				{
					case Stretch.None:
						// Nothing to do: Size is already the size at which we will render the path
						break;

					//case Stretch.Fill:
					//	//(scaleX, scaleY) = GetScale(pathSize, size);
					//	break;

					//case Stretch.Uniform:
					//	(scaleX, scaleY) = GetScale(pathSize, size);
					//	if (scaleX < scaleY)
					//	{
					//		size.Height = size.Width;
					//	}
					//	else
					//	{
					//		size.Width = size.Height;
					//	}
					//	break;

					//case Stretch.UniformToFill:
					//	(scaleX, scaleY) = GetScale(pathSize, size);
					//	scaleX = scaleY = Math.Max(scaleX, scaleY);
					//	if (scaleX > scaleY)
					//	{
					//		size.Height = size.Width;
					//	}
					//	else
					//	{
					//		size.Width = size.Height;
					//	}
					//	break;

					case Stretch.Uniform when size.Width < size.Height:
						size.Height = size.Width;
						break;

					case Stretch.Uniform: // when size.Width > size.Height:
						size.Width = size.Height;
						break;

					case Stretch.UniformToFill when size.Width < size.Height:
						size.Width = size.Height;
						break;

					case Stretch.UniformToFill: // when size.Width > size.Height:
						size.Height = size.Width;
						break;
				}
			}

			//// We take 
			//size = size.AtMost(availableSize);

			//calculatedWidth += strokeThickness;
			//calculatedHeight += strokeThickness;

			return size;
		}

		private protected Size ArrangeAbsoluteShape(Size finalSize, CGPath path)
		{
			//if (path == null)
			//{
			//	return default;
			//}

			//var pathBounds = path.BoundingBox;
			//var pathSize = (Windows.Foundation.Size)pathBounds.Size;
			//if (pathSize == default)
			//{
			//	return default;
			//}

			//// For shapes that has an offset from the origin, if the stretch mode is not None we remove this offset.
			//// cf. remarks of ShouldPreserveOrigin XML doc.
			//if (Stretch == Stretch.None)
			//{
			//	// On iOS 11, the origin (X, Y) of bounds could be infinite, leading to strange results.

			//	if (!nfloat.IsInfinity(pathBounds.X))
			//	{
			//		pathSize.Width += pathBounds.X;
			//	}
			//	if (!nfloat.IsInfinity(pathBounds.Y))
			//	{
			//		pathSize.Height += pathBounds.Y;
			//	}
			//}

			//var halfStrokeThickness = GetHalfStrokeThickness();
			//pathSize.Width += halfStrokeThickness;
			//pathSize.Height += halfStrokeThickness;

			//// Compute the scale factor to apply if the path is smaller than the availableSize and we have to stretch.
			//double scaleX, scaleY;
			//if (pathSize.Width < finalSize.Width || pathSize.Height < finalSize.Height)
			//{
			//	switch (Stretch)
			//	{
			//		case Stretch.Fill:
			//			(scaleX, scaleY) = GetScale(pathSize, finalSize);
			//			break;

			//		case Stretch.Uniform:
			//		{
			//			var scale = GetScale(pathSize, finalSize);
			//			scaleX = scaleY = Math.Min(scale.x, scale.y);
			//			break;
			//		}
			//		case Stretch.UniformToFill:
			//		{
			//			var scale = GetScale(pathSize, finalSize);
			//			scaleX = scaleY = Math.Max(scale.x, scale.y);
			//			break;
			//		}

			//		default:
			//		case Stretch.None:
			//			scaleX = 1;
			//			scaleY = 1;
			//			break;
			//	}
			//}
			//else
			//{
			//	scaleX = scaleY = 1;
			//}

			////calculatedWidth += strokeThickness;
			////calculatedHeight += strokeThickness;

			//var size = new Size(pathSize.Width * scaleX, pathSize.Height * scaleY);


			if (path == null)
			{
				return default;
			}

			var userMinSize = new Size(MinWidth, MinHeight);
			var userSize = new Size(Width, Height);
			var stretch = Stretch;
			var halfStrokeThickness = GetHalfStrokeThickness();

			// 1. Compute and adjust the size of the path itself
			var pathBounds = path.BoundingBox;
			var pathSize = (Size)pathBounds.Size;
			var pathOrigin = new Point();

			if (nfloat.IsInfinity(pathBounds.Right)
				|| nfloat.IsInfinity(pathBounds.Bottom))
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
				}

				return default;
			}

			switch (stretch)
			{
				case Stretch.None:
					pathSize.Width += halfStrokeThickness;
					pathSize.Height += halfStrokeThickness;

					pathSize.Width += pathBounds.X;
					pathSize.Height += pathBounds.Y;
					break;

				default:
					pathOrigin.X -= pathBounds.X;
					pathOrigin.Y -= pathBounds.Y;
					break; 
			}

			//if (stretch == Stretch.None)
			//{
			//	pathSize.Width += halfStrokeThickness;
			//	pathSize.Height += halfStrokeThickness;

			//	pathSize.Width += pathBounds.X;
			//	pathSize.Height += pathBounds.Y;
			//}

			// 2. Like the measure adjust, compute the size of Shape element.
			//    This will be used as the size of the area were the path will be rendered.
			var size = userSize; // The size defined on the Shape has priority over the size of the path!



			//switch (stretch)
			//{
			//	case Stretch.None:
			//		break;

			//	case Stretch.Uniform when double.IsNaN(userSize.Width) && double.IsNaN(userSize.Height):
			//		size = new Size

			//}


			// If no user size defined on a given axis, we either use the size of the path or we try to stretch along this axis.
			if (double.IsNaN(size.Width))
			{
				size.Width = stretch == Stretch.None
					? pathSize.Width
					: Math.Max(finalSize.Width, pathSize.Width);
			}
			if (double.IsNaN(size.Height))
			{
				size.Height = stretch == Stretch.None
					? pathSize.Height
					: Math.Max(finalSize.Height, pathSize.Height);
			}

			// In case userSize was not defined, we still have to apply the min size
			size = size
				.AtLeast(userMinSize)
				.NumberOrDefault(userMinSize);

			// Compute the scale factor to apply if the path is smaller than the availableSize and we have to stretch.
			double scaleX = 1, scaleY = 1;
			var unScaledSize = size;
			switch (stretch)
			{
				case Stretch.Fill:
					(scaleX, scaleY) = GetScale(pathSize, size, minus: StrokeThickness);
					// No need to change the size, it's already correct!
					break;

				case Stretch.Uniform when size.Width < size.Height:
					scaleX = scaleY = GetScale(pathSize.Width, size.Width, minus: StrokeThickness);
					size.Height = size.Width;
					break;

				case Stretch.Uniform: // when size.Width > size.Height:
					scaleX = scaleY = GetScale(pathSize.Height, size.Height, minus: StrokeThickness);
					size.Width = size.Height;
					break;

				case Stretch.UniformToFill when size.Width < size.Height:
					scaleX = scaleY = GetScale(pathSize.Height, size.Height, minus: StrokeThickness);
					size.Width = size.Height;
					break;

				case Stretch.UniformToFill: // when size.Width > size.Height:
					scaleX = scaleY = GetScale(pathSize.Width, size.Width, minus: StrokeThickness);
					size.Height = size.Width;
					break;

					//case Stretch.Uniform:
					//	(scaleX, scaleY) = GetScale(pathSize, size, minus: StrokeThickness);
					//	if (scaleX < scaleY)
					//	{
					//		scaleY = scaleX;
					//		size.Height = size.Width;
					//	}
					//	else
					//	{
					//		scaleX = scaleY;
					//		size.Width = size.Height;
					//	}
					//	break;

					//case Stretch.UniformToFill:
					//	(scaleX, scaleY) = GetScale(pathSize, size, minus: StrokeThickness);
					//	scaleX = scaleY = Math.Max(scaleX, scaleY);
					//	if (scaleX > scaleY)
					//	{
					//		scaleY = scaleX;
					//		size.Height = size.Width;
					//	}
					//	else
					//	{
					//		scaleX = scaleY;
					//		size.Width = size.Height;
					//	}
					//	break;
			}

			var pathAlignment = new Point();
			var horizontalOverflow = stretch == Stretch.UniformToFill && !double.IsNaN(userSize.Width)
				? unScaledSize.Width - finalSize.Width // Reproduces a bug of WinUI where it's the size without the stretch that is being used to compute the alignments below
				: size.Width - finalSize.Width;
			if (horizontalOverflow > 0
				&& (double.IsNaN(userSize.Width) || userSize.Width > finalSize.Width)) // WinUI does not adjust alignment if the shape was smaller than the finalSize
			{
				switch (HorizontalAlignment)
				{
					case HorizontalAlignment.Center:
						pathAlignment.X -= horizontalOverflow / 2.0;
						break;

					case HorizontalAlignment.Right:
						pathAlignment.X -= horizontalOverflow;
						break;
				}
			}
			else if (horizontalOverflow < 0 && HorizontalAlignment == HorizontalAlignment.Stretch)
			{
				// It might happen that even stretched, the shape does not use all the finalSize width,
				// in that case it's centered by WinUI.
				pathAlignment.X -= horizontalOverflow / 2.0;
			}

			var verticalOverflow = stretch == Stretch.UniformToFill && !double.IsNaN(userSize.Height)
				? unScaledSize.Height - finalSize.Height // Reproduces a bug of WinUI where it's the size without the stretch that is being used to compute the alignments below
				: size.Height - finalSize.Height;
			if (verticalOverflow > 0
				&& (double.IsNaN(userSize.Height) || userSize.Height > finalSize.Height)) // WinUI does not adjust alignment if the shape was smaller than the finalSize
			{
				switch (VerticalAlignment)
				{
					case VerticalAlignment.Center:
						pathAlignment.Y -= verticalOverflow / 2.0;
						break;

					case VerticalAlignment.Bottom:
						pathAlignment.Y -= verticalOverflow;
						break;
				}
			}
			else if (verticalOverflow < 0 && VerticalAlignment == VerticalAlignment.Stretch)
			{
				// It might happen that even stretched, the shape does not use all the finalSize height,
				// in that case it's centered by WinUI.
				pathAlignment.Y -= verticalOverflow / 2.0;
			}

			//return (size, pathArea);











			//var path = this.GetPath(SizeFromUISize(Bounds.Size));
			//if (path == null)
			//{
			//	return null;
			//}

			//var pathBounds = path.BoundingBox;

			//if (
			//	nfloat.IsInfinity(pathBounds.Right)
			//	|| nfloat.IsInfinity(pathBounds.Bottom)
			//)
			//{
			//	if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			//	{
			//		this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
			//	}

			//	return default;
			//}

			//var scaleX = _scaleX;
			//var scaleY = _scaleY;

			//var stretchMode = Stretch;
			//switch (stretchMode)
			//{
			//	case Stretch.Fill:
			//	case Stretch.None:
			//		break;
			//	case Stretch.Uniform:
			//		scaleX = Math.Min(_scaleX, _scaleY);
			//		scaleY = scaleX;
			//		break;
			//	case Stretch.UniformToFill:
			//		scaleX = Math.Max(_scaleX, _scaleY);
			//		scaleY = scaleX;
			//		break;
			//}

			var transform = new CGAffineTransform(
				(nfloat)scaleX, 0,
				0, (nfloat)scaleY,
				(nfloat)(pathOrigin.X * scaleX + pathAlignment.X + halfStrokeThickness), (nfloat)(pathOrigin.Y * scaleY + pathAlignment.Y + halfStrokeThickness));
			//var transform = CGAffineTransform.MakeScale((nfloat)scaleX, (nfloat)scaleY);

			//if (Stretch != Stretch.None)
			//{
			//	// When stretching, we can't use 0,0 as the origin, but must instead use the path's bounds.
			//	transform.Translate(-pathBounds.Left * (nfloat)scaleX, -pathBounds.Top * (nfloat)scaleY);
			//}

			//if (!ShouldPreserveOrigin)
			//{
			//	// We need to translate the shape to take in account the stroke thickness
			//	// transform.Translate((nfloat)ActualStrokeThickness * 0.5f, (nfloat)ActualStrokeThickness * 0.5f);
			//	var quarterStrokeThickness = (nfloat)(GetHalfStrokeThickness() / 2.0);
			//	transform.Translate(quarterStrokeThickness, quarterStrokeThickness);
			//}

			//if (nfloat.IsNaN(transform.x0) || nfloat.IsNaN(transform.y0) ||
			//	nfloat.IsNaN(transform.xx) || nfloat.IsNaN(transform.yy) ||
			//	nfloat.IsNaN(transform.xy) || nfloat.IsNaN(transform.yx)
			//)
			//{
			//	// transformedPath creation will crash natively if the transform contains NaNs
			//	throw new InvalidOperationException($"transform {transform} contains NaN values, transformation will fail.");
			//}

			var transformedPath = new CGPath(path, transform);
			Render(transformedPath);

			return size;
		}


		private CALayer CreateLayer(CGPath path)
		{
			//var path = this.GetPath(SizeFromUISize(Bounds.Size));
			//if (path == null)
			//{
			//	return null;
			//}

			//var pathBounds = path.BoundingBox;

			//if (
			//	nfloat.IsInfinity(pathBounds.Right)
			//	|| nfloat.IsInfinity(pathBounds.Bottom)
			//)
			//{
			//	if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			//	{
			//		this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
			//	}

			//	return null;
			//}

			//var scaleX = _scaleX;
			//var scaleY = _scaleY;

			////var stretchMode = Stretch;
			////switch (stretchMode)
			////{
			////	case Stretch.Fill:
			////	case Stretch.None:
			////		break;
			////	case Stretch.Uniform:
			////		scaleX = Math.Min(_scaleX, _scaleY);
			////		scaleY = scaleX;
			////		break;
			////	case Stretch.UniformToFill:
			////		scaleX = Math.Max(_scaleX, _scaleY);
			////		scaleY = scaleX;
			////		break;
			////}

			//var transform = CGAffineTransform.MakeScale(scaleX, scaleY);

			//if (!ShouldPreserveOrigin)
			//{
			//	// When stretching, we can't use 0,0 as the origin, but must instead use the path's bounds.
			//	transform.Translate(-pathBounds.Left * scaleX, -pathBounds.Top * scaleY);
			//}

			////if (!ShouldPreserveOrigin)
			////{
			////	// We need to translate the shape to take in account the stroke thickness
			////	// transform.Translate((nfloat)ActualStrokeThickness * 0.5f, (nfloat)ActualStrokeThickness * 0.5f);
			////	var quarterStrokeThickness = (nfloat)(GetHalfStrokeThickness() / 2.0);
			////	transform.Translate(quarterStrokeThickness, quarterStrokeThickness);
			////}

			//if (nfloat.IsNaN(transform.x0) || nfloat.IsNaN(transform.y0) ||
			//	nfloat.IsNaN(transform.xx) || nfloat.IsNaN(transform.yy) ||
			//	nfloat.IsNaN(transform.xy) || nfloat.IsNaN(transform.yx)
			//)
			//{
			//	// transformedPath creation will crash natively if the transform contains NaNs
			//	throw new InvalidOperationException($"transform {transform} contains NaN values, transformation will fail.");
			//}

			////var colorFill = Fill as SolidColorBrush ?? SolidColorBrushHelper.Transparent;
			////var imageFill = Fill as ImageBrush;
			////var gradientFill = Fill as LinearGradientBrush;
			////var stroke = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;
			//var transformedPath = new CGPath(path, transform);




			var pathLayer = new CAShapeLayer()
			{
				Path = path,
				StrokeColor = (Stroke as SolidColorBrush)?.ColorWithOpacity ?? Colors.Transparent,
				LineWidth = (nfloat)ActualStrokeThickness,
			};

			switch (Fill)
			{
				case SolidColorBrush colorFill:
					pathLayer.FillColor = colorFill.ColorWithOpacity;
					break;

				case ImageBrush imageFill when TryCreateImageBrushLayers(imageFill, GetFillMask(path), out var imageLayer):
					pathLayer.FillColor = Colors.Transparent;
					pathLayer.AddSublayer(imageLayer);
					break;

				case LinearGradientBrush gradientFill:
					var gradientLayer = gradientFill.GetLayer(Frame.Size);
					gradientLayer.Frame = Bounds;
					gradientLayer.Mask = GetFillMask(path);
					gradientLayer.MasksToBounds = true;

					pathLayer.FillColor = Colors.Transparent;
					pathLayer.AddSublayer(gradientLayer);
					break;

				case null:
					pathLayer.FillColor = Colors.Transparent;
					break;

				default:
					Application.Current.RaiseRecoverableUnhandledException(new NotSupportedException($"The brush {Fill} is not supported as Fill for a {this} on this platform."));
					pathLayer.FillColor = Colors.Transparent;
					break;
			}

			CAShapeLayer GetFillMask(CGPath mask)
				=> new CAShapeLayer
				{
					Path = mask,
					Frame = Bounds,
					// We only use the fill color to create the mask area
					FillColor = UIColor.White.CGColor,
				};

			//if (colorFill != null)
			//{
			//	layer.FillColor = colorFill.ColorWithOpacity;
			//}

			//if (imageFill != null)
			//{
			//	var fillMask = 

			//	CreateImageBrushLayers(
			//		layer,
			//		imageFill,
			//		fillMask
			//	);
			//}
			//else if (gradientFill != null)
			//{
			//	var fillMask = new CAShapeLayer()
			//	{
			//		Path = transformedPath,
			//		Frame = Bounds,
			//		// We only use the fill color to create the mask area
			//		FillColor = _Color.White.CGColor,
			//	};

			//	var gradientLayer = gradientFill.GetLayer(Frame.Size);
			//	gradientLayer.Frame = Bounds;
			//	gradientLayer.Mask = fillMask;
			//	gradientLayer.MasksToBounds = true;
			//	layer.AddSublayer(gradientLayer);
			//}

			if (StrokeDashArray != null)
			{
				var pattern = StrokeDashArray.Select(d => (global::Foundation.NSNumber)d).ToArray();

				pathLayer.LineDashPhase = 0; // Starting position of the pattern
				pathLayer.LineDashPattern = pattern;
			}

			return pathLayer;
		}

		private bool TryCreateImageBrushLayers(ImageBrush imageBrush, CAShapeLayer fillMask, out CALayer imageContainerLayer)
		{
			var uiImage = imageBrush.ImageSource.ImageData;
			if (uiImage == null)
			{
				imageContainerLayer = default;
				return false;
			}

			// This layer is the one we apply the mask on. It's the full size of the shape because the mask is as well.
			imageContainerLayer = new CALayer
			{
				Frame = new CGRect(0, 0, Bounds.Width, Bounds.Height),
				Mask = fillMask,
				MasksToBounds = true,
				BackgroundColor = new CGColor(0, 0, 0, 0),
			};

			// The ImageBrush.Stretch will tell us the SIZE of the image we need for the layer
			var aspectRatio = uiImage.Size.AspectRatio();
			CGSize imageSize;
			switch (imageBrush.Stretch)
			{
				case Stretch.None:
					imageSize = uiImage.Size;
					break;
				case Stretch.Uniform:
					var width = Math.Min(Bounds.Width, Bounds.Height * aspectRatio);
					var height = width / aspectRatio;
					imageSize = new CGSize(width, height);
					break;
				case Stretch.UniformToFill:
					width = Math.Max(Bounds.Width, Bounds.Height * aspectRatio);
					height = width / aspectRatio;
					imageSize = new CGSize(width, height);
					break;
				default: // Fill
					imageSize = Bounds.Size;
					break;
			}

			// The ImageBrush.AlignementX/Y will tell us the LOCATION we need for the layer
			double deltaX;
			switch (imageBrush.AlignmentX)
			{
				case AlignmentX.Left:
					deltaX = 0;
					break;
				case AlignmentX.Right:
					deltaX = (double)(Bounds.Width - imageSize.Width);
					break;
				default: // Center
					deltaX = (double)(Bounds.Width - imageSize.Width) * 0.5f;
					break;
			}

			double deltaY;
			switch (imageBrush.AlignmentY)
			{
				case AlignmentY.Top:
					deltaY = 0;
					break;
				case AlignmentY.Bottom:
					deltaY = (double)(Bounds.Height - imageSize.Height);
					break;
				default: // Center
					deltaY = (double)(Bounds.Height - imageSize.Height) * 0.5f;
					break;
			}

			var imageFrame = new CGRect(new CGPoint(deltaX, deltaY), imageSize);

			// This is the layer with the actual image in it. Its frame is the inside of the border.
			var imageLayer = new CALayer
			{
				Contents = uiImage.CGImage,
				Frame = imageFrame,
				MasksToBounds = true,
			};

			imageContainerLayer.AddSublayer(imageLayer);

			return true;
		}

		#region Helper methods
		/// <summary>
		/// Get the defined size for this Shape (Width, Height with at least the Min&lt;Width|Height&gt; if defined)
		/// </summary>
		/// <returns>The defined size that should be used by Rectangle and Ellipse to measure themselves, OR NaN if none defined!</returns>
		private protected Size GetUserSize()
		{
			var userMinSize = new Size(MinWidth, MinHeight);
			var userSize = new Size(Width, Height)
				.AtLeast(userMinSize)
				.NumberOrDefault(userMinSize);

			return userSize;
		}

		/// <summary>
		/// Gets the rounded/adjusted half stroke thickness that should be used by Path, Line, Polyline and Polygon for the measure
		/// </summary>
		private protected double GetHalfStrokeThickness()
			=> Math.Floor((ActualStrokeThickness + .5) / 2.0);

		private static (float x, float y) GetScale(Size pathSize, Size availableSize)
		{
			availableSize = availableSize.NumberOrDefault(pathSize);

			return (
				(float)(double.IsInfinity(pathSize.Width) ? 1 : availableSize.Width / pathSize.Width),
				(float)(double.IsInfinity(pathSize.Height) ? 1 : availableSize.Height / pathSize.Height)
			);
		}
		private static (float x, float y) GetScale(Size pathSize, Size availableSize, double minus)
		{
			availableSize = availableSize.NumberOrDefault(pathSize);

			return (
				(float)(double.IsInfinity(pathSize.Width) ? 1 : (availableSize.Width - minus) / pathSize.Width),
				(float)(double.IsInfinity(pathSize.Height) ? 1 : (availableSize.Height - minus) / pathSize.Height)
			);
		}

		private static float GetScale(double pathSize, double availableSize, double minus)
			=> (float)(double.IsInfinity(pathSize) ? 1 : (availableSize - minus) / pathSize);
		#endregion
	}
}
