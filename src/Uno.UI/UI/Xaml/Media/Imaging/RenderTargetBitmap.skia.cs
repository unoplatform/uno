#nullable enable
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Composition;
using Uno.UI.Xaml.Media;
using SkiaSharp;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		private const int _bitsPerPixel = 32;
		private const int _bitsPerComponent = 8;
		private const int _bytesPerPixel = _bitsPerPixel / _bitsPerComponent;

		delegate void SwapColor(ref byte[] buffer, int byteCount);

		private static readonly SwapColor? _platformSwap = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? SwapRB : default;

		private static ImageData Open(byte[] buffer, int bufferLength, int width, int height)
		{
			if (_platformSwap is not null)
			{
				var swappedBuffer = default(byte[]);
				EnsureBuffer(ref swappedBuffer, bufferLength);
				Array.Copy(buffer, swappedBuffer!, bufferLength);
				_platformSwap(ref swappedBuffer!, bufferLength);
				buffer = swappedBuffer;
			}

			var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try
			{
				// Note: We use the FromPixelCopy which will create a clone of the buffer, so we are ready to be re-used to render another UIElement.
				// (It's needed also for if we swapped the buffer since we are not maintaining a ref on the swappedBuffer)
				var bytesPerRow = width * _bytesPerPixel;
				var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
				var image = SKImage.FromPixelCopy(info, bufferHandle.AddrOfPinnedObject(), bytesPerRow);

				return ImageData.FromCompositionSurface(new SkiaCompositionSurface(image));
			}
			catch (Exception error)
			{
				return ImageData.FromError(error);
			}
			finally
			{
				bufferHandle.Free();
			}
		}

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref byte[]? buffer, Size? scaledSize = null)
		{
			var renderSize = element.RenderSize;
			var visual = element.Visual;

			if (element.RenderSize is { IsEmpty: true }
				|| element.RenderSize is { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}
			var (width, height) = ((int)renderSize.Width, (int)renderSize.Height);
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var surface = SKSurface.Create(info);
			//Ensure Clear
			var canvas = surface.Canvas;
			canvas.Clear(SKColors.Transparent);
			visual.Render(surface);

			var img = surface.Snapshot();

			var bitmap = SKBitmap.FromImage(img);
			if (scaledSize.HasValue)
			{
				var scaledBitmap = bitmap.Resize(
					new SKImageInfo((int)scaledSize.Value.Width, (int)scaledSize.Value.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
					SKFilterQuality.High);
				bitmap.Dispose();
				bitmap = scaledBitmap;
				(width, height) = (bitmap.Width, bitmap.Height);
			}

			var byteCount = bitmap.ByteCount;
			EnsureBuffer(ref buffer, byteCount);
			bitmap.GetPixelSpan().CopyTo(buffer);
			//On macOS color as stored as rgba
			_platformSwap?.Invoke(ref buffer!, byteCount);
			bitmap?.Dispose();
			return (byteCount, width, height);
		}
	}
}
