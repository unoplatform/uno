using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests._Utils;
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

		public static void HasColorAt(FileInfo screenshot, float x, float y, string expectedColorCode, byte tolerance = 0)
			=> HasColorAt(screenshot, x, y, ColorCodeParser.Parse(expectedColorCode), tolerance);

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

		public static void HasPixels(FileInfo screenshot, params ExpectedPixels[] expectations)
		{
			var isSuccess = true;
			var result = new StringBuilder();

			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				foreach (var expectation in expectations)
				{
					var x = expectation.X;
					var y = expectation.Y;

					Assert.GreaterOrEqual(bitmap.Width, x);
					Assert.GreaterOrEqual(bitmap.Height, y);

					result.AppendLine(expectation.Name);
					isSuccess &= Validate(bitmap, expectation, result);
					result.AppendLine();
				}

				if (!isSuccess)
				{
					Assert.Fail(result.ToString());
				}
			}
		}

		private static bool Validate(Bitmap bitmap, ExpectedPixels expectation, StringBuilder report)
		{
			var failMessage = default(string);
			for (var offsetX = 0; offsetX <= expectation.OffsetTolerance.x; offsetX++)
			for (var offsetY = 0; offsetY <= expectation.OffsetTolerance.y; offsetY++)
			{
				if (ValidatePixels(offsetX, offsetY)
					|| (offsetX > 0 && ValidatePixels(-offsetX, offsetY))
					|| (offsetX > 0 && offsetY > 0 && ValidatePixels(-offsetX, -offsetY))
					|| (offsetY > 0 && ValidatePixels(offsetX, -offsetY)))
				{
					report.AppendLine("OK");
					return true;
				}
			}

			report.AppendLine(failMessage);
			return false;

			bool ValidatePixels(int offsetX, int offsetY)
			{
				var isSuccess = true;
				var result = new StringBuilder();

				for (var lin = 0; lin < expectation.Values.GetLength(0); lin++)
				for (var col = 0; col < expectation.Values.GetLength(1); col++)
				{
					var expectedColor = expectation.Values[lin, col];
					if (expectedColor.IsEmpty)
					{
						continue;
					}

					var pixelX = expectation.X + col + offsetX;
					var pixelY = expectation.Y + lin + offsetY;
					var pixel = bitmap.GetPixel(pixelX, pixelY);

					var expected = ToArgbCode(expectedColor);
					var actual = ToArgbCode(pixel);

					//Convert to ARGB value, because 'named colors' are not considered equal to their unnamed equivalents(!)
					if (Math.Abs(pixel.A - expectedColor.A) > expectation.ColorTolerance)
					{
						isSuccess = false;
						result.AppendLine($"{col},{lin}: [{pixelX},{pixelY}] Alpha (expected: {expected} | actual: {actual})");
					}
					if (Math.Abs(pixel.R - expectedColor.R) > expectation.ColorTolerance)
					{
						isSuccess = false;
						result.AppendLine($"{col},{lin}: [{pixelX},{pixelY}] Red (expected: {expected} | actual: {actual})");
					}
					if (Math.Abs(pixel.G - expectedColor.G) > expectation.ColorTolerance)
					{
						isSuccess = false;
						result.AppendLine($"{col},{lin}: [{pixelX},{pixelY}] Green (expected: {expected} | actual: {actual})");
					}
					if (Math.Abs(pixel.B - expectedColor.B) > expectation.ColorTolerance)
					{
						isSuccess = false;
						result.AppendLine($"{col},{lin}: [{pixelX},{pixelY}] Blue (expected: {expected} | actual: {actual})");
					}
				}

				if (failMessage == default) // so we keep only for offset 0,0
				{
					failMessage = result.ToString();
				}

				return isSuccess;
			}
		}

		public static void DoesNotHaveColorAt(FileInfo screenshot, float x, float y, string excludedColorCode, byte tolerance = 0)
			=> DoesNotHaveColorAt(screenshot, x, y, ColorCodeParser.Parse(excludedColorCode), tolerance);

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

	public struct ExpectedPixels
	{
		public static ExpectedPixels At(string name, float x, float y)
			=> new ExpectedPixels(name, x, y, new Color[0, 0]);

		public static ExpectedPixels At(float x, float y, [CallerLineNumber] int line = -1)
			=> new ExpectedPixels($"at line: {line}", x, y, new Color[0,0]);

		public ExpectedPixels Pixels(string[,] pixels) => new ExpectedPixels(Name, X, Y, pixels, ColorTolerance, OffsetTolerance);

		public ExpectedPixels WithColorTolerance(byte tolerance) => new ExpectedPixels(Name, X, Y, Values, tolerance, OffsetTolerance);

		public ExpectedPixels WithPixelTolerance(int x = 0, int y = 0) => new ExpectedPixels(Name, X, Y, Values, ColorTolerance, (x, y));

		public ExpectedPixels(string name, float x, float y, Color[,] pixels, byte colorTolerance = 0, (int x, int y) offsetTolerance = default)
		{
			Name = name;
			X = (int)x;
			Y = (int)y;
			Values = pixels;
			ColorTolerance = colorTolerance;
			OffsetTolerance = offsetTolerance;
		}

		public ExpectedPixels(string name, float x, float y, string[,] pixels, byte colorTolerance = 0, (int x, int y) offsetTolerance = default)
		{
			var colors = new Color[pixels.GetLength(0), pixels.GetLength(1)];
			for (var py = 0; py < pixels.GetLength(0); py++)
			for (var px = 0; px < pixels.GetLength(1); px++)
			{
				var colorCode = pixels[py, px];
				colors[py, px] = string.IsNullOrWhiteSpace(colorCode)
					? Color.Empty
					: ColorCodeParser.Parse(colorCode);
			}

			Name = name;
			X = (int)x;
			Y = (int)y;
			Values = colors;
			ColorTolerance = colorTolerance;
			OffsetTolerance = offsetTolerance;
		}

		public string Name { get; set; }
		public int X { get; }
		public int Y { get; }
		public Color[,] Values { get; }
		public byte ColorTolerance { get; }
		public (int x, int y) OffsetTolerance { get; }
	}
}
