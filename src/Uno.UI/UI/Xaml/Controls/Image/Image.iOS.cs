using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using CoreAnimation;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class Image
	{
		private void SetImage(CGImage cgImage, CGSize _) => SetImage(UIImage.FromImage(cgImage));
		
		private void UpdateContentMode(Stretch stretch)
		{
			if (FeatureConfiguration.Image.LegacyIosAlignment && _native != null)
			{
				switch (stretch)
				{
					case Stretch.Uniform:
						_native.ContentMode = UIViewContentMode.ScaleAspectFit;
						break;

					case Stretch.None:
						_native.ContentMode = UIViewContentMode.Center;
						break;

					case Stretch.UniformToFill:
						_native.ContentMode = UIViewContentMode.ScaleAspectFill;
						break;

					case Stretch.Fill:
						_native.ContentMode = UIViewContentMode.ScaleToFill;
						break;

					default:
						throw new NotSupportedException(
							"Stretch mode {0} is not supported".InvariantCultureFormat(stretch));
				}
			}
			else
			{
				SetNeedsLayout();
			}
		}

		public override void LayoutSubviews()
		{
			try
			{
				base.LayoutSubviews();

				UpdateLayerRect();
			}
			catch (Exception e)
			{
				this.Log().Error($"Layout failed in {GetType()}", e);
			}
		}
		
		private void UpdateLayerRect()
		{
			// Use "Bounds" over "Frame" because it includes all transforms
			var availableSize = Bounds.Size.ToFoundationSize(); ;

			if (SourceImageSize.Width == 0 || SourceImageSize.Height == 0 || availableSize.Width == 0 || availableSize.Height == 0 || (!_native?.HasImage ?? true))
			{
				return; // nothing to do
			}

			if (FeatureConfiguration.Image.LegacyIosAlignment)
			{
				return;
			}

			var imageSize = _native.ImageSize.ToFoundationSize();

			// Calculate the resulting space required on screen for the image
			var containerSize = this.MeasureSource(availableSize, imageSize);

			// Calculate the position of the image to follow stretch and alignment requirements
			var contentRect = this.ArrangeSource(availableSize, containerSize);

			// Calculate the required container to position the image in the AvailableSize
			var containerRect = new Rect(default, availableSize);
			containerRect.Intersect(contentRect);

			// Calculate a relative (0 to 1) X, Y, Width & Height for the image position
			var relativeX = contentRect.X / contentRect.Width;
			var relativeY = contentRect.Y / contentRect.Height;
			var relativeWidth = availableSize.Width / contentRect.Width;
			var relativeHeight = availableSize.Height / contentRect.Height;
			var contentRelativeRect = new CGRect(-relativeX, -relativeY, relativeWidth, relativeHeight);

			// Apply the relative position
			_native.Layer.ContentsRect = contentRelativeRect;

			// Add a clipping mask to prevent the GPU from rendering padding pixels
			_native.Layer.Mask = new CAShapeLayer
			{
				Path = CGPath.FromRect(containerRect.ToCGRect())
			};
		}
	}
}

