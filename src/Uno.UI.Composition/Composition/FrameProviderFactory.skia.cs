#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal static class FrameProviderFactory
{
	public static IFrameProvider Create(SKImage image)
		=> new SingleFrameProvider(image);

	public static bool TryCreate(SKManagedStream stream, Action onFrameChanged, [NotNullWhen(true)] out IFrameProvider? provider)
	{
		var originalPosition = stream.Position;
		using var codec = SKCodec.Create(stream);
		var imageInfo = codec.Info;
		var frameInfos = codec.FrameInfo;
		imageInfo = new SKImageInfo(imageInfo.Width, imageInfo.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var bitmap = new SKBitmap(imageInfo);

		if (codec.FrameInfo.Length < 2)
		{
			if (stream.Seek(originalPosition))
			{
				// Note: codec.GetPixels(imageInfo, bitmap.GetPixels()) doesn't seem to respect SKEncodedOrigin (aka Exif orientation)
				// For non-Gifs, we use FromEncodedData, which appears to respect Exif.
				// For Gifs, we currently don't properly support Exif orientation.
				// If Skia can't give this out-of-the-box, we'll need to apply a transform matrix manually based on codec.EncodedOrigin value.
				// See https://github.com/google/skia/blob/b20651c1aad43e3447830d6ce7a68ca507b398a4/include/codec/SkEncodedOrigin.h#L32-L42
				// We should make sure we test a scenario for an Image whose width and height are different, and is Exif rotated by 90 degree
				// i.e, cause the width and height to swap.
				provider = new SingleFrameProvider(SKImage.FromEncodedData(stream));
				return true;
			}

			// FrameInfo can be zero for single-frame images
			codec.GetPixels(imageInfo, bitmap.GetPixels());
			provider = new SingleFrameProvider(SKImage.FromBitmap(bitmap));
			return true;
		}

		var images = GC.AllocateUninitializedArray<SKImage>(frameInfos.Length);
		var totalDuration = 0;
		for (int i = 0; i < frameInfos.Length; i++)
		{
			var options = new SKCodecOptions(i);
			codec.GetPixels(imageInfo, bitmap.GetPixels(), options);
			var currentBitmap = SKImage.FromBitmap(bitmap);
			if (currentBitmap is null)
			{
				provider = null;
				return false;
			}

			images[i] = currentBitmap;
			totalDuration += frameInfos[i].Duration;
		}

		provider = new GifFrameProvider(images, frameInfos, totalDuration, onFrameChanged);
		return true;
	}
}
