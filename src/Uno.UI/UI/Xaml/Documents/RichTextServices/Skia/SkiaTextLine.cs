// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextLine.h, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Skia implementation of <see cref="TextLine"/>. Wraps a single ParsedText
/// <see cref="RenderLine"/> and projects its metrics onto the WinUI TextLine
/// surface. Glyph rendering of the paragraph goes through ParsedText.Draw, so
/// per-line drawing, caret navigation and collapsing are filled in by later
/// stages.
/// </summary>
internal sealed class SkiaTextLine : TextLine
{
	private readonly ParsedText _parsedText;
	private readonly RenderLine _renderLine;
	private readonly int _lineIndex;
	private readonly TextParagraphProperties _paragraphProperties;

	// Paragraph-relative UTF-16 index of this line's first character, and the top of the line within the
	// paragraph. The index space matches TextLine.Length, which ParagraphNode accumulates into
	// LineMetrics.FirstCharIndex.
	private readonly int _startIndex;
	private readonly float _lineTop;

	public SkiaTextLine(
		ParsedText parsedText,
		RenderLine renderLine,
		int lineIndex,
		SkiaTextLineBreak? lineBreak,
		TextParagraphProperties paragraphProperties)
	{
		_parsedText = parsedText;
		_renderLine = renderLine;
		_lineIndex = lineIndex;
		_paragraphProperties = paragraphProperties;
		m_pTextLineBreak = lineBreak;

		m_width = renderLine.WidthWithoutTrailingSpaces;
		m_widthIncludingTrailingWhitespace = renderLine.Width;
		m_height = renderLine.Height;
		m_textHeight = renderLine.Height;

		// RenderLine.BaselineOffsetY is measured upwards from the line bottom and
		// is negative above the baseline; TextLine.Baseline is measured downwards
		// from the line top, so Baseline = Height + BaselineOffsetY.
		m_baseline = renderLine.Height + renderLine.BaselineOffsetY;
		m_textBaseline = m_baseline;

		var alignment = parsedText.TextAlignment;
		if (parsedText.FlowDirection == FlowDirection.RightToLeft)
		{
			alignment = alignment switch
			{
				TextAlignment.Left => TextAlignment.Right,
				TextAlignment.Right => TextAlignment.Left,
				_ => alignment,
			};
		}

		// WinUI justifies inside line formatting, so a justified wrapped line's own width is
		// already the column width (LsTextLine.cpp:1197-1201). ParsedText justifies at draw time
		// by spreading the spaces, so the width has to be restated here or the paragraph reports
		// - and is arranged at - the unjustified width.
		if (alignment == TextAlignment.Justify && renderLine.Wraps)
		{
			var columnWidth = (float)parsedText.AvailableSize.Width;
			if (columnWidth > m_width)
			{
				m_width = columnWidth;
				m_widthIncludingTrailingWhitespace = Math.Max(m_widthIncludingTrailingWhitespace, columnWidth);
			}
		}

		var (lineOffset, _) = renderLine.GetOffsets((float)parsedText.AvailableSize.Width, alignment);
		m_start = lineOffset;

		ComputeCharacterCounts(renderLine, out m_length, out m_trailingWhitespaceLength, out m_newlineLength);

		// ParagraphTextSource::GetTextRun always yields an EndOfParagraphRun for the paragraph terminator,
		// so even a content-free paragraph formats a line of non-zero length. ParsedText has no such run,
		// and PageNode::MeasureCore reads a zero-length child as "no content fit" and breaks the page -
		// which silently drops every later paragraph.
		if (m_length == 0 && parsedText.IsEmpty && parsedText.LineCount == 1)
		{
			m_length = 1;
		}

		m_dependentLength = 0;
		m_overhangLeading = 0;
		m_overhangTrailing = 0;

		// Alignment follows reading order when the detected paragraph direction matches the one
		// specified on the paragraph properties.
		m_alignmentFollowsReadingOrder = DetectedDirection(parsedText) == paragraphProperties.FlowDirection;

		var intervals = parsedText.GetLineIntervals();
		_startIndex = lineIndex < intervals.Count ? intervals[lineIndex].start : 0;

		float top = 0;
		var renderLines = parsedText.RenderLines;
		for (var i = 0; i < lineIndex && i < renderLines.Count; i++)
		{
			top += renderLines[i].Height;
		}
		_lineTop = top;
	}

	private static FlowDirection DetectedDirection(ParsedText parsedText)
		=> parsedText.IsBaseDirectionRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

	// One past this line's last character, in the same space as _startIndex.
	private int LineEndIndex => _startIndex + (int)m_length;

	// The render line this text line wraps.
	internal RenderLine RenderLine => _renderLine;

	// The whole-paragraph ParsedText this line was vended from. All SkiaTextLines of a
	// paragraph share one ParsedText (the SkiaTextFormatter cache); the render path
	// (RichTextVisual.Paint) draws the paragraph once via ParsedText.Draw.
	internal ParsedText ParsedText => _parsedText;

	// The zero-based index of this line within its paragraph.
	internal int LineIndex => _lineIndex;

	// Counts UTF-16 code units like ParsedText.GetLineIntervals, so that character offsets reported by the
	// layout tree line up with ParsedText hit-testing.
	private static void ComputeCharacterCounts(
		RenderLine line,
		out uint length,
		out uint trailingWhitespaceLength,
		out uint newlineLength)
	{
		uint total = 0;
		uint trailing = 0;
		uint newline = 0;

		var spans = line.SegmentSpans;
		for (var i = 0; i < spans.Count; i++)
		{
			var span = spans[i];
			total += (uint)span.CharacterLengthWithNewLine;

			if (i == spans.Count - 1)
			{
				trailing = (uint)span.TrailingSpaces;
				newline = span.EndsInNewLine ? (uint)span.Segment.LineBreakLength : 0;
			}
		}

		length = total;
		trailingWhitespaceLength = trailing;
		newlineLength = newline;
	}

	// Adds the containers of every embedded object on this line to the given set.
	internal void CollectInlineObjects(HashSet<InlineUIContainer> containers)
	{
		foreach (var segmentSpan in _renderLine.SegmentSpans)
		{
			if (segmentSpan.Segment.Inline is InlineUIContainer container && segmentSpan.Segment.IsInlineObject)
			{
				containers.Add(container);
			}
		}
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Positions the embedded inline objects of this line. Glyph layout is fixed once
	//      ParsedText has run, so only object runs need arranging.
	//------------------------------------------------------------------------
	public override void Arrange(Rect bounds)
	{
		var x = (float)bounds.X;
		if (_renderLine is { CollapsingSymbol: { } symbol, CollapsingSymbolLeads: true })
		{
			x += symbol.Width;
		}

		foreach (var segmentSpan in _renderLine.RenderOrderedSegmentSpans)
		{
			if (segmentSpan.Segment.ObjectRun is { } objectRun)
			{
				// Align the object's ascent to the line's ascent, so its baseline sits on the line's baseline.
				var objectTop = (float)bounds.Y + (m_baseline - segmentSpan.Segment.ObjectMetrics.Baseline);
				objectRun.Arrange(new Point(x, objectTop));
			}

			x += segmentSpan.Width;
		}
	}

	public override void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth)
		=> throw new NotSupportedException(
			"TODO Uno (Stage 6): per-line drawing. Paragraph rendering currently goes through ParsedText.Draw.");

	// TextLine::Collapse — trims the line's glyphs to fit collapsingWidth once the symbol is accounted
	// for, then hangs the symbol off the RenderLine so the paragraph paints it after the kept glyphs.
	public override TextLine Collapse(double collapsingWidth, TextTrimming collapsingStyle, TextCollapsingSymbol? collapsingSymbol)
	{
		if (_collapsed ||
			collapsingSymbol is not TextCollapsingCharacters symbol ||
			collapsingStyle is not (TextTrimming.CharacterEllipsis or TextTrimming.WordEllipsis))
		{
			return this;
		}

		// LsTextLine::FormatCollapsed - the formatting width excludes the symbol and never goes negative.
		var available = Math.Max(0f, (float)(collapsingWidth - symbol.Width));

		// Line Services formats the collapsed line from its first character, so the logical end is what goes.
		var kept = TrimSpansToWidth(_renderLine.SegmentSpans, available, collapsingStyle);
		if (kept is null)
		{
			// Everything fits; nothing to collapse.
			return this;
		}

		var collapsed = _renderLine.CollapseTo(kept, ShapeSymbol(symbol), symbolLeads: _parsedText.IsReadingOrderRightToLeft());

		_parsedText.ReplaceRenderLine(_lineIndex, collapsed);

		return new SkiaTextLine(_parsedText, collapsed, _lineIndex, null, _paragraphProperties) { _collapsed = true };
	}

	// Returns the spans to keep, or null when the line already fits. CharacterEllipsis cuts at the last
	// glyph that fits; WordEllipsis then backs up to the end of the previous word. Line Services never
	// formats an empty line, so a line with no room keeps its first cluster and overflows.
	private static List<RenderSegmentSpan>? TrimSpansToWidth(
		IReadOnlyList<RenderSegmentSpan> spans,
		float available,
		TextTrimming collapsingStyle)
	{
		var kept = new List<RenderSegmentSpan>();
		var width = 0f;

		foreach (var span in spans)
		{
			if (width + span.Width <= available)
			{
				width += span.Width;
				kept.Add(span);
				continue;
			}

			// This span is where the line runs out of room. Keep as many of its glyphs as fit.
			if (span.Segment.IsInlineObject)
			{
				// An object never breaks: as in WinUI, the collapsed line keeps the one that overflows.
				kept.Add(span);
				return kept;
			}

			var glyphs = span.Segment.Glyphs;
			var keptGlyphs = 0;
			var keptWidth = 0f;

			for (var i = 0; i < span.GlyphsLength; i++)
			{
				var advance = GetGlyphAdvance(glyphs[span.GlyphsStart + i], span.CharacterSpacing);
				if (width + keptWidth + advance > available)
				{
					break;
				}

				keptWidth += advance;
				keptGlyphs++;
			}

			// A cluster is never split.
			while (keptGlyphs > 0 && keptGlyphs < span.GlyphsLength && glyphs[span.GlyphsStart + keptGlyphs].Cluster == glyphs[span.GlyphsStart + keptGlyphs - 1].Cluster)
			{
				keptGlyphs--;
				keptWidth -= GetGlyphAdvance(glyphs[span.GlyphsStart + keptGlyphs], span.CharacterSpacing);
			}

			if (collapsingStyle == TextTrimming.WordEllipsis)
			{
				// Back up to the end of the previous word so the ellipsis does not cut mid-word.
				var wordEnd = keptGlyphs;
				while (wordEnd > 0 && !IsBreakOpportunity(span, wordEnd - 1))
				{
					wordEnd--;
				}

				if (wordEnd > 0)
				{
					keptWidth = 0f;
					for (var i = 0; i < wordEnd; i++)
					{
						keptWidth += GetGlyphAdvance(glyphs[span.GlyphsStart + i], span.CharacterSpacing);
					}

					keptGlyphs = wordEnd;
				}
			}

			if (keptGlyphs == 0 && kept.Count == 0)
			{
				if (span.GlyphsLength == 0)
				{
					kept.Add(span);
					return kept;
				}

				var cluster = glyphs[span.GlyphsStart].Cluster;
				do
				{
					keptWidth += GetGlyphAdvance(glyphs[span.GlyphsStart + keptGlyphs], span.CharacterSpacing);
					keptGlyphs++;
				}
				while (keptGlyphs < span.GlyphsLength && glyphs[span.GlyphsStart + keptGlyphs].Cluster == cluster);
			}

			if (keptGlyphs > 0)
			{
				kept.Add(span with
				{
					GlyphsLength = keptGlyphs,
					CharacterLength = span.GetCharacterOffset(keptGlyphs),
					TrailingSpaces = 0,
					Width = keptWidth,
					WidthWithoutTrailingSpaces = keptWidth,
				});
			}

			// The line did not fit, so it is collapsed even when no glyph of this span survives.
			return kept;
		}

		return null;
	}

	private static float GetGlyphAdvance(GlyphInfo glyph, float characterSpacing)
		=> glyph.AdvanceX > 0 ? glyph.AdvanceX + characterSpacing : glyph.AdvanceX;

	private static bool IsBreakOpportunity(RenderSegmentSpan span, int index)
	{
		var text = span.Segment.Text;
		var offset = span.Segment.GetCharacterOffset(span.GlyphsStart + index);
		return offset >= 0 && offset < text.Length && char.IsWhiteSpace(text[offset]);
	}

	private static CollapsedLineSymbol ShapeSymbol(TextCollapsingCharacters symbol)
	{
		var font = symbol.FontDetails;
		Span<char> text = stackalloc char[1];
		text[0] = symbol.CollapsingChar;

		// GlyphRun advances are already in pixels at the font's size, so no text scale is applied here.
		var run = font.FontHandle.Shape(text, TextDirection.LeftToRight);

		var glyphs = new ushort[run.Count];
		var advances = new float[run.Count];
		var width = 0f;

		for (var i = 0; i < run.Count; i++)
		{
			glyphs[i] = run.Glyphs[i];
			advances[i] = run.Advances[i];
			width += advances[i];
		}

		return new CollapsedLineSymbol(font, glyphs, advances, width);
	}

	private bool _collapsed;

	public override bool HasCollapsed => _collapsed;

	// Conservative: assume clusters may span multiple characters (surrogate pairs,
	// combining marks). Caret navigation re-derives clusters from the segment data.
	public override bool HasMultiCharacterClusters => true;

	// Gets the character hit corresponding to the specified distance from the beginning of the line.
	public override CharacterHit GetCharacterHitFromDistance(double distance)
	{
		var point = new Point(m_start + distance, _lineTop + _renderLine.Height / 2.0);
		var index = _parsedText.GetIndexAt(point, ignoreEndingNewLine: false, extendedSelection: false);

		return new CharacterHit(Math.Clamp(index, _startIndex, LineEndIndex), 0);
	}

	// Gets the distance from the beginning of the line to the specified character hit.
	public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
	{
		// A character hit beyond the last caret stop returns the line width.
		if (characterHit.FirstCharacterIndex >= LineEndIndex)
		{
			return m_widthIncludingTrailingWhitespace;
		}

		var index = Math.Clamp(characterHit.FirstCharacterIndex + characterHit.TrailingLength, _startIndex, LineEndIndex);
		return _parsedText.GetRectForIndex(index).X - m_start;
	}

	// Returns the leading edge of the nearest preceding cluster. If there is no previous character,
	// the return value is exactly equal to characterHit. On input a non-zero trailing length means
	// characterHit references the trailing edge of the indicated cluster.
	public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
	{
		var (stopStart, _) = _parsedText.GetCaretStop(characterHit.FirstCharacterIndex);

		// From a leading edge exactly at a caret stop, move to the leading edge of the preceding cluster.
		if (characterHit.TrailingLength == 0 && stopStart == characterHit.FirstCharacterIndex)
		{
			if (stopStart <= _startIndex)
			{
				return characterHit;
			}

			(stopStart, _) = _parsedText.GetCaretStop(stopStart - 1);
		}

		return new CharacterHit(stopStart, 0);
	}

	// Returns the trailing edge of the nearest following cluster. If there is no next character, the
	// return value is exactly equal to characterHit. On output the trailing length points exactly at
	// the trailing edge of the returned cluster.
	public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
	{
		var (stopStart, stopLength) = _parsedText.GetCaretStop(characterHit.FirstCharacterIndex);

		// From the trailing edge of a caret stop, move to the trailing edge of the following cluster.
		if (characterHit.TrailingLength != 0 && characterHit.FirstCharacterIndex + characterHit.TrailingLength == stopStart + stopLength)
		{
			(stopStart, stopLength) = _parsedText.GetCaretStop(stopStart + stopLength);
		}

		if (stopStart >= LineEndIndex)
		{
			return characterHit;
		}

		return new CharacterHit(stopStart, stopLength);
	}

	public override TextBounds[] GetTextBounds(int firstCharacterIndex, int textLength)
	{
		// Bounds are line-relative: ParagraphNode offsets them by the line rect and replaces Height
		// with the line's vertical advance.
		var start = Math.Clamp(firstCharacterIndex, _startIndex, LineEndIndex);
		var end = Math.Clamp(firstCharacterIndex + textLength, start, LineEndIndex);

		if (end <= start)
		{
			return Array.Empty<TextBounds>();
		}

		var direction = GetParagraphDirection();
		var bounds = new List<TextBounds>();

		double runStart = 0, runEnd = 0;
		var hasRun = false;

		for (var i = start; i < end;)
		{
			var rect = _parsedText.GetRectForIndex(i);
			var (stopStart, stopLength) = _parsedText.GetCaretStop(i);
			i = Math.Max(i + 1, stopStart + stopLength);

			var left = rect.X - m_start;
			var right = left + rect.Width;

			if (!hasRun)
			{
				runStart = left;
				runEnd = right;
				hasRun = true;
			}
			else if (Math.Abs(left - runEnd) < 0.01)
			{
				// Contiguous with the current run.
				runEnd = right;
			}
			else
			{
				bounds.Add(new TextBounds(new Rect(runStart, 0, Math.Max(0, runEnd - runStart), m_height), direction));
				runStart = left;
				runEnd = right;
			}
		}

		bounds.Add(new TextBounds(new Rect(runStart, 0, Math.Max(0, runEnd - runStart), m_height), direction));
		return bounds.ToArray();
	}

	public override FlowDirection GetDetectedParagraphDirection() => DetectedDirection(_parsedText);

	public override FlowDirection GetParagraphDirection() => _paragraphProperties.FlowDirection;
}
