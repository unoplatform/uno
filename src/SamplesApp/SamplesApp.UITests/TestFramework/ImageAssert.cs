#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
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
		public static void AreEqual(ScreenshotInfo expected, ScreenshotInfo actual, IAppRect rect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, ScreenshotInfo actual, Rectangle? rect = null, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, IAppRect expectedRect, ScreenshotInfo actual, IAppRect actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, tolerance, line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(ScreenshotInfo expected, ScreenshotInfo actual, IAppRect rect, int permittedPixelError, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), expectedToActualScale, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(ScreenshotInfo expected, ScreenshotInfo actual, Rectangle? rect = null, int permittedPixelError = 5, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, expectedToActualScale, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(ScreenshotInfo expected, IAppRect expectedRect, ScreenshotInfo actual, IAppRect actualRect, int permittedPixelError, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), expectedToActualScale, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, int permittedPixelError, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, PixelTolerance.Cummulative(permittedPixelError), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, tolerance, line);

		public static void AreAlmostEqual(ScreenshotInfo expected, Rectangle expectedRect, Bitmap actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, null, actual, actualRect, expectedToActualScale, tolerance, line);

		private static void AreEqualImpl(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo actual,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			[CallerLineNumber] int line = 0)
		{
			var actualBitmap = actual.GetBitmap();
			AreEqualImpl(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);
		}

		private static void AreEqualImpl(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo actual,
			Bitmap actualBitmap,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			[CallerLineNumber] int line = 0)
		{
			using var assertionScope = new AssertionScope($"{expected.StepName}<=={actual}");
			assertionScope.AddReportable("expectedRect", expectedRect.ToString());
			assertionScope.AddReportable("actualRect", actualRect.ToString());
			assertionScope.AddReportable("expectedToActualScale", expectedToActualScale.ToString(NumberFormatInfo.InvariantInfo));

			var (areEqual, context) = EqualityCheck(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);

			if (areEqual)
			{
				Console.WriteLine(context.ToString());
			}
			else
			{
				assertionScope.FailWithText(context);
			}
		}

		private static (bool areEqual, string context) EqualityCheck(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo actual,
			Bitmap actualBitmap,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			[CallerLineNumber] int line = 0)
		{
			var expectedBitmap = expected.GetBitmap();

			if (expectedRect != FirstQuadrant && actualRect != FirstQuadrant)
			{
				Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));
			}

			if (expectedRect == FirstQuadrant && actualRect == FirstQuadrant)
			{
				var effectiveExpectedBitmapSize = new Size(
					(int)(expectedBitmap.Size.Width * expectedToActualScale),
					(int)(expectedBitmap.Size.Height * expectedToActualScale));
				Assert.AreEqual(effectiveExpectedBitmapSize, actualBitmap.Size, WithContext("Screenshots don't have the same size"));
			}

			expectedRect = Normalize(expectedRect, expectedBitmap.Size);
			actualRect = Normalize(actualRect, actualBitmap.Size);

			var expectedPixels = ExpectedPixels
				.At(actualRect.Location)
				.Pixels(expectedBitmap, expectedRect)
				.Named(expected.StepName)
				.WithTolerance(tolerance);

			var report = GetContext();
			var result = Validate(expectedPixels, actualBitmap, expectedToActualScale, report);

			return (result, report.ToString());

			StringBuilder GetContext()
				=> new StringBuilder()
					.AppendLine($"ImageAssert.AreEqual @ line {line}")
					.AppendLine("pixelTolerance: " + tolerance)
					.AppendLine($"expected: {expected?.StepName} ({expected?.File.Name} {expectedBitmap.Size}){(expectedRect == FirstQuadrant ? null : $" in {expectedRect}")}")
					.AppendLine($"actual  : {actual?.StepName ?? "--unknown--"} ({actual?.File.Name} {actualBitmap.Size}){(actualRect == FirstQuadrant ? null : $" in {actualRect}")}")
					.AppendLine("====================");

			string WithContext(string message)
				=> GetContext()
					.AppendLine(message)
					.ToString();
		}
		#endregion

		#region AreNotEqual
		public static void AreNotEqual(ScreenshotInfo expected, ScreenshotInfo actual, IAppRect rect, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), expectedToActualScale, PixelTolerance.None, line);

		public static void AreNotEqual(ScreenshotInfo expected, ScreenshotInfo actual, Rectangle? rect = null, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, expectedToActualScale, PixelTolerance.None, line);

		public static void AreNotEqual(ScreenshotInfo expected, IAppRect expectedRect, ScreenshotInfo actual, IAppRect actualRect, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), expectedToActualScale, PixelTolerance.None, line);

		public static void AreNotEqual(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, double expectedToActualScale = 1, [CallerLineNumber] int line = 0)
			=> AreNotEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, PixelTolerance.None, line);

		private static void AreNotEqualImpl(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo actual,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			int line)
		{
			var actualBitmap = actual.GetBitmap();
			AreNotEqualImpl(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);
		}

		private static void AreNotEqualImpl(
					ScreenshotInfo expected,
					Rectangle expectedRect,
					ScreenshotInfo actual,
					Bitmap actualBitmap,
					Rectangle actualRect,
					double expectedToActualScale,
					PixelTolerance tolerance,
					int line)
		{
			var result = EqualityCheck(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);

			if (!result.areEqual)
			{
				Console.WriteLine(result.context);
			}
			else
			{
				AssertionScope.Current.FailWithText(result.context);
			}
		}
		#endregion

		#region HasColorAt
		public static void HasColorAt(ScreenshotInfo screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, ColorCodeParser.Parse(expectedColorCode), tolerance, line);

		public static void HasColorAt(ScreenshotInfo screenshot, float x, float y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, line);

		private static void HasColorAtImpl(ScreenshotInfo screenshot, int x, int y, Color expectedColor, byte tolerance, int line)
		{
			var bitmap = screenshot.GetBitmap();

			if (bitmap.Width <= x || bitmap.Height <= y)
			{
				Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
			}

			var pixel = bitmap.GetPixel(x, y);

			if (!AreSameColor(expectedColor, pixel, tolerance, out var difference))
			{
				Assert.Fail(WithContext(builder: builder => builder
					.AppendLine($"Color at ({x},{y}) is not expected")
					.AppendLine($"expected: {ToArgbCode(expectedColor)} {expectedColor}")
					.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
					.AppendLine($"tolerance: {tolerance}")
					.AppendLine($"difference: {difference}")
				));
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.HasColorAt @ line {line}")
					.AppendLine($"screenshot: {screenshot.StepName} ({screenshot.File.Name})")
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region DoesNotHaveColorAt
		public static void DoesNotHaveColorAt(ScreenshotInfo screenshot, float x, float y, string excludedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, ColorCodeParser.Parse(excludedColorCode), tolerance, line);

		public static void DoesNotHaveColorAt(ScreenshotInfo screenshot, float x, float y, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, excludedColor, tolerance, line);

		private static void DoesNotHaveColorAtImpl(ScreenshotInfo screenshot, int x, int y, Color excludedColor, byte tolerance, int line)
		{
			var bitmap = screenshot.GetBitmap();
			if (bitmap.Width <= x || bitmap.Height <= y)
			{
				Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
			}

			var pixel = bitmap.GetPixel(x, y);
			if (AreSameColor(excludedColor, pixel, tolerance, out var difference))
			{
				Assert.Fail(WithContext(builder: builder => builder
					.AppendLine($"Color at ({x},{y}) is not expected")
					.AppendLine($"excluded: {ToArgbCode(excludedColor)} {excludedColor.Name}")
					.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
					.AppendLine($"tolerance: {tolerance}")
					.AppendLine($"difference: {difference}")
				));
			}

			string WithContext(string message = null, Action<StringBuilder> builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.DoesNotHaveColorAt @ line {line}")
					.AppendLine($"screenshot: {screenshot.StepName} ({screenshot.File.Name})")
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
		#endregion

		#region HasPixels
		public static void HasPixels(ScreenshotInfo actual, params ExpectedPixels[] expectations)
		{
			var bitmap = actual.GetBitmap();
			using var assertionScope = new AssertionScope("ImageAssert");

			foreach (var expectation in expectations)
			{
				var x = expectation.Location.X;
				var y = expectation.Location.Y;

				Assert.GreaterOrEqual(bitmap.Width, x);
				Assert.GreaterOrEqual(bitmap.Height, y);

				var result = new StringBuilder();
				result.AppendLine(expectation.Name);
				if (!Validate(expectation, bitmap, 1, result))
				{
					assertionScope.FailWith(result.ToString());
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
				if (offsetX > 0)
				{
					yield return (-offsetX, offsetY);
				}

				if (offsetY > 0)
				{
					yield return (offsetX, -offsetY);
				}

				if (offsetX > 0 && offsetY > 0)
				{
					yield return (-offsetX, -offsetY);
				}
			}
		}

		private static IEnumerable<Point> GetPixelCoordinates(ExpectedPixels expectation)
		{
			var stepX = (int)Math.Max(1, expectation.Tolerance.DiscreteValidation.x);
			var stepY = (int)Math.Max(1, expectation.Tolerance.DiscreteValidation.y);

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
			if (AreSameColor(expectedColor, actualColor, expectation.Tolerance.Color, out var difference, expectation.Tolerance.ColorKind))
			{
				return true;
			}

			if (report != null && offset == default) // Generate report only for offset 0,0
			{
				// If possible we dump the location in the source coordinates space.
				var expectedLocation = expectation.SourceLocation.HasValue
					? $"[{expectation.SourceLocation.Value.X + pixel.X},{expectation.SourceLocation.Value.Y + pixel.Y}] "
					: "";

				report.AppendLine($"{pixel.X},{pixel.Y}: expected: {expectedLocation}{ToArgbCode(expectedColor)} | actual: [{actualX},{actualY}] {ToArgbCode(actualColor)}");
				report.AppendLine($"\ta tolerance of {difference} [{expectation.Tolerance.ColorKind}] would be required for this test to pass.");
				report.AppendLine($"\tCurrent: {expectation.Tolerance}");

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

		private static bool AreSameColor(Color a, Color b, byte tolerance, out int currentDifference, ColorToleranceKind kind = ColorToleranceKind.Exclusive)
		{
			switch (kind)
			{
				case ColorToleranceKind.Cumulative:
				{
					currentDifference =
						Abs(a.A - b.A) +
						Abs(a.R - b.R) +
						Abs(a.G - b.G) +
						Abs(a.B - b.B);
					return currentDifference < tolerance;
				}

				case ColorToleranceKind.Exclusive:
				{
					// comparing ARGB values, because 'named colors' are not considered equal to their unnamed equivalents(!)
					var va = Abs(a.A - b.A);
					var vr = Abs(a.R - b.R);
					var vg = Abs(a.G - b.G);
					var vb = Abs(a.B - b.B);

					currentDifference = Max(Max(va, vr), Max(vg, vb));

					return currentDifference <= tolerance;
				}

				default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private static string ToArgbCode(Color color)
			=> $"{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
