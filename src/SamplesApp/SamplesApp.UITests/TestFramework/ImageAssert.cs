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
			AreAlmostEqualImpl(expected, expectedRect, actual, actualRect, 1, PixelTolerance.None, line);
		}
		#endregion

		#region AreAlmostEqual
		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, IAppRect rect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, rect.ToRectangle(), actual, rect.ToRectangle(), 1, new PixelTolerance((byte)permittedPixelError, ColorToleranceKind.Cumulative, default), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null, int permittedPixelError = 5, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, rect ?? FirstQuadrant, actual, rect ?? FirstQuadrant, 1, new PixelTolerance((byte)permittedPixelError, ColorToleranceKind.Cumulative, default), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, expectedRect.ToRectangle(), actual, actualRect.ToRectangle(), 1, new PixelTolerance((byte)permittedPixelError, ColorToleranceKind.Cumulative, default), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, int permittedPixelError, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, expectedRect, actual, actualRect, 1, new PixelTolerance((byte)permittedPixelError, ColorToleranceKind.Cumulative, default), line);

		/// <summary>
		/// Asserts that two screenshots are equal to each other inside the area of the nominated rect to within the given pixel error
		/// (ie, the A, R, G, or B values cumulatively differ by more than the permitted error for any pixel).
		/// </summary>
		public static void AreAlmostEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, [CallerLineNumber] int line = 0)
			=> AreAlmostEqualImpl(expected, expectedRect, actual, actualRect, expectedToActualScale, tolerance, line);

		private static void AreAlmostEqualImpl(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect, double expectedToActualScale, PixelTolerance tolerance, int line)
		{
			if (expectedRect != FirstQuadrant && actualRect != FirstQuadrant)
			{
				Assert.AreEqual(expectedRect.Size, actualRect.Size, WithContext("Compare rects don't have the same size"));
			}

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				if (expectedRect == FirstQuadrant && actualRect == FirstQuadrant)
				{
					Assert.AreEqual(expectedBitmap.Size, actualBitmap.Size, WithContext("Screenshots don't have the same size"));
				}

				//var width = Math.Min(expectedRect.Width, expectedBitmap.Size.Width);
				//var height = Math.Min(expectedRect.Height, expectedBitmap.Size.Height);

				expectedRect = Normalize(expectedRect, expectedBitmap.Size);
				actualRect = Normalize(actualRect, actualBitmap.Size);

				//(int x, int y) expectedOffset = (
				//	expectedRect.X < 0 ? expectedBitmap.Size.Width + expectedRect.X : expectedRect.X,
				//	expectedRect.Y < 0 ? expectedBitmap.Size.Height + expectedRect.Y : expectedRect.Y
				//);
				//(int x, int y) actualOffset = (
				//	actualRect.X < 0 ? actualBitmap.Size.Width + actualRect.X : actualRect.X,
				//	actualRect.Y < 0 ? actualBitmap.Size.Height + actualRect.Y : actualRect.Y
				//);

				var expectedPixels = ExpectedPixels
					.At(actualRect.Location)
					.Pixels(expectedBitmap, expectedRect)
					.WithTolerance(tolerance);

				var report = GetContext();
				if (!Validate(actualBitmap, expectedPixels, expectedToActualScale, report))
				{
					Assert.Fail(report.ToString());
				}


				//for (var x = 0; x < width; x++)
				//for (var y = 0; y < height; y++)
				//{
				//	var expectedPixel = expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y);
				//	if (!Validate(actualBitmap, ExpectedPixels.At(actualOffset.x, actualOffset.y).Pixels().WithColorTolerance((byte)permittedPixelError), ))
				//	{

				//	}

				//	//var expectedPixel = expectedBitmap.GetPixel(x + expectedOffset.x, y + expectedOffset.y);
				//	//var actualPixel = actualBitmap.GetPixel(x + actualOffset.x, y + actualOffset.y);
				//	//if (expectedPixel == actualPixel)
				//	//{
				//	//	continue;
				//	//}

				//	//var cumulativeError = GetColorDistance(expectedPixel, actualPixel);
				//	//if (cumulativeError > permittedPixelError)
				//	//{
				//	//	Assert.Fail(WithContext(builder: builder => builder
				//	//		.AppendLine($"Difference between pixels at ({x},{y}) exceeds permitted error of {permittedPixelError}")
				//	//		.AppendLine($"expectedPixel: {ToArgbCode(expectedPixel)} {expectedPixel}")
				//	//		.AppendLine($"actualPixel  : {ToArgbCode(actualPixel)} {actualPixel}")
				//	//		.AppendLine($"permittedPixelError: {permittedPixelError}")
				//	//		.AppendLine($"cumulativeError    : {cumulativeError}")
				//	//	));
				//	//}
				//}
			}

			StringBuilder GetContext()
				=> new StringBuilder()
					.AppendLine($"ImageAssert.AreEqual @ line {line}")
					.AppendLine("pixelTolerance: " + tolerance)
					.AppendLine("expected: " + expected?.Name + (expectedRect == FirstQuadrant ? null : $"in {expectedRect}"))
					.AppendLine("actual  : " + actual?.Name + (actualRect == FirstQuadrant ? null : $"in {actualRect}"))
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
					isSuccess &= Validate(bitmap, expectation, 1, result);
					result.AppendLine();
				}

				if (!isSuccess)
				{
					Assert.Fail(result.ToString());
				}
			}
		}

		private static bool Validate(Bitmap actualBitmap, ExpectedPixels expectation, double expectedToActualScale, StringBuilder report)
		{
			report.Append($"{expectation.Name}... ");

			var failMessage = default(string);
			for (var offsetX = 0; offsetX <= expectation.Tolerance.Offset.x; offsetX++)
			for (var offsetY = 0; offsetY <= expectation.Tolerance.Offset.y; offsetY++)
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

			report.AppendLine("FAIELD");
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

					var actualX = (int)((expectation.Location.X + col) * expectedToActualScale + offsetX);
					var actualY = (int)((expectation.Location.Y + lin) * expectedToActualScale + offsetY);
					var actualColor = actualBitmap.GetPixel(actualX, actualY);

					if (!AreSameColor(expectedColor, actualColor, expectation.Tolerance.Color, expectation.Tolerance.Kind))
					{
						isSuccess = false;

						if (failMessage == null)
						{
							// If possible we dump the location in the source coordinates space.
							var expectedLocation = expectation.SourceLocation.HasValue
								? $"[{expectation.SourceLocation.Value.X + col},{expectation.SourceLocation.Value.Y + col}] "
								: "";

							result.AppendLine($"{col},{lin}: expected: {expectedLocation}{ToArgbCode(expectedColor)} | actual: [{actualX},{actualY}] {ToArgbCode(actualColor)}");
						}
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

	public struct ExpectedPixels
	{
		public static ExpectedPixels At(string name, float x, float y)
			=> new ExpectedPixels(name, new Point((int) x, (int)y), default, new Color[0, 0]);

		public static ExpectedPixels At(float x, float y, [CallerLineNumber] int line = -1)
			=> new ExpectedPixels($"at line: {line}", new Point((int)x, (int)y), default, new Color[0, 0]);

		public static ExpectedPixels At(Point location, [CallerLineNumber] int line = -1)
			=> new ExpectedPixels($"at line: {line}", location, default, new Color[0, 0]);

		public static ExpectedPixels At(string name, Point location)
			=> new ExpectedPixels(name, location, default, new Color[0, 0]);

		public ExpectedPixels Named(string name)
			=> new ExpectedPixels(name, Location, SourceLocation, Values, Tolerance);

		public ExpectedPixels Pixels(string[,] pixels)
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

			return new ExpectedPixels(Name, Location, SourceLocation, colors, Tolerance);
		}

		public ExpectedPixels Pixels(Bitmap source, Rectangle rect)
		{
			var colors = new Color[rect.Height, rect.Width];
			for (var py = 0; py < rect.Height; py++)
			for (var px = 0; px < rect.Width; px++)
			{
				colors[py, px] = source.GetPixel(rect.X + px, rect.Y + py);
			}
			
			return new ExpectedPixels(Name, Location, rect.Location, colors, Tolerance);
		}

		public ExpectedPixels Pixels(Bitmap source)
		{
			var colors = new Color[source.Width, source.Height];
			for (var py = 0; py < source.Height; py++)
			for (var px = 0; px < source.Width; px++)
			{
				colors[py, px] = source.GetPixel(px, py);
			}

			return new ExpectedPixels(Name, Location, new Point(0, 0), colors, Tolerance);
		}

		public ExpectedPixels WithTolerance(PixelTolerance tolerance) => new ExpectedPixels(Name, Location, SourceLocation, Values, tolerance);

		public ExpectedPixels WithColorTolerance(byte tolerance) => new ExpectedPixels(Name, Location, SourceLocation, Values, Tolerance.WithColor(tolerance));

		public ExpectedPixels WithPixelTolerance(int x = 0, int y = 0) => new ExpectedPixels(Name, Location, SourceLocation, Values, Tolerance.WithOffset(x, y));

		private ExpectedPixels(string name, Point location, Point? sourceLocation, Color[,] pixels, PixelTolerance tolerance = default)
		{
			Name = name;
			Location = location;
			SourceLocation = sourceLocation;
			Values = pixels;
			Tolerance = tolerance;
		}

		public string Name { get; set; }

		/// <summary>
		/// This is the location where the pixel are expected to be in the "actual" coordinate space
		/// </summary>
		public Point Location { get; }

		/// <summary>
		/// This is the location from where the pixels were loaded.
		/// This is informational only and is not expected to be used anywhere else than for logging / debugging purposes.
		/// </summary>
		public Point? SourceLocation { get; }
		
		public Color[,] Values { get; }

		public PixelTolerance Tolerance { get; }
	}

	public struct PixelTolerance
	{
		public static PixelTolerance None { get; } = new PixelTolerance();

		public PixelTolerance WithColor(byte color) => new PixelTolerance(color, Kind, Offset);

		public PixelTolerance WithKind(ColorToleranceKind kind) => new PixelTolerance(Color, kind, Offset);

		public PixelTolerance WithOffset(int x = 0, int y = 0) => new PixelTolerance(Color, Kind, (x, y));

		public PixelTolerance(byte color, ColorToleranceKind kind, (int x, int y) offset)
		{
			Color = color;
			Kind = kind;
			Offset = offset;
		}

		public ColorToleranceKind Kind { get; set; }

		public byte Color { get; }

		public (int x, int y) Offset { get; }

		/// <inheritdoc />
		public override string ToString()
			=> $"Color {Kind} tolerance of {Color} | Location tolerance of {Offset.x},{Offset.y} pixels.";
	}

	public enum ColorToleranceKind
	{
		/// <summary>
		/// Each component of the pixel (i.e. a, r, g and b) might differ by the provided color tolerance
		/// </summary>
		Exclusive,

		/// <summary>
		/// The A, R, G, or B values cannot cumulatively differ by more than the permitted tolerance
		/// </summary>
		Cumulative
	}
}
