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
using Buffer = Windows.Storage.Streams.Buffer;
using System.Buffers;

namespace Microsoft.UI.Xaml.Media.Imaging
{
#if NOT_IMPLEMENTED
	[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public partial class RenderTargetBitmap : IDisposable
	{
		private static void Swap(ref byte a, ref byte b)
		{
			(a, b) = (b, a);
		}

		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static void SwapRB(ref byte[] buffer, int byteCount)
		{
			for (int i = 0; i < byteCount; i += 4)
			{
				//Swap R and B chanal
				Swap(ref buffer![i], ref buffer![i + 2]);
			}
		}
#if NOT_IMPLEMENTED
		internal const bool IsImplemented = false;
#else
		internal const bool IsImplemented = true;
#endif

		#region PixelWidth
#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelWidthProperty { get; } = DependencyProperty.Register(
			"PixelWidth", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelWidth
		{
			get => (int)GetValue(PixelWidthProperty);
			private set => SetValue(PixelWidthProperty, value);
		}
		#endregion

		#region PixelHeight

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty PixelHeightProperty { get; } = DependencyProperty.Register(
			"PixelHeight", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public int PixelHeight
		{
			get => (int)GetValue(PixelHeightProperty);
			private set => SetValue(PixelHeightProperty, value);
		}
		#endregion

		private byte[]? _buffer;
		private int _bufferSize;

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element, int scaledWidth, int scaledHeight)
			=> AsyncAction.FromTask(ct =>
			{
				try
				{
					UIElement elementToRender = element
						?? Window.Current.Content;
					(_bufferSize, PixelWidth, PixelHeight) = RenderAsBgra8_Premul(elementToRender, ref _buffer, new Size(scaledWidth, scaledHeight));
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
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncAction RenderAsync(UIElement? element)
			=> AsyncAction.FromTask(ct =>
			{
				try
				{
					UIElement elementToRender = element
						?? Window.Current.Content;

					(_bufferSize, PixelWidth, PixelHeight) = RenderAsBgra8_Premul(elementToRender, ref _buffer);
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
		[global::Uno.NotImplemented("NET461", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
		public IAsyncOperation<IBuffer> GetPixelsAsync()
			=> AsyncOperation<IBuffer>.FromTask((op, ct) =>
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
		void IDisposable.Dispose()
		{
			if (_buffer is { })
			{
				ArrayPool<byte>.Shared.Return(_buffer);
			}
		}
	}
}
