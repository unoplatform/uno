#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Graphics.Imaging
{
	public partial class BitmapEncoder
	{
		private readonly BitmapEncoderFormat _imageFormat;
		private readonly Storage.Streams.IRandomAccessStream _stream;
		private SoftwareBitmap? _softwareBitmap;

		private BitmapEncoder(BitmapEncoderFormat imageFormat, Storage.Streams.IRandomAccessStream stream)
		{
			_imageFormat = imageFormat;
			_stream = stream;
		}

		public static IAsyncOperation<BitmapEncoder> CreateAsync(Guid encoderId, Storage.Streams.IRandomAccessStream stream) =>
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
				if (_softwareBitmap is { } bitmap)
				{
					var encode = Encode ?? throw new NotSupportedException("No image codec is registered.");
					var data = encode(bitmap.Pixels, bitmap.PixelWidth, bitmap.PixelHeight, bitmap.BitmapPixelFormat, bitmap.BitmapAlphaMode, _imageFormat, 100);
					_stream.AsStream().Write(data, 0, data.Length);
				}
				return Task.CompletedTask;
			});

		public void SetPixelData(BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[] pixels)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = new SoftwareBitmap(pixels, (int)width, (int)height, pixelFormat, alphaMode);
		}
	}
}
