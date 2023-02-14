#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		/// <inheritdoc />
		private protected override bool IsSourceReady => _buffer != null;

		/// <inheritdoc />
		private protected override bool TryOpenSourceSync([NotNullWhen(true)] out ImageData image)
		{
			image = default;
			if (_buffer is null)
			{
				return false;
			}
			NSData? data;
			if (_bufferSize == 0)
			{
				data = NSData.FromBytes(IntPtr.Zero, 0);
			}
			else
			{
				unsafe
				{
					fixed (byte* ptr = &_buffer[0])
					{
						data = NSData.FromBytes((IntPtr)ptr, (nuint)_bufferSize);
					}
				}
			}
			UIImage? nativeImage = null;
			if (data is { })
			{
				nativeImage = UIImage.LoadFromData(data);
			}

			if (nativeImage is not null)
			{
				image = ImageData.FromNative(nativeImage);
			}
			return image.HasData;
		}

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref byte[]? buffer, Size? scaledSize = null)
		{
			var size = new Size(element.ActualSize.X, element.ActualSize.Y);

			if (size.IsEmpty)
			{
				return (0, 0, 0);
			}
			UIImage img;
			try
			{
				UIGraphics.BeginImageContextWithOptions(size, false, 1f);
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
				var imageRef = img.CGImage!;
				var width = imageRef.Width;
				var height = imageRef.Height;
				var bitsPerPixel = 32;
				var bitsPerComponent = 8;
				var bytesPerPixel = bitsPerPixel / bitsPerComponent;

				var bytesPerRow = width * bytesPerPixel;
				var bufferLength = bytesPerRow * height;
				byte[] bitmapData = new byte[bufferLength];
				var byteCount = (int)bufferLength;

				using var colorSpace = CGColorSpace.CreateDeviceRGB();

				using var context = new CGBitmapContext(bitmapData
				, width
				, height
				, bitsPerComponent
				, bytesPerRow
				, colorSpace
				, CGImageAlphaInfo.PremultipliedLast); // RGBA8

				var rect = new CGRect(0, 0, width, height);
				context.DrawImage(rect, imageRef);
				EnsureBuffer(ref buffer, byteCount);
				global::System.Array.Copy(bitmapData, buffer!, bufferLength);
				SwapRB(ref buffer!, byteCount);
				return (byteCount, (int)width, (int)height);
			}
		}
	}
}
