#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.Helpers;

namespace Uno.UI.RuntimeTests.Helpers
{
	/// <summary>
	/// Represents a <see cref="RenderTargetBitmap"/> to be tested against.
	/// </summary>
	public class RawBitmap
	{
		private byte[]? _pixels;
		private readonly double _implicitScaling;
		private bool _altered;

		private RawBitmap(RenderTargetBitmap bitmap, UIElement renderedElement, double implicitScaling)
		{
			Bitmap = bitmap;
			RenderedElement = renderedElement;
			_implicitScaling = implicitScaling;
		}

		/// <summary>
		/// Prefer using UITestHelper.Screenshot() instead.
		/// </summary>
		public static async Task<RawBitmap> From(RenderTargetBitmap bitmap, UIElement renderedElement, double? implicitScaling = null)
		{
			implicitScaling ??= renderedElement.XamlRoot?.RasterizationScale ?? 1;
			var raw = new RawBitmap(bitmap, renderedElement, implicitScaling.Value);
			await raw.Populate();

			return raw;
		}

		public double ImplicitScaling => _implicitScaling;

		public Size Size => new(Width, Height);

		public int Width => (int)(Bitmap.PixelWidth / _implicitScaling);
		public int Height => (int)(Bitmap.PixelHeight / _implicitScaling);

		/// <summary>
		/// The rendered UIElement
		/// </summary>
		public UIElement RenderedElement { get; }

		/// <summary>
		/// Gets the underlying <see cref="RenderTargetBitmap"/>.
		/// Be aware this might not be the same as of the current state of the RawBitmap (cf. <see cref="MakeOpaque"/>).
		/// Prefer to use the <see cref="GetImageSource"/>.
		/// </summary>
		internal RenderTargetBitmap Bitmap { get; }

		public Color this[int x, int y] => GetPixel(x, y);

		public Color GetPixel(int x, int y)
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			x = (int)(x * _implicitScaling);
			y = (int)(y * _implicitScaling);

			var offset = (y * Bitmap.PixelWidth + x) * 4;
			var a = _pixels[offset + 3];
			var r = _pixels[offset + 2];
			var g = _pixels[offset + 1];
			var b = _pixels[offset + 0];

			return Color.FromArgb(a, r, g, b);
		}

		internal byte[] GetPixels()
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			return _pixels;
		}

		/// <summary>
		/// Enables the <see cref="GetPixel(int, int)"/> method.
		/// </summary>
		/// <returns></returns>
		internal async Task Populate()
		{
			_pixels ??= (await Bitmap.GetPixelsAsync()).ToArray();

			// Image is RGBA-premul, we need to un-multiply it to get the actual color in GetPixel().
			ImageHelper.UnMultiplyAlpha(_pixels);
		}

		internal void MakeOpaque(Color? background = null)
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			_altered = ImageHelper.MakeOpaque(_pixels, background);
		}

		internal async Task<ImageSource> GetImageSource(bool preferOriginal = false)
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			if (_altered && !preferOriginal)
			{
				var output = new WriteableBitmap(Bitmap.PixelWidth, Bitmap.PixelHeight);
				await new MemoryStream(_pixels).AsInputStream().ReadAsync(output.PixelBuffer, output.PixelBuffer.Length, InputStreamOptions.None);
				return output;
			}
			else
			{
				return Bitmap;
			}
		}

#if __SKIA__ && DEBUG // DEBUG: Make the build to fail on CI to avoid forgetting to remove the call (would poluate server or other devs disks!).
		/// <summary>
		/// Save the screenshot into the specified path **for debug purposes only**.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="preferOriginal"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		internal async Task Save(string path, bool preferOriginal = false)
		{
			if (_pixels is null)
			{
				throw new InvalidOperationException("Populate must be invoked first");
			}

			await using var file = File.OpenWrite(path);

			var img = preferOriginal
				? SkiaSharp.SKImage.FromPixelCopy(new SkiaSharp.SKImageInfo(Bitmap.PixelWidth, Bitmap.PixelHeight, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul), (await Bitmap.GetPixelsAsync()).ToArray())
				: SkiaSharp.SKImage.FromPixelCopy(new SkiaSharp.SKImageInfo(Bitmap.PixelWidth, Bitmap.PixelHeight, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Unpremul), _pixels);

			img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).SaveTo(file);
		}
#endif
	}
}
