namespace Windows.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents a span of a segment that is rendered on a render line.
	/// </summary>
	/// <param name="Segment">The entire segment that the span is a part of. The segment gets broken into spans based on rendering needs (e.g. wrapping).</param>
	/// <param name="GlyphsStart">The index of the span's first glyph in Segment. i.e. Segment[GlyphsStart] will get the first glyph in the span.</param>
	/// <param name="GlyphsLength">The number of rendered glyphs in the span. i.e. Segment[GlyphsStart + GlyphsLength - 1] will get the last glyph in the span. This includes rendered trailing spaces but not newline glyphs ('\r', '\n', etc.) and doesn't include any non-rendered glyphs. Non-rendered glyphs are trailing spaces that don't fit the rendering width. In that case, the spaces are just not rendered at all and don't get added to the next line like any other glyph would.</param>
	/// <param name="TrailingSpaces">The number of rendered trailing spaces in the span. Similarly to GlyphsLength, this includes rendered trailing spaces but not newlines ('\r', '\n', etc.) and doesn't include any non-rendered glyphs. Non-rendered glyphs are trailing spaces only.</param>
	/// <param name="Width">The width of the span as rendered. This includes rendered trailing spaces.</param>
	/// <param name="WidthWithoutTrailingSpaces">The width of the span as rendered without any trailing spaces.</param>
	/// <param name="Width">The number of rendered trailing spaces in the span. Similarly to GlyphsLength, this includes rendered trailing spaces but not newlines ('\r', '\n', etc.) and doesn't include any non-rendered glyphs. Non-rendered glyphs are trailing spaces only.</param>
	/// <param name="FullGlyphsLength">The number of rendered and non-rendered glyphs in the span. i.e. Segment[GlyphsStart + FullGlyphsLength - 1] will get the last glyph in the span. This includes all trailing spaces, even the ones that aren't actually rendered due to running out of width. This does not include newline glyphs.</param>
	internal record RenderSegmentSpan(Segment Segment, int GlyphsStart, int GlyphsLength, int TrailingSpaces, float CharacterSpacing, float Width, float WidthWithoutTrailingSpaces, int FullGlyphsLength);
}
