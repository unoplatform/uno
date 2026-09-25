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
using Uno.UI.Composition.Drawing;

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
#endif
		#endregion

		private const int _bytesPerPixel = 4;

		// Serializes RenderAsync calls on this instance: the capture writes into the per-instance buffer,
		// so an overlapping call must not resize/replace it.
		private readonly SemaphoreSlim _renderGate = new(1, 1);

		private static unsafe ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
		{
			try
			{
				// Wrap the BGRA (premultiplied) buffer as a neutral image (copied, so the buffer is reusable).
				var image = ImageEncoderDecoder.Current.CreateImage(width, height, new ReadOnlySpan<byte>(buffer.Pointer.ToPointer(), bufferLength));
				return ImageData.FromCompositionSurface(new CompositionImageSurface(image));
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
					? await RenderToBufferAsync(element.Visual, render)
					: (0, 0, 0);
			}
			finally
			{
				_renderGate.Release();
			}
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

		// Renders the element into an offscreen backend texture at the target pixel size. The caller owns the
		// returned texture and reads it back asynchronously (RenderToBufferAsync / IDrawingFactory.SnapshotAsync).
		private static ITexture RenderToTexture(IDrawingFactory factory, ContainerVisual visual, (double Dpi, int Width, int Height, int TargetWidth, int TargetHeight, int ByteCount, UnmanagedArrayOfBytes Buffer) render)
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

				return factory.RenderOffscreen(render.TargetWidth, render.TargetHeight, session =>
				{
					session.Save();
					session.Scale(scaleX, scaleY);
					visual.RenderRootVisual(session, offsetOverride: Vector2.Zero);
					session.Restore();
				});
			}
			finally
			{
				visual.LayoutClip = previousClip;
				compositor.IsSoftwareRenderer = previousCompMode;
			}
		}

		// Async readback (SnapshotAsync) — the general path. Completes synchronously on CPU/desktop backends and
		// truly asynchronously on WASM WebGPU (where a blocking GPU→CPU map would hang the single JS thread).
		private static async Task<(int ByteCount, int Width, int Height)> RenderToBufferAsync(ContainerVisual visual, (double Dpi, int Width, int Height, int TargetWidth, int TargetHeight, int ByteCount, UnmanagedArrayOfBytes Buffer) render)
		{
			// Capture the factory once — the texture and its snapshot must come from the same backend even if the
			// active backend were swapped across the await.
			// The visual being captured names the backend; resolved (and the texture built) before the await, so the
			// snapshot reads back through the very factory that produced it.
			var factory = visual.CompositionTarget?.Renderer
				?? throw new InvalidOperationException("Cannot render a visual that is not attached to a window's composition target.");
			using var texture = RenderToTexture(factory, visual, render);
			using var image = await factory.SnapshotAsync(texture);
			CopyPixelsTo(image, render.Buffer.Pointer, render.ByteCount);
			return (render.ByteCount, render.TargetWidth, render.TargetHeight);
		}

		private static unsafe void CopyPixelsTo(IImage image, IntPtr destination, int byteCount)
			=> image.CopyPixels(new Span<byte>((void*)destination, byteCount));
	}
}
