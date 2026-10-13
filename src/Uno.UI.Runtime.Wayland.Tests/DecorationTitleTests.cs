#nullable enable

using System.Globalization;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class DecorationTitleTests
{
	[TestMethod]
	public void VeryLongTitleUsesBoundedShapingAndReusesFontAndFitOnRedraw()
	{
		var font = new CountingFont();
		var fontCreations = 0;
		var cache = new WaylandDecorationTitle(() => { fontCreations++; return font; });
		var title = new string('x', 100_000);
		var run = cache.GetRun(title, 32);
		Assert.AreEqual(32, run.Glyphs.Length);
		Assert.IsTrue(font.Shaped.Count <= 2 + (int)Math.Ceiling(Math.Log2(title.Length)), "Fitting must use logarithmically many shaping calls.");
		Assert.IsTrue(font.Shaped.Sum(text => text.Length) < 3 * title.Length, "Shortening a long title must not shape every prefix.");
		var calls = font.Shaped.Count;
		for (var redraw = 0; redraw < 20; redraw++)
		{
			var cached = cache.GetRun(title, 32);
			Assert.AreSame(run.Glyphs, cached.Glyphs);
			Assert.AreSame(run.Positions, cached.Positions);
			Assert.AreSame(font, cache.Font);
		}
		Assert.AreEqual(calls, font.Shaped.Count);
		Assert.AreEqual(1, fontCreations);
		Assert.AreEqual(16, cache.GetRun(title, 16).Glyphs.Length);
		Assert.AreEqual(1, font.Shaped.Count(text => text == title), "Resize must reuse the full title's shaping and cluster boundaries.");
		Assert.AreEqual(1, fontCreations);
		Assert.AreEqual(3, cache.GetRun("new", 32).Glyphs.Length);
	}

	[TestMethod]
	[DataRow(1)]
	[DataRow(3)]
	[DataRow(5)]
	public void TruncationPreservesGraphemesAndShapedLigatureClusters(int width)
	{
		const string title = "A😀e\u0301👩\u200d👩\u200d👧\u200d👦fiXYZ";
		var font = new CountingFont();
		var cache = new WaylandDecorationTitle(() => font);
		var run = cache.GetRun(title, width);
		Assert.AreEqual(width, run.Glyphs.Length);
		var boundaries = StringInfo.ParseCombiningCharacters(title).ToHashSet();
		boundaries.Remove(title.IndexOf("fi", StringComparison.Ordinal) + 1);
		foreach (var candidate in font.Shaped.Skip(1))
		{
			Assert.IsTrue(candidate.EndsWith('…'));
			var prefixLength = candidate.Length - 1;
			Assert.IsTrue(boundaries.Contains(prefixLength), $"Split a grapheme or shaping cluster at {prefixLength}.");
			Assert.AreEqual(title[..prefixLength] + "…", candidate);
		}
	}

	[TestMethod]
	public void EmptyOrNarrowTitleDoesNotDrawAnOverflowingEllipsis()
	{
		var font = new CountingFont();
		var cache = new WaylandDecorationTitle(() => font);
		Assert.AreEqual(0, cache.GetRun("title", 0).Glyphs.Length);
		Assert.AreEqual(0, cache.GetRun("", 10).Glyphs.Length);
		Assert.AreEqual(0, font.Shaped.Count);
		Assert.AreEqual(0, cache.GetRun("title", 0.5f).Glyphs.Length);
	}

	private sealed class CountingFont : IFont
	{
		internal List<string> Shaped { get; } = new();

		public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
		{
			var value = text.ToString();
			Shaped.Add(value);
			var clusters = StringInfo.ParseCombiningCharacters(value).ToList();
			// Model a ligature to ensure fitting uses shaping clusters as well as Unicode boundaries.
			var ligature = value.IndexOf("fi", StringComparison.Ordinal);
			if (ligature >= 0)
			{
				clusters.Remove(ligature + 1);
			}
			return new GlyphRun(Enumerable.Repeat((ushort)1, clusters.Count).ToArray(), new Vector2[clusters.Count],
				Enumerable.Repeat(1f, clusters.Count).ToArray(), clusters.ToArray());
		}

		public void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements)
			=> throw new NotSupportedException();

		public float Ascent => -10;
		public float Descent => 3;
		public float CapHeight => 8;
		public float? UnderlinePosition => null;
		public float? UnderlineThickness => null;
		public float? StrikeoutPosition => null;
		public float? StrikeoutThickness => null;
		public ushort GetGlyphIndex(int codepoint) => 1;
		public bool ContainsGlyph(int codepoint) => true;
		public float GetGlyphAdvance(ushort glyph) => 1;
		public string FamilyName => "Counting";
	}
}
