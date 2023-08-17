#nullable enable
#if !__IOS__ && !__ANDROID__ && !__SKIA__ && !__MACOS__
#define NOT_IMPLEMENTED
#endif

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

namespace Microsoft.UI.Xaml.Media.Imaging
{
#if NOT_IMPLEMENTED
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public partial class RenderTargetBitmap : ImageSource, IDisposable
	{
#if NOT_IMPLEMENTED
		internal const bool IsImplemented = false;
#else
		internal const bool IsImplemented = true;
#endif

		#region PixelWidth
#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelWidthProperty { get; } = DependencyProperty.Register(
			"PixelWidth", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelWidth
		{
			get => (int)GetValue(PixelWidthProperty);
			private set => SetValue(PixelWidthProperty, value);
		}
		#endregion

		#region PixelHeight

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelHeightProperty { get; } = DependencyProperty.Register(
			"PixelHeight", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelHeight
		{
			get => (int)GetValue(PixelHeightProperty);
			private set => SetValue(PixelHeightProperty, value);
		}
		#endregion

		private byte[]? _buffer;
		private int _bufferSize;

		/// <inheritdoc />
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			var width = PixelWidth;
			var height = PixelHeight;

			if (_buffer is null || _bufferSize <= 0 || width <= 0 || height <= 0)
			{
				image = default;
				return false;
			}

			image = Open(_buffer, _bufferSize, width, height);
			InvalidateImageSource();
			return image.HasData;
		}

#if NOT_IMPLEMENTED
		private static ImageData Open(byte[] buffer, int bufferLength, int width, int height)
			=> default;
#endif

#if NOT_IMPLEMENTED
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

#if NOT_IMPLEMENTED
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

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncOperation<IBuffer> GetPixelsAsync()
			=> AsyncOperation.FromTask(ct =>
			{
				if (_buffer is null)
				{
					return Task.FromResult<IBuffer>(new Buffer(Array.Empty<byte>()));
				}
				return Task.FromResult<IBuffer>(new Buffer(_buffer.AsMemory().Slice(0, _bufferSize)));
			});

#if NOT_IMPLEMENTED
		private (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref byte[]? buffer, Size? scaledSize = null)
			=> throw new NotImplementedException("RenderTargetBitmap is not supported on this platform.");
#endif

		void IDisposable.Dispose()
		{
			if (_buffer is not null)
			{
				ArrayPool<byte>.Shared.Return(_buffer);
			}
		}

		#region Misc static helpers
#if !NOT_IMPLEMENTED
		private static void EnsureBuffer(ref byte[]? buffer, int length)
		{
			if (buffer is null)
			{
				buffer = ArrayPool<byte>.Shared.Rent(length);
			}
			else if (buffer.Length < length)
			{
				ArrayPool<byte>.Shared.Return(buffer);
				buffer = ArrayPool<byte>.Shared.Rent(length);
			}
		}
#endif
		#endregion
	}
}
