#if __SKIA__
#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Imaging;
using Windows.Storage;
#if UNO_DRAWING_SKIA
using SkiaSharp;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

// ManagedImageDecoder.TryGetSize runs TryDecode's header readers, so it must refuse every file those refuse (for a JPEG, the
// markers before its first scan and that scan's header) and report the size TryDecode produces.
[TestClass]
public class Given_ManagedImageDecoder
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
	[DataRow("Formats/uno-overalls.png")]
	[DataRow("Formats/uno-overalls.jpg")]
	[DataRow("Formats/uno-overalls.gif")]
	[DataRow("Formats/uno-overalls.bmp")]
	[DataRow("Formats/uno-overalls.webp")]
	[DataRow("Formats/uno-overalls.heic")]
	[DataRow("Formats/animated.gif")]
	[DataRow("Formats/animated.webp")]
	[DataRow("testimage_exif_rotated.jpg")]
	[DataRow("testimage_exif_rotated_different_dimensions.jpg")]
	[DataRow("LargeWisteria.jpg")]
	[DataRow("colored-ellipse.jpg")]
	[DataRow("test_image_1252_836.png")]
	[DataRow("ingredient3.png")]
	[DataRow("BlueSquare.png")]
	[DataRow("bluecrystal.ico")]
	[DataRow("InvalidImage.jpg")]
	public async Task When_Asset_Then_Size_Read_Agrees_With_Decode(string asset)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/" + asset));
		var bytes = (await FileIO.ReadBufferAsync(file)).ToArray();

		AssertAgrees(bytes);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
	[DataRow("png-rgba", 5, 3)]
	[DataRow("png-gray-16", 5, 3)]
	[DataRow("png-palette-1", 5, 3)]
	[DataRow("png-adam7", 5, 3)]
	[DataRow("gif", 5, 3)]
	[DataRow("gif-local-table", 5, 3)]
	[DataRow("gif-2-frames", 5, 3)]
	[DataRow("bmp-8", 5, 3)]
	[DataRow("bmp-24", 5, 3)]
	[DataRow("bmp-32-top-down", 5, 3)]
	[DataRow("bmp-32-bitfields", 5, 3)]
	[DataRow("bmp-core", 5, 3)]
	[DataRow("jpeg", 5, 3)]
	[DataRow("jpeg-exif-1", 5, 3)]
	[DataRow("jpeg-exif-2", 5, 3)]
	[DataRow("jpeg-exif-3", 5, 3)]
	[DataRow("jpeg-exif-4", 5, 3)]
	[DataRow("jpeg-exif-5", 3, 5)]
	[DataRow("jpeg-exif-6", 3, 5)]
	[DataRow("jpeg-exif-7", 3, 5)]
	[DataRow("jpeg-exif-8", 3, 5)]
	[DataRow("webp-vp8", 5, 3)]
	[DataRow("webp-vp8l", 5, 3)]
	public void When_Generated_Image_Then_Size_Read_Agrees_With_Decode(string name, int width, int height)
	{
		var bytes = Generate(name);

		var (decoded, decodedWidth, decodedHeight) = Decode(bytes);
		Assert.IsTrue(decoded, $"{name} should decode.");
		Assert.AreEqual((width, height), (decodedWidth, decodedHeight));
		AssertAgrees(bytes);
	}

	// Each file has a valid header, and makes TryDecode return false before it decompresses anything.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
	[DataRow("png-no-header")]
	[DataRow("png-chunk-overrun")]
	[DataRow("png-interlace-2")]
	[DataRow("png-color-type-5")]
	[DataRow("png-rgb-depth-4")]
	[DataRow("png-palette-depth-16")]
	[DataRow("png-palette-missing")]
	[DataRow("png-palette-short")]
	[DataRow("png-no-image-data")]
	[DataRow("png-over-pixel-cap")]
	[DataRow("png-rgba-16-over-byte-cap")]
	[DataRow("gif-no-color-table")]
	[DataRow("gif-global-table-overrun")]
	[DataRow("gif-local-table-overrun")]
	[DataRow("gif-screen-over-cap")]
	[DataRow("gif-frame-over-cap")]
	[DataRow("gif-lzw-code-size-13")]
	[DataRow("gif-sub-block-overrun")]
	[DataRow("gif-no-image")]
	[DataRow("bmp-short")]
	[DataRow("bmp-info-header-short")]
	[DataRow("bmp-4-bit")]
	[DataRow("bmp-16-bit")]
	[DataRow("bmp-rle8")]
	[DataRow("bmp-bitfields-not-bgra")]
	[DataRow("bmp-pixels-overrun")]
	[DataRow("bmp-over-cap")]
	[DataRow("jpeg-lossless")]
	[DataRow("jpeg-arithmetic")]
	[DataRow("jpeg-no-frame")]
	[DataRow("jpeg-over-pixel-cap")]
	[DataRow("jpeg-over-coefficient-cap")]
	[DataRow("jpeg-no-components")]
	[DataRow("jpeg-quant-table-4")]
	[DataRow("jpeg-sampling-0")]
	[DataRow("jpeg-dqt-table-4")]
	[DataRow("jpeg-dht-table-4")]
	[DataRow("jpeg-dht-overrun")]
	[DataRow("jpeg-scan-component-missing")]
	[DataRow("jpeg-scan-table-4")]
	[DataRow("jpeg-no-huffman-tables")]
	[DataRow("webp-short")]
	[DataRow("webp-chunk-overrun")]
	[DataRow("webp-animated")]
	[DataRow("webp-alpha-chunk")]
	[DataRow("webp-vp8-overrun")]
	[DataRow("webp-vp8-not-key-frame")]
	[DataRow("webp-vp8-start-code")]
	[DataRow("webp-vp8-over-cap")]
	[DataRow("webp-vp8l-signature")]
	[DataRow("webp-vp8l-version")]
	[DataRow("webp-vp8l-over-cap")]
	public void When_Header_Is_Refused_Then_Size_Is_Not_Read(string name)
	{
		var bytes = Generate(name);

		Assert.IsFalse(Decode(bytes).Decoded, $"Precondition: the decoder refuses {name}.");
		Assert.IsFalse(ReadSize(bytes).Read, $"The size read should refuse {name}, as the decoder does.");
		Assert.IsFalse(ReadSizeFromStream(bytes).Read, $"The size read should refuse {name} from a stream too.");
	}

	// Cutting or corrupting a header must neither throw nor hang, and the size read must never refuse what the decoder
	// decodes, nor report another size.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
	[DataRow("png-rgba")]
	[DataRow("png-palette-1")]
	[DataRow("png-adam7")]
	[DataRow("gif-2-frames")]
	[DataRow("gif-local-table")]
	[DataRow("bmp-8")]
	[DataRow("bmp-32-bitfields")]
	[DataRow("bmp-core")]
	[DataRow("jpeg-exif-6")]
	[DataRow("webp-vp8")]
	[DataRow("webp-vp8l")]
	public void When_Header_Is_Cut_Or_Corrupted_Then_Size_Read_Agrees_With_Decode(string name)
	{
		var seed = Generate(name);

		for (var length = 0; length < seed.Length; length++)
		{
			AssertConsistent(seed[..length], $"{name} cut to {length} bytes");
		}

		for (var i = 0; i < Math.Min(seed.Length, 160); i++)
		{
			foreach (var value in new[] { (byte)0x00, (byte)0xFF, (byte)(seed[i] ^ 0x01), (byte)(seed[i] ^ 0x80) })
			{
				var corrupted = (byte[])seed.Clone();
				corrupted[i] = value;
				AssertConsistent(corrupted, $"{name} with byte {i} set to 0x{value:X2}");
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25556")]
	public void When_Exif_Offsets_Are_Hostile_Then_Size_Read_Returns()
	{
		var jpeg = Jpeg(5, 3);
		foreach (var offset in new uint[] { 0, 1, 7, 0x7FFFFFFF, 0x80000000, 0xFFFFFFF0, 0xFFFFFFFF })
		{
			foreach (var count in new ushort[] { 0, 1, 0xFFFF })
			{
				var app1 = Exif(6, ifdOffset: offset, entryCount: count);
				AssertConsistent([.. jpeg[..2], .. app1, .. jpeg[2..]], $"IFD offset 0x{offset:X8}, {count} entries");
			}
		}
	}

	private static void AssertAgrees(byte[] bytes)
	{
		var (decoded, width, height) = Decode(bytes);
		var expected = decoded ? (true, width, height) : (false, 0, 0);
		Assert.AreEqual(expected, ReadSize(bytes), "The size read should agree with the decode.");
		Assert.AreEqual(expected, ReadSizeFromStream(bytes), "The size read from a stream should agree with the decode.");
	}

	private static void AssertConsistent(byte[] bytes, string what)
	{
		var size = ReadSize(bytes);
		Assert.AreEqual(size, ReadSizeFromStream(bytes), $"{what}: the stream read should match the array read.");

		// Decoding a corrupted header that reads as a large image costs its whole allocation; the refusals are what matters.
		if (size.Read && (long)size.Width * size.Height > 1 << 16)
		{
			return;
		}

		var (decoded, width, height) = Decode(bytes);
		if (decoded)
		{
			Assert.AreEqual((true, width, height), size, $"{what}: the size read should agree with the decode.");
		}
	}

	private static (bool Decoded, int Width, int Height) Decode(byte[] bytes)
		=> ManagedImageDecoder.TryDecode(bytes, null, null, out var decoded) ? (true, decoded.Width, decoded.Height) : (false, 0, 0);

	private static (bool Read, int Width, int Height) ReadSize(byte[] bytes)
		=> ManagedImageDecoder.TryGetSize(new ImageBytes(bytes), out var width, out var height) ? (true, width, height) : (false, 0, 0);

	private static (bool Read, int Width, int Height) ReadSizeFromStream(byte[] bytes)
		=> ManagedImageDecoder.TryGetSize(new ImageBytes(new MemoryStream(bytes), 0), out var width, out var height) ? (true, width, height) : (false, 0, 0);

	private static byte[] Generate(string name) => name switch
	{
		"png-rgba" => Png(5, 3),
		"png-gray-16" => Png(5, 3, colorType: 0, bitDepth: 16),
		"png-palette-1" => Png(5, 3, colorType: 3, bitDepth: 1, palette: 6),
		"png-adam7" => Png(5, 3, interlace: 1),
		"png-no-header" => Png(5, 3, header: false),
		"png-chunk-overrun" => Png(5, 3, trailer: [0x00, 0x01, 0x00, 0x00, (byte)'t', (byte)'E', (byte)'X', (byte)'t']),
		"png-interlace-2" => Png(5, 3, interlace: 2),
		"png-color-type-5" => Png(5, 3, colorType: 5),
		"png-rgb-depth-4" => Png(5, 3, colorType: 2, bitDepth: 4),
		"png-palette-depth-16" => Png(5, 3, colorType: 3, bitDepth: 16, palette: 6),
		"png-palette-missing" => Png(5, 3, colorType: 3, bitDepth: 8),
		"png-palette-short" => Png(5, 3, colorType: 3, bitDepth: 8, palette: 2),
		"png-no-image-data" => Png(5, 3, imageData: false),
		"png-over-pixel-cap" => Png(16384, 16384, rows: 1),
		"png-rgba-16-over-byte-cap" => Png(10000, 10000, bitDepth: 16, rows: 1), // 4 bytes a pixel fit, 4 + 8 don't

		"gif" => Gif(5, 3),
		"gif-local-table" => Gif(5, 3, globalTable: false, localTable: true),
		"gif-2-frames" => Gif(5, 3, frames: 2),
		"gif-no-color-table" => Gif(5, 3, globalTable: false),
		"gif-global-table-overrun" => Gif(5, 3)[..16],
		"gif-local-table-overrun" => Gif(5, 3, globalTable: false, localTable: true)[..34],
		"gif-screen-over-cap" => Gif(5, 3, screenWidth: 65535, screenHeight: 65535),
		"gif-frame-over-cap" => Gif(5, 3, frameWidth: 65535, frameHeight: 65535),
		"gif-lzw-code-size-13" => Gif(5, 3, minCodeSize: 13),
		"gif-sub-block-overrun" => Gif(5, 3, subBlocks: [0x40, 0x84, 0x1D]),
		"gif-no-image" => Gif(5, 3, frames: 0),

		"bmp-8" => Bmp(5, 3, bpp: 8),
		"bmp-24" => Bmp(5, 3, bpp: 24),
		"bmp-32-top-down" => Bmp(5, -3, bpp: 32),
		"bmp-32-bitfields" => Bmp(5, 3, bpp: 32, compression: 3, dibSize: 108),
		"bmp-core" => Bmp(5, 3, bpp: 24, dibSize: 12),
		"bmp-short" => Bmp(5, 3)[..25],
		"bmp-info-header-short" => Bmp(5, 3)[..33],
		"bmp-4-bit" => Bmp(5, 3, bpp: 4),
		"bmp-16-bit" => Bmp(5, 3, bpp: 16),
		"bmp-rle8" => Bmp(5, 3, bpp: 8, compression: 1),
		"bmp-bitfields-not-bgra" => Bmp(5, 3, bpp: 32, compression: 3, dibSize: 108, redMask: 0x000000FF, blueMask: 0x00FF0000),
		"bmp-pixels-overrun" => Bmp(5, 3)[..^2],
		"bmp-over-cap" => Bmp(16384, 16384, bpp: 24, pixels: false),

		"jpeg" => Jpeg(5, 3),
		_ when name.StartsWith("jpeg-exif-", StringComparison.Ordinal) => WithExif(Jpeg(5, 3), int.Parse(name["jpeg-exif-".Length..])),
		"jpeg-lossless" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 1] = 0xC3),
		"jpeg-arithmetic" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 1] = 0xC9),
		"jpeg-no-frame" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 1] = 0xEF), // an APP15 segment instead
		"jpeg-over-pixel-cap" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => SetU16BE(j, sof + 5, 65535, 65535)),
		"jpeg-over-coefficient-cap" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => SetU16BE(j, sof + 5, 11000, 11000)),
		"jpeg-no-components" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 9] = 0),
		"jpeg-quant-table-4" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 12] = 4),
		"jpeg-sampling-0" => PatchJpeg(Jpeg(5, 3), 0xC0, (j, sof) => j[sof + 11] = 0x01),
		"jpeg-dqt-table-4" => PatchJpeg(Jpeg(5, 3), 0xDB, (j, dqt) => j[dqt + 4] = (byte)((j[dqt + 4] & 0xF0) | 4)),
		"jpeg-dht-table-4" => PatchJpeg(Jpeg(5, 3), 0xC4, (j, dht) => j[dht + 4] = (byte)((j[dht + 4] & 0xF0) | 4)),
		"jpeg-dht-overrun" => Jpeg(5, 3)[..(FindMarker(Jpeg(5, 3), 0xC4) + 12)],
		"jpeg-scan-component-missing" => PatchJpeg(Jpeg(5, 3), 0xDA, (j, sos) => j[sos + 5] = 0x7F),
		"jpeg-scan-table-4" => PatchJpeg(Jpeg(5, 3), 0xDA, (j, sos) => j[sos + 6] = 0x44),
		"jpeg-no-huffman-tables" => WithoutMarkers(Jpeg(5, 3), 0xC4),

#if UNO_DRAWING_SKIA
		"webp-vp8" => Webp(lossless: false),
		"webp-vp8l" => Webp(lossless: true),
		"webp-alpha-chunk" => Webp(lossless: false, alpha: true),
		"webp-vp8-overrun" => PatchWebp(Webp(lossless: false), (w, chunk) => SetU32LE(w, chunk - 4, (uint)w.Length)),
		"webp-vp8-not-key-frame" => PatchWebp(Webp(lossless: false), (w, chunk) => w[chunk] |= 1),
		"webp-vp8-start-code" => PatchWebp(Webp(lossless: false), (w, chunk) => w[chunk + 3] = 0x9C),
		"webp-vp8-over-cap" => PatchWebp(Webp(lossless: false), (w, chunk) => { SetU16LE(w, chunk + 6, 16383); SetU16LE(w, chunk + 8, 16383); }),
		"webp-vp8l-signature" => PatchWebp(Webp(lossless: true), (w, chunk) => w[chunk] = 0x2E),
		"webp-vp8l-version" => PatchWebp(Webp(lossless: true), (w, chunk) => w[chunk + 4] |= 0x20),
		"webp-vp8l-over-cap" => PatchWebp(Webp(lossless: true), (w, chunk) => SetU32LE(w, chunk + 1, (w[chunk + 4] & 0xF0u) << 24 | 0x0FFFFFFF)),
#endif
		"webp-short" => Riff(new byte[3]), // 15 bytes
		"webp-chunk-overrun" => Riff([.. Chunk("ICCP", new byte[4])[..10]]),
		"webp-animated" => Riff([.. Chunk("VP8X", new byte[10]), .. Chunk("ANIM", new byte[6])]),

		_ => throw new AssertInconclusiveException($"{name} isn't generated in this build."),
	};

	private static byte[] Png(
		int width, int height, int colorType = 6, int bitDepth = 8, int interlace = 0, int palette = -1,
		bool header = true, bool imageData = true, int rows = -1, byte[]? trailer = null)
	{
		using var png = new MemoryStream();
		png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
		if (header)
		{
			var ihdr = new byte[13];
			BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
			BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
			ihdr[8] = (byte)bitDepth;
			ihdr[9] = (byte)colorType;
			ihdr[12] = (byte)interlace;
			WritePngChunk(png, "IHDR", ihdr);
		}

		if (palette >= 0)
		{
			WritePngChunk(png, "PLTE", Enumerable.Range(0, palette).Select(i => (byte)(i * 40)).ToArray());
		}

		if (imageData)
		{
			var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, _ => 4 };
			var stride = ((long)width * channels * Math.Max(1, bitDepth) + 7) / 8;
			// Twice the scanlines an image needs covers an Adam7 one's: filter type 0, all samples 0.
			var raw = new byte[(stride + 1) * (rows < 0 ? height * 2 : rows)];
			using var compressed = new MemoryStream();
			using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
			{
				zlib.Write(raw);
			}

			WritePngChunk(png, "IDAT", compressed.ToArray());
		}

		if (trailer is not null)
		{
			png.Write(trailer);
		}

		WritePngChunk(png, "IEND", []);
		return png.ToArray();
	}

	private static void WritePngChunk(Stream png, string type, byte[] data)
	{
		var head = new byte[8];
		BinaryPrimitives.WriteInt32BigEndian(head, data.Length);
		Encoding.ASCII.GetBytes(type, head.AsSpan(4));
		png.Write(head);
		png.Write(data);
		var crc = new byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32([.. head[4..], .. data]));
		png.Write(crc);
	}

	private static uint Crc32(byte[] data)
	{
		var crc = 0xFFFFFFFFu;
		foreach (var b in data)
		{
			crc ^= b;
			for (var k = 0; k < 8; k++)
			{
				crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
			}
		}

		return ~crc;
	}

	// The LZW sub-blocks of an image whose pixels are all index 0, at a 2-bit minimum code size: codes clear, 0 and end.
	private static readonly byte[] _lzwZeros = [0x02, 0x44, 0x01, 0x00];

	private static byte[] Gif(
		int width, int height, bool globalTable = true, bool localTable = false, int frames = 1,
		int screenWidth = -1, int screenHeight = -1, int frameWidth = -1, int frameHeight = -1, int minCodeSize = 2,
		byte[]? subBlocks = null)
	{
		using var gif = new MemoryStream();
		gif.Write("GIF89a"u8);
		WriteU16LE(gif, screenWidth < 0 ? width : screenWidth);
		WriteU16LE(gif, screenHeight < 0 ? height : screenHeight);
		gif.Write([(byte)(globalTable ? 0x80 : 0x00), 0, 0]);
		if (globalTable)
		{
			gif.Write([0, 0, 0, 255, 255, 255]);
		}

		for (var i = 0; i < frames; i++)
		{
			gif.Write([0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00]); // graphic control: 100 ms
			gif.WriteByte(0x2C);
			WriteU16LE(gif, 0);
			WriteU16LE(gif, 0);
			WriteU16LE(gif, frameWidth < 0 ? width : frameWidth);
			WriteU16LE(gif, frameHeight < 0 ? height : frameHeight);
			gif.WriteByte((byte)(localTable ? 0x80 : 0x00));
			if (localTable)
			{
				gif.Write([255, 0, 0, 0, 0, 255]);
			}

			gif.WriteByte((byte)minCodeSize);
			gif.Write(subBlocks ?? _lzwZeros);
		}

		if (subBlocks is null)
		{
			gif.WriteByte(0x3B);
		}

		return gif.ToArray();
	}

	private static byte[] Bmp(
		int width, int height, int bpp = 24, int compression = 0, int dibSize = 40, bool pixels = true,
		uint redMask = 0x00FF0000, uint blueMask = 0x000000FF)
	{
		var paletteSize = bpp <= 8 ? (dibSize == 12 ? 3 : 4) << bpp : 0;
		var stride = (width * bpp / 8 + 3) & ~3;
		var pixelOffset = 14 + dibSize + paletteSize;
		var length = pixelOffset + (pixels ? stride * Math.Abs(height) : 0);
		var bmp = new byte[length];
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		SetU32LE(bmp, 2, (uint)length);
		SetU32LE(bmp, 10, (uint)pixelOffset);
		SetU32LE(bmp, 14, (uint)dibSize);
		if (dibSize == 12)
		{
			SetU16LE(bmp, 18, width);
			SetU16LE(bmp, 20, height);
			SetU16LE(bmp, 22, 1);
			SetU16LE(bmp, 24, bpp);
		}
		else
		{
			SetU32LE(bmp, 18, (uint)width);
			SetU32LE(bmp, 22, (uint)height);
			SetU16LE(bmp, 26, 1);
			SetU16LE(bmp, 28, bpp);
			SetU32LE(bmp, 30, (uint)compression);
			if (compression == 3)
			{
				SetU32LE(bmp, 54, redMask);
				SetU32LE(bmp, 58, 0x0000FF00);
				SetU32LE(bmp, 62, blueMask);
				SetU32LE(bmp, 66, 0xFF000000);
			}
		}

		for (var i = pixelOffset; i < length; i++)
		{
			bmp[i] = (byte)(i * 7);
		}

		return bmp;
	}

	private static byte[] Jpeg(int width, int height)
	{
		using var jpeg = new MemoryStream();
		var pixels = Enumerable.Range(0, width * height * 4).Select(i => (byte)(i % 4 == 3 ? 255 : i * 13)).ToArray();
		ManagedImageEncoder.Encode(jpeg, pixels, width, height, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, BitmapEncoderFormat.Jpeg, 90);
		return jpeg.ToArray();
	}

	private static byte[] WithExif(byte[] jpeg, int orientation) => [.. jpeg[..2], .. Exif(orientation), .. jpeg[2..]];

	// APP1: "Exif\0\0", a big-endian TIFF header, and an IFD whose entries are all Orientation (a SHORT).
	private static byte[] Exif(int orientation, uint ifdOffset = 8, ushort entryCount = 1)
	{
		var entries = Math.Min((int)entryCount, 4);
		var app1 = new byte[4 + 6 + 8 + 2 + entries * 12 + 4];
		app1[0] = 0xFF;
		app1[1] = 0xE1;
		SetU16BE(app1, 2, app1.Length - 2);
		"Exif\0\0"u8.CopyTo(app1.AsSpan(4));
		"MM\0*"u8.CopyTo(app1.AsSpan(10));
		BinaryPrimitives.WriteUInt32BigEndian(app1.AsSpan(14), ifdOffset);
		BinaryPrimitives.WriteUInt16BigEndian(app1.AsSpan(18), entryCount);
		for (var i = 0; i < entries; i++)
		{
			var entry = 20 + i * 12;
			BinaryPrimitives.WriteUInt16BigEndian(app1.AsSpan(entry), 0x0112);
			BinaryPrimitives.WriteUInt16BigEndian(app1.AsSpan(entry + 2), 3);
			BinaryPrimitives.WriteUInt32BigEndian(app1.AsSpan(entry + 4), 1);
			BinaryPrimitives.WriteUInt16BigEndian(app1.AsSpan(entry + 8), (ushort)orientation);
		}

		return app1;
	}

	private static int FindMarker(byte[] jpeg, byte marker)
	{
		for (var p = 2; p + 3 < jpeg.Length; p += 2 + ((jpeg[p + 2] << 8) | jpeg[p + 3]))
		{
			if (jpeg[p] == 0xFF && jpeg[p + 1] == marker)
			{
				return p;
			}
		}

		throw new InvalidOperationException($"No 0x{marker:X2} marker.");
	}

	// Turns each of the marker's segments into an APP15 segment, which the decoder skips.
	private static byte[] WithoutMarkers(byte[] jpeg, byte marker)
	{
		for (var p = 2; p + 3 < jpeg.Length && jpeg[p + 1] != 0xDA; p += 2 + ((jpeg[p + 2] << 8) | jpeg[p + 3]))
		{
			if (jpeg[p] == 0xFF && jpeg[p + 1] == marker)
			{
				jpeg[p + 1] = 0xEF;
			}
		}

		return jpeg;
	}

	/// <param name="patch">Receives the file and the offset of the marker's 0xFF.</param>
	private static byte[] PatchJpeg(byte[] jpeg, byte marker, Action<byte[], int> patch)
	{
		patch(jpeg, FindMarker(jpeg, marker));
		return jpeg;
	}

#if UNO_DRAWING_SKIA
	private static byte[] Webp(bool lossless, bool alpha = false)
	{
		using var bitmap = new SKBitmap(new SKImageInfo(5, 3, SKColorType.Bgra8888, SKAlphaType.Premul));
		bitmap.Erase(alpha ? SKColors.Red.WithAlpha(0x80) : SKColors.Red);
		using var pixmap = bitmap.PeekPixels();
		using var data = pixmap.Encode(new SKWebpEncoderOptions(lossless ? SKWebpEncoderCompression.Lossless : SKWebpEncoderCompression.Lossy, 90))!;
		return data.ToArray();
	}

	/// <param name="patch">Receives the file and the offset of the image bitstream's data.</param>
	private static byte[] PatchWebp(byte[] webp, Action<byte[], int> patch)
	{
		for (var p = 12; p + 8 <= webp.Length; p += 8 + (int)((BinaryPrimitives.ReadUInt32LittleEndian(webp.AsSpan(p + 4)) + 1) & ~1u))
		{
			if (webp.AsSpan(p, 4).SequenceEqual("VP8 "u8) || webp.AsSpan(p, 4).SequenceEqual("VP8L"u8))
			{
				patch(webp, p + 8);
				return webp;
			}
		}

		throw new InvalidOperationException("No VP8 or VP8L chunk.");
	}
#endif

	private static byte[] Riff(byte[] chunks)
	{
		var riff = new byte[12 + chunks.Length];
		"RIFF"u8.CopyTo(riff);
		SetU32LE(riff, 4, (uint)(riff.Length - 8));
		"WEBP"u8.CopyTo(riff.AsSpan(8));
		chunks.CopyTo(riff, 12);
		return riff;
	}

	private static byte[] Chunk(string id, byte[] data)
	{
		var chunk = new byte[8 + data.Length + (data.Length & 1)];
		Encoding.ASCII.GetBytes(id, chunk);
		SetU32LE(chunk, 4, (uint)data.Length);
		data.CopyTo(chunk, 8);
		return chunk;
	}

	private static void WriteU16LE(Stream stream, int value) => stream.Write([(byte)value, (byte)(value >> 8)]);

	private static void SetU16LE(byte[] d, int offset, int value) => BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(offset), (ushort)value);

	private static void SetU32LE(byte[] d, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(offset), value);

	private static void SetU16BE(byte[] d, int offset, int value) => BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(offset), (ushort)value);

	private static void SetU16BE(byte[] d, int offset, int height, int width)
	{
		SetU16BE(d, offset, height);
		SetU16BE(d, offset + 2, width);
	}
}
#endif
