#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Decodes encoded image streams into neutral pixels. This is a CPU-only, render-backend-independent seam:
/// a decoder can be supplied independently of the graphics/render backend (managed decoder, Skia codec, or a
/// platform codec), and it produces the neutral <see cref="IImage"/>/<see cref="IImageFrames"/> currency any
/// render backend consumes (via <see cref="IDrawingFactory.CreateImageTexture"/>).
/// </summary>
public interface IImageDecoder
{
	/// <summary>
	/// Decodes an encoded stream (PNG/JPEG/GIF/…) into one or more frames, applying EXIF orientation and, when a
	/// target size is given, scaling. Returns false when the bytes can't be decoded. Dispose the frames to release.
	/// </summary>
	bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out IImageFrames? frames);

	/// <summary>Wraps raw BGRA (premultiplied) pixels as a single-frame <see cref="IImageFrames"/>.</summary>
	IImageFrames CreateFrame(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul);

	/// <summary>Wraps an already-decoded <see cref="IImage"/> as a single-frame <see cref="IImageFrames"/>, taking
	/// ownership of the image (disposing the frames releases it).</summary>
	IImageFrames CreateFrames(IImage image);
}

/// <summary>
/// Process-wide image decoder, set at the composition root independently of the graphics backend. Unset access
/// throws (there is no hidden default) — a platform head registers its decoder at startup.
/// </summary>
public static class ImageDecoder
{
	private static IImageDecoder? _current;

	public static IImageDecoder Current
	{
		get => _current ?? throw new InvalidOperationException(
			"No IImageDecoder registered. Set ImageDecoder.Current during app initialization (the Skia head does this in SkiaBackend.Register).");
		set => _current = value;
	}
}
