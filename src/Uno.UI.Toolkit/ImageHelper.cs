#nullable enable

using System;
using System.Linq;
using Windows.UI;

namespace Uno.Helpers;

internal static class ImageHelper
{
	/// <summary>
	/// Make all pixels of the given buffer opaque.
	/// </summary>
	/// <param name="rgba8PixelsBuffer">The pixels buffer (must not be pre-multiplied!).</param>
	/// <param name="background">The **opaque** background to use.</param>
	public static bool MakeOpaque(byte[] rgba8PixelsBuffer, Color? background = null)
	{
		if (background is { A: not 255 })
		{
			throw new ArgumentException("The background color must be opaque.", nameof(background));
		}

		background ??= Colors.White;

		var modified = false;
		for (var i = 0; i < rgba8PixelsBuffer.Length; i += 4)
		{
			var a = rgba8PixelsBuffer[i + 3];
			if (a == 255)
			{
				continue;
			}

			var r = rgba8PixelsBuffer[i + 2];
			var g = rgba8PixelsBuffer[i + 1];
			var b = rgba8PixelsBuffer[i + 0];

			var opaque = Color.FromArgb(a, r, g, b).ToOpaque(background.Value);

			rgba8PixelsBuffer[i + 3] = opaque.A; // 255
			rgba8PixelsBuffer[i + 2] = opaque.R;
			rgba8PixelsBuffer[i + 1] = opaque.G;
			rgba8PixelsBuffer[i + 0] = opaque.B;

			modified = true;
		}

		return modified;
	}

	/// <summary>
	/// Un-multiply the alpha channel of each pixel of the given buffer.
	/// </summary>
	/// <param name="rgba8PremulPixelsBuffer">The pixel buffer.</param>
	public static void UnMultiplyAlpha(byte[] rgba8PremulPixelsBuffer)
	{
		for (var i = 0; i < rgba8PremulPixelsBuffer.Length; i += 4)
		{
			var a = rgba8PremulPixelsBuffer[i + 3];
			var r = rgba8PremulPixelsBuffer[i + 2];
			var g = rgba8PremulPixelsBuffer[i + 1];
			var b = rgba8PremulPixelsBuffer[i + 0];

			//a = a;
			r = (byte)(255.0 * r / a);
			g = (byte)(255.0 * g / a);
			b = (byte)(255.0 * b / a);

			rgba8PremulPixelsBuffer[i + 2] = r;
			rgba8PremulPixelsBuffer[i + 1] = g;
			rgba8PremulPixelsBuffer[i + 0] = b;
		}
	}
}
