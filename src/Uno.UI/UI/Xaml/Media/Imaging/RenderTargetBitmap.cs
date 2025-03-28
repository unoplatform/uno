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
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Windows.UI.Xaml.Media.Imaging
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

#if __ANDROID__
		private byte[]? _buffer;
#else
		private UnmanagedArrayOfBytes? _buffer;
#endif
		private int _bufferSize;

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

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element, int scaledWidth, int scaledHeight)
			=> AsyncAction.FromTask(ct =>
			{
				try
				{
					element ??= WinUICoreServices.Instance.MainVisualTree?.PublicRootVisual;

					if (element is null)
					{
						throw new InvalidOperationException("No visual tree is available and no UIElement was provided for render");
					}

					(_bufferSize, PixelWidth, PixelHeight) = RenderAsBgra8_Premul(element!, ref _buffer, new Size(scaledWidth, scaledHeight));
#if __WASM__ || __SKIA__
					InvalidateSource();
#endif
				}
				catch (Exception error)
				{
					this.Log().Error("Failed to render element to bitmap.", error);
				}

				return Task.CompletedTask;
			});

#if !HAS_RENDER_TARGET_BITMAP
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element)
			=> AsyncAction.FromTask(ct =>
			{
				try
				{
					element ??= WinUICoreServices.Instance.MainVisualTree?.RootElement;

					if (element is null)
					{
						throw new InvalidOperationException("No window or element to render");
					}

					(_bufferSize, PixelWidth, PixelHeight) = RenderAsBgra8_Premul(element!, ref _buffer);
#if __WASM__ || __SKIA__
					InvalidateSource();
#endif
				}
				catch (Exception error)
				{
					this.Log().Error("Failed to render element to bitmap.", error);
				}

				return Task.CompletedTask;
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

#if __ANDROID__
				return Task.FromResult<IBuffer>(new Buffer(_buffer.AsMemory().Slice(0, _bufferSize)));
#else
				unsafe
				{
					var mem = new UnmanagedMemoryManager<byte>((byte*)_buffer.Pointer.ToPointer(), _bufferSize);
					return Task.FromResult<IBuffer>(new Buffer(mem.Memory.Slice(0, _bufferSize)));
				}
#endif
			});

		#region Misc static helpers
#if HAS_RENDER_TARGET_BITMAP
#if __ANDROID__
		private static void EnsureBuffer(ref byte[]? buffer, int length)
		{
			if (buffer is null || buffer.Length < length)
			{
				buffer = new byte[length];
			}
		}
#else
		private static void EnsureBuffer(ref UnmanagedArrayOfBytes? buffer, int length)
		{
			if (buffer is null || buffer.Length < length)
			{
				buffer = new UnmanagedArrayOfBytes(length);
			}
		}
#endif
#endif
		#endregion
	}
}
