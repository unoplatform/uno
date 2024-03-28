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
using System.Runtime.InteropServices;

using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Windows.UI.Xaml.Media.Imaging
{
#if NOT_IMPLEMENTED
	[global::Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public partial class RenderTargetBitmap : ImageSource
	{
#if !__ANDROID__
		// This is to avoid LOH array allocations
		private unsafe class UnmanagedArrayOfBytes
		{
			public nint Pointer;
			public int Length { get; }

			public UnmanagedArrayOfBytes(int length)
			{
				Length = length;
				Pointer = Marshal.AllocHGlobal(length);
				GC.AddMemoryPressure(length);
			}

			public byte this[int index]
			{
				get
				{
					return ((byte*)Pointer.ToPointer())[index];
				}
				set
				{
					((byte*)Pointer.ToPointer())[index] = value;
				}
			}

			~UnmanagedArrayOfBytes()
			{
				Marshal.FreeHGlobal(Pointer);
				GC.RemoveMemoryPressure(Length);
			}
		}

		// https://stackoverflow.com/questions/52190423/c-sharp-access-unmanaged-array-using-memoryt-or-arraysegmentt
		private sealed unsafe class UnmanagedMemoryManager<T> : MemoryManager<T>
			where T : unmanaged
		{
			private readonly T* _pointer;
			private readonly int _length;

			/// <summary>
			/// Create a new UnmanagedMemoryManager instance at the given pointer and size
			/// </summary>
			public UnmanagedMemoryManager(T* pointer, int length)
			{
				if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
				_pointer = pointer;
				_length = length;
			}
			/// <summary>
			/// Obtains a span that represents the region
			/// </summary>
			public override Span<T> GetSpan() => new Span<T>(_pointer, _length);

			/// <summary>
			/// Provides access to a pointer that represents the data (note: no actual pin occurs)
			/// </summary>
			public override MemoryHandle Pin(int elementIndex = 0)
			{
				if (elementIndex < 0 || elementIndex >= _length)
					throw new ArgumentOutOfRangeException(nameof(elementIndex));
				return new MemoryHandle(_pointer + elementIndex);
			}

			/// <summary>
			/// Has no effect
			/// </summary>
			public override void Unpin() { }

			/// <summary>
			/// Releases all resources associated with this object
			/// </summary>
			protected override void Dispose(bool disposing) { }
		}
#endif

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

#if !__ANDROID__
		private UnmanagedArrayOfBytes? _buffer;
#else
		private byte[]? _buffer;
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

#if NOT_IMPLEMENTED
		private static ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
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

#if !__ANDROID__
				unsafe
				{
					var mem = new UnmanagedMemoryManager<byte>((byte*)_buffer.Pointer.ToPointer(), _bufferSize);
					return Task.FromResult<IBuffer>(new Buffer(mem.Memory.Slice(0, _bufferSize)));
				}
#else
				return Task.FromResult<IBuffer>(new Buffer(_buffer.AsMemory().Slice(0, _bufferSize)));
#endif
			});

#if NOT_IMPLEMENTED
		private (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize = null)
			=> throw new NotImplementedException("RenderTargetBitmap is not supported on this platform.");
#endif

		#region Misc static helpers
#if !NOT_IMPLEMENTED
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
