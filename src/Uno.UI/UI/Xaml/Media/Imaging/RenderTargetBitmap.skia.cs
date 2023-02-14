#nullable enable
using System.Runtime.InteropServices;
using Windows.Foundation;
namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		delegate void SwapColor(ref byte[] buffer, int byteCount);

		static readonly SwapColor? PlatformSwap = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
			? new SwapColor(SwapRB)
			: default(SwapColor);

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref byte[]? buffer, Size? scaledSize = null)
		{
			var renderSize = element.RenderSize;
			var visual = element.Visual;

			if (element.RenderSize is { IsEmpty: true }
			 || element.RenderSize is { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}
			(int width, int height) = ((int)renderSize.Width, (int)renderSize.Height);
			var info = new SkiaSharp.
				SKImageInfo(width, height, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
			using SkiaSharp.SKSurface surface = SkiaSharp.SKSurface.Create(info);
			//Ensure Clear
			var canvas = surface.Canvas;
			canvas.Clear(SkiaSharp.SKColors.Transparent);
			visual.Render(surface);

			var img = surface.Snapshot();

			var bitmap = SkiaSharp.SKBitmap.FromImage(img);
			if (scaledSize.HasValue)
			{
				var resize = bitmap.Resize(new SkiaSharp.SKImageInfo((int)scaledSize.Value.Width, (int)scaledSize.Value.Height, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul)
					, SkiaSharp.SKFilterQuality.High);
				bitmap.Dispose();
				bitmap = resize;
				(width, height) = (bitmap.Width, bitmap.Height);
			}

			var byteCount = bitmap.ByteCount;
			EnsureBuffer(ref buffer, byteCount);
			bitmap.GetPixelSpan().CopyTo(buffer);
			//On macOS color as stored as rgba
			PlatformSwap?.Invoke(ref buffer!, byteCount);
			bitmap?.Dispose();
			return (byteCount, width, height);
		}
	}
}
