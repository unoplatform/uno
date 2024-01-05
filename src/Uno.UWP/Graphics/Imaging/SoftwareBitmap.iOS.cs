using System;
using CoreGraphics;
using UIKit;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap
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

			bitmap.Image = UIImage.FromImage(Copy(image.CGImage!));
		}

		public static SoftwareBitmap Copy(global::Windows.Graphics.Imaging.SoftwareBitmap source) =>
			new SoftwareBitmap(UIImage.FromImage(Copy(source.Image.CGImage!)), false);

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

	}
}
