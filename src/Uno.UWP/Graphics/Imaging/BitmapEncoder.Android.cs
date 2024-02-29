using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.IO;
using Android.Graphics;
using Java.Nio;
using System.Threading.Tasks;

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
		private readonly Bitmap.CompressFormat _imageFormat;
		private readonly Storage.Streams.IRandomAccessStream _stream;
		private SoftwareBitmap? _softwareBitmap;

		private BitmapEncoder(Bitmap.CompressFormat imageFormat
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
				_softwareBitmap?.Bitmap?
			   .Compress(_imageFormat, 100, _stream.AsStream());
				return Task.CompletedTask;
			});

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

			var destination = Bitmap.CreateBitmap((int)width, (int)height, Bitmap.Config.Argb8888!)!;

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
