#nullable enable

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Imaging;

namespace Uno.UI.Tests.CompositionTests;

// Round-trips the SkiaSharp-free ManagedImageEncoder through the ManagedImageDecoder (used here as the oracle):
// lossless formats (BMP/PNG) must reproduce pixels exactly; palette formats (GIF) must be exact after ≤256-colour
// quantization and must round-trip transparency; JPEG (lossy) must stay within a tight per-channel average.
[TestClass]
public class ManagedImageCodecRoundTripTests
{
	[TestMethod]
	public void When_Bmp_Opaque_Gradient_RoundTrips_Exactly()
	{
		var (rgba, w, h) = MakeGradient(24, 18);
		AssertLosslessRgb(rgba, w, h, BitmapEncoderFormat.Bmp);
	}

	[TestMethod]
	public void When_Bmp_FlatBlocks_RoundTrips_Exactly()
	{
		var (rgba, w, h) = MakeFlatBlocks(20, 20);
		AssertLosslessRgb(rgba, w, h, BitmapEncoderFormat.Bmp);
	}

	[TestMethod]
	public void When_Png_Opaque_Gradient_RoundTrips_Exactly()
	{
		var (rgba, w, h) = MakeGradient(24, 18);
		AssertLosslessRgba(rgba, w, h, BitmapEncoderFormat.Png);
	}

	[TestMethod]
	public void When_Png_Alpha_RoundTrips_Exactly()
	{
		var (rgba, w, h) = MakeAlpha(20, 16);
		AssertLosslessRgba(rgba, w, h, BitmapEncoderFormat.Png);
	}

	[TestMethod]
	public void When_Gif_FlatBlocks_RoundTrips_Exactly()
	{
		var (rgba, w, h) = MakeFlatBlocks(20, 20);
		AssertGifExact(rgba, w, h);
	}

	[TestMethod]
	public void When_Gif_GrayscaleRamp_256Colors_RoundTrips_Exactly()
	{
		// 256 distinct opaque colours → quantization is an identity, so the round-trip must be exact.
		var (rgba, w, h) = MakeGrayscaleRamp();
		AssertGifExact(rgba, w, h);
	}

	[TestMethod]
	public void When_Gif_Transparency_RoundTrips()
	{
		var (rgba, w, h) = MakeAlphaBinary(20, 16);
		AssertGifExact(rgba, w, h);
	}

	[TestMethod]
	public void When_Jpeg_Gradient_Quality90_IsCloseEnough()
	{
		var (rgba, w, h) = MakeGradient(32, 32);
		AssertJpegClose(rgba, w, h, quality: 90, avgTolerance: 3.0);
	}

	[TestMethod]
	public void When_Jpeg_FlatBlocks_Quality90_IsCloseEnough()
	{
		var (rgba, w, h) = MakeFlatBlocks(24, 24);
		AssertJpegClose(rgba, w, h, quality: 90, avgTolerance: 3.0);
	}

	[TestMethod]
	public void When_Jpeg_OddSize_Quality90_IsCloseEnough()
	{
		var (rgba, w, h) = MakeGradient(17, 9); // exercises partial-block edge padding
		AssertJpegClose(rgba, w, h, quality: 90, avgTolerance: 3.5);
	}

	private static void AssertLosslessRgb(byte[] rgba, int w, int h, BitmapEncoderFormat format)
	{
		var decoded = EncodeDecode(rgba, w, h, format);
		var expected = ToPremultipliedBgra(rgba, w, h);
		AssertMaxDiff(expected, decoded, w, h, includeAlpha: false, maxAllowed: 0, format.ToString());
	}

	private static void AssertLosslessRgba(byte[] rgba, int w, int h, BitmapEncoderFormat format)
	{
		var decoded = EncodeDecode(rgba, w, h, format);
		var expected = ToPremultipliedBgra(rgba, w, h);
		AssertMaxDiff(expected, decoded, w, h, includeAlpha: true, maxAllowed: 0, format.ToString());
	}

	private static void AssertGifExact(byte[] rgba, int w, int h)
	{
		var decoded = EncodeDecode(rgba, w, h, BitmapEncoderFormat.Gif);
		// GIF treats alpha < 128 as fully transparent → decodes to (0,0,0,0); opaque colours map through an exact
		// palette (≤256 distinct colours in these fixtures), so the decoded BGRA must match bit-for-bit.
		var expected = new byte[w * h * 4];
		for (var i = 0; i < w * h; i++)
		{
			if (rgba[i * 4 + 3] < 128)
			{
				continue; // stays (0,0,0,0)
			}

			expected[i * 4] = rgba[i * 4 + 2];
			expected[i * 4 + 1] = rgba[i * 4 + 1];
			expected[i * 4 + 2] = rgba[i * 4];
			expected[i * 4 + 3] = 255;
		}

		AssertMaxDiff(expected, decoded, w, h, includeAlpha: true, maxAllowed: 0, "Gif");
	}

	private static void AssertJpegClose(byte[] rgba, int w, int h, int quality, double avgTolerance)
	{
		var decoded = EncodeDecode(rgba, w, h, BitmapEncoderFormat.Jpeg, quality);
		var expected = ToPremultipliedBgra(rgba, w, h);

		long total = 0;
		var max = 0;
		var samples = 0;
		for (var i = 0; i < w * h; i++)
		{
			for (var c = 0; c < 3; c++) // B,G,R
			{
				var diff = Math.Abs(decoded[i * 4 + c] - expected[i * 4 + c]);
				total += diff;
				max = Math.Max(max, diff);
				samples++;
			}
		}

		var avg = (double)total / samples;
		Assert.IsTrue(avg < avgTolerance, $"JPEG avg per-channel diff {avg:F3} exceeded {avgTolerance} (max {max}).");
		Assert.IsTrue(max < 64, $"JPEG max per-channel diff {max} indicates structural garbage.");
	}

	private static byte[] EncodeDecode(byte[] rgba, int w, int h, BitmapEncoderFormat format, int quality = 90)
	{
		using var ms = new MemoryStream();
		ManagedImageEncoder.Encode(ms, rgba, w, h, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, format, quality);
		var encoded = ms.ToArray();
		Assert.IsTrue(ManagedImageDecoder.TryDecode(encoded, null, null, out var decoded), $"{format} failed to decode.");
		Assert.AreEqual(w, decoded!.Width, $"{format} width mismatch.");
		Assert.AreEqual(h, decoded.Height, $"{format} height mismatch.");
		return decoded.Frames[0];
	}

	private static void AssertMaxDiff(byte[] expected, byte[] actual, int w, int h, bool includeAlpha, int maxAllowed, string format)
	{
		var channels = includeAlpha ? 4 : 3;
		for (var i = 0; i < w * h; i++)
		{
			for (var c = 0; c < channels; c++)
			{
				var diff = Math.Abs(expected[i * 4 + c] - actual[i * 4 + c]);
				if (diff > maxAllowed)
				{
					Assert.Fail($"{format} pixel {i} channel {c}: expected {expected[i * 4 + c]}, got {actual[i * 4 + c]} (diff {diff}).");
				}
			}
		}
	}

	private static byte[] ToPremultipliedBgra(byte[] rgba, int w, int h)
	{
		var bgra = new byte[w * h * 4];
		for (var i = 0; i < w * h; i++)
		{
			var r = rgba[i * 4];
			var g = rgba[i * 4 + 1];
			var b = rgba[i * 4 + 2];
			var a = rgba[i * 4 + 3];
			if (a == 255)
			{
				bgra[i * 4] = b;
				bgra[i * 4 + 1] = g;
				bgra[i * 4 + 2] = r;
				bgra[i * 4 + 3] = 255;
			}
			else
			{
				bgra[i * 4] = (byte)(b * a / 255);
				bgra[i * 4 + 1] = (byte)(g * a / 255);
				bgra[i * 4 + 2] = (byte)(r * a / 255);
				bgra[i * 4 + 3] = a;
			}
		}

		return bgra;
	}

	private static (byte[] rgba, int w, int h) MakeGradient(int w, int h)
	{
		var rgba = new byte[w * h * 4];
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var i = (y * w + x) * 4;
				rgba[i] = (byte)(x * 255 / Math.Max(1, w - 1));
				rgba[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
				rgba[i + 2] = (byte)((x + y) * 255 / Math.Max(1, w + h - 2));
				rgba[i + 3] = 255;
			}
		}

		return (rgba, w, h);
	}

	private static (byte[] rgba, int w, int h) MakeFlatBlocks(int w, int h)
	{
		var colors = new byte[][]
		{
			new byte[] { 255, 0, 0 },
			new byte[] { 0, 255, 0 },
			new byte[] { 0, 0, 255 },
			new byte[] { 255, 255, 0 },
		};

		var rgba = new byte[w * h * 4];
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var i = (y * w + x) * 4;
				var c = colors[(x < w / 2 ? 0 : 1) + (y < h / 2 ? 0 : 2)];
				rgba[i] = c[0];
				rgba[i + 1] = c[1];
				rgba[i + 2] = c[2];
				rgba[i + 3] = 255;
			}
		}

		return (rgba, w, h);
	}

	private static (byte[] rgba, int w, int h) MakeAlpha(int w, int h)
	{
		var rgba = new byte[w * h * 4];
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var i = (y * w + x) * 4;
				rgba[i] = (byte)(x * 255 / Math.Max(1, w - 1));
				rgba[i + 1] = (byte)(y * 255 / Math.Max(1, h - 1));
				rgba[i + 2] = 128;
				rgba[i + 3] = (byte)((x * 255 / Math.Max(1, w - 1) + 40) % 256); // varying translucency
			}
		}

		return (rgba, w, h);
	}

	private static (byte[] rgba, int w, int h) MakeAlphaBinary(int w, int h)
	{
		var rgba = new byte[w * h * 4];
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var i = (y * w + x) * 4;
				var transparent = ((x / 4) + (y / 4)) % 2 == 0;
				rgba[i] = 200;
				rgba[i + 1] = 40;
				rgba[i + 2] = 120;
				rgba[i + 3] = (byte)(transparent ? 0 : 255);
			}
		}

		return (rgba, w, h);
	}

	private static (byte[] rgba, int w, int h) MakeGrayscaleRamp()
	{
		const int w = 16;
		const int h = 16;
		var rgba = new byte[w * h * 4];
		for (var i = 0; i < w * h; i++)
		{
			var v = (byte)i; // 0..255, all distinct
			rgba[i * 4] = v;
			rgba[i * 4 + 1] = v;
			rgba[i * 4 + 2] = v;
			rgba[i * 4 + 3] = 255;
		}

		return (rgba, w, h);
	}
}

// Guards the managed decoders against hostile input (remote / clipboard / ImageSource bytes are untrusted): a
// crafted chunk length must not spin the chunk walker forever, and header-declared dimensions must not drive an
// unbounded allocation. The [Timeout]s make the infinite-loop cases fail-before / pass-after (the loop never returns
// pre-fix); the outsized-dimension cases assert a clean rejection instead of a multi-gigabyte allocation.
[TestClass]
public class Given_ManagedImageDecoder_Hardening
{
	[TestMethod]
	[Timeout(10000)]
	public void When_Png_CraftedChunkLength_DoesNotHang()
	{
		// PNG signature + one chunk whose big-endian length (0xFFFFFFF4) is negative-as-int and whose type matches no
		// branch: pre-fix, p never advances and the walker loops forever; post-fix, the bounds check bails to false.
		var png = new byte[16];
		WritePngSignature(png);
		png[8] = 0xFF; png[9] = 0xFF; png[10] = 0xFF; png[11] = 0xF4; // length
		png[12] = (byte)'j'; png[13] = (byte)'u'; png[14] = (byte)'n'; png[15] = (byte)'k'; // unknown type

		Assert.IsFalse(ManagedImageDecoder.TryDecode(png, null, null, out _));
	}

	[TestMethod]
	[Timeout(10000)]
	public void When_Webp_CraftedChunkSize_DoesNotHang()
	{
		// RIFF/WEBP + one chunk whose little-endian size (0xFFFFFFF8) is negative-as-int and whose FourCC matches no
		// branch: pre-fix, p rewinds and the walker loops forever; post-fix, the bounds check bails to false.
		var webp = new byte[20];
		webp[0] = (byte)'R'; webp[1] = (byte)'I'; webp[2] = (byte)'F'; webp[3] = (byte)'F';
		webp[4] = 12; // file size (unused)
		webp[8] = (byte)'W'; webp[9] = (byte)'E'; webp[10] = (byte)'B'; webp[11] = (byte)'P';
		webp[12] = (byte)'j'; webp[13] = (byte)'u'; webp[14] = (byte)'n'; webp[15] = (byte)'k'; // unknown FourCC
		webp[16] = 0xF8; webp[17] = 0xFF; webp[18] = 0xFF; webp[19] = 0xFF; // size

		Assert.IsFalse(ManagedImageDecoder.TryDecode(webp, null, null, out _));
	}

	[TestMethod]
	[Timeout(10000)]
	public void When_Png_OutsizedDimensions_RejectedWithoutHugeAllocation()
	{
		// A ~33-byte PNG header declaring 20000x20000 would force a ~1.6 GB allocation before any pixel data; the pixel
		// cap must reject it up front.
		var png = new byte[33];
		WritePngSignature(png);
		png[8] = 0x00; png[9] = 0x00; png[10] = 0x00; png[11] = 0x0D; // IHDR length = 13
		png[12] = (byte)'I'; png[13] = (byte)'H'; png[14] = (byte)'D'; png[15] = (byte)'R';
		png[16] = 0x00; png[17] = 0x00; png[18] = 0x4E; png[19] = 0x20; // width  = 20000
		png[20] = 0x00; png[21] = 0x00; png[22] = 0x4E; png[23] = 0x20; // height = 20000
		png[24] = 8;  // bit depth
		png[25] = 6;  // colour type RGBA
		png[26] = 0;  // compression
		png[27] = 0;  // filter
		png[28] = 0;  // interlace

		Assert.IsFalse(ManagedImageDecoder.TryDecode(png, null, null, out _));
	}

	[TestMethod]
	[Timeout(10000)]
	public void When_Bmp_OutsizedDimensions_RejectedWithoutHugeAllocation()
	{
		// BM + a 40-byte BITMAPINFOHEADER declaring 20000x20000, 32bpp, uncompressed — the pixel cap must reject it.
		var bmp = new byte[54];
		bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
		bmp[10] = 54;   // pixel data offset
		bmp[14] = 40;   // DIB header size
		bmp[18] = 0x20; bmp[19] = 0x4E; // width  = 20000 (LE)
		bmp[22] = 0x20; bmp[23] = 0x4E; // height = 20000 (LE)
		bmp[28] = 32;   // bpp
		// compression (offset 30) = 0

		Assert.IsFalse(ManagedImageDecoder.TryDecode(bmp, null, null, out _));
	}

	private static void WritePngSignature(byte[] d)
	{
		d[0] = 0x89; d[1] = 0x50; d[2] = 0x4E; d[3] = 0x47;
		d[4] = 0x0D; d[5] = 0x0A; d[6] = 0x1A; d[7] = 0x0A;
	}
}
