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
			var scaled = ScaleToTargetIfNeeded(image, origin, codecWidth, codecHeight, targetWidth, targetHeight);
			if (scaled != image)
			{
				image.Dispose();
			}
			provider = new SingleFrameProvider(scaled);
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
				DisposeDecodedFrames(images, i);
				provider = null;
				return false;
			}

			var currentBitmap = GetImage(bitmap, origin);
			if (currentBitmap is null)
			{
				DisposeDecodedFrames(images, i);
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

		// Scale frames in a second pass. The first pass needs unscaled images
		// because they serve as reference frames during decoding.
		for (int i = 0; i < images.Length; i++)
		{
			var scaled = ScaleToTargetIfNeeded(images[i], origin, codecWidth, codecHeight, targetWidth, targetHeight);
			if (scaled != images[i])
			{
				images[i].Dispose();
				images[i] = scaled;
			}
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

		bool swaps = SkEncodedOriginSwapsWidthHeight(origin);
		var (targetCodecW, targetCodecH, _, _) = ComputeTargetDimensions(
			codecWidth, codecHeight, swaps, targetWidth, targetHeight);

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
		var (_, _, dstW, dstH) = ComputeTargetDimensions(
			codecWidth, codecHeight, swaps, targetWidth, targetHeight);

		if (dstW == image.Width && dstH == image.Height)
		{
			return image;
		}

		var dstInfo = new SKImageInfo(dstW, dstH, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var dstBitmap = new SKBitmap(dstInfo);
		using var srcBitmap = SKBitmap.FromImage(image);
		if (!srcBitmap.ScalePixels(dstBitmap, new SKSamplingOptions(SKCubicResampler.CatmullRom)))
		{
			return image;
		}
		return SKImage.FromBitmap(dstBitmap);
	}

	private static void DisposeDecodedFrames(SKImage[] images, int count)
	{
		for (int j = 0; j < count; j++)
		{
			images[j]?.Dispose();
		}
	}

	/// <summary>
	/// Computes target dimensions in both codec (pre-rotation) and display (post-rotation) space.
	/// <paramref name="targetWidth"/> and <paramref name="targetHeight"/> are in display space.
	/// When only one dimension is specified, the other is computed to preserve the aspect ratio
	/// of the original image. The returned codec dimensions are the display dimensions with the
	/// width/height swap reversed, suitable for passing to the decoder.
	/// </summary>
	private static (int codecW, int codecH, int displayW, int displayH) ComputeTargetDimensions(
		int codecWidth, int codecHeight, bool swaps,
		int? targetWidth, int? targetHeight)
	{
		// Convert codec dimensions to display space (post-EXIF-rotation).
		// For 90°/270° rotations, width and height are swapped.
		int displayWidth = swaps ? codecHeight : codecWidth;
		int displayHeight = swaps ? codecWidth : codecHeight;

		// Compute the target size in display space. Aspect-ratio
		// calculations use display-space dimensions throughout.
		int displayW, displayH;
		if (targetWidth is > 0 && targetHeight is > 0)
		{
			displayW = targetWidth.Value;
			displayH = targetHeight.Value;
		}
		else if (targetWidth is > 0)
		{
			displayW = targetWidth.Value;
			displayH = (int)Math.Max(1, (long)displayHeight * displayW / displayWidth);
		}
		else
		{
			displayH = targetHeight!.Value;
			displayW = (int)Math.Max(1, (long)displayWidth * displayH / displayHeight);
		}

		// Convert display-space target back to codec space (pre-rotation)
		// by reversing the swap, so the decoder sees the right dimensions.
		int codecW = swaps ? displayH : displayW;
		int codecH = swaps ? displayW : displayH;

		return (codecW, codecH, displayW, displayH);
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
