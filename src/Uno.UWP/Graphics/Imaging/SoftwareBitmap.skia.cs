using System;
using SkiaSharp;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;

namespace Windows.Graphics.Imaging
{
	static class SoftwareBitmapExtensions
	{
		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static SKColorType ToSKColorType(this BitmapPixelFormat format) =>
			format switch
			{
				BitmapPixelFormat.Unknown => SKColorType.Unknown,
				BitmapPixelFormat.Rgba16 => SKColorType.Rgba16161616,
				BitmapPixelFormat.Rgba8 => SKColorType.Rgba8888,
				BitmapPixelFormat.Gray8 => SKColorType.Gray8,
				BitmapPixelFormat.Bgra8 => SKColorType.Bgra8888,
				_ => throw new global::System.NotSupportedException(nameof(format))
			};

		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static BitmapPixelFormat ToBitmapPixelFormat(this SKColorType format) =>
			format switch
			{
				SKColorType.Unknown => BitmapPixelFormat.Unknown,
				SKColorType.Rgba16161616 => BitmapPixelFormat.Rgba16,
				SKColorType.Rgba8888 => BitmapPixelFormat.Rgba8,
				SKColorType.Gray8 => BitmapPixelFormat.Gray8,
				SKColorType.Bgra8888 => BitmapPixelFormat.Bgra8,
				_ => throw new global::System.NotSupportedException(nameof(format))
			};

		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static SKAlphaType ToSKAlphaType(this global::Windows.Graphics.Imaging.BitmapAlphaMode alpha) =>
			alpha switch
			{
				BitmapAlphaMode.Ignore => SKAlphaType.Opaque,
				BitmapAlphaMode.Straight => SKAlphaType.Unpremul,
				BitmapAlphaMode.Premultiplied => SKAlphaType.Premul,
				_ => throw new global::System.NotSupportedException(nameof(alpha))
			};

		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static BitmapAlphaMode ToBitmapAlphaMode(this SKAlphaType alpha) =>
			alpha switch
			{
				SKAlphaType.Opaque => BitmapAlphaMode.Ignore,
				SKAlphaType.Unpremul => BitmapAlphaMode.Straight,
				SKAlphaType.Premul => BitmapAlphaMode.Premultiplied,
				_ => throw new global::System.NotSupportedException(nameof(alpha))
			};
	}

	partial class SoftwareBitmap : IDisposable
	{
		private readonly SKBitmap bitmap;

		internal SoftwareBitmap(SKBitmap bitmap, bool isReadOnly = false)
		{
			this.bitmap = bitmap;
			IsReadOnly = isReadOnly;
		}

		public SoftwareBitmap(global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			var info = new SKImageInfo(width, height, format.ToSKColorType());
			bitmap = new SKBitmap(info);
		}

		public SoftwareBitmap(global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			var info = new SKImageInfo(width, height, format.ToSKColorType(), alpha.ToSKAlphaType());
			bitmap = new SKBitmap(info);
		}

		public BitmapAlphaMode BitmapAlphaMode =>
			bitmap.AlphaType.ToBitmapAlphaMode();

		public BitmapPixelFormat BitmapPixelFormat =>
			bitmap.ColorType.ToBitmapPixelFormat();

		public bool IsReadOnly { get; }

		public int PixelHeight =>
			bitmap.Height;

		public int PixelWidth =>
			bitmap.Width;

		internal SKBitmap Bitmap => bitmap;

		public SoftwareBitmap GetReadOnlyView() =>
			new SoftwareBitmap(bitmap, true);

		public void CopyTo(SoftwareBitmap destination)
		{
			if (destination.IsReadOnly)
			{
				throw new ArgumentException("Destionanion is ReadOnly", nameof(destination));
			}
			using var canvas = new SKCanvas(destination.bitmap);
			canvas.DrawBitmap(bitmap, 0, 0);
			canvas.Flush();
		}

		public static SoftwareBitmap Copy(SoftwareBitmap source) =>
			new SoftwareBitmap(source.bitmap.Copy(), false);

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			return CreateCopyFromBuffer(source, format, width, height, global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
		}
		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			// Get pixels
			var pixelArray = source.ToArray();
			var info = new SKImageInfo(width, height, format.ToSKColorType(), alpha.ToSKAlphaType());

			// create an empty bitmap
			var destination = new SKBitmap();

			// pin the managed array so that the GC doesn't move it
			var gcHandle = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);

			// install the pixels with the color type of the pixel data

			var success = destination.
				InstallPixels(info
				, gcHandle.AddrOfPinnedObject()
				, info.RowBytes
				, (address, context) => ((GCHandle)context).Free(), gcHandle);

			if (!success)
			{
				throw new ArgumentException($"The pixel format of {nameof(source)} is not {format}.", nameof(source));
			}

			return new SoftwareBitmap(destination);
		}

		public void Dispose()
		{
			bitmap?.Dispose();
		}
	}
}
