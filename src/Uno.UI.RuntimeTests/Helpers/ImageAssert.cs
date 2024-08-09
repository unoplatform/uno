#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Windows.UI;
using static System.Math;

using Rectangle = System.Drawing.Rectangle;
using SamplesApp.UITests;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// Screen shot based assertions, to validate individual colors of an image
/// </summary>
public static partial class ImageAssert
{
	#region HasColorAt
	public static void HasColorAt(RawBitmap screenshot, Windows.Foundation.Point location, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)location.X, (int)location.Y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

	public static void HasColorAt(RawBitmap screenshot, Windows.Foundation.Point location, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)location.X, (int)location.Y, expectedColor, tolerance, line);

	public static void HasColorAt(RawBitmap screenshot, float x, float y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

	public static void HasColorAt(RawBitmap screenshot, float x, float y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtImpl(screenshot, (int)x, (int)y, expectedColor, tolerance, line);

	public static void HasColorAtChild(RawBitmap screenshot, UIElement child, double x, double y, string expectedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> HasColorAtChild(screenshot, child, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), expectedColorCode), tolerance, line);

	public static void HasColorAtChild(RawBitmap screenshot, UIElement child, double x, double y, Color expectedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
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
		(int x, int y, int diff, Color color) min = (-1, -1, int.MaxValue, default);
		for (var x = rect.Left; x < rect.Right; x++)
		{
			for (var y = rect.Top; y < rect.Bottom; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (AreSameColor(expectedColor, pixel, tolerance, out var diff))
				{
					return;
				}
				else if (diff < min.diff)
				{
					min = (x, y, diff, pixel);
				}
			}
		}

		Assert.Fail($"Expected '{ToArgbCode(expectedColor)}' in rectangle '{rect}', but no pixel has this color. The closest pixel found is '{ToArgbCode(min.color)}' at '{min.x},{min.y}' with a (exclusive) difference of {min.diff}.");
	}

	/// <summary>
	/// Asserts that a given screenshot does not have a specific color anywhere within a given rectangle.
	/// </summary>
	public static void DoesNotHaveColorInRectangle(RawBitmap screenshot, Rectangle rect, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
	{
		var bitmap = screenshot;
		for (var x = rect.Left; x < rect.Right; x++)
		{
			for (var y = rect.Top; y < rect.Bottom; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (AreSameColor(excludedColor, pixel, tolerance, out var diff))
				{
					Assert.Fail($"Color '{ToArgbCode(excludedColor)}' was found at ({x}, {y}) in rectangle '{rect}' (Exclusive difference of {diff}).");
				}
			}
		}
	}

	private static void HasColorAtImpl(RawBitmap screenshot, int x, int y, Color expectedColor, byte tolerance, int line)
	{
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
	public static void DoesNotHaveColorAt(RawBitmap screenshot, float x, float y, string excludedColorCode, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, (Color)XamlBindingHelper.ConvertValue(typeof(Color), excludedColorCode), tolerance, line);

	public static void DoesNotHaveColorAt(RawBitmap screenshot, float x, float y, Color excludedColor, byte tolerance = 0, [CallerLineNumber] int line = 0)
		=> DoesNotHaveColorAtImpl(screenshot, (int)x, (int)y, excludedColor, tolerance, line);

	private static void DoesNotHaveColorAtImpl(RawBitmap screenshot, int x, int y, Color excludedColor, byte tolerance, int line)
	{
		var bitmap = screenshot;
		if (bitmap.Width <= x || bitmap.Height <= y)
		{
			Assert.Fail(WithContext($"Coordinates ({x}, {y}) falls outside of screenshot dimension {bitmap.Size}"));
		}

		var pixel = bitmap.GetPixel(x, y);
		if (AreSameColor(excludedColor, pixel, tolerance, out var difference))
		{
			Assert.Fail(WithContext(builder: builder => builder
				.AppendLine($"Color at ({x},{y}) is not expected")
				.AppendLine($"excluded: {ToArgbCode(excludedColor)} {excludedColor.ToString()}")
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
	public static void HasPixels(RawBitmap actual, params ExpectedPixels[] expectations)
	{
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

	internal static Rect GetColorBounds(RawBitmap rawBitmap, Color color, byte tolerance = 0)
	{
		var minX = int.MaxValue;
		var minY = int.MaxValue;
		var maxX = int.MinValue;
		var maxY = int.MinValue;

		for (int x = 0; x < rawBitmap.Width; x++)
		{
			for (int y = 0; y < rawBitmap.Height; y++)
			{
				if (AreSameColor(color, rawBitmap.GetPixel(x, y), tolerance, out _))
				{
					minX = Math.Min(minX, x);
					minY = Math.Min(minY, y);
					maxX = Math.Max(maxX, x);
					maxY = Math.Max(maxY, y);
				}
			}
		}

		return new Rect(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
	}

	public static async Task AreEqualAsync(RawBitmap actual, RawBitmap expected)
	{
		if (!await AreRenderTargetBitmapsEqualAsync(actual.Bitmap, expected.Bitmap))
		{
			Assert.Fail("The bitmaps are not the same");
		}
	}

	public static async Task AreNotEqualAsync(RawBitmap actual, RawBitmap expected)
	{
		if (await AreRenderTargetBitmapsEqualAsync(actual.Bitmap, expected.Bitmap))
		{
			Assert.Fail("The bitmaps are the same");
		}
	}

	private static async Task<bool> AreRenderTargetBitmapsEqualAsync(RenderTargetBitmap bitmap1, RenderTargetBitmap bitmap2)
	{
		if (bitmap1.PixelWidth != bitmap2.PixelWidth || bitmap1.PixelHeight != bitmap2.PixelHeight)
		{
			return false;
		}

		var buffer1 = await bitmap1.GetPixelsAsync();
		var buffer2 = await bitmap2.GetPixelsAsync();

		using var reader1 = DataReader.FromBuffer(buffer1);
		using var reader2 = DataReader.FromBuffer(buffer2);
		while (reader1.UnconsumedBufferLength > 0 && reader2.UnconsumedBufferLength > 0)
		{
			if (reader1.ReadByte() != reader2.ReadByte())
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Asserts that two image are similar within the given <see href="https://en.wikipedia.org/wiki/Root-mean-square_deviation">RMSE</see>
	/// The method it based roughly on ImageMagick implementation to ensure consistency.
	/// If the error is greater than or equal to 0.022, the differences are visible to human eyes.
	/// <paramref name="actual">The image to compare with reference</paramref>
	/// <paramref name="expected">Reference image.</paramref>
	/// <paramref name="imperceptibilityThreshold">It is the threshold beyond which the compared images are not considered equal. Default value is 0.022.</paramref>>
	/// </summary>
	public static async Task AreSimilarAsync(RawBitmap actual, RawBitmap expected, double imperceptibilityThreshold = 0.022)
	{
		await actual.Populate();
		await expected.Populate();

		using var assertionScope = new AssertionScope("ImageAssert");

		if (actual.Width != expected.Width || actual.Height != expected.Height)
		{
			assertionScope.FailWith($"Images have different resolutions. {Environment.NewLine}expected:({expected.Width},{expected.Height}){Environment.NewLine}actual  :({actual.Width},{actual.Height})");
		}

		var quantity = actual.Width * actual.Height;
		double squaresError = 0;

		const double scale = 1 / 255d;

		for (var x = 0; x < actual.Width; x++)
		{
			double localError = 0;

			for (var y = 0; y < actual.Height; y++)
			{
				var expectedAlpha = expected[x, y].A * scale;
				var actualAlpha = actual[x, y].A * scale;

				var r = scale * (expectedAlpha * expected[x, y].R - actualAlpha * actual[x, y].R);
				var g = scale * (expectedAlpha * expected[x, y].G - actualAlpha * actual[x, y].G);
				var b = scale * (expectedAlpha * expected[x, y].B - actualAlpha * actual[x, y].B);
				var a = expectedAlpha - actualAlpha;

				var error = r * r + g * g + b * b + a * a;

				localError += error;
			}

			squaresError += localError;
		}

		var meanSquaresError = squaresError / quantity;

		const int channelCount = 4;

		meanSquaresError = meanSquaresError / channelCount;
		var sqrtMeanSquaresError = Sqrt(meanSquaresError);
		if (sqrtMeanSquaresError >= imperceptibilityThreshold)
		{
			assertionScope.FailWith($"the actual image is not the same as the expected one.{Environment.NewLine}actual RSMD: {sqrtMeanSquaresError}{Environment.NewLine}threshold: {imperceptibilityThreshold}");
		}
	}
}
