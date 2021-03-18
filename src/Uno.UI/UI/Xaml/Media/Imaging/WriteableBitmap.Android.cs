using System;
using System.IO;
using Windows.Foundation;
using Windows.Storage.Streams;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private protected override bool IsSourceReady => true;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out Bitmap image)
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

			image = Bitmap.CreateBitmap(drawableBuffer, PixelWidth, PixelHeight, Bitmap.Config.Argb8888);
			return true;
		}
	}
}
