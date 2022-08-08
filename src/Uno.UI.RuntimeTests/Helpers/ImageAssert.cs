#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Uno.UITest;
using Windows.UI;
using static System.Math;

using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using SamplesApp.UITests;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	/// <summary>
	/// Screenshot based assertions, to validate individual colors of an image
	/// </summary>
	public static class ImageAssert
	{
#region HasColorAt
		public static Task HasColorAt(RawBitmap screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

		public static Task HasColorAt(RawBitmap screenshot, float x, float y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, line);

		public static Task HasColorAtChild(RawBitmap screenshot, UIElement child, double x, double y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> HasColorAtChild(screenshot, child, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);
		
		public static async Task HasColorAtChild(RawBitmap screenshot, UIElement child, double x, double y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		{
			var point = child.TransformToVisual(screenshot.RenderedElement).TransformPoint(new Windows.Foundation.Point(x, y));
			HasColorAtImpl(screenshot, (int)point.X, (int)point.Y, expectedColor, tolerance, line);
		}

		/// <summary>
		/// Asserts that a given screenshot has a color anywhere at a given rectangle.
		/// </summary>
		public static void HasColorInRectangle(RawBitmap screenshot, Rectangle rect, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		{
			var bitmap = screenshot;
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

		private static async Task HasColorAtImpl(RawBitmap screenshot, int x, int y, Color expectedColor, byte tolerance, int line)
		{
			await screenshot.Populate();

			var bitmap = screenshot;

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

			string WithContext(string? message = null, Action<StringBuilder>? builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.HasColorAt @ line {line}")
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
#endregion

#region DoesNotHaveColorAt
		public static Task DoesNotHaveColorAt(RawBitmap screenshot, float x, float y, string excludedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), excludedColorCode), tolerance, line);

		public static Task DoesNotHaveColorAt(RawBitmap screenshot, float x, float y, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
			=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, excludedColor, tolerance, line);

		private static async Task DoesNotHaveColorAtImpl(RawBitmap screenshot, int x, int y, Color excludedColor, byte tolerance, int line)
		{
			var bitmap = screenshot;
			await bitmap.Populate();

			if (bitmap.Width <= x || bitmap.Height <= y)
			{
				Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
			}

			var pixel = bitmap.GetPixel(x, y);
			if (AreSameColor(excludedColor, pixel, tolerance, out var difference))
			{
				Assert.Fail(WithContext(builder: builder => builder
					.AppendLine($"Color at ({x},{y}) is not expected")
					.AppendLine($"excluded: {ToArgbCode(excludedColor)} {ColorHelper.ToDisplayName(excludedColor)}")
					.AppendLine($"actual  : {ToArgbCode(pixel)} {pixel}")
					.AppendLine($"tolerance: {tolerance}")
					.AppendLine($"difference: {difference}")
				));
			}

			string WithContext(string? message = null, Action<StringBuilder>? builder = null)
			{
				return new StringBuilder()
					.AppendLine($"ImageAssert.DoesNotHaveColorAt @ line {line}")
					.AppendLine("====================")
					.ApplyIf(message != null, sb => sb.AppendLine(message))
					.ApplyIf(builder != null, builder)
					.ToString();
			}
		}
#endregion

#region HasPixels
		public static async Task HasPixels(RawBitmap actual, params ExpectedPixels[] expectations)
		{
			await actual.Populate();

			var bitmap = actual;
			using var assertionScope = new AssertionScope("ImageAssert");

			foreach (var expectation in expectations)
			{
				var x = expectation.Location.X;
				var y = expectation.Location.Y;

				Assert.IsTrue(bitmap.Width >= x);
				Assert.IsTrue(bitmap.Height >= y);

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
		private static bool Validate(ExpectedPixels expectation, RawBitmap actualBitmap, double expectedToActualScale, StringBuilder report)
		{
			foreach (var pixels in expectation.GetAllPossibilities())
			{
				report.AppendLine($"{pixels.Name}:");

				if (pixels.Values is null)
				{
					report.AppendLine("INVALID EXPECTATION: No pixels defined.");
					continue;
				}

				bool isSuccess;
				switch (pixels.Tolerance.OffsetKind)
				{
					case LocationToleranceKind.PerRange:
						isSuccess = GetLocationOffsets(pixels)
							.Any(offset => GetPixelCoordinates(pixels)
								.All(pixel => ValidatePixel(actualBitmap, pixels, expectedToActualScale, pixel, offset, report)));
						break;

					case LocationToleranceKind.PerPixel:
						isSuccess = GetPixelCoordinates(pixels)
							.All(pixel => GetLocationOffsets(pixels)
								.Any(offset => ValidatePixel(actualBitmap, pixels, expectedToActualScale, pixel, offset, report)));
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(pixels.Tolerance.OffsetKind));
				}

				if (isSuccess) // otherwise the report has already been full-filled
				{
					report?.AppendLine("\tOK");
					return isSuccess;
				}
			}

			return false;
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
			RawBitmap actualBitmap,
			ExpectedPixels expectation,
			double expectedToActualScale,
			Point pixel,
			(int x, int y) offset,
			StringBuilder report)
		{
			var expectedColor = expectation.Values[pixel.Y, pixel.X];
			if (expectedColor == Colors.Transparent)
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
			=> new (
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
