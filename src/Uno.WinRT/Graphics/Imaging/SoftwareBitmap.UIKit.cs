using System;
using CoreGraphics;
using UIKit;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap : IDisposable
	{
		private UIImage image;

		internal SoftwareBitmap(UIImage image, bool isReadOnly)
		{
			this.image = image;
			IsReadOnly = isReadOnly;
		}

		internal UIImage Image
		{
			get => image;
			private set
			{
				image?.Dispose();
				image = value;
			}
		}

		public void CopyTo(global::Windows.Graphics.Imaging.SoftwareBitmap bitmap)
		{
			if (bitmap.IsReadOnly)
			{
				throw new ArgumentException("Destionanion is ReadOnly", nameof(bitmap));
			}

			bitmap.Image = UIImage.FromImage(Copy(image.CGImage));
		}

		public static SoftwareBitmap Copy(global::Windows.Graphics.Imaging.SoftwareBitmap source) =>
			new SoftwareBitmap(UIImage.FromImage(Copy(source.Image.CGImage)), false);

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			return CreateCopyFromBuffer(source, format, width, height, global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
		}

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			if (format != BitmapPixelFormat.Rgba8
				&& format != BitmapPixelFormat.Bgra8)
			{
				throw new NotSupportedException($"The {format} pixels format is not supported.");
			}
			// Get pixels
			var pixels = source.ToArray();
			var destination = FromPixels(pixels, format, width, height, alpha);
			return new SoftwareBitmap(UIImage.FromImage(destination), false);
		}

		public BitmapAlphaMode BitmapAlphaMode
		{
			get
			{
				var alfaInfo = image.CGImage.AlphaInfo;
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
			(int)image.CGImage.Height;

		public int PixelWidth =>
			(int)image.CGImage.Width;

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
			return context.ToImage();
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

			return context.ToImage();

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
