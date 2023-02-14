using System;
using System.Collections.Generic;
using System.Text;
using HarfBuzzSharp;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;

#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class Run
	{
		private List<Segment>? _segments;

		internal IReadOnlyList<Segment> Segments => _segments ??= _segments = GetSegments();

		private List<Segment> GetSegments()
		{
			// TODO: Implement Bidi algorithm here to split segments by direction prior to doing the below processing on each directional piece.
			// TODO: Implement fallback font for emoji/international char segments

			List<Segment> segments = new();
			using HarfBuzzSharp.Buffer buffer = new();
			var font = FontInfo.Font;
			var paint = Paint;

			font.GetScale(out int fontScale, out _);
			float textSizeY = paint.TextSize / fontScale;
			float textSizeX = textSizeY * paint.TextScaleX;

			var text = Text.AsSpan();
			int s = 0;
			int i = 0;

			while (i < text.Length)
			{
				int leadingSpaces = 0;
				int trailingSpaces = 0;
				int lineBreakLength = 0;
				bool wordBreakAfter = false;

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

				int length = i - s;

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

				var glyphs = GetGlyphs(buffer, s, textSizeX, textSizeY);
				var segment = new Segment(this, direction, s, length, leadingSpaces, trailingSpaces, lineBreakLength, wordBreakAfter, glyphs);

				segments.Add(segment);
				buffer.ClearContents();
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

			static List<TextFormatting.GlyphInfo> GetGlyphs(HarfBuzzSharp.Buffer buffer, int clusterStart, float textSizeX, float textSizeY)
			{
				int length = buffer.Length;
				var hbGlyphs = buffer.GlyphInfos;
				var hbPositions = buffer.GlyphPositions;

				List<TextFormatting.GlyphInfo> glyphs = new(length);

				for (int i = 0; i < length; i++)
				{
					var hbGlyph = hbGlyphs[i];
					var hbPos = hbPositions[i];

					TextFormatting.GlyphInfo glyph = new(
						(ushort)hbGlyph.Codepoint,
						clusterStart + (int)hbGlyph.Cluster,
						hbPos.XAdvance * textSizeX,
						hbPos.XOffset * textSizeX,
						hbPos.YOffset * textSizeY
					);

					glyphs.Add(glyph);
				}

				return glyphs;
			}
		}

		partial void InvalidateSegmentsPartial() => _segments = null;
	}
}
