#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24652")]
	[DataRow(11f)]
	[DataRow(14f)]
	[DataRow(20f)]
	public async Task When_Skia_Draws_GlyphRun_Then_Matches_Skia_Text_Pipeline(float fontSize)
	{
		var data = await LoadFontData();
		var font = CreateSkiaFont(data, fontSize);

		var (run, positions, info, baseline) = Layout(font, fontSize);

		using var actual = CreateSurface(info);
		new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current).DrawGlyphRun(font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		// Spelled out rather than read from SkiaFontProvider so the Windows-only grayscale edging is covered too.
		var edging = OperatingSystem.IsWindows() ? SKFontEdging.Antialias : SKFontEdging.SubpixelAntialias;

		using var expected = CreateSurface(info);
		using (var typeface = SKTypeface.FromData(SKData.CreateCopy(data), 0))
		using (var skFont = new SKFont(typeface, fontSize) { Edging = edging, Subpixel = true })
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

	// Outline text must stay on Skia's text pipeline too, drawn as a stroked text blob.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Skia_Strokes_GlyphRun_Then_Matches_Skia_Stroked_Text()
	{
		const float fontSize = 20f;
		const float strokeWidth = 1f;
		var data = await LoadFontData();
		var font = CreateSkiaFont(data, fontSize);

		var (run, positions, info, baseline) = Layout(font, fontSize);

		using var actual = CreateSurface(info);
		new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current).StrokeGlyphRun(font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black, strokeWidth);

		using var expected = CreateSurface(info);
		using (var builder = new SKTextBlobBuilder())
		using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth })
		{
			var points = new SKPoint[positions.Length];
			for (var i = 0; i < positions.Length; i++)
			{
				points[i] = new SKPoint(positions[i].X, positions[i].Y);
			}

			builder.AddPositionedRun(run.Glyphs, ((SkiaFont)font).NativeFont, points);
			using var blob = builder.Build();
			expected.Canvas.DrawText(blob, 0, baseline, paint);
		}

		var mismatches = CountMismatches(actual, expected, info, out var inked);
		Assert.IsTrue(inked > 0, "Nothing was stroked.");
		Assert.AreEqual(0, mismatches, $"{mismatches} pixels differ from Skia's stroked text rendering.");
	}

	// A font the Skia backend can't draw natively falls back to the portable outline renderer.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Skia_Draws_GlyphRun_Of_Foreign_Font_Then_Falls_Back_To_Outlines()
	{
		const float fontSize = 14f;
		var font = CreateSkiaFont(await LoadFontData(), fontSize);

		var (run, positions, info, baseline) = Layout(font, fontSize);

		using var actual = CreateSurface(info);
		new SkiaDrawingSession(actual.Canvas, DrawingFactory.Current).DrawGlyphRun(new ForeignFont(font), run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		using var expected = CreateSurface(info);
		GlyphRunRenderer.Draw(new SkiaDrawingSession(expected.Canvas, DrawingFactory.Current), font, run.Glyphs, positions, baseline, Microsoft.UI.Colors.Black);

		var mismatches = CountMismatches(actual, expected, info, out var inked);
		Assert.IsTrue(inked > 0, "The fallback drew nothing.");
		Assert.AreEqual(0, mismatches, $"{mismatches} pixels differ from the outline renderer.");
	}

	// Every paint draws the images in the same order, so plain LRU eviction would miss on every lookup once a paint
	// draws more images than the cap.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_A_Paint_Draws_More_Images_Than_The_Cap_Then_A_Repaint_Reuses_Their_Textures()
	{
		const int count = GlyphRunRenderer.ImageTextureCache.Cap + 6;
		var images = new (object Key, IImage Image)[count];
		for (var i = 0; i < count; i++)
		{
			images[i] = (new object(), ImageEncoderDecoder.Current.CreateImage(2, 2, new byte[2 * 2 * 4]));
		}

		try
		{
			using var surface = SKSurface.Create(new SKImageInfo(8, 8));
			var paint = new SkiaDrawingSession(surface.Canvas, DrawingFactory.Current);
			var textures = new ITexture[count];
			for (var i = 0; i < count; i++)
			{
				textures[i] = GlyphRunRenderer.ImageTextureCache.Get(paint, images[i].Key, images[i].Image);
			}

			var repaint = new SkiaDrawingSession(surface.Canvas, DrawingFactory.Current);
			var hits = 0;
			for (var i = 0; i < count; i++)
			{
				if (ReferenceEquals(textures[i], GlyphRunRenderer.ImageTextureCache.Get(repaint, images[i].Key, images[i].Image)))
				{
					hits++;
				}
			}

			Assert.AreEqual(count, hits);
		}
		finally
		{
			foreach (var (_, image) in images)
			{
				image.Dispose();
			}
		}
	}

	// Format runs, undo snapshots and fragments clone the image state, and each clone decodes its own image.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Inline_Image_Is_Cloned_Then_Its_Texture_Is_Shared()
	{
		var encoded = Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(4, 4, Microsoft.UI.Colors.CornflowerBlue);
		Assert.IsTrue(Microsoft.UI.Text.InlineImageState.TryCreate(encoded, 4, 4, 4, Microsoft.UI.Text.VerticalCharacterAlignment.Baseline, null, Microsoft.UI.Text.InlineImageEncoding.Unknown, out var original));
		var clone = original.Clone();

		var originalImage = original.GetDecodedImage();
		var cloneImage = clone.GetDecodedImage();
		Assert.IsNotNull(originalImage);
		Assert.IsNotNull(cloneImage);

		using var surface = SKSurface.Create(new SKImageInfo(8, 8));
		var session = new SkiaDrawingSession(surface.Canvas, DrawingFactory.Current);
		Assert.AreSame(
			GlyphRunRenderer.ImageTextureCache.Get(session, original.TextureKey, originalImage),
			GlyphRunRenderer.ImageTextureCache.Get(session, clone.TextureKey, cloneImage));
	}

	private static async Task<byte[]> LoadFontData()
	{
		var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Uno.UI.RuntimeTests/Assets/Fonts/Roboto-Regular.ttf"));
		using var stream = await file.OpenStreamForReadAsync();
		using MemoryStream buffer = new();
		await stream.CopyToAsync(buffer);
		return buffer.ToArray();
	}

	// Built with the Skia provider directly: the registered one is not Skia's when the app runs another drawing backend.
	private static IFont CreateSkiaFont(byte[] data, float fontSize)
	{
		var font = new SkiaFontProvider().CreateFont(data, null, new FontWeight(400), FontStretch.Normal, FontStyle.Normal, fontSize);
		Assert.IsInstanceOfType<SkiaFont>(font);
		return font;
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
