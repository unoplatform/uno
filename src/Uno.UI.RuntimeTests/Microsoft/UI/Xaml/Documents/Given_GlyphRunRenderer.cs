#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if __SKIA__
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.UI.Text;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

[TestClass]
[RunsOnUIThread]
public class Given_GlyphRunRenderer
{
#if __SKIA__
	private const string Text = "The quick brown fox jumps over the lazy dog";

	// Text must go through Skia's text pipeline (glyph cache + scaler hinting), not be filled as glyph
	// outlines (#24652).
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[DataRow(11f)]
	[DataRow(14f)]
	[DataRow(20f)]
	public void When_Skia_Draws_GlyphRun_Then_Matches_Skia_Text_Pipeline(float fontSize)
	{
		if (!TryCreateDefaultFont(fontSize, out var data, out var faceIndex, out var font))
		{
			return;
		}

		var (run, positions, info, baseline) = Layout(font, fontSize);

		using var actual = CreateSurface(info);
		new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current).DrawGlyphRun(font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		using var expected = CreateSurface(info);
		using (var typeface = SKTypeface.FromData(SKData.CreateCopy(data), faceIndex))
		using (var skFont = new SKFont(typeface, fontSize) { Edging = SkiaFontProvider.TextEdging, Subpixel = true })
		using (var builder = new SKTextBlobBuilder())
		using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
		{
			var points = new SKPoint[positions.Length];
			for (var i = 0; i < positions.Length; i++)
			{
				points[i] = new SKPoint(positions[i].X, positions[i].Y);
			}

			builder.AddPositionedRun(run.Glyphs, skFont, points);
			using var blob = builder.Build();
			expected.Canvas.DrawText(blob, 0, baseline, paint);
		}

		var mismatches = CountMismatches(actual, expected, info, out _);
		Assert.AreEqual(0, mismatches, $"{mismatches} pixels differ from Skia's text rendering at {fontSize}px.");
	}

	// A font the Skia backend can't draw natively falls back to the portable outline renderer.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Skia_Draws_GlyphRun_Of_Foreign_Font_Then_Falls_Back_To_Outlines()
	{
		const float fontSize = 14f;
		if (!TryCreateDefaultFont(fontSize, out _, out _, out var font))
		{
			return;
		}

		var (run, positions, info, baseline) = Layout(font, fontSize);

		using var actual = CreateSurface(info);
		new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current).DrawGlyphRun(new ForeignFont(font), run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		using var expected = CreateSurface(info);
		GlyphRunRenderer.Draw(new SkiaDrawingSession(expected.Canvas, DrawingFactory.Current), font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		var mismatches = CountMismatches(actual, expected, info, out var inked);
		Assert.IsTrue(inked > 0, "The fallback drew nothing.");
		Assert.AreEqual(0, mismatches, $"{mismatches} pixels differ from the outline renderer.");
	}

	private static bool TryCreateDefaultFont(float fontSize, out byte[] data, out int faceIndex, out IFont font)
	{
		data = [];
		font = null!;

		using var stream = SKTypeface.Default.OpenStream(out faceIndex);
		if (stream is null)
		{
			Assert.Inconclusive("The default typeface exposes no font data.");
			return false;
		}

		data = new byte[stream.Length];
		stream.Read(data, data.Length);

		if (FontProvider.Current.CreateFont(data, SKTypeface.Default.FamilyName, new FontWeight(400), FontStretch.Normal, FontStyle.Normal, fontSize) is not SkiaFont skiaFont)
		{
			Assert.Inconclusive("The registered font provider is not the Skia one.");
			return false;
		}

		font = skiaFont;
		return true;
	}

	private static (GlyphRun Run, Vector2[] Positions, SKImageInfo Info, float Baseline) Layout(IFont font, float fontSize)
	{
		var run = font.Shape(Text, TextDirection.LeftToRight);
		var positions = new Vector2[run.Count];
		var x = 2f;
		for (var i = 0; i < run.Count; i++)
		{
			positions[i] = new Vector2(x + run.Offsets[i].X, run.Offsets[i].Y);
			x += run.Advances[i];
		}

		return (run, positions, new SKImageInfo((int)Math.Ceiling(x) + 4, (int)(fontSize * 2)), fontSize * 1.4f);
	}

	private static SKSurface CreateSurface(SKImageInfo info)
	{
		var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.White);
		return surface;
	}

	private static int CountMismatches(SKSurface actual, SKSurface expected, SKImageInfo info, out int inked)
	{
		using var actualImage = actual.Snapshot();
		using var expectedImage = expected.Snapshot();
		using var actualPixels = actualImage.PeekPixels();
		using var expectedPixels = expectedImage.PeekPixels();

		var mismatches = 0;
		inked = 0;
		for (var py = 0; py < info.Height; py++)
		{
			for (var px = 0; px < info.Width; px++)
			{
				var pixel = actualPixels.GetPixelColor(px, py);
				if (pixel != SKColors.White)
				{
					inked++;
				}

				if (pixel != expectedPixels.GetPixelColor(px, py))
				{
					mismatches++;
				}
			}
		}

		return mismatches;
	}

	// Wraps a Skia font in a type the Skia backend doesn't recognize, so it must take the portable path.
	private sealed class ForeignFont(IFont inner) : IFont
	{
		public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true) => inner.Shape(text, direction, enableLigatures);

		public void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements)
			=> inner.BuildGlyphRun(geometry, glyphs, positions, baselineY, elements);

		public float Ascent => inner.Ascent;

		public float Descent => inner.Descent;

		public float CapHeight => inner.CapHeight;

		public float? UnderlinePosition => inner.UnderlinePosition;

		public float? UnderlineThickness => inner.UnderlineThickness;

		public float? StrikeoutPosition => inner.StrikeoutPosition;

		public float? StrikeoutThickness => inner.StrikeoutThickness;

		public ushort GetGlyphIndex(int codepoint) => inner.GetGlyphIndex(codepoint);

		public bool ContainsGlyph(int codepoint) => inner.ContainsGlyph(codepoint);

		public float GetGlyphAdvance(ushort glyph) => inner.GetGlyphAdvance(glyph);

		public string FamilyName => inner.FamilyName;
	}
#endif
}
