#if __SKIA__
using SkiaSharp;

namespace Uno.UI.RuntimeTests.Helpers;

public static class SkiaImageAssert
{
	public static void ArePixelsEqual(byte[] expected, byte[] actual)
	{
		using var expectedBitmap = SKBitmap.Decode(expected);
		using var actualBitmap = SKBitmap.Decode(actual);

		Assert.IsNotNull(expectedBitmap, "Expected image could not be decoded by Skia");
		Assert.IsNotNull(actualBitmap, "Actual image could not be decoded by Skia");
		Assert.AreEqual(expectedBitmap.Width, actualBitmap.Width, "Image width mismatch");
		Assert.AreEqual(expectedBitmap.Height, actualBitmap.Height, "Image height mismatch");

		for (var y = 0; y < expectedBitmap.Height; y++)
		{
			for (var x = 0; x < expectedBitmap.Width; x++)
			{
				var expectedPixel = expectedBitmap.GetPixel(x, y);
				var actualPixel = actualBitmap.GetPixel(x, y);
				Assert.AreEqual(expectedPixel, actualPixel, $"Pixel mismatch at ({x}, {y}): expected {expectedPixel}, got {actualPixel}");
			}
		}
	}
}
#endif
