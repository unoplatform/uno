using System;
using System.Collections.Generic;
using SkiaSharp;
using Windows.Foundation;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Windows.Graphics.Imaging
{
	public partial class BitmapEncoder
	{
		private readonly SKEncodedImageFormat _imageFormat;
		private readonly Storage.Streams.IRandomAccessStream _stream;
		private SoftwareBitmap? _softwareBitmap;

		private BitmapEncoder(SKEncodedImageFormat imageFormat
			, Storage.Streams.IRandomAccessStream stream)
		{
			_imageFormat = imageFormat;
			_stream = stream;
		}

		public static IAsyncOperation<BitmapEncoder> CreateAsync(Guid encoderId
			, Storage.Streams.IRandomAccessStream stream) =>
			AsyncOperation.FromTask(ct =>
			{
				if (!_encoderMap.TryGetValue(encoderId, out var imageFormat))
				{
					throw new NotImplementedException($"Encoder {encoderId} in not implemented.", new ArgumentException(nameof(encoderId)));
				}
				return Task.FromResult(new BitmapEncoder(imageFormat, stream));
			});

		public void SetSoftwareBitmap(SoftwareBitmap bitmap)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = bitmap;
		}

		public IAsyncAction FlushAsync() =>
			AsyncAction.FromTask(ct =>
				{
					using var data = _softwareBitmap?.Bitmap?.Encode(_imageFormat, 100);
					data?.SaveTo(_stream.AsStream());
					return Task.CompletedTask;
				});

		public void SetPixelData(global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat, global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[] pixels)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = null;
			var info = new SKImageInfo((int)width
				, (int)height
				, pixelFormat.ToSKColorType()
				, alphaMode.ToSKAlphaType());

			// create an empty bitmap
			var destination = new SKBitmap();

			// pin the managed array so that the GC doesn't move it
			var gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

			// install the pixels with the color type of the pixel data

			var success = destination.
				InstallPixels(info
				, gcHandle.AddrOfPinnedObject()
				, info.RowBytes
				, (address, context) => ((GCHandle)context).Free(), gcHandle);

			if (success)
			{
				_softwareBitmap = new SoftwareBitmap(destination);
			}
		}
	}
}
