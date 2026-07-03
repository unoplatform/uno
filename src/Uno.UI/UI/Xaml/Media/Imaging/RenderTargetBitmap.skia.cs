#nullable enable
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Media;
using SkiaSharp;

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

		private async Task<(int ByteCount, int Width, int Height)> RenderAsBgra8_PremulAsync(UIElement element, Size? scaledSize = null)
		{
			var renderSize = element.RenderSize;
			var visual = element.Visual;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}

			// Note: RenderTargetBitmap returns images with the current DPI (a 50x50 Border rendered on WinUI will return a 75x75 image)
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));

			if (await TryRenderHardwareAcceleratedAsync(element, dpi, width, height, scaledSize) is { } hardwareResult)
			{
				return hardwareResult;
			}

			var previousClip = visual.LayoutClip;
			try
			{
				// Remove any existing layout clip, we want to render the full element, not
				// the clipped part based on the existing parent's layout slot.
				visual.LayoutClip = null;

				return RenderSoftware(visual, dpi, width, height, ref _buffer, scaledSize);
			}
			finally
			{
				visual.LayoutClip = previousClip;
			}
		}

		/// <summary>
		/// Renders through the GRContext of the element's window instead of a raster surface.
		/// The visual tree is recorded into an SKPicture on the UI thread (mirroring the regular
		/// rendering pipeline), replayed into a GPU surface during the CompositionTarget's next
		/// render pass, and the pixels are read back into the pixel buffer. Returns null when
		/// hardware rendering is unavailable, so the caller falls back to software.
		/// </summary>
		private async Task<(int ByteCount, int Width, int Height)?> TryRenderHardwareAcceleratedAsync(UIElement element, double dpi, int width, int height, Size? scaledSize)
		{
			if (element.XamlRoot?.VisualTree.ContentRoot.CompositionTarget is not { } compositionTarget)
			{
				return null;
			}

			using var picture = RecordPicture(element.Visual, dpi, width, height);

			var (targetWidth, targetHeight) = scaledSize is { } size ? ((int)size.Width, (int)size.Height) : (width, height);
			var targetInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
			EnsureBuffer(ref _buffer, targetInfo.BytesSize);
			var destination = _buffer!.Pointer;

			var pixelsRead = false;
			var executed = await compositionTarget.TryExecuteOnNextRenderAsync(context =>
			{
				// Bgra8888 avoids a conversion during read-back but isn't a renderable format on
				// every GPU backend, so fall back to Rgba8888 (ReadPixels converts while copying).
				using var surface =
					SKSurface.Create(context, budgeted: false, new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul)) ??
					SKSurface.Create(context, budgeted: false, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
				if (surface is null)
				{
					return;
				}

				var canvas = surface.Canvas;
				canvas.Clear(SKColors.Transparent);
				canvas.DrawPicture(picture);

				if (scaledSize is null)
				{
					pixelsRead = surface.ReadPixels(targetInfo, destination, targetInfo.RowBytes, 0, 0);
				}
				else
				{
					using var snapshot = surface.Snapshot();
					using var scaledSurface =
						SKSurface.Create(context, budgeted: false, targetInfo) ??
						SKSurface.Create(context, budgeted: false, targetInfo.WithColorType(SKColorType.Rgba8888));
					if (scaledSurface is null)
					{
						return;
					}

					scaledSurface.Canvas.DrawImage(snapshot, SKRect.Create(targetWidth, targetHeight), new SKSamplingOptions(SKCubicResampler.CatmullRom));
					pixelsRead = scaledSurface.ReadPixels(targetInfo, destination, targetInfo.RowBytes, 0, 0);
				}
			});

			return executed && pixelsRead ? (targetInfo.BytesSize, targetWidth, targetHeight) : null;
		}

		private static SKPicture RecordPicture(ContainerVisual visual, double dpi, int width, int height)
		{
			var previousClip = visual.LayoutClip;
			try
			{
				// Remove any existing layout clip, we want to render the full element, not
				// the clipped part based on the existing parent's layout slot. Restored before
				// the recording is handed off, so the visual isn't left mutated while the
				// replay is pending on the render thread.
				visual.LayoutClip = null;

				using var recorder = new SKPictureRecorder();
				var canvas = recorder.BeginRecording(new SKRect(0, 0, width, height));
				canvas.Clear(SKColors.Transparent);
				canvas.Scale((float)dpi);
				visual.RenderRootVisual(canvas, offsetOverride: Vector2.Zero);
				return recorder.EndRecording();
			}
			finally
			{
				visual.LayoutClip = previousClip;
			}
		}

		private static (int ByteCount, int Width, int Height) RenderSoftware(Visual visual, double dpi, int width, int height, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize)
		{
			var compositor = Compositor.GetSharedCompositor();

			bool? previousCompMode = compositor.IsSoftwareRenderer;
			compositor.IsSoftwareRenderer = true;

			try
			{
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

				return (byteCount, width, height);
			}
			finally
			{
				compositor.IsSoftwareRenderer = previousCompMode;
			}
		}
	}
}
