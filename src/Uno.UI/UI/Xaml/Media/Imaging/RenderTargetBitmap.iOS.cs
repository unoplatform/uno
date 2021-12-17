#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
		private protected override bool TryOpenSourceSync([NotNullWhen(true)] out UIImage? image)
		{
			image = UIImage.LoadFromData(NSData.FromArray(_buffer));
			return image != null;
		}

		private byte[] RenderAsPng(UIElement element, Size? scaledSize = null)
		{
			UIImage img;
			try
			{
				UIGraphics.BeginImageContextWithOptions(new Size(element.ActualSize.X, element.ActualSize.Y), false, 1f);
				var ctx = UIGraphics.GetCurrentContext();
				ctx.SetFillColor(Colors.Transparent); // This is only for pixels not used, but the bitmap as the same size of the element. We keep it only for safety!
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
