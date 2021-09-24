using System;
using System.IO;
using System.Linq;
using Android.Graphics;
using Uno.UI;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		/// <inheritdoc />
		private protected override bool IsSourceReady => _buffer != null;

		/// <inheritdoc />
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out Bitmap image)
		{
			image = BitmapFactory.DecodeByteArray(_buffer, 0, _buffer.Length);
			return image != null;
		}

		private static byte[] RenderAsPng(UIElement element, Size? scaledSize = null)
		{
			var width = (int)ViewHelper.LogicalToPhysicalPixels(element.ActualSize.X);
			var height = (int)ViewHelper.LogicalToPhysicalPixels(element.ActualSize.Y);
			var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
			var canvas = new Canvas(bitmap);

			// Make sure the element has been Layouted 
			element.Layout(0, 0, width, height);

			// Render on the canvas
			canvas.DrawColor(Colors.White);
			element.Draw(canvas);

			if (scaledSize.HasValue)
			{
				canvas.Scale((float)(scaledSize.Value.Width / (float)width), (float)(scaledSize.Value.Height / (float)height));
			}

			using var stream = new MemoryStream();
			bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);

			return stream.ToArray();
		}
	}
}
