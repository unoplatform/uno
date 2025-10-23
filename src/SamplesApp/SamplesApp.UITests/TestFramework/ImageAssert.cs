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
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests._Utils;
using Uno.UITest;
using static System.Math;
using SkiaSharp;

namespace SamplesApp.UITests.TestFramework
{
	public static partial class ImageAssert
	{
		private static bool IgnoreImageAsserts { get; } = false;

		static ImageAssert()
		{
			// Provides the ability to mark as ignored tests using ImageAssert.
			// See https://github.com/unoplatform/uno/issues/9550
			if (bool.TryParse(Environment.GetEnvironmentVariable("UNO_UITEST_IGNORE_IMAGEASSERTS"), out var ignoreImageAsserts))
			{
				IgnoreImageAsserts = ignoreImageAsserts;
			}
		}

		public static Rectangle FirstQuadrant { get; } = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

		#region Are[Almost]Equal
		public static void AreEqual(ScreenshotInfo expected, ScreenshotInfo actual, IAppRect rect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), expectedToActualScale, tolerance, line);
		public static async Task AreEqualAsync(ScreenshotInfo expected, ScreenshotInfo actual, IAppRect rect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, ScreenshotInfo actual, Rectangle? rect = null, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, expectedToActualScale, tolerance, line);

		public static async Task AreEqualAsync(ScreenshotInfo expected, ScreenshotInfo actual, Rectangle? rect = null, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, IAppRect expectedRect, ScreenshotInfo actual, IAppRect actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), expectedToActualScale, tolerance, line);

		public static async Task AreEqualAsync(ScreenshotInfo expected, IAppRect expectedRect, ScreenshotInfo actual, IAppRect actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), expectedToActualScale, tolerance, line);

		public static void AreEqual(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
			=> AreEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, tolerance, line);

		public static async Task AreEqualAsync(ScreenshotInfo expected, Rectangle expectedRect, ScreenshotInfo actual, Rectangle actualRect, double expectedToActualScale = 1, PixelTolerance tolerance = default, [CallerLineNumber] int line = 0)
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

		public static void AreAlmostEqual(ScreenshotInfo expected, Rectangle expectedRect, PlatformBitmap actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
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
			TryIgnoreImageAssert();

			var actualBitmap = actual.GetBitmap();
			AreEqualImpl(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);
		}


		private static void AreEqualImpl(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo? actual,
			PlatformBitmap actualBitmap,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			[CallerLineNumber] int line = 0)
		{
			TryIgnoreImageAssert();

			using var assertionScope = new AssertionScope($"{expected.StepName}<=={actual}");
			var assertionChain = AssertionChain.GetOrCreate();
			assertionChain.AddReportable("expectedRect", expectedRect.ToString());
			assertionChain.AddReportable("actualRect", actualRect.ToString());
			assertionChain.AddReportable("expectedToActualScale", expectedToActualScale.ToString(NumberFormatInfo.InvariantInfo));

			var (areEqual, context) = EqualityCheck(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);

			if (areEqual)
			{
				Console.WriteLine(context.ToString());
			}
			else
			{
				assertionChain.FailWithText(context);
			}
		}

		private static (bool areEqual, string context) EqualityCheck(
			ScreenshotInfo expected,
			Rectangle expectedRect,
			ScreenshotInfo? actual,
			PlatformBitmap actualBitmap,
			Rectangle actualRect,
			double expectedToActualScale,
			PixelTolerance tolerance,
			[CallerLineNumber] int line = 0)
		{
			TryIgnoreImageAssert();

			var expectedBitmap = expected.GetBitmap();

			if (expectedRect != FirstQuadrant && actualRect != FirstQuadrant)
			{
				Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));
			}

			if (expectedRect == FirstQuadrant && actualRect == FirstQuadrant)
			{
				var effectiveExpectedBitmapSize = new Size(
					(int)(expectedBitmap.Width * expectedToActualScale),
					(int)(expectedBitmap.Height * expectedToActualScale));
				Assert.AreEqual(effectiveExpectedBitmapSize, new Size(actualBitmap.Width, actualBitmap.Height), WithContext("Screenshots don't have the same size"));
			}

			expectedRect = Normalize(expectedRect, new(expectedBitmap.Width, expectedBitmap.Height));
			actualRect = Normalize(actualRect, new(actualBitmap.Width, actualBitmap.Height));

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
					.AppendLine($"expected: {expected?.StepName} ({expected?.File.Name} {expectedBitmap.Width}x{expectedBitmap.Height}){(expectedRect == FirstQuadrant ? null : $" in {expectedRect}")}")
					.AppendLine($"actual  : {actual?.StepName ?? "--unknown--"} ({actual?.File.Name} {actualBitmap.Width}x{actualBitmap.Height}){(actualRect == FirstQuadrant ? null : $" in {actualRect}")}")
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
					PlatformBitmap actualBitmap,
					Rectangle actualRect,
					double expectedToActualScale,
					PixelTolerance tolerance,
					int line)
		{
			TryIgnoreImageAssert();

			var result = EqualityCheck(expected, expectedRect, actual, actualBitmap, actualRect, expectedToActualScale, tolerance, line);

			if (!result.areEqual)
			{
				Console.WriteLine(result.context);
			}
			else
			{
				AssertionChain.GetOrCreate()
					.FailWithText(result.context);
			}
		}
		#endregion

		#region HasColorAt
		public static void HasColorAt(ScreenshotInfo screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, double scale = 1.0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, ColorCodeParser.Parse(expectedColorCode), tolerance, scale, line);

		public static void HasColorAt(ScreenshotInfo screenshot, float x, float y, Color expectedColor, byte tolerance = 0, double scale = 1.0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, scale, line);

		/// <summary>
		/// Asserts that a given screenshot has a color anywhere at a given rectangle.
		/// </summary>
		public static void HasColorInRectangle(ScreenshotInfo screenshot, Rectangle rect, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		{
			TryIgnoreImageAssert();

			var bitmap = screenshot.GetBitmap();
			for (var x = rect.Left; x < rect.Right; x++)
			{
				for (var y = rect.Top; y < rect.Bottom; y++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (AreSameColor(expectedColor, pixel, tolerance, out _))
					{
						return;
					}
				}
			}

			Assert.Fail($"Expected '{ToArgbCode(expectedColor)}' in rectangle '{rect}'.");
		}

		/// <summary>
		/// Asserts that a given screenshot has a color anywhere at a given rectangle.
		/// </summary>
		public static void DoesNotHaveColorInRectangle(ScreenshotInfo screenshot, Rectangle rect, Color unexpectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		{
			TryIgnoreImageAssert();

			var bitmap = screenshot.GetBitmap();
			for (var x = rect.Left; x < rect.Right; x++)
			{
				for (var y = rect.Top; y < rect.Bottom; y++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (AreSameColor(unexpectedColor, pixel, tolerance, out _))
					{
						Assert.Fail($"Unexpected '{ToArgbCode(unexpectedColor)}' in rectangle '{rect}'.");
					}
				}
			}
		}

		private static void HasColorAtImpl(ScreenshotInfo screenshot, int x, int y, Color expectedColor, byte tolerance, double scale, int line)
		{
			TryIgnoreImageAssert();

			var bitmap = screenshot.GetBitmap();

			x = (int)(x * scale);
			y = (int)(y * scale);

			if (bitmap.Width <= x || bitmap.Height <= y)
			{
				Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Width}x{bitmap.Height}"));
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

			string WithContext(string? message = null, Action<StringBuilder>? builder = null)
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
			TryIgnoreImageAssert();

			var bitmap = screenshot.GetBitmap();
			if (bitmap.Width <= x || bitmap.Height <= y)
			{
				Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Width}x{bitmap.Height}"));
			}

			var pixel = bitmap.GetPixel(x, y);
			if (AreSameColor(excludedColor, pixel, tolerance, out var difference))
			{
				Assert.Fail(WithContext(builder: builder => builder
					.AppendLine($"Color at ({x},{y}) is not expected")
					.AppendLine($"excluded: {ToArgbCode(excludedColor)}")
					.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
					.AppendLine($"tolerance: {tolerance}")
					.AppendLine($"difference: {difference}")
				));
			}

			string WithContext(string? message = null, Action<StringBuilder>? builder = null)
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
			TryIgnoreImageAssert();

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
					AssertionChain.GetOrCreate()
						.FailWith(result.ToString());
				}
			}
		}
		#endregion


		/// <summary>
		/// See https://github.com/unoplatform/uno/issues/9550
		/// </summary>
		internal static void TryIgnoreImageAssert()
		{
			if (IgnoreImageAsserts)
			{
				Assert.Ignore("UNO_UITEST_IGNORE_IMAGEASSERTS was set, ignoring test which uses ImageAssert");
			}
		}
	}
}
