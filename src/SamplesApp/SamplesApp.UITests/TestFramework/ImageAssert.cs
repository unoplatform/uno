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
		private static readonly Rectangle FirstQuadrant = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

		#region AreEqual
		public static void AreEqual(FileInfo expected, FileInfo actual, IAppRect rect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), line);

		public static void AreEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, line);

		public static void AreEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), line);

		public static void AreEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, line);

		private static void AreEqualImpl(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int line)
		{
			Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size, actualBitmap.Size, WithContext("Screenshots don't have the same size"));

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
						var expectedColor = expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y);
						var actualColor = actualBitmap.GetPixel(x + actualOffset.x, y + actualOffset.y);

						if (expectedColor != actualColor)
						{
							Assert.Fail(WithContext(builder: builder => builder
								.AppendLine($"Pixels at ({x}, {y}) are not the same")
								.AppendLine($"expectedColor: {ToArgbCode(expectedColor)} {expectedColor}")
								.AppendLine($"actualColor  : {ToArgbCode(actualColor)} {actualColor}")
							));
						}
					}
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.AreEqual @ line {line}")
					.AppendLine("expected: " + expected?.Name + (expectedRect == FirstQuadrant ? null : $"in {expectedRect}"))
					.AppendLine("actual  : " + actual?.Name + (actualRect == FirstQuadrant ? null : $"in {actualRect}"))
					.AppendLine("====================")
					.ApplyIf(message != null, x => x.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region AreAlmostEqual
		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, IAppRect rect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), permittedPixelError, line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, int permittedPixelError = 5, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, permittedPixelError, line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), permittedPixelError, line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, expectedRect, actual, actualRect, permittedPixelError, line);

		private static void AreAlmostEqualImpl(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int permittedPixelError, int line)
		{
			Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size, actualBitmap.Size, WithContext("Screenshots don't have the same size"));

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

						var cumulativeError = GetColorDistance(expectedPixel, actualPixel);
						if (cumulativeError > permittedPixelError)
						{
							Assert.Fail(WithContext(builder: builder => builder
								.AppendLine($"Difference between pixels at ({x},{y}) exceeds permitted error of {permittedPixelError}")
								.AppendLine($"expectedPixel: {ToArgbCode(expectedPixel)} {expectedPixel}")
								.AppendLine($"actualPixel  : {ToArgbCode(actualPixel)} {actualPixel}")
								.AppendLine($"permittedPixelError: {permittedPixelError}")
								.AppendLine($"cumulativeError    : {cumulativeError}")
							));
						}
					}
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.AreAlmostEqual @ line {line}")
					.AppendLine("permittedPixelError: " + permittedPixelError)
					.AppendLine("expected: " + expected?.Name + (expectedRect == FirstQuadrant ? null : $"in {expectedRect}"))
					.AppendLine("actual  : " + actual?.Name + (actualRect == FirstQuadrant ? null : $"in {actualRect}"))
					.AppendLine("====================")
					.ApplyIf(message != null, x => x.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region AreNotEqual
		public static void AreNotEqual(FileInfo expected, FileInfo actual, IAppRect rect, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), line);

		public static void AreNotEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, line);

		public static void AreNotEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), line);

		public static void AreNotEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, expectedRect, actual, actualRect, line);

		private static void AreNotEqualImpl(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int line)
		{
			Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size, actualBitmap.Size, WithContext("Screenshots don't have the same size"));

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

				Assert.Fail(WithContext("Screenshots are equals."));
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.AreNotEqual @ line {line}")
					.AppendLine("expected: " + expected?.Name + (expectedRect == FirstQuadrant ? null : $"in {expectedRect}"))
					.AppendLine("actual  : " + actual?.Name + (actualRect == FirstQuadrant ? null : $"in {actualRect}"))
					.AppendLine("====================")
					.ApplyIf(message != null, x => x.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region HasColorAt
		public static void HasColorAt(FileInfo screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, ColorCodeParser.Parse(expectedColorCode), tolerance, line);

		public static void HasColorAt(FileInfo screenshot, float x, float y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, line);

		private static void HasColorAtImpl(FileInfo screenshot, int x, int y, Color expectedColor, byte tolerance, int line)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				if (bitmap.Width <= x || bitmap.Height <= y)
				{
					Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
				}

				var pixel = bitmap.GetPixel(x, y);

				if (!AreSameColor(expectedColor, pixel))
				{
					Assert.Fail(WithContext(builder: builder => builder
						.AppendLine($"Color at ({x},{y}) is not expected")
						.AppendLine($"expected: {ToArgbCode(expectedColor)} {expectedColor}")
						.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
						.AppendLine($"tolerance: {tolerance}")
					));
				}
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.HasColorAt @ line {line}")
					.AppendLine("screenshot: " + screenshot?.Name)
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region DoesNotHaveColorAt
		public static void DoesNotHaveColorAt(FileInfo screenshot, float x, float y, string excludedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, ColorCodeParser.Parse(excludedColorCode), tolerance, line);

		public static void DoesNotHaveColorAt(FileInfo screenshot, float x, float y, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, excludedColor, tolerance, line);

		private static void DoesNotHaveColorAtImpl(FileInfo screenshot, int x, int y, Color excludedColor, byte tolerance, int line)
		{
			using (var bitmap = new Bitmap(screenshot.FullName))
			{
				if (bitmap.Width <= x || bitmap.Height <= y)
				{
					Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
				}

				var pixel = bitmap.GetPixel(x, y);
				if (AreSameColor(excludedColor, pixel, tolerance))
				{
					Assert.Fail(WithContext(builder: builder => builder
						.AppendLine($"Color at ({x},{y}) is not expected")
						.AppendLine($"excluded: {ToArgbCode(excludedColor)} {excludedColor.Name}")
						.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
						.AppendLine($"tolerance: {tolerance}")
					));
				}
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.DoesNotHaveColorAt @ line {line}")
					.AppendLine("screenshot: " + screenshot?.Name)
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region HasPixels
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

						if (!AreSameColor(expectedColor, pixel, expectation.ColorTolerance))
						{
							isSuccess = false;
							result.AppendLine($"{col},{lin}: [{pixelX},{pixelY}] (expected: {expected} | actual: {actual})");
						}
					}

				if (failMessage == default) // so we keep only for offset 0,0
				{
					failMessage = result.ToString();
				}

				return isSuccess;
			}
		}
		#endregion

		private static int GetColorDistance(Color a, Color b)
		{
			return
				Abs(a.A - b.A) +
				Abs(a.R - b.R) +
				Abs(a.G - b.G) +
				Abs(a.B - b.B);
		}

		private static bool AreSameColor(Color a, Color b, byte tolerance = 0)
		{
			// comparing ARGB values, because 'named colors' are not considered equal to their unnamed equivalents(!)
			return
				Abs(a.A - b.A) <= tolerance &&
				Abs(a.R - b.R) <= tolerance &&
				Abs(a.G - b.G) <= tolerance &&
				Abs(a.B - b.B) <= tolerance;
		}

		private static string ToArgbCode(Color color)
			=> $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	public struct ExpectedPixels
	{
		public static ExpectedPixels At(string name, float x, float y)
			=> new ExpectedPixels(name, x, y, new Color[0, 0]);

		public static ExpectedPixels At(float x, float y, [CallerLineNumber] int line = -1)
			=> new ExpectedPixels($"at line: {line}", x, y, new Color[0, 0]);

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
