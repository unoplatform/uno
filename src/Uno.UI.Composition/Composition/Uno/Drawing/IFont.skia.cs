#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font handle: the text layer talks only to this (never to a concrete Skia/native font type). It
/// <em>shapes</em> a text run into positioned glyphs, turns those glyphs into drawable output, exposes the metrics
/// the layout code needs, and answers glyph-coverage queries. Shaping is a font capability, so the shaper
/// (HarfBuzz / CoreText / DirectWrite) is an implementation detail — no raw sfnt tables leak onto the seam. A
/// backend impl (<c>SkiaFont</c>) may use its native font internally; <c>ManagedFont</c> is fully SkiaSharp-free.
/// Font <em>resolution</em> (family/style → face) stays outside this handle (<see cref="IFontProvider"/>).
/// </summary>
public interface IFont
{
	/// <summary>
	/// Shapes a run of text (already itemized to a single font/script/direction by the layout engine) into
	/// positioned glyphs. Offsets and advances are in pixels at this font's size; clusters map each glyph back to
	/// the index in <paramref name="text"/> it originated from. <paramref name="enableLigatures"/> gates the
	/// OpenType <c>liga</c> feature (the layout engine disables it where ligatures would break caret/selection).
	/// Glyphs are returned in the shaper's output order (not reversed for RTL).
	/// </summary>
	GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true);

	/// <summary>
	/// Shapes a run, letting the shaper guess the run's direction from its script (used by the segment itemizer that
	/// hasn't resolved bidi itself) and reporting it back via <paramref name="resolvedDirection"/>. Otherwise
	/// identical to <see cref="Shape(ReadOnlySpan{char}, TextDirection, bool)"/>.
	/// </summary>
	GlyphRun Shape(ReadOnlySpan<char> text, out TextDirection resolvedDirection, bool enableLigatures = true);

	/// <summary>
	/// Builds the combined filled outline of the run's outline glyphs (color glyphs are excluded — draw those
	/// via <see cref="AppendColorGlyphImages"/>). Each glyph is placed at its position, shifted by <paramref name="baselineY"/>.
	/// </summary>
	IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY);

	/// <summary>Whether the font may contain color glyphs; when false, callers can skip <see cref="AppendColorGlyphImages"/>.</summary>
	bool HasColorGlyphs { get; }

	/// <summary>Appends the run's color glyphs as positioned images (no-op when <see cref="HasColorGlyphs"/> is false).</summary>
	void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output);

	// --- Metrics (pixels at this font's size; SkiaSharp sign convention: Ascent <= 0 above the baseline,
	//     Descent >= 0 below it, so line height = Descent - Ascent). ---

	/// <summary>Distance from the baseline to the top of the text, negative (above the baseline).</summary>
	float Ascent { get; }

	/// <summary>Distance from the baseline to the bottom of the text, positive (below the baseline).</summary>
	float Descent { get; }

	/// <summary>Recommended extra spacing between lines, in pixels.</summary>
	float LineGap { get; }

	/// <summary>Underline stroke offset from the baseline (positive below), or null if the font doesn't specify one.</summary>
	float? UnderlinePosition { get; }

	/// <summary>Underline stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	float? UnderlineThickness { get; }

	/// <summary>Strikeout stroke offset from the baseline (negative above), or null if the font doesn't specify one.</summary>
	float? StrikeoutPosition { get; }

	/// <summary>Strikeout stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	float? StrikeoutThickness { get; }

	// --- Glyph coverage ("does *this* font have a glyph for the codepoint?"; which font to fall back to is a
	//     resolution concern, not a per-font one). ---

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
/// <see cref="Offsets"/> and <see cref="Advances"/> are in pixels at the font's size; <see cref="Clusters"/> maps
/// each glyph to the originating index in the shaped text (for hit-testing / cluster grouping).
/// </summary>
public readonly struct GlyphRun
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

/// <summary>A color glyph rasterized to an <see cref="IImage"/>, with the destination rectangle to draw it at.</summary>
public readonly record struct PositionedGlyphImage(IImage Image, float X, float Y, float Width, float Height);
