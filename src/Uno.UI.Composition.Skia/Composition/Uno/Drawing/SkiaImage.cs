#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IImage"/> wrapping an <see cref="SKImage"/>.</summary>
internal sealed class SkiaImage : IImage
{
	public SkiaImage(SKImage image) => Image = image;

	public SKImage Image { get; }

	public int PixelWidth => Image.Width;

	public int PixelHeight => Image.Height;

	public unsafe void CopyPixels(Span<byte> destination)
	{
		var info = new SKImageInfo(PixelWidth, PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		fixed (byte* dst = destination)
		{
			Image.ReadPixels(info, (nint)dst, info.RowBytes, 0, 0);
		}
	}
}
