#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.Helpers;

namespace Uno.UI.RuntimeTests.Helpers
{
	/// <summary>
	/// Represents a <see cref="RenderTargetBitmap"/> to be tested against.
	/// </summary>
	public class RawBitmap
	{
		private readonly TestBitmap _bitmap;

		internal RawBitmap(TestBitmap bitmap)
		{
			_bitmap = bitmap;
		}

		/// <summary>
		/// Prefer using UITestHelper.Screenshot() instead.
		/// </summary>
		public static async Task<RawBitmap> From(RenderTargetBitmap bitmap, UIElement renderedElement, double? implicitScaling = null)
			=> new(await TestBitmap.From(bitmap, renderedElement, implicitScaling));

		public double ImplicitScaling => _bitmap.ImplicitScaling;

		public Size Size => _bitmap.Size;

		public int Width => _bitmap.Width;
		public int Height => _bitmap.Height;

		public Color this[int x, int y] => _bitmap[x, y];

		public Color GetPixel(int x, int y) => _bitmap.GetPixel(x, y);

		internal byte[] GetPixels() => _bitmap.GetRawPixels();

		public static implicit operator TestBitmap(RawBitmap bitmap)
			=> bitmap._bitmap;
	}
}
