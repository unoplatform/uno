#nullable enable

#if __SKIA__
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.Managed;
using Windows.Graphics.Imaging;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_ImageEncoderDecoder
{
	[TestMethod]
	public unsafe void When_Managed_Image_Pixels_Are_Written_In_Place_Then_It_Holds_Them()
	{
		using var image = CreateAndAssertPixels(new ManagedImageDecoderBackend(), out var written);

		fixed (byte* pixels = ((ManagedImage)image).Pixels)
		{
			Assert.AreEqual(written, (IntPtr)pixels, "The image should hold the memory that was written, not a copy of it.");
		}
	}

#if UNO_DRAWING_SKIA
	[TestMethod]
	public void When_Skia_Image_Pixels_Are_Written_In_Place_Then_It_Holds_Them()
	{
		using var image = CreateAndAssertPixels(new SkiaImageDecoderBackend(), out var written);

		// The image draws from the memory that was written, not from a copy of it.
		using var pixmap = ((SkiaImage)image).Image.PeekPixels();
		Assert.AreEqual(written, pixmap.GetPixels());
	}
#endif

	[TestMethod]
	public unsafe void When_Codec_Does_Not_Override_Then_Default_Copies_Written_Pixels()
	{
		using var image = CreateAndAssertPixels(new ForwardingCodec(ManagedBackend.CreateImageDecoder()), out var written);

		// The default wrote into a buffer of its own and created the image from it.
		fixed (byte* pixels = ((ManagedImage)image).Pixels)
		{
			Assert.AreNotEqual(written, (IntPtr)pixels);
		}
	}

	private static unsafe IImage CreateAndAssertPixels(IImageEncoderDecoder codec, out IntPtr written)
	{
		const int width = 3;
		const int height = 2;
		var expected = new byte[width * height * 4];
		for (var i = 0; i < width * height; i++)
		{
			// Opaque, so any channel value is a valid premultiplied one.
			expected[i * 4] = (byte)(i * 40);
			expected[i * 4 + 1] = (byte)(i * 20 + 10);
			expected[i * 4 + 2] = (byte)(250 - i * 30);
			expected[i * 4 + 3] = 255;
		}

		var address = IntPtr.Zero;
		var image = codec.CreateImage(width, height, expected, (pixels, source) =>
		{
			Assert.AreEqual(source.Length, pixels.Length, "The span should cover the image's tightly packed pixels.");
			source.CopyTo(pixels);
			fixed (byte* pointer = pixels)
			{
				address = (IntPtr)pointer;
			}
		});

		Assert.AreEqual(width, image.PixelWidth);
		Assert.AreEqual(height, image.PixelHeight);
		var actual = new byte[expected.Length];
		image.CopyPixels(actual);
		CollectionAssert.AreEqual(expected, actual);

		written = address;
		return image;
	}

	// A codec written before the in-place overload existed: it only implements the other members.
	private sealed class ForwardingCodec(IImageEncoderDecoder inner) : IImageEncoderDecoder
	{
		public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames)
			=> inner.TryDecode(stream, targetWidth, targetHeight, out frames);

		public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
			=> inner.CreateImage(pixelWidth, pixelHeight, bgraPremul);

		public ImageFrames CreateFrames(IImage image) => inner.CreateFrames(image);

		public void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
			=> inner.Encode(destination, pixels, width, height, pixelFormat, alphaMode, format, quality);
	}
}
#endif
