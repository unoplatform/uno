#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Graphics.Imaging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The CPU-only, render-backend-independent image codec seam: it decodes encoded streams into neutral pixels and
/// encodes neutral pixels back to a compressed image, supplied independently of the graphics backend (managed,
/// Skia, or platform codec). It produces the neutral <see cref="IImage"/>/<see cref="ImageFrames"/> currency any
/// render backend consumes, and the byte stream <c>BitmapEncoder</c> writes.
/// </summary>
public interface IImageEncoderDecoder
{
	/// <summary>
	/// Decodes an encoded stream (PNG/JPEG/GIF/…) into one or more frames, applying EXIF orientation and, when a
	/// target size is given, scaling. Returns false when the bytes can't be decoded. Dispose the frames to release.
	/// </summary>
	bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames);

	/// <summary>Wraps raw BGRA (premultiplied) pixels as a single neutral <see cref="IImage"/> (copied, so the source buffer stays reusable).</summary>
	IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul);

	/// <summary>Wraps an already-decoded <see cref="IImage"/> as a single-frame <see cref="ImageFrames"/> for the
	/// animation/frame-provider path, taking ownership of the image (disposing the frames releases it).</summary>
	ImageFrames CreateFrames(IImage image);

	/// <summary>
	/// Encodes a raw pixel buffer (<paramref name="pixelFormat"/>/<paramref name="alphaMode"/>) to the compressed
	/// <paramref name="format"/>, writing the file bytes into <paramref name="destination"/> — the inverse of
	/// <see cref="TryDecode"/> and the engine behind <c>Windows.Graphics.Imaging.BitmapEncoder</c>. Throws for a
	/// format the codec can't produce.
	/// </summary>
	void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality);
}

/// <summary>
/// Process-wide image codec, set at the composition root independently of the graphics backend. Unset access
/// throws (no hidden default) — a platform head registers its codec at startup. Registering it also wires the
/// codec's <see cref="IImageEncoderDecoder.Encode"/> down into <see cref="BitmapEncoder"/>.
/// </summary>
public static class ImageEncoderDecoder
{
	private static IImageEncoderDecoder? _current;

	public static IImageEncoderDecoder Current
	{
		get => _current ?? throw new InvalidOperationException(
			"No IImageEncoderDecoder registered. Register an image codec via the host builder (.ImageEncoderDecoder), or reference the Skia backend for the built-in default.");
		internal set => SetCurrent(value ?? throw new ArgumentNullException(nameof(value)));
	}

	/// <summary>Whether an image codec has been registered (used by the host builder's fail-fast seam check).</summary>
	internal static bool IsRegistered => _current is not null;

	/// <summary>
	/// Registers <paramref name="codec"/> only if none is registered yet. Framework-internal (per-seam fallback);
	/// app-side registration goes through the host builder's .ImageEncoderDecoder extension.
	/// </summary>
	internal static void RegisterDefault(IImageEncoderDecoder codec)
	{
		if (_current is null)
		{
			SetCurrent(codec ?? throw new ArgumentNullException(nameof(codec)));
		}
	}

	private static void SetCurrent(IImageEncoderDecoder codec)
	{
		_current = codec;
		// Hand the encode capability down to Uno.UWP's BitmapEncoder (below this assembly) as a plain delegate —
		// a top-down assignment, no reflection. BitmapEncoder.Encode is reachable via InternalsVisibleTo.
		BitmapEncoder.Encode = codec.Encode;
	}
}
