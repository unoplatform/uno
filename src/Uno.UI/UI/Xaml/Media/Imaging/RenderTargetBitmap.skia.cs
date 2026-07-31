#nullable enable
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		private const int _bytesPerPixel = 4;

		// Serializes RenderAsync calls on this instance: the capture writes into the per-instance buffer,
		// so an overlapping call must not resize/replace it.
		private readonly SemaphoreSlim _renderGate = new(1, 1);

		private static unsafe ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
		{
			try
			{
				// Wrap the BGRA (premultiplied) buffer as a neutral single-frame image (copied, so the buffer is reusable).
				var frames = ImageDecoder.Current.CreateFrame(width, height, new ReadOnlySpan<byte>(buffer.Pointer.ToPointer(), bufferLength));
				return ImageData.FromCompositionSurface(new CompositionImageSurface(frames.Frames[0]));
			}
			catch (Exception error)
			{
				return ImageData.FromError(error);
			}
		}

		/// <summary>
		/// Re-renders the element into a neutral offscreen image (the backend rasterizes it — CPU on the Skia
		/// backend) and reads its pixels back into the buffer. DPI + target scaling are baked into the drawing
		/// session, so the element is drawn directly at the requested pixel size.
		/// </summary>
		private async Task<(int ByteCount, int Width, int Height)> RenderAsBgra8_PremulAsync(UIElement element, Size? scaledSize = null)
		{
			await _renderGate.WaitAsync();
			try
			{
				return PrepareRender(element, scaledSize) is { } render
					? RenderToBuffer(element.Visual, render)
					: (0, 0, 0);
			}
			finally
			{
				_renderGate.Release();
			}
		}

		/// <summary>
		/// Renders synchronously on the UI thread. For internal callers that cannot yield to the dispatcher —
		/// e.g. the drag visual must be captured within the DragStarting sequence so DragEnter/DragOver still
		/// fire synchronously right after it, matching WinUI.
		/// </summary>
		internal void RenderSync(UIElement element, int scaledWidth, int scaledHeight)
		{
			(_bufferSize, PixelWidth, PixelHeight) = PrepareRender(element, new Size(scaledWidth, scaledHeight)) is { } render
				? RenderToBuffer(element.Visual, render)
				: (0, 0, 0);
			InvalidateSource();
		}

		/// <summary>
		/// Computes the render dimensions and sizes the pixel buffer accordingly; null when the element has
		/// nothing to render.
		/// </summary>
		private (double Dpi, int Width, int Height, int TargetWidth, int TargetHeight, int ByteCount, UnmanagedArrayOfBytes Buffer)? PrepareRender(UIElement element, Size? scaledSize)
		{
			var renderSize = element.RenderSize;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return null;
			}

			// RenderTargetBitmap returns images at the current DPI (a 50x50 Border on WinUI returns 75x75 at 1.5x).
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));
			var (targetWidth, targetHeight) = scaledSize is { } size ? ((int)size.Width, (int)size.Height) : (width, height);
			var byteCount = targetWidth * targetHeight * _bytesPerPixel;
			EnsureBuffer(ref _buffer, byteCount);

			return (dpi, width, height, targetWidth, targetHeight, byteCount, _buffer!);
		}

		private static unsafe (int ByteCount, int Width, int Height) RenderToBuffer(ContainerVisual visual, (double Dpi, int Width, int Height, int TargetWidth, int TargetHeight, int ByteCount, UnmanagedArrayOfBytes Buffer) render)
		{
			var compositor = Compositor.GetSharedCompositor();
			var previousCompMode = compositor.IsSoftwareRenderer;
			var previousClip = visual.LayoutClip;
			try
			{
				// The offscreen render rasterizes on the CPU; effect brushes consult this while rendering.
				compositor.IsSoftwareRenderer = true;
				// Render the full element, ignoring the parent's layout slot clip.
				visual.LayoutClip = null;

				// Scale the logical element size to fill the target pixel box (== DPI when no explicit target size).
				var scaleX = render.Width == 0 ? (float)render.Dpi : (float)(render.TargetWidth * render.Dpi / render.Width);
				var scaleY = render.Height == 0 ? (float)render.Dpi : (float)(render.TargetHeight * render.Dpi / render.Height);

				var image = DrawingFactory.Current.RenderOffscreen(render.TargetWidth, render.TargetHeight, session =>
				{
					session.Save();
					session.Scale(scaleX, scaleY);
					visual.RenderRootVisual(session, offsetOverride: Vector2.Zero);
					session.Restore();
				});

				image.CopyPixels(new Span<byte>(render.Buffer.Pointer.ToPointer(), render.ByteCount));
				return (render.ByteCount, render.TargetWidth, render.TargetHeight);
			}
			finally
			{
				visual.LayoutClip = previousClip;
				compositor.IsSoftwareRenderer = previousCompMode;
			}
		}
	}
}
