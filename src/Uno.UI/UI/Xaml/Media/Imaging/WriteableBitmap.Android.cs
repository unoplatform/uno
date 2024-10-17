using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Graphics;
using Java.Nio;
using Uno.Extensions;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private protected override bool IsSourceReady => true;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			var drawableBuffer = new int[PixelWidth * PixelHeight];
			var sourceBuffer = _buffer.ToArray();

			// WriteableBitmap PixelBuffer is using BGRA format, Android's bitmap input buffer
			// requires Argb8888, so we swap bytes to conform to this format.

			for (int i = 0; i < drawableBuffer.Length; i++)
			{
				var a = sourceBuffer[i * 4 + 3];
				var r = sourceBuffer[i * 4 + 2];
				var g = sourceBuffer[i * 4 + 1];
				var b = sourceBuffer[i * 4 + 0];

				drawableBuffer[i] = (a << 24) | (r << 16) | (g << 8) | b;
			}

			image = ImageData.FromBitmap(Bitmap.CreateBitmap(drawableBuffer, PixelWidth, PixelHeight, Bitmap.Config.Argb8888));
			return image.HasData;
		}

		private void DecodeStreamIntoBuffer()
		{
			if (Stream.CanSeek)
			{
				Stream.Position = 0;
			}

			var image = BitmapFactory.DecodeStream(Stream);

			var pixels = new int[PixelWidth * PixelHeight];
			image.GetPixels(pixels, 0, PixelWidth, 0, 0, PixelWidth, PixelHeight);

			var pixelsBytes = MemoryMarshal.Cast<int, byte>(pixels.AsSpan());

			pixelsBytes.CopyTo(_buffer.Span);
			Debug.Assert(_buffer.Span.Length == PixelWidth * PixelHeight * 4);
		}
	}
}
