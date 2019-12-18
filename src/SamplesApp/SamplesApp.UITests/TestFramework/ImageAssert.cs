using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest;

namespace SamplesApp.UITests.TestFramework
{
	public static class ImageAssert
	{
		public static void AssertScreenshotsAreEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AssertScreenshotsAreEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

		public static void AssertScreenshotsAreEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MaxValue);
			AssertScreenshotsAreEqual(expected, rect.Value, actual, rect.Value);
		}

		public static void AssertScreenshotsAreEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AssertScreenshotsAreEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public static void AssertScreenshotsAreEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				for (var x = 0; x < Math.Min(expectedRect.Width, expectedBitmap.Size.Width); x++)
					for (var y = 0; y < Math.Min(expectedRect.Height, expectedBitmap.Size.Height); y++)
					{
						Assert.AreEqual(
							expectedBitmap.GetPixel(x + expectedRect.X, y + expectedRect.Y),
							actualBitmap.GetPixel(x + actualRect.X, y + actualRect.Y),
							$"Pixel {x},{y}");
					}
			}
		}

		public static void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AssertScreenshotsAreNotEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
		public static void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MinValue);
			AssertScreenshotsAreNotEqual(expected, rect.Value, actual, rect.Value);
		}

		public static void AssertScreenshotsAreNotEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AssertScreenshotsAreNotEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public static void AssertScreenshotsAreNotEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				for (var x = 0; x < Math.Min(expectedRect.Width, expectedBitmap.Size.Width); x++)
					for (var y = 0; y < Math.Min(expectedRect.Height, expectedBitmap.Size.Height); y++)
					{
						if (expectedBitmap.GetPixel(x + expectedRect.X, y + expectedRect.Y) != actualBitmap.GetPixel(x + actualRect.X, y + actualRect.Y))
						{
							return;
						}
					}

				Assert.Fail("Screenshots are equals.");
			}
		}

		public static void AssertHasColorAt(FileInfo screenshot, float x, float y, Color expectedColor)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				Assert.GreaterOrEqual(bitmap.Width, (int)x);
				Assert.GreaterOrEqual(bitmap.Height, (int)y);
				var pixel = bitmap.GetPixel((int)x, (int)y);

				Assert.AreEqual(expectedColor.ToArgb(), pixel.ToArgb()); //Convert to ARGB value, because 'named colors' are not considered equal to their unnamed equivalents(!)
			}
		}

		public static void AssertDoesNotHaveColorAt(FileInfo screenshot, float x, float y, Color excludedColor)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				Assert.GreaterOrEqual(bitmap.Width, (int)x);
				Assert.GreaterOrEqual(bitmap.Height, (int)y);
				var pixel = bitmap.GetPixel((int)x, (int)y);

				Assert.AreNotEqual(excludedColor.ToArgb(), pixel.ToArgb()); //Convert to ARGB value, because 'named colors' are not considered equal to their unnamed equivalents(!)
			}
		}
	}
}
