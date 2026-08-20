#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font handle: the text layer talks only to this (never to a concrete Skia/native font type). It shapes a
/// text run into positioned glyphs, turns those glyphs into drawable output, exposes layout metrics, and answers
/// glyph-coverage queries. The shaper (HarfBuzz / CoreText / DirectWrite) is an implementation detail. Font
/// resolution (family/style → face) stays outside this handle (<see cref="IFontProvider"/>).
/// </summary>
public interface IFont
{
	/// <summary>
	/// Shapes a run of text (already itemized to a single font/script/direction by the layout engine) into positioned
	/// glyphs. Offsets and advances are in pixels at this font's size; clusters map each glyph back to its originating
	/// index in <paramref name="text"/>. <paramref name="enableLigatures"/> gates the OpenType <c>liga</c> feature.
	/// Glyphs are returned in the shaper's output order (not reversed for RTL).
	/// </summary>
	GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true);

	/// <summary>
	/// Turns a shaped run into a sequence of drawable elements, appended to <paramref name="elements"/> in draw order.
	/// Each is a merged monochrome <see cref="GlyphOutline"/>, a <see cref="GlyphColorLayers"/> vector colour glyph, or
	/// a rasterized <see cref="GlyphImage"/> colour glyph. Each glyph is shifted by <paramref name="baselineY"/>. The
	/// caller owns and disposes any <see cref="IGeometry"/> carried by the elements.
	/// </summary>
	/// <param name="geometry">Registered geometry factory the font builds its glyph outlines with, so it never names a concrete <see cref="IGeometry"/> type.</param>
	void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements);

	// Metrics in pixels at this font's size, baseline-relative: Ascent <= 0 (above), Descent >= 0 (below),
	// so line height = Descent - Ascent.

	/// <summary>Distance from the baseline to the top of the text, negative (above the baseline).</summary>
	float Ascent { get; }

	/// <summary>Distance from the baseline to the bottom of the text, positive (below the baseline).</summary>
	float Descent { get; }

	/// <summary>Underline stroke offset from the baseline (positive below), or null if the font doesn't specify one.</summary>
	float? UnderlinePosition { get; }

	/// <summary>Underline stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	float? UnderlineThickness { get; }

	/// <summary>Strikeout stroke offset from the baseline (negative above), or null if the font doesn't specify one.</summary>
	float? StrikeoutPosition { get; }

	/// <summary>Strikeout stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	float? StrikeoutThickness { get; }

	// Glyph coverage of this font; which font to fall back to is a resolution concern (IFontProvider).

	/// <summary>Maps a Unicode codepoint to a glyph index (0 = .notdef / not covered).</summary>
	ushort GetGlyphIndex(int codepoint);

	/// <summary>Whether this font has a glyph for <paramref name="codepoint"/>.</summary>
	bool ContainsGlyph(int codepoint);

	/// <summary>The glyph's horizontal advance in pixels at this font's size.</summary>
	float GetGlyphAdvance(ushort glyph);

	/// <summary>The font's family name (used to group fallback runs); empty if unknown.</summary>
	string FamilyName { get; }
}

/// <summary>
/// The output of <see cref="IFont.Shape"/>: parallel arrays describing the positioned glyphs of one shaped run.
/// <see cref="Offsets"/> and <see cref="Advances"/> are in pixels at the font's size.
/// </summary>
public readonly record struct GlyphRun
{
	public GlyphRun(ushort[] glyphs, Vector2[] offsets, float[] advances, int[] clusters)
	{
		Glyphs = glyphs;
		Offsets = offsets;
		Advances = advances;
		Clusters = clusters;
	}

	/// <summary>Glyph indices into the font.</summary>
	public ushort[] Glyphs { get; }

	/// <summary>Per-glyph pen offset (pixels) applied on top of the running advance when placing the glyph.</summary>
	public Vector2[] Offsets { get; }

	/// <summary>Per-glyph horizontal advance in pixels.</summary>
	public float[] Advances { get; }

	/// <summary>Per-glyph originating cluster (index into the shaped text).</summary>
	public int[] Clusters { get; }

	/// <summary>Number of glyphs in the run.</summary>
	public int Count => Glyphs.Length;
}

/// <summary>Text run direction handed to <see cref="IFont.Shape"/> by the (bidi-resolved) layout engine.</summary>
public enum TextDirection
{
	LeftToRight,
	RightToLeft,
}

/// <summary>One drawable element of a shaped glyph run (see <see cref="IFont.BuildGlyphRun"/>): a monochrome outline,
/// a vector colour glyph, or a rasterized colour glyph.</summary>
public abstract record GlyphRunElement;

/// <summary>The run's monochrome outline glyphs merged into one positioned geometry; fill with the run's text colour.
/// The caller disposes <see cref="Outline"/>.</summary>
public sealed record GlyphOutline(IGeometry Outline) : GlyphRunElement;

/// <summary>A colour glyph as positioned vector layers (D2D COLR); fill each layer's geometry with its own colour, in
/// order. The caller disposes each layer's geometry.</summary>
public sealed record GlyphColorLayers(IReadOnlyList<GlyphColorLayer> Layers) : GlyphRunElement;

/// <summary>One layer of a <see cref="GlyphColorLayers"/> colour glyph.</summary>
public readonly record struct GlyphColorLayer(IGeometry Geometry, Color Color);

/// <summary>A colour glyph the font could only rasterize: <see cref="Pixels"/> holds BGRA8888-premultiplied pixels of
/// size <see cref="PixelWidth"/>×<see cref="PixelHeight"/>, to be drawn at (<see cref="X"/>, <see cref="Y"/>).</summary>
public sealed record GlyphImage(byte[] Pixels, int PixelWidth, int PixelHeight, float X, float Y) : GlyphRunElement;
