#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Buffer = Windows.Storage.Streams.Buffer;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using System.Numerics;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Media.Imaging
{
#if !HAS_RENDER_TARGET_BITMAP
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public partial class RenderTargetBitmap : ImageSource
	{
#if !HAS_RENDER_TARGET_BITMAP
		internal const bool IsImplemented = false;
#else
		internal const bool IsImplemented = true;
#endif

		/// <summary>
		/// Initializes a new instance of the RenderTargetBitmap class.
		/// </summary>
#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public RenderTargetBitmap()
		{
		}

#if !HAS_RENDER_TARGET_BITMAP
		// The partial API that has to be implemented in each platform

		private static ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
			=> default;

		private (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize = null)
			=> throw new NotImplementedException("RenderTargetBitmap is not supported on this platform.");
#endif

#if !__SKIA__
		// Skia provides a natively async implementation (GPU-accelerated when available);
		// other platforms wrap their synchronous implementation.
		private Task<(int ByteCount, int Width, int Height)> RenderAsBgra8_PremulAsync(UIElement element, Size? scaledSize = null)
			=> Task.FromResult(RenderAsBgra8_Premul(element, ref _buffer, scaledSize));
#endif

		#region PixelWidth
#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelWidthProperty { get; } = DependencyProperty.Register(
			"PixelWidth", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelWidth
		{
			get => (int)GetValue(PixelWidthProperty);
			private set => SetValue(PixelWidthProperty, value);
		}
		#endregion

		#region PixelHeight

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelHeightProperty { get; } = DependencyProperty.Register(
			"PixelHeight", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelHeight
		{
			get => (int)GetValue(PixelHeightProperty);
			private set => SetValue(PixelHeightProperty, value);
		}
		#endregion

		private UnmanagedArrayOfBytes? _buffer;
		private int _bufferSize;

#if __SKIA__
		private protected override unsafe bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out Task<ImageData>? asyncImage)
		{
			int width = PixelWidth;
			int height = PixelHeight;

			if (_buffer is not { } buffer || _bufferSize <= 0 || width <= 0 || height <= 0)
			{
				asyncImage = default;
				return false;
			}

			UnmanagedArrayOfBytes copy = new UnmanagedArrayOfBytes(_buffer.Length);
			Unsafe.CopyBlock(copy.Pointer.ToPointer(), _buffer.Pointer.ToPointer(), (uint)_buffer.Length);

			TaskCompletionSource<ImageData> tcs = new TaskCompletionSource<ImageData>();
			_ = Task.Run(() =>
			{
				try
				{
					tcs.TrySetResult(Open(buffer, _bufferSize, width, height));
				}
				catch (Exception e)
				{
					tcs.TrySetResult(ImageData.FromError(e));
				}
			}, ct);

			asyncImage = tcs.Task.ContinueWith(task =>
			{
				InvalidateImageSource();
				return task.Result;
			}, ct, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

			return true;
		}
#else
		/// <inheritdoc />
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			var width = PixelWidth;
			var height = PixelHeight;

			if (_buffer is not { } buffer || _bufferSize <= 0 || width <= 0 || height <= 0)
			{
				image = default;
				return false;
			}

			image = Open(buffer, _bufferSize, width, height);
			InvalidateImageSource();
			return image.HasData;
		}
#endif

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element, int scaledWidth, int scaledHeight)
			=> RenderAsync(element, new Size(scaledWidth, scaledHeight));

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element)
			=> RenderAsync(element, scaledSize: null);

		private IAsyncAction RenderAsync(UIElement? element, Size? scaledSize)
			=> AsyncAction.FromTask(async ct =>
			{
				try
				{
					// A null element renders the window's root visual (what's presented on
					// screen, including popups).
					element ??= WinUICoreServices.Instance.MainVisualTree?.RootElement;

					if (element is null)
					{
						throw new InvalidOperationException("No visual tree is available and no UIElement was provided for render");
					}

					(_bufferSize, PixelWidth, PixelHeight) = await RenderAsBgra8_PremulAsync(element, scaledSize);
#if __SKIA__
					InvalidateSource();
#endif
				}
				catch (Exception error)
				{
					this.Log().Error("Failed to render element to bitmap.", error);
				}
			});

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncOperation<IBuffer> GetPixelsAsync()
			=> AsyncOperation.FromTask(ct =>
			{
				if (_buffer is null)
				{
					return Task.FromResult<IBuffer>(new Buffer([]));
				}

				unsafe
				{
					var mem = new UnmanagedMemoryManager<byte>((byte*)_buffer.Pointer.ToPointer(), _bufferSize);
					return Task.FromResult<IBuffer>(new Buffer(mem.Memory.Slice(0, _bufferSize)));
				}
			});

		#region Misc static helpers
#if HAS_RENDER_TARGET_BITMAP
		private static void EnsureBuffer(ref UnmanagedArrayOfBytes? buffer, int length)
		{
			if (buffer is null || buffer.Length < length)
			{
				buffer = new UnmanagedArrayOfBytes(length);
			}
		}

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
#endif
		#endregion
	}
}
