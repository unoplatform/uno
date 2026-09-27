using System;
using Windows.Foundation;
using System.IO;
using Android.Graphics;
using Java.Nio;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
		private readonly BitmapEncoderFormat _imageFormat;
		private readonly Storage.Streams.IRandomAccessStream _stream;
		private SoftwareBitmap _softwareBitmap;

		private BitmapEncoder(BitmapEncoderFormat imageFormat
			, Storage.Streams.IRandomAccessStream stream)
		{
			_imageFormat = imageFormat;
			_stream = stream;
		}

		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.BitmapEncoder> CreateAsync(global::System.Guid encoderId, global::Windows.Storage.Streams.IRandomAccessStream stream) =>
			AsyncOperation.FromTask<BitmapEncoder>(ct =>
			{
				if (!_encoderMap.TryGetValue(encoderId, out var imageFormat))
				{
					throw new NotImplementedException($"Encoder {encoderId} in not implemented.", new ArgumentException(nameof(encoderId)));
				}
				return Task.FromResult(new BitmapEncoder(imageFormat, stream));
			});


		public IAsyncAction FlushAsync() =>
			AsyncAction.FromTask(ct =>
			{
				if (_softwareBitmap?.Bitmap is { } bitmap)
				{
					// Prefer the registered neutral codec (so Skia-on-Android encodes through the same codec as
					// desktop); fall back to Android's native encoder when none is registered (a native-only head).
					if (ResolveEncode() is { } encode)
					{
						var width = bitmap.Width;
						var height = bitmap.Height;
						// GetPixels yields non-premultiplied 0xAARRGGBB; repack to straight RGBA for the neutral codec.
						var argb = new int[width * height];
						bitmap.GetPixels(argb, 0, width, 0, 0, width, height);
						var pixels = new byte[width * height * 4];
						for (var i = 0; i < argb.Length; i++)
						{
							var c = argb[i];
							pixels[i * 4] = (byte)((c >> 16) & 0xFF);
							pixels[i * 4 + 1] = (byte)((c >> 8) & 0xFF);
							pixels[i * 4 + 2] = (byte)(c & 0xFF);
							pixels[i * 4 + 3] = (byte)((c >> 24) & 0xFF);
						}
						encode(_stream.AsStream(), pixels, width, height, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, _imageFormat, 100);
					}
					else
					{
						bitmap.Compress(ToCompressFormat(_imageFormat), 100, _stream.AsStream());
					}
				}
				return Task.CompletedTask;
			});

		private static Bitmap.CompressFormat ToCompressFormat(BitmapEncoderFormat format) =>
			format switch
			{
				BitmapEncoderFormat.Jpeg => Bitmap.CompressFormat.Jpeg,
				BitmapEncoderFormat.Png => Bitmap.CompressFormat.Png,
				_ => throw new NotSupportedException($"The native Android encoder does not support {format}; register a managed or Skia image codec to encode it.")
			};

		public void SetSoftwareBitmap(Windows.Graphics.Imaging.SoftwareBitmap bitmap)
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
			_softwareBitmap = null;

			var destination = Bitmap.CreateBitmap((int)width, (int)height, Bitmap.Config.Argb8888);

			static void Swap(ref byte foo, ref byte bar)
			{
				(foo, bar) = (bar, foo);
			}

			if (pixelFormat == BitmapPixelFormat.Bgra8)
			{
				//Android Store Argb8888 as rbga
				var byteCount = pixels.Length;
				for (int i = 0; i < byteCount; i += 4)
				{
					//Swap R and B chanal
					Swap(ref pixels[i], ref pixels[i + 2]);
				}
			}
			using var buffer = ByteBuffer.Wrap(pixels);
			destination.CopyPixelsFromBuffer(buffer);

			if (destination is { })
			{
				_softwareBitmap = new SoftwareBitmap(destination);
			}
		}
	}

}
