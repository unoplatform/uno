#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp image decode used by <see cref="SkiaDrawingBackend"/>: turns encoded bytes into one or more BGRA frames,
/// applying EXIF orientation, codec-native downscaling, animated-frame composition, and exact scaling to a target size.
/// This is the backend-internal "technique"; callers see only the neutral <see cref="IImageFrames"/>.
/// </summary>
internal static class SkiaImageDecoder
{
	public static bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out SkiaImageFrames? frames)
	{
		using var managedStream = new SKManagedStream(stream);
		using var codec = SKCodec.Create(managedStream);
		if (codec is null)
		{
			frames = null;
			return false;
		}

		var origin = codec.EncodedOrigin;
		var codecWidth = codec.Info.Width;
		var codecHeight = codec.Info.Height;

		// Use the smallest codec-supported decode size that covers the target (JPEG supports 1/2, 1/4, 1/8 native
		// downscaling; PNG and others return native size), then scale to the exact target afterwards.
		var decodeSize = GetSupportedDecodeDimensions(codec, origin, targetWidth, targetHeight);
		var imageInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		var frameInfos = codec.FrameInfo;
		using var bitmap = new SKBitmap(imageInfo);

		if (frameInfos.Length < 2)
		{
			// FrameInfo can be zero for single-frame images.
			var result = codec.GetPixels(imageInfo, bitmap.GetPixels());
			if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
			{
				frames = null;
				return false;
			}

			var image = GetImage(bitmap, origin);
			var scaled = ScaleToTargetIfNeeded(image, origin, codecWidth, codecHeight, targetWidth, targetHeight);
			if (scaled != image)
			{
				image.Dispose();
			}

			frames = new SkiaImageFrames(new[] { scaled }, new[] { 0 });
			return true;
		}

		var images = GC.AllocateUninitializedArray<SKImage>(frameInfos.Length);
		var durations = new int[frameInfos.Length];

		for (var i = 0; i < frameInfos.Length; i++)
		{
			var requiredFrame = frameInfos[i].RequiredFrame;
			if (requiredFrame == -1)
			{
				bitmap.Erase(SKColor.Empty);
			}
			else if (requiredFrame != i - 1)
			{
				// The required frame isn't the immediately preceding one — restore its pixels before decoding.
				using var restoreCanvas = new SKCanvas(bitmap);
				restoreCanvas.Clear(SKColor.Empty);
				restoreCanvas.DrawImage(images[requiredFrame], 0, 0, SKSamplingOptions.Default, null);
			}

			var options = new SKCodecOptions(i, requiredFrame);
			var result = codec.GetPixels(imageInfo, bitmap.GetPixels(), options);
			if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
			{
				DisposeDecodedFrames(images, i);
				frames = null;
				return false;
			}

			var currentImage = GetImage(bitmap, origin);
			if (currentImage is null)
			{
				DisposeDecodedFrames(images, i);
				frames = null;
				return false;
			}

			images[i] = currentImage;

			// Clamp zero-duration frames to 100ms to avoid division-by-zero and match common animated behavior.
			var duration = frameInfos[i].Duration;
			durations[i] = duration > 0 ? duration : 100;
		}

		// Scale in a second pass: the first pass needs unscaled images as reference frames during decoding.
		for (var i = 0; i < images.Length; i++)
		{
			var scaled = ScaleToTargetIfNeeded(images[i], origin, codecWidth, codecHeight, targetWidth, targetHeight);
			if (scaled != images[i])
			{
				images[i].Dispose();
				images[i] = scaled;
			}
		}

		frames = new SkiaImageFrames(images, durations);
		return true;
	}

	private static SKSizeI GetSupportedDecodeDimensions(SKCodec codec, SKEncodedOrigin origin, int? targetWidth, int? targetHeight)
	{
		if (targetWidth is <= 0) targetWidth = null;
		if (targetHeight is <= 0) targetHeight = null;

		var codecWidth = codec.Info.Width;
		var codecHeight = codec.Info.Height;
		if (targetWidth is null && targetHeight is null)
		{
			return new SKSizeI(codecWidth, codecHeight);
		}

		var swaps = SwapsWidthHeight(origin);
		var (targetCodecW, targetCodecH, _, _) = ComputeTargetDimensions(codecWidth, codecHeight, swaps, targetWidth, targetHeight);
		var desiredScale = Math.Max((float)targetCodecW / codecWidth, (float)targetCodecH / codecHeight);
		return codec.GetScaledDimensions(desiredScale);
	}

	private static SKImage ScaleToTargetIfNeeded(SKImage image, SKEncodedOrigin origin, int codecWidth, int codecHeight, int? targetWidth, int? targetHeight)
	{
		if (targetWidth is <= 0) targetWidth = null;
		if (targetHeight is <= 0) targetHeight = null;

		if (targetWidth is null && targetHeight is null)
		{
			return image;
		}

		var swaps = SwapsWidthHeight(origin);
		var (_, _, dstW, dstH) = ComputeTargetDimensions(codecWidth, codecHeight, swaps, targetWidth, targetHeight);
		if (dstW == image.Width && dstH == image.Height)
		{
			return image;
		}

		var dstInfo = new SKImageInfo(dstW, dstH, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var dstBitmap = new SKBitmap(dstInfo);
		using var srcBitmap = SKBitmap.FromImage(image);
		return srcBitmap.ScalePixels(dstBitmap, new SKSamplingOptions(SKCubicResampler.CatmullRom))
			? SKImage.FromBitmap(dstBitmap)
			: image;
	}

	private static void DisposeDecodedFrames(SKImage[] images, int count)
	{
		for (var j = 0; j < count; j++)
		{
			images[j]?.Dispose();
		}
	}

	private static (int codecW, int codecH, int displayW, int displayH) ComputeTargetDimensions(int codecWidth, int codecHeight, bool swaps, int? targetWidth, int? targetHeight)
	{
		var displayWidth = swaps ? codecHeight : codecWidth;
		var displayHeight = swaps ? codecWidth : codecHeight;

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

		var codecW = swaps ? displayH : displayW;
		var codecH = swaps ? displayW : displayH;
		return (codecW, codecH, displayW, displayH);
	}

	private static SKImage GetImage(SKBitmap bitmap, SKEncodedOrigin origin)
	{
		var info = bitmap.Info;
		if (SwapsWidthHeight(origin))
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
		canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default, null);
		return SKImage.FromBitmap(newBitmap);
	}

	// https://github.com/google/skia/blob/b20651c1aad43e3447830d6ce7a68ca507b398a4/include/codec/SkEncodedOrigin.h#L32-L42
	private static SKMatrix GetExifMatrix(SKEncodedOrigin origin, int width, int height) => origin switch
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

	private static bool SwapsWidthHeight(SKEncodedOrigin origin) => origin is
		SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
}
