using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Markup;
using System.Diagnostics;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using GlyphInfo = Microsoft.UI.Xaml.Documents.TextFormatting.GlyphInfo;
#nullable enable
using SegmentInfo = (int LeadingSpaces, int TrailingSpaces, int LineBreakLength, Uno.UI.Composition.Drawing.IFont? Font, int NextStartingIndex);
#nullable disable

namespace Microsoft.UI.Xaml.Documents
{
	[ContentProperty(Name = nameof(Text))]
	public partial class Run : Inline
	{
		#region Text Dependency Property

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(Run),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					propertyChangedCallback: (s, e) => ((Run)s).OnTextChanged()
				)
			);

		public void OnTextChanged()
		{
			OnTextChangedPartial();
			// The run's length feeds every ancestor's cached position counts, so drop those first.
			MarkDirty();
			InvalidateInlines(true);
			InvalidateSegmentsPartial();
		}

		partial void OnTextChangedPartial();

		#endregion

		protected override void OnForegroundChanged()
		{
			base.OnForegroundChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontSizeChanged()
		{
			base.OnFontSizeChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStyleChanged()
		{
			base.OnFontStyleChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontStretchChanged()
		{
			base.OnFontStretchChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnFontWeightChanged()
		{
			base.OnFontWeightChanged();
			InvalidateInlinesForFormatChange();
			InvalidateSegmentsPartial();
		}

		protected override void OnBaseLineAlignmentChanged()
		{
			base.OnBaseLineAlignmentChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnCharacterSpacingChanged()
		{
			base.OnCharacterSpacingChanged();
			InvalidateInlinesForFormatChange();
		}

		protected override void OnTextDecorationsChanged()
		{
			base.OnTextDecorationsChanged();
			InvalidateInlinesForFormatChange();
		}

		partial void InvalidateSegmentsPartial();

#nullable enable
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

		private void OnFlowDirectionChanged() => InvalidateInlinesForFormatChange();

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
		// resolution may return distinct IFont instances for the same physical font). An empty family name
		// carries no identity, so it never matches: a font with no typeface would otherwise group with any other.
		private static bool SameFont(IFont a, IFont b)
			=> ReferenceEquals(a, b) || (a.FamilyName.Length > 0 && a.FamilyName == b.FamilyName);

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
				// By reference, not by family: the handle is only here because the default could not render this
				// codepoint, so a packaged subset that declares the same family name as the installed font it was
				// cut from still has to be treated as a different font -- grouping them draws .notdef.
				if (fontHandle is not null && !ReferenceEquals(fontHandle, defaultFontHandle))
				{
					// The handle already carries the requested weight/stretch/style (the provider resolved the
					// fallback family for them), so it only needs wrapping with this run's size.
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

					// Legacy non-bidi path (superseded by UnicodeText): the shaper guesses each segment's direction
					// from its script. Ligatures are disabled because a TextBox needs each source char to stay
					// separately addressable (uno#15528, uno#16788).
					var glyphRun = segmentFont.Shape(text.Slice(i, shapedLength), out var textDirection, enableLigatures: false);
					var direction = textDirection is TextDirection.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
					if (direction == FlowDirection.LeftToRight &&
						segments.Count > 0 && segments[^1].Direction == FlowDirection.RightToLeft &&
						trailingSpaces + leadingSpaces == length)
					{
						// A spaces-only segment is guessed LeftToRight; keep it with the RightToLeft segment it follows.
						direction = FlowDirection.RightToLeft;
					}

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

				// Offsets/advances are already in pixels (IFont.Shape scaled them).
				for (var index = 0; index < count; index++)
				{
					glyphs.Add(new TextFormatting.GlyphInfo(
						glyphRun.Glyphs[index],
						clusterStart + glyphRun.Clusters[index],
						glyphRun.Advances[index],
						glyphRun.Offsets[index].X,
						glyphRun.Offsets[index].Y));
				}

				if (rtl)
				{
					// Mirror hb_buffer_reverse_clusters: the shaper emits an RTL run in visual order, so reversing it
					// gives ascending clusters, and re-reversing each cluster keeps a mark next to the base it attaches
					// to (the pen advances in list order, so a flat reverse drops it one advance away).
					glyphs.Reverse();
					for (var start = 0; start < count;)
					{
						var end = start + 1;
						while (end < count && glyphs[end].Cluster == glyphs[start].Cluster)
						{
							end++;
						}

						glyphs.Reverse(start, end - start);
						start = end;
					}
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
#nullable disable
	}
}
