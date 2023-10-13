#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.UITest;

#if IS_RUNTIME_UI_TESTS
using Windows.UI;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Windows.UI.Xaml.Markup;
using _Bitmap = Uno.UI.RuntimeTests.Helpers.RawBitmap;

namespace Uno.UI.RuntimeTests.Helpers;
#else
using System.Drawing;
using _Bitmap = SamplesApp.UITests.PlatformBitmap;
using SamplesApp.UITests._Utils;
namespace SamplesApp.UITests.TestFramework;
#endif

public record struct ExpectedPixels
{
	#region Fluent declaration
	public static ExpectedPixels At(string name, float x, float y)
		=> new() { Name = name, Location = new Point((int)x, (int)y) };

	public static ExpectedPixels At(float x, float y, [CallerLineNumber] int line = -1)
		=> new() { Name = $"at line: {line}", Location = new Point((int)x, (int)y) };

	public static ExpectedPixels At(Point location, [CallerLineNumber] int line = -1)
		=> new() { Name = $"at line: {line}", Location = location };

	public static ExpectedPixels At(string name, Point location)
		=> new() { Name = name, Location = location };

	public static ExpectedPixels UniformRect(
		IAppRect rect,
		string color,
		[CallerMemberName] string name = "",
		[CallerLineNumber] int line = -1)
	{
		var c = GetColorFromString(color);
		var colors = new Color[(int)rect.Height, (int)rect.Width];

		for (var py = (int)rect.Height; py < (int)rect.Height; py++)
			for (var px = (int)rect.Width; px < (int)rect.Width; px++)
			{
				colors[py, px] = c;
			}

		var location = new Point((int)rect.X, (int)rect.Y);

		return new ExpectedPixels
		{
			Name = name,
			Location = location,
			SourceLocation = location,
			Values = colors
		};
	}

	public ExpectedPixels Named(string name)
		=> this with { Name = name };

	public ExpectedPixels Pixel(Color color)
		=> this with { Values = new[,] { { color } } };

	public ExpectedPixels Pixel(string color)
		=> this with { Values = new[,] { { GetColorFromString(color) } } };

	public ExpectedPixels Pixels(string[,] pixels)
	{
		var colors = new Color[pixels.GetLength(0), pixels.GetLength(1)];
		for (var py = 0; py < pixels.GetLength(0); py++)
			for (var px = 0; px < pixels.GetLength(1); px++)
			{
				var colorCode = pixels[py, px];
				colors[py, px] = GetColorFromString(colorCode);
			}

		return this with { Values = colors };
	}

	public ExpectedPixels Pixels(_Bitmap source, Rectangle rect)
	{
		try
		{
			var colors = new Color[rect.Height, rect.Width];
			for (var py = 0; py < rect.Height; py++)
				for (var px = 0; px < rect.Width; px++)
				{
					colors[py, px] = source.GetPixel(rect.X + px, rect.Y + py);
				}

			return this with { SourceLocation = rect.Location, Values = colors };
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Unable to create a pixel array of {rect.Width}x{rect.Height} (bitmap is {source.Width}x{source.Height}).", ex);
		}
	}

	public ExpectedPixels Pixels(_Bitmap source)
	{
		var colors = new Color[source.Height, source.Width];
		for (var py = 0; py < source.Height; py++)
			for (var px = 0; px < source.Width; px++)
			{
				colors[py, px] = source.GetPixel(px, py);
			}

		return this with { SourceLocation = new Point(0, 0), Values = colors };
	}

	public ExpectedPixels WithTolerance(PixelTolerance tolerance)
		=> this with { Tolerance = tolerance };

	public ExpectedPixels WithColorTolerance(byte tolerance, ColorToleranceKind kind = default)
		=> this with { Tolerance = Tolerance.WithColor(tolerance, kind) };

	public ExpectedPixels WithPixelTolerance(int x = 0, int y = 0, LocationToleranceKind kind = default)
		=> this with { Tolerance = Tolerance.WithOffset(x, y, kind) };

	public ExpectedPixels Or(ExpectedPixels alternative)
		=> this with
		{
			Alternatives = Alternatives is null
				? alternative.GetAllPossibilities().ToArray()
				: Alternatives.Concat(alternative.GetAllPossibilities()).ToArray()
		};

	public ExpectedPixels OrPixel(Color alternativeColor)
		=> Or(this with { Values = new[,] { { alternativeColor } }, Alternatives = null });

	public ExpectedPixels OrPixel(string alternativeColor)
		=> Or(this with { Values = new[,] { { GetColorFromString(alternativeColor) } }, Alternatives = null });
	#endregion

	private ExpectedPixels(
		string name,
		Point location,
		Point? sourceLocation,
		Color[,] pixels,
		PixelTolerance tolerance,
		ExpectedPixels[] alternatives)
	{
		Name = name;
		Location = location;
		SourceLocation = sourceLocation;
		Values = pixels ?? new Color[0, 0];
		Tolerance = tolerance;
		Alternatives = alternatives ?? Array.Empty<ExpectedPixels>();
	}

	public string Name { get; init; }

	/// <summary>
	/// This is the location where the pixels are expected to be in the "actual" coordinate space
	/// </summary>
	public Point Location { get; init; }

	/// <summary>
	/// This is the location from where the pixels were loaded.
	/// This is informational only and is not expected to be used anywhere else than for logging / debugging purposes.
	/// </summary>
	public Point? SourceLocation { get; init; }

	public Color[,] Values { get; init; } = new Color[0, 0];

	public PixelTolerance Tolerance { get; init; } = PixelTolerance.None;

	public ExpectedPixels[]? Alternatives { get; init; } = Array.Empty<ExpectedPixels>();

	public IEnumerable<ExpectedPixels> GetAllPossibilities()
	{
		yield return this;
		if (Alternatives is not null)
		{
			foreach (var alternative in Alternatives)
			{
				yield return alternative;
			}
		}
	}

	private static Color GetColorFromString(string colorCode) =>
		string.IsNullOrWhiteSpace(colorCode)
#if IS_RUNTIME_UI_TESTS
			? Colors.Transparent
			: (Color)XamlBindingHelper.ConvertValue(typeof(Color), colorCode);
#else
			? Color.Empty
			: ColorCodeParser.Parse(colorCode);
#endif
}

static class ColorExtensions
{
#if IS_RUNTIME_UI_TESTS
	public static Color ToColor(this Color color)
		=> color;
#else
	public static System.Drawing.Color ToColor(this SkiaSharp.SKColor color)
		=> System.Drawing.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
#endif
}

public struct PixelTolerance
{
	#region Fluent declaration
	public static PixelTolerance None { get; } = new();

	public static PixelTolerance Cummulative(int color)
		=> new((byte)color, ColorToleranceKind.Cumulative, default, default, default);
	public static PixelTolerance Exclusive(byte color)
		=> new(color, ColorToleranceKind.Exclusive, default, default, default);

	public PixelTolerance WithColor(byte color)
		=> new(color, ColorKind, Offset, OffsetKind, DiscreteValidation);
	public PixelTolerance WithColor(int color, ColorToleranceKind colorKind)
		=> new((byte)color, colorKind, Offset, OffsetKind, DiscreteValidation);
	public PixelTolerance WithKind(ColorToleranceKind colorKind)
		=> new(Color, colorKind, Offset, OffsetKind, DiscreteValidation);
	public PixelTolerance WithOffset(int x = 0, int y = 0)
		=> new(Color, ColorKind, (x, y), OffsetKind, DiscreteValidation);
	public PixelTolerance WithOffset(int x, int y, LocationToleranceKind offsetKind)
		=> new(Color, ColorKind, (x, y), offsetKind, DiscreteValidation);
	public PixelTolerance WithKind(LocationToleranceKind offsetKind)
		=> new(Color, ColorKind, Offset, offsetKind, DiscreteValidation);
	public PixelTolerance Discrete(uint discreteValidation = 20)
		=> new(Color, ColorKind, Offset, OffsetKind, (discreteValidation, discreteValidation));
	#endregion

	public PixelTolerance(byte color, ColorToleranceKind colorKind, (int x, int y) offset, LocationToleranceKind offsetKind, (uint x, uint y) discreteValidation)
	{
		Color = color;
		ColorKind = colorKind;
		Offset = offset;
		OffsetKind = offsetKind;
		DiscreteValidation = discreteValidation;
	}

	public byte Color { get; }
	public ColorToleranceKind ColorKind { get; }

	public (int x, int y) Offset { get; }
	public LocationToleranceKind OffsetKind { get; }

	/// <summary>
	/// Configure how many pixels might be ignored for validation.
	/// Validation engine will actually test one pixel each <see cref="DiscreteValidation"/>.
	/// </summary>
	public (uint x, uint y) DiscreteValidation { get; }

	/// <inheritdoc />
	public override string ToString() =>
		Color > 0
			? $"Color {ColorKind} tolerance of {Color} | Location {OffsetKind} tolerance of {Offset.x},{Offset.y} pixels."
			: "No color tolerance";
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

public enum LocationToleranceKind
{
	/// <summary>
	/// The offset applies to all pixel in <see cref="ExpectedPixels.Values"/> at once
	/// (i.e once the offset has be computed, all pixels must be the same)
	/// </summary>
	PerRange,

	/// <summary>
	/// Each pixel might be offset by the given offset, independently of other pixels defined in the <see cref="ExpectedPixels.Values"/>.
	/// (I.e. pixel[0,0] may be offset by x=1, y=5, while pixel[5,5] may have an offset of x=-2, y=0)
	/// </summary>
	PerPixel,
}
