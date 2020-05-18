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
		public static Rectangle FirstQuadrant { get; } = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

		#region Are[Almost]Equal
		public static void AreEqual(FileInfo expected, FileInfo actual, IAppRect rect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), 1, PixelTolerance.None, line);

		public static void AreEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, 1, PixelTolerance.None, line);

		public static void AreEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), 1, PixelTolerance.None, line);

		public static void AreEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, 1, PixelTolerance.None, line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, IAppRect rect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), 1, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, int permittedPixelError = 5, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, 1, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), 1, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, 1, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, tolerance, line);

		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, Bitmap actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, null, actual, actualRect, expectedToActualScale, tolerance, line);

		private static void AreEqualImpl(
			FileInfo expected,
			Rectangle expectedRect,
			FileInfo actual,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			int line)
		{
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				AreEqualImpl(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);
			}
		}

		private static void AreEqualImpl(
			FileInfo expected,
			Rectangle expectedRect,
			FileInfo actual,
			Bitmap actualBitmap,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			int line)
		{
			if (expectedRect != FirstQuadrant && actualRect != FirstQuadrant)
			{
				Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));
			}

			using (var expectedBitmap = new Bitmap(expected.FullName))
			{
				if (expectedRect == FirstQuadrant && actualRect == FirstQuadrant)
				{
					Assert.AreEqual(expectedBitmap.Size, actualBitmap.Size, WithContext("Screenshots don't have the same size"));
				}

				expectedRect = Normalize(expectedRect, expectedBitmap.Size);
				actualRect = Normalize(actualRect, actualBitmap.Size);

				var expectedPixels = ExpectedPixels
					.At(actualRect.Location)
					.Pixels(expectedBitmap, expectedRect)
					.Named(expected.Name)
					.WithTolerance(tolerance);

				var report = GetContext();
				if (Validate(expectedPixels, actualBitmap, expectedToActualScale, report))
				{
					Console.WriteLine(report.ToString());
				}
				else
				{
					Assert.Fail(report.ToString());
				}
			}

			StringBuilder GetContext()
				=> new StringBuilder()
					.AppendLine($"ImageAssert.AreEqual @ line {line}")
					.AppendLine("pixelTolerance: " + tolerance)
					.AppendLine("expected: " + expected?.Name + (expectedRect == FirstQuadrant ? null : $" in {expectedRect}"))
					.AppendLine("actual  : " + (actual?.Name ?? "--unknown--") + (actualRect == FirstQuadrant ? null : $" in {actualRect}"))
					.AppendLine("====================");

			string WithContext(string message)
				=> GetContext()
					.AppendLine(message)
					.ToString();
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
		public static void HasPixels(FileInfo actual, params ExpectedPixels[] expectations)
		{
			var isSuccess = true;
			var result = new StringBuilder();

			using (var bitmap = new Bitmap(actual.FullName))
			{
				foreach (var expectation in expectations)
				{
					var x = expectation.Location.X;
					var y = expectation.Location.Y;

					Assert.GreaterOrEqual(bitmap.Width, x);
					Assert.GreaterOrEqual(bitmap.Height, y);

					result.AppendLine(expectation.Name);
					isSuccess &= Validate(expectation, bitmap, 1, result);
					result.AppendLine();
				}

				if (!isSuccess)
				{
					Assert.Fail(result.ToString());
				}
			}
		}
		#endregion

		#region Validation core (ExpectedPixels)
		private static bool Validate(ExpectedPixels expectation, Bitmap actualBitmap, double expectedToActualScale, StringBuilder report)
		{
			report?.AppendLine($"{expectation.Name}:");

			bool isSuccess;
			switch (expectation.Tolerance.OffsetKind)
			{
				case LocationToleranceKind.PerRange:
					isSuccess = GetLocationOffsets(expectation)
						.Any(offset => GetPixelCoordinates(expectation)
							.All(pixel => ValidatePixel(actualBitmap, expectation, expectedToActualScale, pixel, offset, report)));
					break;

				case LocationToleranceKind.PerPixel:
					isSuccess = GetPixelCoordinates(expectation)
						.All(pixel => GetLocationOffsets(expectation)
							.Any(offset => ValidatePixel(actualBitmap, expectation, expectedToActualScale, pixel, offset, report)));
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(expectation.Tolerance.OffsetKind));
			}

			if (isSuccess) // otherwise the report has already been full-filled
			{
				report?.AppendLine("\tOK");
			}
			return isSuccess;
		}

		private static IEnumerable<(int x, int y)> GetLocationOffsets(ExpectedPixels expectation)
		{
			for (var offsetX = 0; offsetX <= expectation.Tolerance.Offset.x; offsetX++)
			for (var offsetY = 0; offsetY <= expectation.Tolerance.Offset.y; offsetY++)
			{
				yield return (offsetX, offsetY);
				if (offsetX > 0) yield return (-offsetX, offsetY);
				if (offsetY > 0) yield return (offsetX, -offsetY);
				if (offsetX > 0 && offsetY > 0) yield return (-offsetX, -offsetY);
			}
		}

		private static IEnumerable<Point> GetPixelCoordinates(ExpectedPixels expectation)
		{
			var stepX = (int)Math.Min(1, expectation.Tolerance.DiscreteValidation.x);
			var stepY = (int)Math.Min(1, expectation.Tolerance.DiscreteValidation.y);

			for (var lin = 0; lin < expectation.Values.GetLength(0); lin+= stepY)
			for (var col = 0; col < expectation.Values.GetLength(1); col+= stepX)
			{
				yield return new Point(col, lin);
			}
		}

		private static bool ValidatePixel(
			Bitmap actualBitmap,
			ExpectedPixels expectation,
			double expectedToActualScale,
			Point pixel,
			(int x, int y) offset,
			StringBuilder report = null)
		{
			var expectedColor = expectation.Values[pixel.Y, pixel.X];
			if (expectedColor.IsEmpty)
			{
				return true;
			}

			var actualX = (int) ((expectation.Location.X + pixel.X) * expectedToActualScale + offset.x);
			var actualY = (int) ((expectation.Location.Y + pixel.Y) * expectedToActualScale + offset.y);
			if (actualX < 0 || actualY < 0
				|| actualX >= actualBitmap.Width || actualY >= actualBitmap.Height)
			{
				return false;
			}

			var actualColor = actualBitmap.GetPixel(actualX, actualY);
			if (AreSameColor(expectedColor, actualColor, expectation.Tolerance.Color, expectation.Tolerance.ColorKind))
			{
				return true;
			}

			if (report != null && offset != default) // Generate report only for offset 0,0
			{
				// If possible we dump the location in the source coordinates space.
				var expectedLocation = expectation.SourceLocation.HasValue
					? $"[{expectation.SourceLocation.Value.X + pixel.X},{expectation.SourceLocation.Value.Y + pixel.Y}] "
					: "";

				report.AppendLine($"{pixel.X},{pixel.Y}: expected: {expectedLocation}{ToArgbCode(expectedColor)} | actual: [{actualX},{actualY}] {ToArgbCode(actualColor)}");
			}

			return false;
		}
		#endregion

		private static Rectangle Normalize(Rectangle rect, Size size)
			=> new Rectangle(
				rect.X < 0 ? size.Width + rect.X : rect.X,
				rect.Y < 0 ? size.Height + rect.Y : rect.Y,
				Math.Min(rect.Width, size.Width),
				Math.Min(rect.Height, size.Height));

		private static bool AreSameColor(Color a, Color b, byte tolerance = 0, ColorToleranceKind kind = ColorToleranceKind.Exclusive)
		{
			switch (kind)
			{
				case ColorToleranceKind.Cumulative:
					return
						Abs(a.A - b.A) +
						Abs(a.R - b.R) +
						Abs(a.G - b.G) +
						Abs(a.B - b.B) < tolerance;

				case ColorToleranceKind.Exclusive:
					// comparing ARGB values, because 'named colors' are not considered equal to their unnamed equivalents(!)
					return
						Abs(a.A - b.A) <= tolerance &&
						Abs(a.R - b.R) <= tolerance &&
						Abs(a.G - b.G) <= tolerance &&
						Abs(a.B - b.B) <= tolerance;

				default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private static string ToArgbCode(Color color)
			=> $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
