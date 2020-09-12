using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using SamplesApp.UITests._Utils;

namespace SamplesApp.UITests.TestFramework
{
	public struct ExpectedPixels
	{
		#region Fluent declaration
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
		#endregion

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
		#region Fluent declaration
		public static PixelTolerance None { get; } = new PixelTolerance();

		public static PixelTolerance Cummulative(int color)
			=> new PixelTolerance((byte)color, ColorToleranceKind.Cumulative, default, default, default);
		public static PixelTolerance Exclusive(byte color)
			=> new PixelTolerance(color, ColorToleranceKind.Exclusive, default, default, default);

		public PixelTolerance WithColor(byte color)
			=> new PixelTolerance(color, ColorKind, Offset, OffsetKind, DiscreteValidation);
		public PixelTolerance WithColor(int color, ColorToleranceKind colorKind)
			=> new PixelTolerance((byte)color, colorKind, Offset, OffsetKind, DiscreteValidation);
		public PixelTolerance WithKind(ColorToleranceKind colorKind)
			=> new PixelTolerance(Color, colorKind, Offset, OffsetKind, DiscreteValidation);
		public PixelTolerance WithOffset(int x = 0, int y = 0)
			=> new PixelTolerance(Color, ColorKind, (x, y), OffsetKind, DiscreteValidation);
		public PixelTolerance WithOffset(int x, int y, LocationToleranceKind offsetKind)
			=> new PixelTolerance(Color, ColorKind, (x, y), offsetKind, DiscreteValidation);
		public PixelTolerance WithKind(LocationToleranceKind offsetKind)
			=> new PixelTolerance(Color, ColorKind, Offset, offsetKind, DiscreteValidation);
		public PixelTolerance Discrete(uint discreteValidation = 20)
			=> new PixelTolerance(Color, ColorKind, Offset, OffsetKind, (discreteValidation, discreteValidation));
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
		public override string ToString()
			=> $"Color {ColorKind} tolerance of {Color} | Location {OffsetKind} tolerance of {Offset.x},{Offset.y} pixels.";
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
}
