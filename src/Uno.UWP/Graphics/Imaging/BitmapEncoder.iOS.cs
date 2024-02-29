using System;
using System.Collections.Generic;
using Foundation;
using UIKit;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
		private readonly Func<UIImage, NSData> _encoder;
		private readonly global::Windows.Storage.Streams.IRandomAccessStream _stream;
		private global::Windows.Graphics.Imaging.SoftwareBitmap? _softwareBitmap;

		private BitmapEncoder(Func<UIImage, NSData> encoder
			, Storage.Streams.IRandomAccessStream stream)
		{
			_encoder = encoder;
			_stream = stream;
		}

		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.BitmapEncoder> CreateAsync(global::System.Guid encoderId, global::Windows.Storage.Streams.IRandomAccessStream stream)
			=> AsyncOperation.FromTask<BitmapEncoder>(ct =>
			{
				if (!_encoderMap.TryGetValue(encoderId, out var encoder))
				{
					throw new NotImplementedException($"Encoder {encoderId} in not implemented.", new ArgumentException(nameof(encoderId)));
				}
				return Task.FromResult(new BitmapEncoder(encoder, stream));
			});

		public global::Windows.Foundation.IAsyncAction FlushAsync()
			=> AsyncAction.FromTask(async ct =>
			{
				var image = _softwareBitmap?.Image;
				if (image is { })
				{
					using var data = _encoder(image);
					await _stream.WriteAsync(data.ToArray().AsBuffer());
				}
			});

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

		private static NSData AsPNG(UIImage image) => image.AsPNG()!;
		private static NSData AsJPEG(UIImage image) => image.AsJPEG(1)!;
	}
}
