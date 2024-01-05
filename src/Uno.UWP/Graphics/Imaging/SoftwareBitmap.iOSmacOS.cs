using System;
using CoreGraphics;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap : IDisposable
	{
		public BitmapAlphaMode BitmapAlphaMode
		{
			get
			{
				var alfaInfo = image.CGImage!.AlphaInfo;
				if (alfaInfo.HasFlag(CoreGraphics.CGImageAlphaInfo.PremultipliedFirst)
					|| alfaInfo.HasFlag(CoreGraphics.CGImageAlphaInfo.PremultipliedLast))
				{
					return BitmapAlphaMode.Premultiplied;
				}
				else if (alfaInfo.HasFlag(CoreGraphics.CGImageAlphaInfo.None)
					|| alfaInfo.HasFlag(CoreGraphics.CGImageAlphaInfo.NoneSkipFirst)
					|| alfaInfo.HasFlag(CoreGraphics.CGImageAlphaInfo.NoneSkipLast)
					)
				{
					return BitmapAlphaMode.Ignore;
				}
				return BitmapAlphaMode.Straight;
			}
		}

		public BitmapPixelFormat BitmapPixelFormat =>
			BitmapPixelFormat.Rgba8;

		public bool IsReadOnly { get; }

		public int PixelHeight =>
			(int)image.CGImage!.Height;

		public int PixelWidth =>
			(int)image.CGImage!.Width;

		public SoftwareBitmap GetReadOnlyView() =>
			new SoftwareBitmap(image, true);

		private static CGImage Copy(CGImage imageRef)
		{
			var width = imageRef.Width;
			var height = imageRef.Height;
			var bitsPerPixel = 32;
			var bitsPerComponent = 8;
			var bytesPerPixel = bitsPerPixel / bitsPerComponent;
			var bytesPerRow = width * bytesPerPixel;
			var bufferLength = bytesPerRow * height;
			byte[] bitmapData = new byte[bufferLength];
			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(bitmapData
						, width
						, height
						, bitsPerComponent
						, bytesPerRow
						, colorSpace
						, CGImageAlphaInfo.PremultipliedLast);
			var rect = new CGRect(0, 0, width, height);
			context.DrawImage(rect, imageRef);
			return context.ToImage()!;
		}

		private static CGImage FromPixels(byte[] pixels, BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			// If Bgra Swap chanal B with R
			if (format == BitmapPixelFormat.Bgra8)
			{
				var byteCount = pixels.Length;
				for (int i = 0; i < byteCount; i += 4)
				{
					Swap(ref pixels[i], ref pixels[i + 2]);
				}
			}
			var bitsPerPixel = 32;
			var bitsPerComponent = 8;
			var bytesPerPixel = bitsPerPixel / bitsPerComponent;
			var bytesPerRow = width * bytesPerPixel;
			var bufferLength = bytesPerRow * height;
			using var proivder = new CGDataProvider(pixels);
			using var colorSpace = CGColorSpace.CreateDeviceRGB();

			var imageRef = new CGImage(width
				, height
				, bitsPerComponent
				, bitsPerPixel
				, bytesPerRow
				, colorSpace
				, alpha == BitmapAlphaMode.Premultiplied
					? CGBitmapFlags.ByteOrderDefault | CGBitmapFlags.PremultipliedLast
					: CGBitmapFlags.ByteOrderDefault | CGBitmapFlags.Last
				, proivder
				, default
				, true
				, CGColorRenderingIntent.Default);

			byte[] bitmapData = new byte[bufferLength];

			using var context = new CGBitmapContext(bitmapData
						, width
						, height
						, bitsPerComponent
						, bytesPerRow
						, colorSpace
						, CGImageAlphaInfo.PremultipliedLast);
			var rect = new CGRect(0, 0, width, height);
			context.DrawImage(rect, imageRef);

			return context.ToImage()!;

			static void Swap(ref byte foo, ref byte bar)
			{
				(foo, bar) = (bar, foo);
			}
		}

		public void Dispose()
		{
			image?.Dispose();
		}
	}
}
