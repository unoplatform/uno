#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Graphics.Imaging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free <see cref="IImageEncoderDecoder"/>: decodes via <see cref="ManagedImageDecoder"/> and wraps the result
/// as managed, byte[]-backed <see cref="IImage"/>/<see cref="ImageFrames"/> — no Skia object is ever created.
/// Register as <see cref="ImageEncoderDecoder.Current"/> so an image-bearing app can run with no native libSkiaSharp.
/// Formats the managed decoder can't handle return false (there is no Skia fallback here).
/// </summary>
public sealed class ManagedImageDecoderBackend : IImageEncoderDecoder
{
	public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames)
	{
		var bytes = ReadAllBytes(stream);
		if (ManagedImageDecoder.TryDecode(bytes, targetWidth, targetHeight, out var decoded))
		{
			var images = new IImage[decoded.Frames.Length];
			for (var i = 0; i < images.Length; i++)
			{
				images[i] = new ManagedImage(decoded.Width, decoded.Height, decoded.Frames[i]);
			}

			frames = new ImageFrames(images, decoded.DurationsMs);
			return true;
		}

		frames = null;
		return false;
	}

	public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
		=> new ManagedImage(pixelWidth, pixelHeight, bgraPremul.ToArray());

	public ImageFrames CreateFrames(IImage image)
		=> new(new[] { image }, new[] { 0 });

	public byte[] Encode(byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
		=> ManagedImageEncoder.Encode(pixels, width, height, pixelFormat, alphaMode, format, quality);

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
}

/// <summary>A managed, byte[]-backed <see cref="IImage"/>. Pixels are BGRA8888 premultiplied, tightly packed.</summary>
internal sealed class ManagedImage : IImage
{
	private readonly byte[] _bgraPremul;

	public ManagedImage(int pixelWidth, int pixelHeight, byte[] bgraPremul)
	{
		PixelWidth = pixelWidth;
		PixelHeight = pixelHeight;
		_bgraPremul = bgraPremul;
	}

	public int PixelWidth { get; }

	public int PixelHeight { get; }

	public void CopyPixels(Span<byte> destination)
		=> _bgraPremul.AsSpan(0, Math.Min(_bgraPremul.Length, destination.Length)).CopyTo(destination);
}
