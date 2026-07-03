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

		/// <summary>
		/// The visual tree is recorded into an SKPicture on the UI thread (mirroring the regular
		/// rendering pipeline) and replayed into a GRContext-backed surface during the
		/// CompositionTarget's next render pass, so the rendering is hardware-accelerated. When
		/// the target renders in software (or the replay fails), the same logic runs on a raster
		/// surface instead.
		/// </summary>
		private async Task<(int ByteCount, int Width, int Height)> RenderAsBgra8_PremulAsync(UIElement element, Size? scaledSize = null)
		{
			var renderSize = element.RenderSize;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}

			// Note: RenderTargetBitmap returns images with the current DPI (a 50x50 Border rendered on WinUI will return a 75x75 image)
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));

			var (targetWidth, targetHeight) = scaledSize is { } size ? ((int)size.Width, (int)size.Height) : (width, height);
			var targetInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
			EnsureBuffer(ref _buffer, targetInfo.BytesSize);
			var destination = _buffer!.Pointer;

			if (element.XamlRoot?.VisualTree.ContentRoot.CompositionTarget is { } compositionTarget)
			{
				using var picture = RecordPicture(element.Visual, dpi, width, height, forSoftwareRendering: false);

				var pixelsRead = false;
				var executed = await compositionTarget.TryExecuteOnNextRenderAsync(context =>
					// Bgra8888 avoids a conversion during read-back but isn't a renderable format
					// on every GPU backend, so fall back to Rgba8888 (ReadPixels converts).
					pixelsRead = RenderPictureToBuffer(picture, width, height, targetInfo, destination,
						info => SKSurface.Create(context, budgeted: false, info) ??
							SKSurface.Create(context, budgeted: false, info.WithColorType(SKColorType.Rgba8888))));
				if (executed && pixelsRead)
				{
					return (targetInfo.BytesSize, targetWidth, targetHeight);
				}
			}

			{
				// Software fallback: re-record since effect brushes bake different (software-
				// compatible) filters into the picture when IsSoftwareRenderer is set.
				using var picture = RecordPicture(element.Visual, dpi, width, height, forSoftwareRendering: true);

				if (!RenderPictureToBuffer(picture, width, height, targetInfo, destination, static info => SKSurface.Create(info)))
				{
					throw new InvalidOperationException("Failed to render the element into a raster surface.");
				}

				return (targetInfo.BytesSize, targetWidth, targetHeight);
			}
		}

		private static SKPicture RecordPicture(ContainerVisual visual, double dpi, int width, int height, bool forSoftwareRendering)
		{
			var compositor = Compositor.GetSharedCompositor();
			var previousCompMode = compositor.IsSoftwareRenderer;
			var previousClip = visual.LayoutClip;
			try
			{
				// Effect brushes consult IsSoftwareRenderer while recording to generate filters
				// the target surface can rasterize.
				if (forSoftwareRendering)
				{
					compositor.IsSoftwareRenderer = true;
				}

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
				compositor.IsSoftwareRenderer = previousCompMode;
			}
		}

		/// <summary>
		/// Replays <paramref name="picture"/> into a surface obtained from
		/// <paramref name="createSurface"/> — the single point where the hardware and software
		/// paths differ — resampling into <paramref name="targetInfo"/>'s size when it doesn't
		/// match, and reads the pixels back into <paramref name="destination"/>.
		/// </summary>
		private static bool RenderPictureToBuffer(SKPicture picture, int width, int height, SKImageInfo targetInfo, IntPtr destination, Func<SKImageInfo, SKSurface?> createSurface)
		{
			using var surface = createSurface(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
			if (surface is null)
			{
				return false;
			}

			var canvas = surface.Canvas;
			canvas.Clear(SKColors.Transparent);
			canvas.DrawPicture(picture);

			if (targetInfo.Width == width && targetInfo.Height == height)
			{
				return surface.ReadPixels(targetInfo, destination, targetInfo.RowBytes, 0, 0);
			}

			using var snapshot = surface.Snapshot();
			using var scaledSurface = createSurface(targetInfo);
			if (scaledSurface is null)
			{
				return false;
			}

			scaledSurface.Canvas.DrawImage(snapshot, SKRect.Create(targetInfo.Width, targetInfo.Height), new SKSamplingOptions(SKCubicResampler.CatmullRom));
			return scaledSurface.ReadPixels(targetInfo, destination, targetInfo.RowBytes, 0, 0);
		}
	}
}
