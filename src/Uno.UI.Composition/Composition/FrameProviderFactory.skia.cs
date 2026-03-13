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

		// Use the smallest codec-supported decode size that covers the target.
		// JPEG codecs support native downscaling (1/2, 1/4, 1/8) which saves memory.
		// PNG and other codecs that don't support scaling return the native size.
		// After decoding at the supported size, we scale to the exact target.
		var decodeSize = GetSupportedDecodeDimensions(codec, origin, targetWidth, targetHeight);
		var imageInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		var frameInfos = codec.FrameInfo;
		using var bitmap = new SKBitmap(imageInfo);

		if (codec.FrameInfo.Length < 2)
		{
			// FrameInfo can be zero for single-frame images
			var result = codec.GetPixels(imageInfo, bitmap.GetPixels());
			if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
			{
				provider = null;
				return false;
			}
			var image = GetImage(bitmap, origin);
			image = ScaleToTargetIfNeeded(image, origin, codecWidth, codecHeight, targetWidth, targetHeight);
			provider = new SingleFrameProvider(image);
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
			var result = codec.GetPixels(imageInfo, bitmap.GetPixels(), options);
			if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
			{
				provider = null;
				return false;
			}

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
	/// Returns the smallest codec-supported decode dimensions that cover the target size.
	/// For codecs that support native downscaling (e.g. JPEG: 1/2, 1/4, 1/8), this
	/// returns a reduced size that saves memory. For codecs that only support native
	/// size (e.g. PNG), this returns the original dimensions.
	/// </summary>
	private static SKSizeI GetSupportedDecodeDimensions(
		SKCodec codec, SKEncodedOrigin origin,
		int? targetWidth, int? targetHeight)
	{
		// Normalize non-positive dimensions to null so downstream branches
		// only deal with genuinely positive values.
		if (targetWidth is <= 0) targetWidth = null;
		if (targetHeight is <= 0) targetHeight = null;

		var codecWidth = codec.Info.Width;
		var codecHeight = codec.Info.Height;

		if (targetWidth is null && targetHeight is null)
		{
			return new SKSizeI(codecWidth, codecHeight);
		}

		// Target dimensions are in display (post-rotation) space.
		// Convert to codec (pre-rotation) space for the scale calculation.
		bool swaps = SkEncodedOriginSwapsWidthHeight(origin);
		int displayWidth = swaps ? codecHeight : codecWidth;
		int displayHeight = swaps ? codecWidth : codecHeight;

		int targetCodecW, targetCodecH;

		if (targetWidth is > 0 && targetHeight is > 0)
		{
			targetCodecW = swaps ? targetHeight.Value : targetWidth.Value;
			targetCodecH = swaps ? targetWidth.Value : targetHeight.Value;
		}
		else if (targetWidth is > 0)
		{
			int targetDisplayW = targetWidth.Value;
			int targetDisplayH = (int)Math.Max(1, (long)displayHeight * targetDisplayW / displayWidth);
			targetCodecW = swaps ? targetDisplayH : targetDisplayW;
			targetCodecH = swaps ? targetDisplayW : targetDisplayH;
		}
		else
		{
			int targetDisplayH = targetHeight!.Value;
			int targetDisplayW = (int)Math.Max(1, (long)displayWidth * targetDisplayH / displayHeight);
			targetCodecW = swaps ? targetDisplayH : targetDisplayW;
			targetCodecH = swaps ? targetDisplayW : targetDisplayH;
		}

		// Use the larger scale factor so the decoded image covers the target in both dimensions.
		float desiredScale = Math.Max(
			(float)targetCodecW / codecWidth,
			(float)targetCodecH / codecHeight);

		return codec.GetScaledDimensions(desiredScale);
	}

	/// <summary>
	/// Scales an image (already in display/post-EXIF-rotation space) to the exact
	/// target dimensions. When only one dimension is specified, the other is computed
	/// to preserve the source aspect ratio.
	/// </summary>
	private static SKImage ScaleToTargetIfNeeded(
		SKImage image, SKEncodedOrigin origin,
		int codecWidth, int codecHeight,
		int? targetWidth, int? targetHeight)
	{
		if (targetWidth is <= 0) targetWidth = null;
		if (targetHeight is <= 0) targetHeight = null;

		if (targetWidth is null && targetHeight is null)
		{
			return image;
		}

		bool swaps = SkEncodedOriginSwapsWidthHeight(origin);
		int originalDisplayW = swaps ? codecHeight : codecWidth;
		int originalDisplayH = swaps ? codecWidth : codecHeight;

		int dstW, dstH;
		if (targetWidth is > 0 && targetHeight is > 0)
		{
			dstW = targetWidth.Value;
			dstH = targetHeight.Value;
		}
		else if (targetWidth is > 0)
		{
			dstW = targetWidth.Value;
			dstH = (int)Math.Max(1, (long)originalDisplayH * dstW / originalDisplayW);
		}
		else
		{
			dstH = targetHeight!.Value;
			dstW = (int)Math.Max(1, (long)originalDisplayW * dstH / originalDisplayH);
		}

		if (dstW == image.Width && dstH == image.Height)
		{
			return image;
		}

		var dstInfo = new SKImageInfo(dstW, dstH, SKColorType.Bgra8888, SKAlphaType.Premul);
		var dstBitmap = new SKBitmap(dstInfo);
		using var srcBitmap = SKBitmap.FromImage(image);
		srcBitmap.ScalePixels(dstBitmap, new SKSamplingOptions(SKCubicResampler.CatmullRom));
		return SKImage.FromBitmap(dstBitmap);
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
			// Reflected across x - axis.Rotated 90° counter - clockwise.
			SKEncodedOrigin.LeftTop or

			// Rotated 90° clockwise.
			SKEncodedOrigin.RightTop or

			// Reflected across x-axis. Rotated 90° clockwise.
			SKEncodedOrigin.RightBottom or

			// Rotated 90° counter-clockwise.
			SKEncodedOrigin.LeftBottom;
	}

}
