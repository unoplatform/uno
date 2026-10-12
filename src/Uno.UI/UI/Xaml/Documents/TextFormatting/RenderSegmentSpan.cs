using System;

namespace Microsoft.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents a span of a segment that is rendered on a render line.
	/// </summary>
	/// <param name="Segment">The entire segment that the span is a part of. The segment gets broken into spans based on rendering needs (e.g. wrapping).</param>
	/// <param name="GlyphsStart">The index of the first rendered glyph in Segment as an offset from the span. i.e. Segment[GlyphsStart] will get the first rendered glyph in the span.</param>
	/// <param name="GlyphsLength">The number of rendered glyphs in the span. i.e. Segment[GlyphsStart + GlyphsLength - 1] will get the last rendered glyph in the span. This includes rendered leading and trailing spaces but not newline glyphs ('\r', '\n', etc.) and doesn't include any non-rendered glyphs. Non-rendered glyphs are leading or trailing spaces that don't fit the rendering width. In that case, the spaces are just not rendered at all and don't get added to the next line like any other glyph would.</param>
	/// <param name="LeadingSpaces">The number of rendered leading spaces in the span.</param>
	/// <param name="TrailingSpaces">The number of rendered trailing spaces in the span.</param>
	/// <param name="Width">The width of the span as rendered. This includes rendered trailing spaces.</param>
	/// <param name="WidthWithoutTrailingSpaces">The width of the span as rendered without any trailing spaces.</param>
	/// <param name="CharacterStart">The offset, in UTF-16 code units from the segment start, of the first character the span covers, rendered or not.</param>
	/// <param name="CharacterLength">The number of UTF-16 code units the span covers, including non-rendered leading and trailing spaces but not the line break. A cluster of several code units (a surrogate pair, a combining sequence) renders as fewer glyphs, so this can exceed the glyph count.</param>
	internal record RenderSegmentSpan(Segment Segment, int GlyphsStart, int GlyphsLength, int LeadingSpaces, int TrailingSpaces, float CharacterSpacing, float Width, float WidthWithoutTrailingSpaces, int CharacterStart, int CharacterLength)
	{
		/// <summary>
		/// Gets whether the span carries its segment's line break: it is a LineBreak element, or the last text before a line break character.
		/// </summary>
		public bool EndsInNewLine => Segment.Inline switch
		{
			// A LineBreak segment has no Text to inspect (Segment.Text throws); the break is its whole content.
			LineBreak => Segment.LineBreakLength > 0,
			Run => Segment.LineBreakAfter && Segment.Text.TrimEnd().Length <= CharacterStart + CharacterLength,
			_ => false,
		};

		/// <summary>
		/// Gets the number of UTF-16 code units the span occupies in its paragraph's character space, including the line break it carries.
		/// </summary>
		public int CharacterLengthWithNewLine => CharacterLength + (EndsInNewLine ? Segment.LineBreakLength : 0);

		/// <summary>
		/// Creates a span whose character range covers the glyphs [<paramref name="fullGlyphsStart"/>, <paramref name="fullGlyphsStart"/> + <paramref name="fullGlyphsLength"/>),
		/// the rendered ones and the non-rendered leading and trailing spaces.
		/// </summary>
		public static RenderSegmentSpan Create(Segment segment, int glyphsStart, int glyphsLength, int leadingSpaces, int trailingSpaces, float characterSpacing, float width, float widthWithoutTrailingSpaces, int fullGlyphsStart, int fullGlyphsLength)
		{
			var characterStart = segment.GetCharacterOffset(fullGlyphsStart);
			var characterEnd = segment.GetCharacterOffset(fullGlyphsStart + fullGlyphsLength);
			return new(segment, glyphsStart, glyphsLength, leadingSpaces, trailingSpaces, characterSpacing, width, widthWithoutTrailingSpaces, characterStart, characterEnd - characterStart);
		}

		/// <summary>
		/// Gets the index, among the rendered glyphs, of the first glyph at or after <paramref name="characterOffset"/> (relative to <see cref="CharacterStart"/>),
		/// clamped to [0, <see cref="GlyphsLength"/>].
		/// </summary>
		public int GetRenderedGlyphIndex(int characterOffset)
			=> Segment.Inline is Run
				? Math.Clamp(Segment.GetGlyphIndex(CharacterStart + characterOffset) - GlyphsStart, 0, GlyphsLength)
				: 0;

		/// <summary>
		/// Gets the offset, relative to <see cref="CharacterStart"/>, of the cluster of the rendered glyph at <paramref name="renderedGlyphIndex"/>.
		/// </summary>
		public int GetCharacterOffset(int renderedGlyphIndex) => Segment.GetCharacterOffset(GlyphsStart + renderedGlyphIndex) - CharacterStart;
	}
}
