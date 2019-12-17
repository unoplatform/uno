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
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MinValue);

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Height");

				for (var x = rect.Value.X; x < Math.Min(rect.Value.Width, expectedBitmap.Size.Width); x++)
					for (var y = rect.Value.Y; y < Math.Min(rect.Value.Height, expectedBitmap.Size.Height); y++)
					{
						Assert.AreEqual(expectedBitmap.GetPixel(x, y), actualBitmap.GetPixel(x, y), $"Pixel {x},{y} (rel: {x - rect.Value.X},{y - rect.Value.Y})");
					}
			}
		}

		public static void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AssertScreenshotsAreNotEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

		public static void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MinValue);

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				if (expectedBitmap.Size.Width != actualBitmap.Size.Width
					|| expectedBitmap.Size.Height != actualBitmap.Size.Height)
				{
					return;
				}

				for (var x = rect.Value.X; x < Math.Min(rect.Value.Width, expectedBitmap.Size.Width); x++)
					for (var y = rect.Value.Y; y < Math.Min(rect.Value.Height, expectedBitmap.Size.Height); y++)
					{
						if (expectedBitmap.GetPixel(x, y) != actualBitmap.GetPixel(x, y))
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
