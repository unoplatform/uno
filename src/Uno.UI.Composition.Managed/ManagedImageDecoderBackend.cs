#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free <see cref="IImageEncoderDecoder"/>: decodes via <see cref="ManagedImageDecoder"/> and wraps the result
/// as managed, byte[]-backed <see cref="IImage"/>/<see cref="ImageFrames"/> — no Skia object is ever created.
/// Register as <see cref="ImageEncoderDecoder.Current"/> so an image-bearing app can run with no native libSkiaSharp.
/// Formats the managed decoder can't handle return false (there is no Skia fallback here).
/// </summary>
internal sealed class ManagedImageDecoderBackend : IImageEncoderDecoder
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
		=> new ManagedImage(pixelWidth, pixelHeight, bgraPremul);

	public IImage CreateImage<TState>(int pixelWidth, int pixelHeight, TState state, SpanAction<byte, TState> writePixels)
	{
		var image = new ManagedImage(pixelWidth, pixelHeight);
		try
		{
			writePixels(image.Pixels, state);
			return image;
		}
		catch
		{
			image.Dispose();
			throw;
		}
	}

	public ImageFrames CreateFrames(IImage image)
		=> new(new[] { image }, DecodedImage.SingleFrameDurations);

	public void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
		=> ManagedImageEncoder.Encode(destination, pixels, width, height, pixelFormat, alphaMode, format, quality);

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
internal sealed unsafe class ManagedImage : DrawingResource, IImage
{
	private IntPtr _bgraPremul;
	private readonly int _length;

	// The decoder hands over a managed array it allocated per frame; copy it into a buffer this image owns so the
	// array can be collected straight away instead of being pinned on the large-object heap for the image's whole
	// lifetime. Retained big arrays are the ones that fragment.
	public ManagedImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
		: this(pixelWidth, pixelHeight, bgraPremul.Length)
		=> bgraPremul.CopyTo(Pixels);

	/// <summary>An image whose pixels are uninitialized until written through <see cref="Pixels"/>.</summary>
	public ManagedImage(int pixelWidth, int pixelHeight)
		: this(pixelWidth, pixelHeight, checked(pixelWidth * pixelHeight * 4))
	{
	}

	private ManagedImage(int pixelWidth, int pixelHeight, int length)
	{
		PixelWidth = pixelWidth;
		PixelHeight = pixelHeight;
		_length = length;
		_bgraPremul = (IntPtr)NativeMemory.Alloc((nuint)Math.Max(1, _length));
	}

	internal Span<byte> Pixels => new((void*)_bgraPremul, _length);

	public int PixelWidth { get; }

	public int PixelHeight { get; }

	public void CopyPixels(Span<byte> destination)
		=> new ReadOnlySpan<byte>((void*)_bgraPremul, _length)
			.Slice(0, Math.Min(_length, destination.Length))
			.CopyTo(destination);

	protected override void Free()
	{
		var buffer = System.Threading.Interlocked.Exchange(ref _bgraPremul, IntPtr.Zero);
		if (buffer != IntPtr.Zero) { NativeMemory.Free((void*)buffer); }
	}

	// Unmanaged, so a missed Release would lose the buffer rather than leave it to the GC.
	~ManagedImage() => Free();
}
