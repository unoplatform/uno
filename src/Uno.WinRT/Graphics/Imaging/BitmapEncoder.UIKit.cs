using System;
using System.IO;
using CoreGraphics;
using Foundation;
using UIKit;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
		private readonly BitmapEncoderFormat _imageFormat;
		private readonly global::Windows.Storage.Streams.IRandomAccessStream _stream;
		private global::Windows.Graphics.Imaging.SoftwareBitmap _softwareBitmap;

		private BitmapEncoder(BitmapEncoderFormat imageFormat
			, Storage.Streams.IRandomAccessStream stream)
		{
			_imageFormat = imageFormat;
			_stream = stream;
		}

		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.BitmapEncoder> CreateAsync(global::System.Guid encoderId, global::Windows.Storage.Streams.IRandomAccessStream stream)
			=> AsyncOperation.FromTask<BitmapEncoder>(ct =>
			{
				if (!_encoderMap.TryGetValue(encoderId, out var imageFormat))
				{
					throw new NotImplementedException($"Encoder {encoderId} in not implemented.", new ArgumentException(nameof(encoderId)));
				}
				return Task.FromResult(new BitmapEncoder(imageFormat, stream));
			});

		public global::Windows.Foundation.IAsyncAction FlushAsync()
			=> AsyncAction.FromTask(async ct =>
			{
				var image = _softwareBitmap?.Image;
				if (image?.CGImage is { } cgImage)
				{
					// Prefer the registered neutral codec (so Skia-on-iOS encodes through the same codec as desktop);
					// fall back to the native UIImage encoder when none is registered (a native-only head).
					if (ResolveEncode() is { } encode)
					{
						var pixels = ReadRgba(cgImage, out var width, out var height);
						using var ms = new MemoryStream();
						encode(ms, pixels, width, height, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, _imageFormat, 100);
						await _stream.WriteAsync(ms.ToArray().AsBuffer());
					}
					else
					{
						using var data = ToNativeEncoder(_imageFormat)(image);
						await _stream.WriteAsync(data.ToArray().AsBuffer());
					}
				}
			});

		// Renders the CGImage into a known-layout context to read straight RGBA (premultiplied) bytes — the same
		// CoreGraphics path SoftwareBitmap.Apple.cs uses to normalize images.
		private static byte[] ReadRgba(CGImage cgImage, out int width, out int height)
		{
			width = (int)cgImage.Width;
			height = (int)cgImage.Height;
			var bytesPerRow = width * 4;
			var pixels = new byte[bytesPerRow * height];
			using var colorSpace = CGColorSpace.CreateDeviceRGB();
			using var context = new CGBitmapContext(pixels, width, height, 8, bytesPerRow, colorSpace, CGImageAlphaInfo.PremultipliedLast);
			context.DrawImage(new CGRect(0, 0, width, height), cgImage);
			return pixels;
		}

		private static Func<UIImage, NSData> ToNativeEncoder(BitmapEncoderFormat format) =>
			format switch
			{
				BitmapEncoderFormat.Jpeg => AsJPEG,
				BitmapEncoderFormat.Png => AsPNG,
				_ => throw new NotSupportedException($"The native iOS encoder does not support {format}; register a managed or Skia image codec to encode it.")
			};

		public void SetSoftwareBitmap(global::Windows.Graphics.Imaging.SoftwareBitmap bitmap)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = bitmap;
		}

		public void SetPixelData(global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat, global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[] pixels)
		{
			if (pixelFormat != BitmapPixelFormat.Rgba8
				&& pixelFormat != BitmapPixelFormat.Bgra8)
			{
				throw new NotSupportedException($"The {pixelFormat} pixels format is not supported.");
			}
			_softwareBitmap?.Dispose();
			_softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels.AsBuffer(), pixelFormat, (int)width, (int)height, alphaMode);
		}

		private static NSData AsPNG(UIImage image) => image.AsPNG();
		private static NSData AsJPEG(UIImage image) => image.AsJPEG(1);
	}
}
