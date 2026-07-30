#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The SkiaSharp <see cref="IImageDecoder"/>: SKCodec, with the optional SkiaSharp-free managed parse tried in
/// front (the managed parse still wraps into a Skia-backed <see cref="IImage"/> until a managed image exists).
/// </summary>
internal sealed class SkiaImageDecoderBackend : IImageDecoder
{
	public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out IImageFrames? frames)
	{
		if (ManagedImageDecoder.Enabled)
		{
			// Try the SkiaSharp-free decoder first; buffer the bytes so we can still fall back to the codec.
			var bytes = ReadAllBytes(stream);
			if (ManagedImageDecoder.TryDecode(bytes, targetWidth, targetHeight, out var decoded))
			{
				frames = ToImageFrames(decoded);
				return true;
			}

			stream = new MemoryStream(bytes, writable: false);
		}

		if (SkiaImageDecoder.TryDecode(stream, targetWidth, targetHeight, out var skiaFrames))
		{
			frames = skiaFrames;
			return true;
		}

		frames = null;
		return false;
	}

	public IImageFrames CreateFrame(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		return SkiaImageFrames.FromImage(SKImage.FromPixelCopy(info, bgraPremul));
	}

	public IImageFrames CreateFrames(IImage image) => SkiaImageFrames.FromImage(((SkiaImage)image).Image);

	private static byte[] ReadAllBytes(Stream stream)
	{
		if (stream is MemoryStream ms)
		{
			return ms.ToArray();
		}

		using var buffer = new MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
	}

	private static SkiaImageFrames ToImageFrames(DecodedImage decoded)
	{
		var info = new SKImageInfo(decoded.Width, decoded.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		var images = new SKImage[decoded.Frames.Length];
		for (var i = 0; i < images.Length; i++)
		{
			images[i] = SKImage.FromPixelCopy(info, decoded.Frames[i]);
		}

		return new SkiaImageFrames(images, decoded.DurationsMs);
	}
}
