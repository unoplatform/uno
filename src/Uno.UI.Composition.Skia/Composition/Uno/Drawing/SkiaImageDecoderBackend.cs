#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The SkiaSharp <see cref="IImageDecoder"/>: the SKCodec pipeline. An app that wants SkiaSharp-free decoding
/// registers <see cref="ManagedImageDecoderBackend"/> as <see cref="ImageDecoder.Current"/> instead.
/// </summary>
internal sealed class SkiaImageDecoderBackend : IImageDecoder
{
	public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames)
		=> SkiaImageDecoder.TryDecode(stream, targetWidth, targetHeight, out frames);

	public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		return new SkiaImage(SKImage.FromPixelCopy(info, bgraPremul));
	}

	public ImageFrames CreateFrames(IImage image) => new(new[] { image }, new[] { 0 });
}
