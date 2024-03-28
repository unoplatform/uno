#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.Foundation;
using CoreGraphics;
using Foundation;
using Uno.UI;
using AppKit;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		private const int _bitsPerPixel = 32;
		private const int _bitsPerComponent = 8;
		private const int _bytesPerPixel = _bitsPerPixel / _bitsPerComponent;

		/// <inheritdoc />
		private protected override bool IsSourceReady => _buffer != null;

		private static ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
		{
			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(
				buffer.Pointer,
				width,
				height,
				_bitsPerComponent,
				width * _bytesPerPixel,
				colorSpace,
				CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst);

			using var cgImage = context.ToImage();
			if (cgImage is not null)
			{
				return ImageData.FromNative(new NSImage(cgImage, new CGSize(width, height)));
			}

			return default;
		}

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize = null)
		{
			var size = new Size(element.ActualSize.X, element.ActualSize.Y);

			if (size.IsEmpty)
			{
				return (0, 0, 0);
			}
			NSImage? nsImage = default;
			try
			{
				nsImage = new NSImage(size);
				nsImage.LockFocusFlipped(element.IsFlipped);
				var ctx = NSGraphicsContext.CurrentContext!.GraphicsPort;
				ctx.SetFillColor(Colors.Transparent); // This is only for pixels not used, but the bitmap as the same size of the element. We keep it only for safety!
				element.Layer!.RenderInContext(ctx);
			}
			finally
			{
				nsImage?.UnlockFocus();
			}

			if (scaledSize.HasValue)
			{
				using var unscaled = nsImage;
				nsImage = new NSImage(scaledSize.Value);
				nsImage.LockFocus();
				var ctx = NSGraphicsContext.CurrentContext!.GraphicsPort;
				ctx.SetFillColor(Colors.Transparent);
				ctx.DrawImage(new CGRect(0, 0, scaledSize.Value.Width, scaledSize.Value.Height), unscaled.CGImage);
				nsImage.UnlockFocus();
			}

			using (nsImage)
			{
				var cgImage = nsImage.CGImage!;
				var width = cgImage.Width;
				var height = cgImage.Height;
				var bytesPerRow = width * _bytesPerPixel;
				var bufferLength = (int)(bytesPerRow * height);

				EnsureBuffer(ref buffer, bufferLength);

				using var colorSpace = CGColorSpace.CreateDeviceRGB();
				using var context = new CGBitmapContext(
					buffer!.Pointer,
					width,
					height,
					_bitsPerComponent,
					bytesPerRow,
					colorSpace,
					CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst); // BGRA8

				var rect = new CGRect(0, 0, width, height);
				context.DrawImage(rect, cgImage);

				return (bufferLength, (int)width, (int)height);
			}
		}
	}
}
