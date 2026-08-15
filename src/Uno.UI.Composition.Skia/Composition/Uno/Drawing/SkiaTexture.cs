#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="ITexture"/> — an owned <see cref="SKImage"/> uploaded from neutral pixels.</summary>
internal sealed class SkiaTexture : ITexture
{
	public SkiaTexture(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;

	public unsafe void CopyPixels(Span<byte> destination)
	{
		var info = new SKImageInfo(Image.Width, Image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		fixed (byte* dst = destination)
		{
			Image.ReadPixels(info, (nint)dst, info.RowBytes, 0, 0);
		}
	}

	public void Dispose() => Image.Dispose();
}
