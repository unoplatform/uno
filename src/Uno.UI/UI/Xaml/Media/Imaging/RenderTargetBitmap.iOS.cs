using System;
using System.Linq;
using Windows.Foundation;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		/// <inheritdoc />
		private protected override bool IsSourceReady => _buffer != null;

		/// <inheritdoc />
		private protected override bool TryOpenSourceSync(out UIImage image)
		{
			image = UIImage.LoadFromData(NSData.FromArray(_buffer));
			return true;
		}

		private byte[] RenderAsPng(UIElement element, Size? scaledSize = null)
		{
			UIImage img;
			try
			{
				UIGraphics.BeginImageContextWithOptions(new Size(element.ActualSize.X, element.ActualSize.Y), true, 1f);
				var ctx = UIGraphics.GetCurrentContext();
				ctx.SetFillColor(Colors.White);
				element.Layer.RenderInContext(ctx);
				img = UIGraphics.GetImageFromCurrentImageContext();
			}
			finally
			{
				UIGraphics.EndImageContext();
			}

			if (scaledSize.HasValue)
			{
				using var unscaled = img;
				img = unscaled.Scale(scaledSize.Value);
			}

			using (img)
			{
				return img.AsPNG().ToArray();
			}
		}
	}
}
