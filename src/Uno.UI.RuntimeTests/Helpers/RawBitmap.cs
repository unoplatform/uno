#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Windows.UI;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	/// <summary>
	/// Represents a <see cref="RenderTargetBitmap"/> to be tested against.
	/// </summary>
	public class RawBitmap
	{
		private byte[]? _pixels;

		private RawBitmap(RenderTargetBitmap bitmap, UIElement renderedElement)
		{
			Bitmap = bitmap;
			RenderedElement = renderedElement;
		}

		public static async Task<RawBitmap> From(RenderTargetBitmap bitmap, UIElement renderedElement)
		{
			var raw = new RawBitmap(bitmap, renderedElement);
			await raw.Populate();

			return raw;
		}

		public Size Size => new(Bitmap.PixelWidth, Bitmap.PixelHeight);

		public int Width => Bitmap.PixelWidth;
		public int Height => Bitmap.PixelHeight;

		/// <summary>
		/// The rendered UIElement
		/// </summary>
		public UIElement RenderedElement { get; }

		public RenderTargetBitmap Bitmap { get; }

		public Color GetPixel(int x, int y)
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			var offset = (y * Width + x) * 4;
			var a = _pixels[offset + 3];
			var r = _pixels[offset + 2];
			var g = _pixels[offset + 1];
			var b = _pixels[offset + 0];

			return Color.FromArgb(a, r, g, b);
		}

		/// <summary>
		/// Enables the <see cref="GetPixel(int, int)"/> method.
		/// </summary>
		/// <returns></returns>
		internal async Task Populate()
			=> _pixels ??= (await Bitmap.GetPixelsAsync()).ToArray();
	}
}
