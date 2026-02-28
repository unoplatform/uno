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
		=> TryCreate(stream, onFrameChanged, null, null, out provider);

	public static bool TryCreate(SKManagedStream stream, Action onFrameChanged, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out IFrameProvider? provider)
	{
		using var codec = SKCodec.Create(stream);

		if (codec is null)
		{
			provider = null;
			return false;
		}

		var origin = codec.EncodedOrigin;
		var codecWidth = codec.Info.Width;
		var codecHeight = codec.Info.Height;
		var (decodeWidth, decodeHeight) = ComputeDecodeDimensions(codecWidth, codecHeight, origin, targetWidth, targetHeight);

		var imageInfo = new SKImageInfo(decodeWidth, decodeHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		var frameInfos = codec.FrameInfo;
		using var bitmap = new SKBitmap(imageInfo);

		if (codec.FrameInfo.Length < 2)
		{
			// FrameInfo can be zero for single-frame images
			codec.GetPixels(imageInfo, bitmap.GetPixels());
			provider = new SingleFrameProvider(GetImage(bitmap, origin));
			return true;
		}

		var images = GC.AllocateUninitializedArray<SKImage>(frameInfos.Length);
		var durations = new int[frameInfos.Length];
		long totalDuration = 0;

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

			var currentBitmap = GetImage(bitmap, origin);
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

	/// <summary>
	/// Computes the decode dimensions for an image, accounting for EXIF orientation.
	/// The target dimensions are specified in post-rotation (display) space,
	/// while the codec dimensions are in pre-rotation space.
	/// </summary>
	internal static (int Width, int Height) ComputeDecodeDimensions(
		int codecWidth, int codecHeight,
		SKEncodedOrigin origin,
		int? requestedWidth, int? requestedHeight)
	{
		if (requestedWidth is null && requestedHeight is null)
		{
			return (codecWidth, codecHeight);
		}

		// The user specifies dimensions in post-rotation (display) space.
		// The codec works in pre-rotation space. If EXIF swaps W/H, we need
		// to swap the user's requested dimensions to match the codec's space.
		bool swaps = SkEncodedOriginSwapsWidthHeight(origin);
		int displayWidth = swaps ? codecHeight : codecWidth;
		int displayHeight = swaps ? codecWidth : codecHeight;

		int targetDisplayW, targetDisplayH;

		if (requestedWidth is > 0 && requestedHeight is > 0)
		{
			targetDisplayW = requestedWidth.Value;
			targetDisplayH = requestedHeight.Value;
		}
		else if (requestedWidth is > 0)
		{
			targetDisplayW = requestedWidth.Value;
			targetDisplayH = (int)Math.Max(1, (long)displayHeight * targetDisplayW / displayWidth);
		}
		else // requestedHeight > 0
		{
			targetDisplayH = requestedHeight!.Value;
			targetDisplayW = (int)Math.Max(1, (long)displayWidth * targetDisplayH / displayHeight);
		}

		// Convert back to codec (pre-rotation) space
		int decodeW = swaps ? targetDisplayH : targetDisplayW;
		int decodeH = swaps ? targetDisplayW : targetDisplayH;

		return (Math.Max(1, decodeW), Math.Max(1, decodeH));
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
