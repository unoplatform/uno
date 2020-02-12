using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest;
using static System.Math;

namespace SamplesApp.UITests.TestFramework
{
	public static class ImageAssert
	{
		public static void AreEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AreEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

		public static void AreEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MaxValue);
			AreEqual(expected, rect.Value, actual, rect.Value);
		}

		public static void AreEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AreEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public static void AreEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				var width = Math.Min(expectedRect.Width, expectedBitmap.Size.Width);
				var height = Math.Min(expectedRect.Height, expectedBitmap.Size.Height);

				(int x, int y) expectedOffset = (
					expectedRect.X < 0 ? expectedBitmap.Size.Width + expectedRect.X : expectedRect.X,
					expectedRect.Y < 0 ? expectedBitmap.Size.Height + expectedRect.Y : expectedRect.Y
				);
				(int x, int y) actualOffset = (
					actualRect.X < 0 ? actualBitmap.Size.Width + actualRect.X : actualRect.X,
					actualRect.Y < 0 ? actualBitmap.Size.Height + actualRect.Y : actualRect.Y
				);

				for (var x = 0; x < width; x++)
					for (var y = 0; y < height; y++)
					{
						Assert.AreEqual(
							expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y),
							actualBitmap.GetPixel(x + actualOffset.x, y + actualOffset.y),
							$"Pixel {x},{y}");
					}
			}
		}

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, IAppRect rect, int permittedPixelError)
			=> AreAlmostEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height), permittedPixelError);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, int permittedPixelError = 5)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MaxValue);
			AreAlmostEqual(expected, rect.Value, actual, rect.Value, permittedPixelError);
		}

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, int permittedPixelError)
			=> AreAlmostEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height),
				permittedPixelError);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int permittedPixelError)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				var width = Math.Min(expectedRect.Width, expectedBitmap.Size.Width);
				var height = Math.Min(expectedRect.Height, expectedBitmap.Size.Height);

				(int x, int y) expectedOffset = (
					expectedRect.X < 0 ? expectedBitmap.Size.Width + expectedRect.X : expectedRect.X,
					expectedRect.Y < 0 ? expectedBitmap.Size.Height + expectedRect.Y : expectedRect.Y
				);
				(int x, int y) actualOffset = (
					actualRect.X < 0 ? actualBitmap.Size.Width + actualRect.X : actualRect.X,
					actualRect.Y < 0 ? actualBitmap.Size.Height + actualRect.Y : actualRect.Y
				);

				for (var x = 0; x < width; x++)
					for (var y = 0; y < height; y++)
					{
						var expectedPixel = expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y);
						var actualPixel = actualBitmap.GetPixel(x + actualOffset.x, y + actualOffset.y);
						if (expectedPixel == actualPixel)
						{
							continue;
						}

						var cumulativeError = Abs(expectedPixel.R - actualPixel.R)
							+ Abs(expectedPixel.G - actualPixel.G)
							+ Abs(expectedPixel.B - actualPixel.B)
							+ Abs(expectedPixel.A - actualPixel.A);

						if (cumulativeError > permittedPixelError)
						{
							Assert.Fail($"Difference between expected={expectedPixel} and actual={actualPixel} at {x},{y} exceeds permitted error of {permittedPixelError}");
						}
					}
			}
		}

		public static void AreNotEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AreNotEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
		public static void AreNotEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MaxValue);
			AreNotEqual(expected, rect.Value, actual, rect.Value);
		}

		public static void AreNotEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AreNotEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public static void AreNotEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				var width = Math.Min(expectedRect.Width, expectedBitmap.Size.Width);
				var height = Math.Min(expectedRect.Height, expectedBitmap.Size.Height);

				(int x, int y) expectedOffset = (
					expectedRect.X < 0 ? expectedBitmap.Size.Width + expectedRect.X : expectedRect.X,
					expectedRect.Y < 0 ? expectedBitmap.Size.Height + expectedRect.Y : expectedRect.Y
				);
				(int x, int y) actualOffset = (
					actualRect.X < 0 ? actualBitmap.Size.Width + actualRect.X : actualRect.X,
					actualRect.Y < 0 ? actualBitmap.Size.Height + actualRect.Y : actualRect.Y
				);

				for (var x = 0; x < width; x++)
					for (var y = 0; y < height; y++)
					{
						if (expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y) != actualBitmap.GetPixel(x + actualOffset.x, y + actualOffset.y))
						{
							return;
						}
					}

				Assert.Fail("Screenshots are equals.");
			}
		}

		public static void HasColorAt(FileInfo screenshot, float x, float y, Color expectedColor, byte tolerance = 0)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				Assert.GreaterOrEqual(bitmap.Width, (int)x);
				Assert.GreaterOrEqual(bitmap.Height, (int)y);
				var pixel = bitmap.GetPixel((int)x, (int)y);

				var expected = ToArgbCode(expectedColor);
				var actual = ToArgbCode(pixel);

				//Convert to ARGB value, because 'named colors' are not considered equal to their unnamed equivalents(!)
				Assert.IsTrue(Math.Abs(pixel.A - expectedColor.A) <= tolerance, $"[{x},{y}] Alpha (expected: {expected} | actual: {actual})");
				Assert.IsTrue(Math.Abs(pixel.R - expectedColor.R) <= tolerance, $"[{x},{y}] Red (expected: {expected} | actual: {actual})");
				Assert.IsTrue(Math.Abs(pixel.G - expectedColor.G) <= tolerance, $"[{x},{y}] Green (expected: {expected} | actual: {actual})");
				Assert.IsTrue(Math.Abs(pixel.B - expectedColor.B) <= tolerance, $"[{x},{y}] Blue (expected: {expected} | actual: {actual})");
			}
		}

		public static void DoesNotHaveColorAt(FileInfo screenshot, float x, float y, Color excludedColor, byte tolerance = 0)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				Assert.GreaterOrEqual(bitmap.Width, (int)x);
				Assert.GreaterOrEqual(bitmap.Height, (int)y);
				var pixel = bitmap.GetPixel((int)x, (int)y);

				var excluded = ToArgbCode(excludedColor);
				var actual = ToArgbCode(pixel);

				//Convert to ARGB value, because 'named colors' are not considered equal to their unnamed equivalents(!)
				var equals = Math.Abs(pixel.A - excludedColor.A) <= tolerance
					&& Math.Abs(pixel.R - excludedColor.R) <= tolerance
					&& Math.Abs(pixel.G - excludedColor.G) <= tolerance
					&& Math.Abs(pixel.B - excludedColor.B) <= tolerance;

				Assert.IsFalse(equals, $"[{x},{y}] Alpha (excluded: {excluded} | actual: {actual})");
			}
		}
		private static string ToArgbCode(Color color)
			=> $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
