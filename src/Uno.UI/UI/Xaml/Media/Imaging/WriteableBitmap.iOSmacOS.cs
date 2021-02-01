using System;
using System.IO;
using Windows.Foundation;
using Windows.Storage.Streams;
using CoreGraphics;
using Foundation;

#if __MACOS__
using _NativeImage = AppKit.NSImage;
#else
using _NativeImage = UIKit.UIImage;
#endif

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private protected override bool IsSourceReady => true;

		private protected override bool TryOpenSourceSync(out _NativeImage image)
		{
			// Convert RGB colorspace.
			var bgraBuffer = _buffer.ToArray();
			var rgbaBuffer = new byte[bgraBuffer.Length];

			for (var i = 0; i < bgraBuffer.Length; i += 4)
			{
				rgbaBuffer[i + 3] = bgraBuffer[i + 3]; // a
				rgbaBuffer[i + 0] = bgraBuffer[i + 2]; // r
				rgbaBuffer[i + 1] = bgraBuffer[i + 1]; // g
				rgbaBuffer[i + 2] = bgraBuffer[i + 0]; // b
			}

			using (var dataProvider = new CGDataProvider(rgbaBuffer, 0, rgbaBuffer.Length))
			using (var colorSpace = CGColorSpace.CreateDeviceRGB())
			{
				const int bitsPerComponent = 8;
				const int bytesPerPixel = 4;

				var img = new CGImage(
					PixelWidth,
					PixelHeight,
					bitsPerComponent: bitsPerComponent,
					bitsPerPixel: bitsPerComponent * bytesPerPixel,
					bytesPerRow: bytesPerPixel * PixelWidth,
					colorSpace,
					CGImageAlphaInfo.Last,
					dataProvider,
					decode: null,
					shouldInterpolate: false,
					CGColorRenderingIntent.Default);

#if __MACOS__
				image = new _NativeImage(img, new CGSize(PixelWidth, PixelHeight));
#else
				image = _NativeImage.FromImage(img);
#endif
				return true;
			}
		}
	}
}
