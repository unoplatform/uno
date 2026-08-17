#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;
using Windows.Graphics.Imaging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The SkiaSharp <see cref="IImageEncoderDecoder"/>: the SKCodec decode pipeline + SKBitmap.Encode. An app that
/// wants SkiaSharp-free imaging registers <see cref="ManagedImageDecoderBackend"/> as
/// <see cref="ImageEncoderDecoder.Current"/> instead.
/// </summary>
internal sealed class SkiaImageDecoderBackend : IImageEncoderDecoder
{
	public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames)
		=> SkiaImageDecoder.TryDecode(stream, targetWidth, targetHeight, out frames);

	public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		return new SkiaImage(SKImage.FromPixelCopy(info, bgraPremul));
	}

	public ImageFrames CreateFrames(IImage image) => new(new[] { image }, new[] { 0 });

	public byte[] Encode(byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
	{
		var info = new SKImageInfo(width, height, ToSKColorType(pixelFormat), ToSKAlphaType(alphaMode));

		using var bitmap = new SKBitmap();
		var gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		if (!bitmap.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, (_, ctx) => ((GCHandle)ctx!).Free(), gcHandle))
		{
			gcHandle.Free();
			throw new InvalidOperationException("Failed to install pixels for encoding.");
		}

		using var data = bitmap.Encode(ToSKEncodedImageFormat(format), quality)
			?? throw new NotSupportedException($"Encoding to {format} is not supported by the Skia backend.");
		return data.ToArray();
	}

	private static SKColorType ToSKColorType(BitmapPixelFormat format) =>
		format switch
		{
			BitmapPixelFormat.Rgba16 => SKColorType.Rgba16161616,
			BitmapPixelFormat.Rgba8 => SKColorType.Rgba8888,
			BitmapPixelFormat.Gray8 => SKColorType.Gray8,
			BitmapPixelFormat.Bgra8 => SKColorType.Bgra8888,
			_ => throw new NotSupportedException(nameof(format))
		};

	private static SKAlphaType ToSKAlphaType(BitmapAlphaMode alpha) =>
		alpha switch
		{
			BitmapAlphaMode.Ignore => SKAlphaType.Opaque,
			BitmapAlphaMode.Straight => SKAlphaType.Unpremul,
			BitmapAlphaMode.Premultiplied => SKAlphaType.Premul,
			_ => throw new NotSupportedException(nameof(alpha))
		};

	private static SKEncodedImageFormat ToSKEncodedImageFormat(BitmapEncoderFormat format) =>
		format switch
		{
			BitmapEncoderFormat.Bmp => SKEncodedImageFormat.Bmp,
			BitmapEncoderFormat.Gif => SKEncodedImageFormat.Gif,
			BitmapEncoderFormat.Jpeg => SKEncodedImageFormat.Jpeg,
			BitmapEncoderFormat.Png => SKEncodedImageFormat.Png,
			BitmapEncoderFormat.Heif => SKEncodedImageFormat.Heif,
			_ => throw new NotSupportedException(nameof(format))
		};
}
