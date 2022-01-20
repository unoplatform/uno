#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
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
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, [NotNullWhen(true)] out Bitmap? image)
		{
			image = BitmapFactory.DecodeByteArray(_buffer, 0, _buffer.Length);
			return image != null;
		}

		private static byte[] RenderAsPng(UIElement element, Size? scaledSize = null)
		{
			var logical = element.ActualSize.ToSize();
			var physical = logical.LogicalToPhysicalPixels();
			var bitmap = Bitmap.CreateBitmap((int)physical.Width, (int)physical.Height, Bitmap.Config.Argb8888!)
				?? throw new InvalidOperationException("Failed to create target native bitmap.");
			var canvas = new Canvas(bitmap);

			// Make sure the element has been Layouted 
			element.Layout(0, 0, (int)physical.Width, (int)physical.Height);

			// Render on the canvas
			canvas.DrawColor(Colors.Transparent);
			element.Draw(canvas);

			if (scaledSize is {} targetSize)
			{
				bitmap = Bitmap.CreateScaledBitmap(bitmap, (int)targetSize.Width, (int)targetSize.Height, false)
					?? throw new InvalidOperationException("Failed to scaled native bitmap to the requested size.");
			}

			using var stream = new MemoryStream();
			bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);

			return stream.ToArray();
		}
	}
}
