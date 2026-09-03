#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using GlyphInfo = Microsoft.UI.Xaml.Documents.TextFormatting.GlyphInfo;

using SegmentInfo = (int LeadingSpaces, int TrailingSpaces, int LineBreakLength, Uno.UI.Composition.Drawing.IFont? Font, int NextStartingIndex);

namespace Microsoft.UI.Xaml.Documents
{
	partial class Run
	{
		private List<Segment>? _segments;

		internal IReadOnlyList<Segment> Segments => _segments ??= GetSegments();

		public global::Microsoft.UI.Xaml.FlowDirection FlowDirection
		{
			get => (global::Microsoft.UI.Xaml.FlowDirection)this.GetValue(FlowDirectionProperty);
			set => this.SetValue(FlowDirectionProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty FlowDirectionProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(FlowDirection), typeof(FlowDirection),
				typeof(Run),
				new FrameworkPropertyMetadata(default(FlowDirection), FrameworkPropertyMetadataOptions.Inherits, (DependencyObject dO, DependencyPropertyChangedEventArgs args) => ((Run)dO).OnFlowDirectionChanged()));

		private void OnFlowDirectionChanged()
		{
			InvalidateInlines(false);
		}

		private static (int CodePoint, int Length) GetCodePoint(ReadOnlySpan<char> text, int i)
		{
			if (i + 1 < text.Length &&
				char.IsSurrogate(text[i]) &&
				char.IsSurrogatePair(text[i], text[i + 1]))
			{
				var codepoint = (int)((text[i] - 0xD800) * 0x400 + (text[i + 1] - 0xDC00) + 0x10000);
				return (codepoint, 2);
			}

			return (text[i], 1);
		}

		private SegmentInfo GetSegmentStartingFrom(int i, ReadOnlySpan<char> text)
		{
			var fontInfo = FontInfo;

			var defaultFont = fontInfo.FontHandle;

			if (i < text.Length && text[i] == '\t')
			{
				return (LeadingSpaces: 0, TrailingSpaces: 0, LineBreakLength: 0, Font: defaultFont, NextStartingIndex: i + 1);
			}

			int leadingSpaces = 0;
			int trailingSpaces = 0;
			int lineBreakLength = 0;
			IFont? segmentFont = null;

			// Count leading spaces
			while (i < text.Length && char.IsWhiteSpace(text[i]) && !Unicode.IsLineBreak(text[i]) && text[i] != '\t')
			{
				leadingSpaces++;

				// The leading spaces should use the originally specified font.
				// This is very important for two scenarios:
				// 1. A fallback font that may be calculated later in this method may have different AdvanceX value for space character
				// 2. The specified font could actually contain actual drawing for the space character. This is extremely uncommon and is currently
				//    not supported by the drawing logic, where we just advance x-coordinate to emulate space characters.
				segmentFont = defaultFont;

				i++;
			}

			// Keep the segment going until we hit a word break opportunity or a line break
			while (i < text.Length)
			{
				if (ProcessLineBreak(text, ref i, ref lineBreakLength))
				{
					break;
				}

				// Since tabs require special handling, we put tabs in separate segments.
				// Also, we don't consider tabs "spaces" since they don't get the general space treatment.
				if (text[i] == '\t')
				{
					return (leadingSpaces, trailingSpaces, lineBreakLength, segmentFont, i);
				}

				if (Unicode.HasWordBreakOpportunityAfter(text, i) || (i + 1 < text.Length && Unicode.HasWordBreakOpportunityBefore(text, i + 1)))
				{
					if (char.IsWhiteSpace(text[i]))
					{
						if (segmentFont is not null && !SameFont(segmentFont, defaultFont))
						{
							// Don't include the trailing space in the current segment if it doesn't use the originally specified font.
							// The reasons are the same as explained for leading spaces in the beginning of this method.
							break;
						}

						trailingSpaces++;
					}

					i++;
					break;
				}

				var (codepoint, codepointLength) = GetCodePoint(text, i);

				// This legacy segmentation path is synchronous, so it only consults the synchronously-available (installed)
				// match; deferred fallback (e.g. browser Noto fetch) is handled by the active UnicodeText path.
				IFont? currentFont;
				if (defaultFont.ContainsGlyph(codepoint))
				{
					currentFont = defaultFont;
				}
				else
				{
					var match = FontProvider.Current.MatchCharacterAsync(codepoint, FontWeight, FontStretch, FontStyle, (float)FontSize);
					currentFont = match.IsCompletedSuccessfully ? match.Result : null;
				}

				if (currentFont is null)
				{
					// The requested glyph isn't found by the OS.
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Failed to match codepoint '{codepoint}' (length: {codepointLength}).");
					}

					// Move over the current codepoint.
					i += codepointLength;
				}
				else if (segmentFont is null || SameFont(currentFont, segmentFont))
				{
					segmentFont = currentFont;
					i += codepointLength;
				}
				else
				{
					// Always break the current segment if the previous font and the current font are both non-null
					// and are different.
					break;
				}
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

					if (char.IsWhiteSpace(text[i]) && text[i] != '\t')
					{
						if (segmentFont is not null && !SameFont(segmentFont, defaultFont))
						{
							// Don't include the trailing space in the current segment if it doesn't use the originally specified font.
							// The reasons are the same as explained for leading spaces in the beginning of this method.
							break;
						}

						trailingSpaces++;
						i++;
					}
					else
					{
						break;
					}
				}
			}

			return (leadingSpaces, trailingSpaces, lineBreakLength, segmentFont, i);
		}

		// Two handles are the "same font" for segment-grouping when they refer to the same family (fallback
		// resolution may return distinct IFont instances for the same physical font).
		private static bool SameFont(IFont a, IFont b) => ReferenceEquals(a, b) || a.FamilyName == b.FamilyName;

		private List<Segment> GetSegments()
		{
			// TODO: Implement Bidi algorithm here to split segments by direction prior to doing the below processing on each directional piece.
			// TODO: Implement fallback font for international char segments
			List<Segment> segments = new();
			var fontInfo = FontInfo;
			var defaultFontHandle = fontInfo.FontHandle;

			var text = Text.AsSpan();
			int i = 0;

			while (i < text.Length)
			{
				var (leadingSpaces, trailingSpaces, lineBreakLength, fontHandle, nextStartingIndex) = GetSegmentStartingFrom(i, text);

				int length = nextStartingIndex - i;
				FontDetails? fallbackFont = null;
				IFont segmentFont;
				if (fontHandle is not null && !SameFont(fontHandle, defaultFontHandle))
				{
					// MatchCharacter returns installed fonts synchronously, so the fallback FontDetails is
					// built directly from the resolved handle (no async family re-resolution).
					fallbackFont = FontDetails.Create(fontHandle, (float)FontSize);
					segmentFont = fallbackFont.FontHandle;
				}
				else
				{
					segmentFont = defaultFontHandle;
				}

				if (length > 0)
				{
					// Skip the second line break char so it stays part of the same cluster as the first.
					var shapedLength = lineBreakLength == 2 ? length - 1 : length;

					// Legacy non-bidi path (superseded by UnicodeText): shape each segment in the run's own
					// FlowDirection rather than resolving bidi. Ligatures are disabled because a TextBox needs each
					// source char to stay separately addressable (uno#15528, uno#16788).
					var direction = this.FlowDirection;
					var textDirection = direction == FlowDirection.RightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;
					var glyphRun = segmentFont.Shape(text.Slice(i, shapedLength), textDirection, enableLigatures: false);
					var glyphs = GetGlyphs(glyphRun, i, textDirection is TextDirection.RightToLeft);

					Debug.Assert(!(Text.AsSpan(i, length).Contains('\t')) || length == 1);
					if (length == 1 && text[i] == '\t')
					{
						glyphs[0] = glyphs[0] with { GlyphId = defaultFontHandle.GetGlyphIndex(' ') };
					}

					var segment = new Segment(this, direction, i, length, leadingSpaces, trailingSpaces, lineBreakLength, glyphs, fallbackFont);

					segments.Add(segment);
				}

				i = nextStartingIndex;
			}

			return segments;

			// Local functions:

			static List<GlyphInfo> GetGlyphs(GlyphRun glyphRun, int clusterStart, bool rtl)
			{
				var count = glyphRun.Count;
				List<TextFormatting.GlyphInfo> glyphs = new(count);

				// Mirror HarfBuzz's ReverseClusters for RTL runs. Ligatures are disabled here, so clusters are 1:1 and
				// a plain reverse matches. Offsets/advances are already in pixels (IFont.Shape scaled them).
				for (var k = 0; k < count; k++)
				{
					var index = rtl ? count - 1 - k : k;
					glyphs.Add(new TextFormatting.GlyphInfo(
						glyphRun.Glyphs[index],
						clusterStart + glyphRun.Clusters[index],
						glyphRun.Advances[index],
						glyphRun.Offsets[index].X,
						glyphRun.Offsets[index].Y));
				}

				return glyphs;
			}
		}

		internal override void InvalidateTextScaleFontInfo()
		{
			base.InvalidateTextScaleFontInfo();
			_segments = null;
		}

		private static bool ProcessLineBreak(ReadOnlySpan<char> text, ref int i, ref int lineBreakLength)
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

		partial void InvalidateSegmentsPartial() => _segments = null;
	}
}
