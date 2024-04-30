#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal static class FrameProviderFactory
{
	public static IFrameProvider Create(SKImage image)
		=> new SingleFrameProvider(image);

	public static bool TryCreate(SKCodec codec, Action onFrameChanged, [NotNullWhen(true)] out IFrameProvider? provider)
	{
		var imageInfo = codec.Info;
		var frameInfos = codec.FrameInfo;
		imageInfo = new SKImageInfo(imageInfo.Width, imageInfo.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		var bitmap = new SKBitmap(imageInfo);

		if (codec.FrameInfo.Length < 2)
		{
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
