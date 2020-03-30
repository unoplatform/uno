using CoreGraphics;
using System;
using System.Linq;
using Uno.Extensions;
using CoreAnimation;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Uno.Logging;
using Uno.UI;
#if __IOS__
using UIKit;
using _Color = UIKit.UIColor;
#elif __MACOS__
using _Color = AppKit.NSColor;
#endif

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class ArbitraryShapeBase
	{
		// Drawing scale
		private float _scaleX;
		private float _scaleY;

		public ArbitraryShapeBase()
		{
#if __IOS__
			ClipsToBounds = true;
#elif __MACOS__
			WantsLayer = true;
#endif
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			RefreshShape();
		}

		protected abstract CGPath GetPath(Size availableSize);

		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();
			RefreshShape();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			RefreshShape();
		}

		private CGRect GetActualSize()
		{
			return Bounds;
		}

		private IDisposable BuildDrawableLayer()
		{
			if (Bounds == CGRect.Empty)
			{
				return Disposable.Empty;
			}

			var newLayer = CreateLayerOrDefault();
			if (newLayer == null)
			{
				return Disposable.Empty;
			}

			Layer.AddSublayer(newLayer);
			return Disposable.Create(() => newLayer.RemoveFromSuperLayer());
		}

		private CALayer CreateLayerOrDefault()
		{
			var path = this.GetPath(SizeFromUISize(Bounds.Size));
			if (path == null)
			{
				return null;
			}

			var pathBounds = path.BoundingBox;

			if (
				nfloat.IsInfinity(pathBounds.Right)
				|| nfloat.IsInfinity(pathBounds.Bottom)
			)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Ignoring path with invalid bounds {pathBounds}");
				}

				return null;
			}

			var scaleX = _scaleX;
			var scaleY = _scaleY;

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

			var transform = CGAffineTransform.MakeScale(scaleX, scaleY);

			if (!ShouldPreserveOrigin)
			{
				// When stretching, we can't use 0,0 as the origin, but must instead use the path's bounds.
				transform.Translate(-pathBounds.Left * scaleX, -pathBounds.Top * scaleY);
			}

			//if (!ShouldPreserveOrigin)
			//{
			//	// We need to translate the shape to take in account the stroke thickness
			//	// transform.Translate((nfloat)ActualStrokeThickness * 0.5f, (nfloat)ActualStrokeThickness * 0.5f);
			//	var quarterStrokeThickness = (nfloat)(GetHalfStrokeThickness() / 2.0);
			//	transform.Translate(quarterStrokeThickness, quarterStrokeThickness);
			//}

			if (nfloat.IsNaN(transform.x0) || nfloat.IsNaN(transform.y0) ||
				nfloat.IsNaN(transform.xx) || nfloat.IsNaN(transform.yy) ||
				nfloat.IsNaN(transform.xy) || nfloat.IsNaN(transform.yx)
			)
			{
				// transformedPath creation will crash natively if the transform contains NaNs
				throw new InvalidOperationException($"transform {transform} contains NaN values, transformation will fail.");
			}

			//var colorFill = Fill as SolidColorBrush ?? SolidColorBrushHelper.Transparent;
			//var imageFill = Fill as ImageBrush;
			//var gradientFill = Fill as LinearGradientBrush;
			//var stroke = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;
			var transformedPath = new CGPath(path, transform);
			var pathLayer = new CAShapeLayer()
			{
				Path = transformedPath,
				StrokeColor = (Stroke as SolidColorBrush)?.ColorWithOpacity ?? Colors.Transparent,
				LineWidth = (nfloat)ActualStrokeThickness,
			};

			switch (Fill)
			{
				case SolidColorBrush colorFill:
					pathLayer.FillColor = colorFill.ColorWithOpacity;
					break;

				case ImageBrush imageFill when TryCreateImageBrushLayers(imageFill, GetFillMask(transformedPath), out var imageLayer):
					pathLayer.FillColor = Colors.Transparent;
					pathLayer.AddSublayer(imageLayer);
					break;

				case LinearGradientBrush gradientFill:
					var gradientLayer = gradientFill.GetLayer(Frame.Size);
					gradientLayer.Frame = Bounds;
					gradientLayer.Mask = GetFillMask(transformedPath);
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
					FillColor = _Color.White.CGColor,
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

		protected override Size MeasureOverride(Size availableSize)
		{
			var path = GetPath(availableSize);
			if (path == null)
			{
				return default;
			}

			var pathBounds = path.BoundingBox;
			var pathSize = (Windows.Foundation.Size)pathBounds.Size;
			if (pathSize == default)
			{
				return default;
			}

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

			// For shapes that has an offset from the origin, if the stretch mode is not None we remove this offset.
			// cf. remarks of ShouldPreserveOrigin XML doc.
			if (ShouldPreserveOrigin)
			{
				//pathWidth += bounds.X;
				//pathHeight += bounds.Y;

				if (!nfloat.IsInfinity(pathBounds.X))
				{
					pathSize.Width += pathBounds.X;
				}
				if (!nfloat.IsInfinity(pathBounds.Y))
				{
					pathSize.Height += pathBounds.Y;
				}
			}

			//var availableWidth = availableSize.Width;
			//var availableHeight = availableSize.Height;
			//var userWidth = this.Width;
			//var userHeight = this.Height;

			// As weird as it seems, it's how WinUI behaves for the measure!
			// Note: .5 so if thickness is 1, we have 1, but if .8 we have 0 ... like WinUI
			//var halfStrokeThickness = Math.Floor((ActualStrokeThickness + .5f) / 2.0);
			var halfStrokeThickness = GetHalfStrokeThickness();
			pathSize = pathSize.Add(new Size(halfStrokeThickness, halfStrokeThickness));

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
			switch (Stretch)
			{
				case Stretch.None:
					_scaleX = 1;
					_scaleY = 1;
					break;

				case Stretch.Fill:
					(_scaleX, _scaleY) = GetScale(pathSize, availableSize);
					break;

				case Stretch.Uniform:
				{
					var scale = GetScale(pathSize, availableSize);
					_scaleX = _scaleY = Math.Min(scale.x, scale.y);
					break;
				}
				case Stretch.UniformToFill:
				{
					var scale = GetScale(pathSize, availableSize);
					_scaleX = _scaleY = Math.Max(scale.x, scale.y);
					break;
				}
			}

			//calculatedWidth += strokeThickness;
			//calculatedHeight += strokeThickness;

			return new Size(pathSize.Width * _scaleX, pathSize.Height * _scaleY);
		}

		private protected double GetHalfStrokeThickness()
			=> Math.Floor((ActualStrokeThickness + .5) / 2.0);

		private static (float x, float y) GetScale(Size pathSize, Size availableSize)
		{
			availableSize = availableSize.NumberOrDefault(pathSize);

			return (
				(float)(double.IsInfinity(availableSize.Width) ? 1 : pathSize.Width / availableSize.Width),
				(float)(double.IsInfinity(availableSize.Height) ? 1 : pathSize.Height / availableSize.Height)
			);
		}
	}
}
