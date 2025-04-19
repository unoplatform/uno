#nullable enable
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		private const int _bitsPerPixel = 32;
		private const int _bitsPerComponent = 8;
		private const int _bytesPerPixel = _bitsPerPixel / _bitsPerComponent;

		private static ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
		{
			try
			{
				// Note: We use the FromPixelCopy which will create a clone of the buffer, so we are ready to be re-used to render another UIElement.
				// (It's needed also if we swapped the buffer since we are not maintaining a ref on the swappedBuffer)
				var bytesPerRow = width * _bytesPerPixel;
				var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
				var image = SKImage.FromPixelCopy(info, buffer.Pointer, bytesPerRow);

				return ImageData.FromCompositionSurface(new SkiaCompositionSurface(image));
			}
			catch (Exception error)
			{
				return ImageData.FromError(error);
			}
		}

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize = null)
		{
			var compositor = Compositor.GetSharedCompositor();

			bool? previousCompMode = compositor.IsSoftwareRenderer;
			compositor.IsSoftwareRenderer = true;

			var renderSize = element.RenderSize;
			var visual = element.Visual;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}

			// Note: RenderTargetBitmap returns images with the current DPI (a 50x50 Border rendered on WinUI will return a 75x75 image)
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var surface = SKSurface.Create(info);
			//Ensure Clear
			var canvas = surface.Canvas;
			canvas.Clear(SKColors.Transparent);
			canvas.Scale((float)dpi);
			visual.RenderRootVisual(canvas, offsetOverride: Vector2.Zero);

			var img = surface.Snapshot();

			var bitmap = img.ToSKBitmap();
			if (scaledSize.HasValue)
			{
				var scaledBitmap = bitmap.Resize(
					new SKImageInfo((int)scaledSize.Value.Width, (int)scaledSize.Value.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
					new SKSamplingOptions(SKCubicResampler.CatmullRom));
				bitmap.Dispose();
				bitmap = scaledBitmap;
				(width, height) = (bitmap.Width, bitmap.Height);
			}

			var byteCount = bitmap.ByteCount;
			EnsureBuffer(ref buffer, byteCount);
			unsafe
			{
				bitmap.GetPixelSpan().CopyTo(new Span<byte>(buffer!.Pointer.ToPointer(), byteCount));
			}
			bitmap?.Dispose();

			compositor.IsSoftwareRenderer = previousCompMode;

			return (byteCount, width, height);
		}
	}
}
