using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using CoreAnimation;
using CoreGraphics;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.UI.Xaml.Media;
using static System.Double;
using ObjCRuntime;
using Uno.UI.Xaml.Media;

#if __IOS__
using _Color = UIKit.UIColor;
#elif __MACOS__
using AppKit;
using _Color = AppKit.NSColor;
#endif

namespace Windows.UI.Xaml.Shapes
{
	partial class Shape
	{
		private CAShapeLayer _shapeLayer;

		public Shape()
		{
			// Background color is black by default, if and only if overriding Draw(CGRect rect).
#if __IOS__
			base.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
#elif __MACOS__
			base.WantsLayer = true;
			base.Layer.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
#endif
		}

		private CGRect GetPathBoundingBox(CGPath path) => path.PathBoundingBox;


		#region Rendering (Native)
		private protected void Render(CGPath path, FillRule fillRule = FillRule.EvenOdd)
		{
			// Remove the old layer if any
			_shapeLayer?.RemoveFromSuperLayer();

			// Well ... nothing to do !
			if (path == null)
			{
				_shapeLayer = null;
				return;
			}

			_shapeLayer = CreateLayer(path, fillRule);
			Layer.AddSublayer(_shapeLayer);
		}

		private void OnFillBrushChanged() => UpdateRender();

		private void OnStrokeBrushChanged() => UpdateRender();

		private void UpdateRender()
		{
			if (_shapeLayer is null)
			{
				// layer needs to be created
				InvalidateArrange();
				return;
			}

			SetFillAndStroke(_shapeLayer);
		}

		private void SetFillAndStroke(CAShapeLayer pathLayer)
		{
			RemoveSublayers();

			CGColor fillColor;
			switch (Fill)
			{
				case SolidColorBrush colorFill:
					fillColor = colorFill.ColorWithOpacity;
					break;

				case ImageBrush imageFill when TryCreateImageBrushLayers(imageFill, GetFillMask(pathLayer.Path), out var imageLayer):
					fillColor = Colors.Transparent;
					pathLayer.AddSublayer(imageLayer);
					break;

				case GradientBrush gradientFill:
					var gradientLayer = gradientFill.GetLayer(Frame.Size);
					gradientLayer.Frame = Bounds;
					gradientLayer.Mask ??= GetFillMask(pathLayer.Path);
					gradientLayer.MasksToBounds = true;

					fillColor = Colors.Transparent;
					pathLayer.AddSublayer(gradientLayer);
					break;

				case null:
					fillColor = Colors.Transparent;
					break;

				default:
					Application.Current.RaiseRecoverableUnhandledException(new NotSupportedException($"The brush {Fill} is not supported as Fill for a {this} on this platform."));
					fillColor = Colors.Transparent;
					break;
			}

			pathLayer.StrokeColor = Brush.GetColorWithOpacity(Stroke, Colors.Transparent);
			pathLayer.LineWidth = (nfloat)ActualStrokeThickness;
			pathLayer.FillColor = fillColor;

			// Make sure to hold native object ref until it has been retained by native itself
			// https://github.com/unoplatform/uno/issues/10283
			GC.KeepAlive(fillColor);

			if (StrokeDashArray is { } sda)
			{
				var pattern = sda.Select(d => (global::Foundation.NSNumber)d).ToArray();

				pathLayer.LineDashPhase = 0; // Starting position of the pattern
				pathLayer.LineDashPattern = pattern;
			}
			else if (pathLayer.LineDashPattern is { })
			{
				pathLayer.LineDashPattern = null;
			}

			CAShapeLayer GetFillMask(CGPath mask)
			{
				return new CAShapeLayer
				{
					Path = mask,
					Frame = Bounds,
					// We only use the fill color to create the mask area
					FillColor = _Color.White.CGColor,
				};
			}

			void RemoveSublayers()
			{
				pathLayer.Sublayers = null;
			}
		}

		private CAShapeLayer CreateLayer(CGPath path, FillRule fillRule)
		{
			var pathLayer = new CAShapeLayer() { Path = path, FillRule = fillRule.ToCAShapeLayerFillRule() };

			SetFillAndStroke(pathLayer);

			return pathLayer;
		}

		private bool TryCreateImageBrushLayers(ImageBrush imageBrush, CAShapeLayer fillMask, out CALayer imageContainerLayer)
		{
			if (imageBrush.ImageSource == null || !imageBrush.ImageSource.TryOpenSync(out var imageData) || imageData.Kind != ImageDataKind.NativeImage)
			{
				imageContainerLayer = default;
				return false;
			}

			var uiImage = imageData.NativeImage;

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
		#endregion

	}
}
