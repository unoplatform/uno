#nullable enable

using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap : IDisposable
	{
		private byte[] _pixels;
		private readonly int _width;
		private readonly int _height;
		private readonly BitmapPixelFormat _format;
		private readonly BitmapAlphaMode _alpha;

		internal SoftwareBitmap(byte[] pixels, int width, int height, BitmapPixelFormat format, BitmapAlphaMode alpha, bool isReadOnly = false)
		{
			_pixels = pixels;
			_width = width;
			_height = height;
			_format = format;
			_alpha = alpha;
			IsReadOnly = isReadOnly;
		}

		public SoftwareBitmap(BitmapPixelFormat format, int width, int height)
			: this(format, width, height, BitmapAlphaMode.Premultiplied)
		{
		}

		public SoftwareBitmap(BitmapPixelFormat format, int width, int height, BitmapAlphaMode alpha)
			: this(new byte[GetByteCount(format, width, height)], width, height, format, alpha)
		{
		}

		private static int GetBytesPerPixel(BitmapPixelFormat format) =>
			format switch
			{
				BitmapPixelFormat.Rgba16 => 8,
				BitmapPixelFormat.Rgba8 => 4,
				BitmapPixelFormat.Bgra8 => 4,
				BitmapPixelFormat.Gray8 => 1,
				_ => throw new NotSupportedException(nameof(format))
			};

		private static int GetByteCount(BitmapPixelFormat format, int width, int height)
			=> GetBytesPerPixel(format) * width * height;

		public BitmapAlphaMode BitmapAlphaMode => _alpha;

		public BitmapPixelFormat BitmapPixelFormat => _format;

		public bool IsReadOnly { get; }

		public int PixelHeight => _height;

		public int PixelWidth => _width;

		internal byte[] Pixels => _pixels;

		internal int RowBytes => GetBytesPerPixel(_format) * _width;

		public SoftwareBitmap GetReadOnlyView() =>
			new SoftwareBitmap(_pixels, _width, _height, _format, _alpha, true);

		public void CopyTo(SoftwareBitmap bitmap)
		{
			if (bitmap.IsReadOnly)
			{
				throw new ArgumentException("Destination is ReadOnly", nameof(bitmap));
			}
			if (bitmap._width != _width || bitmap._height != _height || bitmap._format != _format)
			{
				throw new NotSupportedException("SoftwareBitmap.CopyTo requires matching dimensions and pixel format.");
			}
			Buffer.BlockCopy(_pixels, 0, bitmap._pixels, 0, Math.Min(_pixels.Length, bitmap._pixels.Length));
		}

		public static SoftwareBitmap Copy(SoftwareBitmap source) =>
			new SoftwareBitmap((byte[])source._pixels.Clone(), source._width, source._height, source._format, source._alpha);

		public static SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, BitmapPixelFormat format, int width, int height)
			=> CreateCopyFromBuffer(source, format, width, height, BitmapAlphaMode.Premultiplied);

		public static SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, BitmapPixelFormat format, int width, int height, BitmapAlphaMode alpha)
		{
			var pixels = source.ToArray();
			var expected = GetByteCount(format, width, height);
			if (pixels.Length < expected)
			{
				throw new ArgumentException($"The pixel format of {nameof(source)} is not {format}.", nameof(source));
			}
			return new SoftwareBitmap(pixels, width, height, format, alpha);
		}

		public void Dispose() => _pixels = Array.Empty<byte>();
	}
}
