#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font handle: the text layer talks only to this (never to a concrete Skia/native font type). It turns a
/// shaped glyph run into drawable output, exposes the metrics the layout code needs, answers glyph-coverage queries,
/// and serves the raw sfnt tables used to build the shaper (HarfBuzz) face. A backend impl (<c>SkiaFont</c>) may use
/// its native font internally; <c>ManagedFont</c> is fully SkiaSharp-free. Font <em>resolution</em> (family/style →
/// face) and shaping stay outside this handle.
/// </summary>
public interface IFont
{
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

	// --- Shaper face source: the raw sfnt so the text layer can build a HarfBuzz face without touching a Skia
	//     typeface. SkiaFont serves tables from its (already variable-instanced) typeface, so shaping is unchanged. ---

	/// <summary>The font's units-per-em (design grid), needed to scale shaped advances.</summary>
	int UnitsPerEm { get; }

	/// <summary>Returns the bytes of the sfnt table with the given tag, or null if absent.</summary>
	byte[]? GetFontTable(uint tag);
}

/// <summary>A color glyph rasterized to an <see cref="IImage"/>, with the destination rectangle to draw it at.</summary>
public readonly record struct PositionedGlyphImage(IImage Image, float X, float Y, float Width, float Height);
