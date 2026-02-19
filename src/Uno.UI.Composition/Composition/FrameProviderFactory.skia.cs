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
		using var codec = SKCodec.Create(stream);

		if (codec is null)
		{
			provider = null;
			return false;
		}

		var imageInfo = codec.Info;
		var frameInfos = codec.FrameInfo;
		imageInfo = new SKImageInfo(imageInfo.Width, imageInfo.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var bitmap = new SKBitmap(imageInfo);

		if (codec.FrameInfo.Length < 2)
		{
			// FrameInfo can be zero for single-frame images
			codec.GetPixels(imageInfo, bitmap.GetPixels());
			provider = new SingleFrameProvider(GetImage(bitmap, codec.EncodedOrigin));
			return true;
		}

		var images = GC.AllocateUninitializedArray<SKImage>(frameInfos.Length);
		var durations = new int[frameInfos.Length];
		var totalDuration = 0;

		for (int i = 0; i < frameInfos.Length; i++)
		{
			var requiredFrame = frameInfos[i].RequiredFrame;

			if (requiredFrame == -1)
			{
				// Independent frame - clear the bitmap before decoding.
				bitmap.Erase(SKColor.Empty);
			}
			else if (requiredFrame != i - 1)
			{
				// The required frame is not the immediately preceding one, so we
				// need to restore its pixels into the bitmap before decoding.
				using var restoreCanvas = new SKCanvas(bitmap);
				restoreCanvas.Clear(SKColor.Empty);
				restoreCanvas.DrawImage(images[requiredFrame], 0, 0);
			}
			// When requiredFrame == i - 1, the bitmap already contains the
			// correct prior frame pixels from the previous iteration.

			var options = new SKCodecOptions(i, requiredFrame);
			codec.GetPixels(imageInfo, bitmap.GetPixels(), options);

			var currentBitmap = GetImage(bitmap, codec.EncodedOrigin);
			if (currentBitmap is null)
			{
				provider = null;
				return false;
			}

			images[i] = currentBitmap;

			// Clamp zero-duration frames to 100ms to prevent division-by-zero
			// and match common animated image behavior.
			var duration = frameInfos[i].Duration;
			durations[i] = duration > 0 ? duration : 100;
			totalDuration += durations[i];
		}

		provider = new AnimatedImageFrameProvider(images, durations, totalDuration, onFrameChanged);
		return true;
	}

	private static SKImage GetImage(SKBitmap bitmap, SKEncodedOrigin origin)
	{
		var info = bitmap.Info;
		if (SkEncodedOriginSwapsWidthHeight(origin))
		{
			info = new SKImageInfo(info.Height, info.Width, SKColorType.Bgra8888, SKAlphaType.Premul);
		}

		var matrix = GetExifMatrix(origin, info.Width, info.Height);
		if (matrix.IsIdentity)
		{
			return SKImage.FromBitmap(bitmap);
		}

		var newBitmap = new SKBitmap(info);
		using var canvas = new SKCanvas(newBitmap);
		canvas.SetMatrix(matrix);
		canvas.DrawBitmap(bitmap, 0, 0);
		return SKImage.FromBitmap(newBitmap);
	}

	// https://github.com/google/skia/blob/b20651c1aad43e3447830d6ce7a68ca507b398a4/include/codec/SkEncodedOrigin.h#L32-L42
	private static SKMatrix GetExifMatrix(SKEncodedOrigin origin, int width, int height)
	{
		return origin switch
		{
			SKEncodedOrigin.TopLeft => SKMatrix.Identity,
			SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
			SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
			SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
			SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
			SKEncodedOrigin.RightTop => new SKMatrix(0, -1, width, 1, 0, 0, 0, 0, 1),
			SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
			SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, height, 0, 0, 1),
			_ => throw new ArgumentException($"Unexpected SKEncodedOrigin value '{origin}'.", nameof(origin)),
		};
	}

	private static bool SkEncodedOriginSwapsWidthHeight(SKEncodedOrigin origin)
	{
		return origin is
			// Reflected across x - axis.Rotated 90� counter - clockwise.
			SKEncodedOrigin.LeftTop or

			// Rotated 90� clockwise.
			SKEncodedOrigin.RightTop or

			// Reflected across x-axis. Rotated 90� clockwise.
			SKEncodedOrigin.RightBottom or

			// Rotated 90� counter-clockwise.
			SKEncodedOrigin.LeftBottom;
	}

}
