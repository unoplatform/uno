using System;
using System.Collections.Generic;
using HarfBuzzSharp;
using SkiaSharp;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.Extensions;
using Buffer = HarfBuzzSharp.Buffer;
using GlyphInfo = Microsoft.UI.Xaml.Documents.TextFormatting.GlyphInfo;

#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Run
	{
		private const int SpacesPerTab = 8;
		private const int TabCodepoint = 3; // we give tab the same HarfBuzz codepoint as Space (which is not equal to `(ushort)' '` for some reason

		private List<Segment>? _segments;

		internal IReadOnlyList<Segment> Segments => _segments ??= _segments = GetSegments();

		private List<Segment> GetSegments()
		{
			// TODO: Implement Bidi algorithm here to split segments by direction prior to doing the below processing on each directional piece.
			// TODO: Implement fallback font for international char segments

			List<Segment> segments = new();
			using HarfBuzzSharp.Buffer buffer = new();
			var fontInfo = FontInfo;
			var defaultFont = fontInfo.Font;
			int defaultFontScale;
			float defaultTextSizeY, defaultTextSizeX;
			var font = defaultFont;
			var paint = Paint;
			var fontSize = paint.TextSize;

			font.GetScale(out int fontScale, out _);
			float textSizeY = fontInfo.SKFontSize / fontScale;
			float textSizeX = textSizeY * fontInfo.SKFontScaleX;
			defaultFontScale = fontScale;
			defaultTextSizeX = textSizeX;
			defaultTextSizeY = textSizeY;
			var text = Text.AsSpan();
			int s = 0;
			int i = 0;
			SKTypeface? symbolTypeface = default;
			FontDetails? fallbackFont = default;
			var surrogate = false;

			while (i < text.Length)
			{
				int leadingSpaces = 0;
				int trailingSpaces = 0;
				int lineBreakLength = 0;
				bool wordBreakAfter = false;

				if (symbolTypeface is { })
				{
					var fi = FontDetailsCache.GetFont(symbolTypeface.FamilyName, (float)FontSize, FontWeight, FontStyle);
					font = fi.Font;
					wordBreakAfter = Unicode.HasWordBreakOpportunityAfter(text, i) || (i + 1 < text.Length && Unicode.HasWordBreakOpportunityBefore(text, i + 1));
					font.GetScale(out fontScale, out _);
					textSizeY = fontSize / fontScale;
					textSizeX = textSizeY * paint.TextScaleX;
					i += surrogate ? 2 : 1;
					surrogate = false;
					fallbackFont = fi;
					symbolTypeface = default;
				}
				else
				{
					// Count leading spaces
					while (i < text.Length && char.IsWhiteSpace(text[i]) && !Unicode.IsLineBreak(text[i]))
					{
						leadingSpaces++;
						i++;
					}

					// Keep the segment going until we hit a word break opportunity or a line break
					while (i < text.Length)
					{
						if (ProcessLineBreak(text, ref i, ref lineBreakLength))
						{
							break;
						}

						if (Unicode.HasWordBreakOpportunityAfter(text, i) || (i + 1 < text.Length && Unicode.HasWordBreakOpportunityBefore(text, i + 1)))
						{
							if (char.IsWhiteSpace(text[i]))
							{
								trailingSpaces++;
							}
							wordBreakAfter = true;
							i++;
							break;
						}

						if (i + 1 < text.Length
							&& char.IsSurrogate(text[i])
							&& char.IsSurrogatePair(text[i], text[i + 1]))
						{
							var fontManager = SKFontManager.Default;
							var codepoint = (int)((text[i] - 0xD800) * 0x400 + (text[i + 1] - 0xDC00) + 0x10000);
							symbolTypeface = fontManager
								.MatchCharacter(codepoint);

							if (symbolTypeface is not null)
							{
								surrogate = true;
							}
							else
							{
								// Under some Linux systems, the symbol may not be found
								// in the default font and 
								// we have to skip the character and continue segments
								// evaluation.

								if (this.Log().IsEnabled(LogLevel.Trace))
								{
									this.Log().Trace($"Failed to match surrogate in the default system font (0x{codepoint:X4}, {(char)codepoint})");
								}

								i++;
							}
							break;
						}
						else if (!fontInfo.SKFont.ContainsGlyph(text[i]))
						{
							symbolTypeface = SKFontManager.Default
								.MatchCharacter(text[i]);

							if (symbolTypeface is null)
							{
								// Under some Linux systems, the symbol may not be found
								// in the default font and 
								// we have to skip the character and continue segments
								// evaluation.
								if (this.Log().IsEnabled(LogLevel.Trace))
								{
									this.Log().Trace($"Failed to match symbol in the default system font (0x{(int)text[i]:X4}, {text[i]})");
								}

								i++;
							}

							break;
						}
						i++;
					}

					// Tack on any trailing spaces or line breaks if this segment does not yet end in a line break

					if (lineBreakLength == 0)
					{
						while (i < text.Length)
						{
							if (ProcessLineBreak(text, ref i, ref lineBreakLength))
							{
								break;
							}

							if (char.IsWhiteSpace(text[i]))
							{
								trailingSpaces++;
								i++;
							}
							else
							{
								break;
							}
						}
					}
				}

				int length = i - s;
				if (length > 0)
				{

					if (lineBreakLength == 2)
					{
						buffer.AddUtf16(text.Slice(s, length - 1)); // Skip second line break char so that it is considered part of the same cluster as the first
					}
					else
					{
						buffer.AddUtf16(text.Slice(s, length));
					}

					// TODO: Set the segment properties instead of using HB guessing like below.
					// - Set direction using Bidi algorithm.
					// - Set Language and Script on buffer. From HarfBuzz docs:

					// Script is crucial for choosing the proper shaping behaviour for scripts that require it (e.g. Arabic) and the which OpenType features defined
					// in the font to be applied.

					// Languages are crucial for selecting which OpenType feature to apply to the buffer which can result in applying language-specific behaviour.
					// Languages are orthogonal to the scripts, and though they are related, they are different concepts and should not be confused with each other.

					// buffer.Direction = ...
					// buffer.Language = ...
					// buffer.Script = ...

					// Guess the above properties for now before shaping:
					buffer.GuessSegmentProperties();
					var direction = buffer.Direction == Direction.LeftToRight ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

					font.Shape(buffer);

					if (buffer.Direction == Direction.RightToLeft)
					{
						buffer.ReverseClusters();
					}

					var glyphs = GetGlyphs(buffer, Text.AsSpan(s, length), fontInfo, s, textSizeX, textSizeY);

					var segment = new Segment(this, direction, s, length, leadingSpaces, trailingSpaces, lineBreakLength, wordBreakAfter, glyphs, fallbackFont);

					segments.Add(segment);
					buffer.ClearContents();
				}
				fallbackFont = default;
				font = defaultFont;
				textSizeY = defaultTextSizeY;
				textSizeX = defaultTextSizeX;
				fontScale = defaultFontScale;
				s = i;
			}

			return segments;

			// Local functions:

			static bool ProcessLineBreak(ReadOnlySpan<char> text, ref int i, ref int lineBreakLength)
			{
				if (Unicode.IsLineBreak(text[i]))
				{
					if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
					{
						lineBreakLength = 2;
						i += 2;
					}
					else
					{
						lineBreakLength = 1;
						i++;
					}

					return true;
				}

				return false;
			}

			static List<GlyphInfo> GetGlyphs(Buffer buffer, ReadOnlySpan<char> textSpan, FontDetails fontInfo, int clusterStart, float textSizeX, float textSizeY)
			{
				int length = buffer.Length;
				var hbGlyphs = buffer.GetGlyphInfoSpan();
				var hbPositions = buffer.GetGlyphPositionSpan();

				List<TextFormatting.GlyphInfo> glyphs = new(length);

				for (int i = 0; i < length; i++)
				{
					var hbGlyph = hbGlyphs[i];
					var hbPos = hbPositions[i];

					// We add special handling for tabs, which don't get rendered correctly, and treated as an unknown glyph
					TextFormatting.GlyphInfo glyph = new(
						textSpan[i] == '\t' ? (ushort)TabCodepoint : (ushort)hbGlyph.Codepoint,
						clusterStart + (int)hbGlyph.Cluster,
						textSpan[i] == '\t' ? _measureTab(fontInfo.SKFont) : hbPos.XAdvance * textSizeX,
						hbPos.XOffset * textSizeX,
						hbPos.YOffset * textSizeY
					);

					glyphs.Add(glyph);
				}

				return glyphs;
			}
		}

		partial void InvalidateSegmentsPartial() => _segments = null;

		private static Func<SKFont, float> _measureTab =
			((Func<SKFont, float>?)(font =>
			{
				ReadOnlySpan<ushort> space = [' '];
				return font.MeasureText(space) * SpacesPerTab;
			}))
			.AsMemoized();
	}
}
