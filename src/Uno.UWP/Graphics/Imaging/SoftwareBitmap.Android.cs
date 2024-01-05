using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Android.Graphics;
using Uno.UI;
using Windows.Foundation;
using Java.Nio;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap : IDisposable
	{
		private readonly Bitmap bitmap;

		internal SoftwareBitmap(Bitmap bitmap, bool isReadOnly = false)
		{
			this.bitmap = bitmap;
			IsReadOnly = isReadOnly;
		}

		public BitmapAlphaMode BitmapAlphaMode =>
			this.bitmap.IsPremultiplied
			 ? Imaging.BitmapAlphaMode.Premultiplied
			 : Imaging.BitmapAlphaMode.Straight;

		public BitmapPixelFormat BitmapPixelFormat =>
			Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8;

		public bool IsReadOnly { get; }

		public int PixelHeight =>
			bitmap.Height;

		public int PixelWidth =>
			bitmap.Width;

		internal Bitmap Bitmap => bitmap;

		public SoftwareBitmap GetReadOnlyView() =>
			new SoftwareBitmap(bitmap, true);

		public void CopyTo(global::Windows.Graphics.Imaging.SoftwareBitmap bitmap)
		{
			if (bitmap.IsReadOnly)
			{
				throw new ArgumentException("Destionanion is ReadOnly", nameof(bitmap));
			}

			using (var canvas = new Android.Graphics.Canvas(bitmap.Bitmap))
			{
				var sourceRect = new Android.Graphics.Rect(0, 0, this.Bitmap.Width, this.Bitmap.Height);
				var destRect = new Android.Graphics.Rect(0, 0, bitmap.Bitmap.Width, bitmap.Bitmap.Height);
				canvas.DrawBitmap(this.Bitmap, sourceRect, destRect, null);
			}
		}

		public static SoftwareBitmap Copy(SoftwareBitmap source) =>
			new SoftwareBitmap(source.bitmap.Copy(null, true)!, false);

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			return CreateCopyFromBuffer(source, format, width, height, global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
		}

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			// Get pixels
			var pixels = source.ToArray();
			if (format != BitmapPixelFormat.Rgba8
				&& format != BitmapPixelFormat.Bgra8)
			{
				throw new NotSupportedException($"The {format} pixels format is not supported.");
			}

			static void Swap(ref byte foo, ref byte bar)
			{
				(foo, bar) = (bar, foo);
			}

			if (format == BitmapPixelFormat.Bgra8)
			{
				//Android Store Argb8888 as rbga
				var byteCount = pixels.Length;
				for (int i = 0; i < byteCount; i += 4)
				{
					//Swap R and B chanal
					Swap(ref pixels[i], ref pixels[i + 2]);
				}
			}
			var destination = Bitmap.CreateBitmap((int)width, (int)height, Bitmap.Config.Argb8888!)!;

			destination.SetPremultiplied(alpha == BitmapAlphaMode.Premultiplied);

			using var buffer = ByteBuffer.Wrap(pixels);
			destination.CopyPixelsFromBuffer(buffer);

			return new SoftwareBitmap(destination);
		}

		public void Dispose()
		{
			bitmap?.Dispose();
		}
	}
}
