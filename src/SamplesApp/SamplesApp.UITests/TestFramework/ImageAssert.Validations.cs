#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest;
using static System.Math;

#if IS_RUNTIME_UI_TESTS
using Windows.UI;

using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Microsoft.UI.Xaml.Markup;
using _Bitmap = Uno.UI.RuntimeTests.Helpers.RawBitmap;
using SamplesApp.UITests;
using Microsoft.UI.Xaml;
using SamplesApp.UITests.TestFramework;
using _Color = Windows.UI.Color;

namespace Uno.UI.RuntimeTests.Helpers;
#else
using System.Drawing;
using _Bitmap = SamplesApp.UITests.PlatformBitmap;
using _Color = System.Drawing.Color;
using SamplesApp.UITests._Utils;
namespace SamplesApp.UITests.TestFramework;
#endif


/// <summary>
/// Screenshot based assertions, to validate individual colors of an image
/// </summary>
public static partial class ImageAssert
{
	#region Validation core (ExpectedPixels)
	private static bool Validate(ExpectedPixels expectation, _Bitmap actualBitmap, double expectedToActualScale, StringBuilder report)
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

		for (var lin = 0; lin < expectation.Values.GetLength(0); lin += stepY)
			for (var col = 0; col < expectation.Values.GetLength(1); col += stepX)
			{
				yield return new Point(col, lin);
			}
	}

	private static bool ValidatePixel(
		_Bitmap actualBitmap,
		ExpectedPixels expectation,
		double expectedToActualScale,
		Point pixel,
		(int x, int y) offset,
		StringBuilder report)
	{
		var expectedColor = expectation.Values[pixel.Y, pixel.X];

#if IS_RUNTIME_UI_TESTS
		if (expectedColor == Colors.Transparent)
#else
		if (expectedColor.IsEmpty)
#endif
		{
			return true;
		}

		var actualX = (int)((expectation.Location.X + pixel.X) * expectedToActualScale + offset.x);
		var actualY = (int)((expectation.Location.Y + pixel.Y) * expectedToActualScale + offset.y);
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
		=> new(
			rect.X < 0 ? size.Width + rect.X : rect.X,
			rect.Y < 0 ? size.Height + rect.Y : rect.Y,
			Math.Min(rect.Width, size.Width),
			Math.Min(rect.Height, size.Height));

	private static bool AreSameColor(_Color a, _Color b, byte tolerance, out int currentDifference, ColorToleranceKind kind = ColorToleranceKind.Exclusive)
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

