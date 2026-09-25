#nullable enable

using System;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if __SKIA__
using Microsoft.UI.Xaml.Documents;
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
	// Text must go through Skia's text pipeline (glyph cache + scaler hinting), not be filled as glyph
	// outlines (#24652).
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[DataRow(11f)]
	[DataRow(14f)]
	[DataRow(20f)]
	public void When_Skia_Draws_GlyphRun_Then_Matches_Skia_Text_Pipeline(float fontSize)
	{
		const string text = "The quick brown fox jumps over the lazy dog";

		using var stream = SKTypeface.Default.OpenStream(out var faceIndex);
		if (stream is null)
		{
			Assert.Inconclusive("The default typeface exposes no font data.");
			return;
		}

		var data = new byte[stream.Length];
		stream.Read(data, data.Length);

		var font = FontProvider.Current.CreateFont(data, SKTypeface.Default.FamilyName, new FontWeight(400), FontStretch.Normal, FontStyle.Normal, fontSize);
		if (font is not SkiaFont)
		{
			Assert.Inconclusive("The registered font provider is not the Skia one.");
			return;
		}

		var run = font.Shape(text, TextDirection.LeftToRight);
		var positions = new Vector2[run.Count];
		var x = 2f;
		for (var i = 0; i < run.Count; i++)
		{
			positions[i] = new Vector2(x + run.Offsets[i].X, run.Offsets[i].Y);
			x += run.Advances[i];
		}

		var info = new SKImageInfo((int)Math.Ceiling(x) + 4, (int)(fontSize * 2));
		var baseline = fontSize * 1.4f;

		using var actual = SKSurface.Create(info);
		actual.Canvas.Clear(SKColors.White);
		GlyphRunRenderer.Draw(new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current), font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		using var expected = SKSurface.Create(info);
		expected.Canvas.Clear(SKColors.White);
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

		using var actualImage = actual.Snapshot();
		using var expectedImage = expected.Snapshot();
		using var actualPixels = actualImage.PeekPixels();
		using var expectedPixels = expectedImage.PeekPixels();

		var mismatches = 0;
		for (var py = 0; py < info.Height; py++)
		{
			for (var px = 0; px < info.Width; px++)
			{
				if (actualPixels.GetPixelColor(px, py) != expectedPixels.GetPixelColor(px, py))
				{
					mismatches++;
				}
			}
		}

		Assert.AreEqual(0, mismatches, $"{mismatches} pixels differ from Skia's text rendering at {fontSize}px.");
	}
#endif
}
