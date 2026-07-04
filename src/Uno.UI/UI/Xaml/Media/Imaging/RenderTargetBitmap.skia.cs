#nullable enable
using System;
using System.Numerics;
using System.Threading;
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

		// Serializes RenderAsync calls on this instance: the GPU replay runs asynchronously and
		// writes into the per-instance buffer, so an overlapping call must not resize/replace it.
		private readonly SemaphoreSlim _renderGate = new(1, 1);

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
			await _renderGate.WaitAsync();
			try
			{
				if (PrepareRender(element, scaledSize) is not { } render)
				{
					return (0, 0, 0);
				}

				if (element.XamlRoot?.VisualTree.ContentRoot.CompositionTarget is { } compositionTarget)
				{
					using var picture = RecordPicture(element.Visual, render.Dpi, render.Width, render.Height, forSoftwareRendering: false);

					var pixelsRead = false;
					var executed = await compositionTarget.TryExecuteOnNextRenderAsync(context =>
						// Bgra8888 avoids a conversion during read-back but isn't a renderable format
						// on every GPU backend, so fall back to Rgba8888 (ReadPixels converts).
						// Note: `render` (and the buffer it roots) is captured by the job, keeping the
						// buffer's native memory alive until the replay completes.
						pixelsRead = RenderPictureToBuffer(picture, render,
							info => SKSurface.Create(context, budgeted: false, info) ??
								SKSurface.Create(context, budgeted: false, info.WithColorType(SKColorType.Rgba8888))));
					if (executed && pixelsRead)
					{
						return (render.TargetInfo.BytesSize, render.TargetInfo.Width, render.TargetInfo.Height);
					}
				}

				return RenderSoftware(element.Visual, render);
			}
			finally
			{
				_renderGate.Release();
			}
		}

		/// <summary>
		/// Renders synchronously on the UI thread using the software rasterizer. For internal
		/// callers that cannot yield to the dispatcher — e.g. the drag visual must be captured
		/// within the DragStarting sequence so DragEnter/DragOver still fire synchronously right
		/// after it, matching WinUI. Must not be called while a RenderAsync is in flight on the
		/// same instance.
		/// </summary>
		internal void RenderSync(UIElement element, int scaledWidth, int scaledHeight)
		{
			(_bufferSize, PixelWidth, PixelHeight) = PrepareRender(element, new Size(scaledWidth, scaledHeight)) is { } render
				? RenderSoftware(element.Visual, render)
				: (0, 0, 0);
			InvalidateSource();
		}

		/// <summary>
		/// Computes the render dimensions and sizes the pixel buffer accordingly; null when the
		/// element has nothing to render.
		/// </summary>
		private (double Dpi, int Width, int Height, SKImageInfo TargetInfo, UnmanagedArrayOfBytes Buffer)? PrepareRender(UIElement element, Size? scaledSize)
		{
			var renderSize = element.RenderSize;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return null;
			}

			// Note: RenderTargetBitmap returns images with the current DPI (a 50x50 Border rendered on WinUI will return a 75x75 image)
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));

			var (targetWidth, targetHeight) = scaledSize is { } size ? ((int)size.Width, (int)size.Height) : (width, height);
			var targetInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
			EnsureBuffer(ref _buffer, targetInfo.BytesSize);

			return (dpi, width, height, targetInfo, _buffer!);
		}

		private static (int ByteCount, int Width, int Height) RenderSoftware(ContainerVisual visual, (double Dpi, int Width, int Height, SKImageInfo TargetInfo, UnmanagedArrayOfBytes Buffer) render)
		{
			using var picture = RecordPicture(visual, render.Dpi, render.Width, render.Height, forSoftwareRendering: true);

			if (!RenderPictureToBuffer(picture, render, static info => SKSurface.Create(info)))
			{
				throw new InvalidOperationException("Failed to render the element into a raster surface.");
			}

			return (render.TargetInfo.BytesSize, render.TargetInfo.Width, render.TargetInfo.Height);
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
		/// paths differ — resampling into the target size when it doesn't match, and reads the
		/// pixels back into the buffer.
		/// </summary>
		private static bool RenderPictureToBuffer(SKPicture picture, (double Dpi, int Width, int Height, SKImageInfo TargetInfo, UnmanagedArrayOfBytes Buffer) render, Func<SKImageInfo, SKSurface?> createSurface)
		{
			var (_, width, height, targetInfo, buffer) = render;

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
				return surface.ReadPixels(targetInfo, buffer.Pointer, targetInfo.RowBytes, 0, 0);
			}

			using var snapshot = surface.Snapshot();
			using var scaledSurface = createSurface(targetInfo);
			if (scaledSurface is null)
			{
				return false;
			}

			scaledSurface.Canvas.DrawImage(snapshot, SKRect.Create(targetInfo.Width, targetInfo.Height), new SKSamplingOptions(SKCubicResampler.CatmullRom));
			return scaledSurface.ReadPixels(targetInfo, buffer.Pointer, targetInfo.RowBytes, 0, 0);
		}
	}
}
