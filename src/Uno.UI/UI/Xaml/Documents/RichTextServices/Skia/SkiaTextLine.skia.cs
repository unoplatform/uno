// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextLine.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;

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

	// Paragraph-relative (glyph-space) index of this line's first character, and the top of the line
	// within the paragraph. The glyph space matches TextLine.Length, which ParagraphNode accumulates
	// into LineMetrics.FirstCharIndex.
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

		var (lineOffset, _) = renderLine.GetOffsets((float)parsedText.AvailableSize.Width, alignment);
		m_start = lineOffset;

		ComputeCharacterCounts(renderLine, out m_length, out m_trailingWhitespaceLength, out m_newlineLength);
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

	// One past this line's last character, in the same glyph space as _startIndex.
	private int LineEndIndex => _startIndex + (int)m_length;

	// The render line this text line wraps.
	internal RenderLine RenderLine => _renderLine;

	// The whole-paragraph ParsedText this line was vended from. All SkiaTextLines of a
	// paragraph share one ParsedText (the SkiaTextFormatter cache); the render path
	// (RichTextVisual.Paint) draws the paragraph once via ParsedText.Draw.
	internal ParsedText ParsedText => _parsedText;

	// The zero-based index of this line within its paragraph.
	internal int LineIndex => _lineIndex;

	// Replicates ParsedText's per-line character counting (see GlyphsLengthWithCR /
	// GetLineIntervals there) so that character offsets reported by the layout tree
	// line up with ParsedText hit-testing.
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
			total += (uint)span.FullGlyphsLength;

			if (SpanEndsInNewLine(span))
			{
				total += (uint)span.Segment.LineBreakLength;

				if (i == spans.Count - 1)
				{
					newline = (uint)span.Segment.LineBreakLength;
				}
			}

			if (i == spans.Count - 1)
			{
				trailing = (uint)span.TrailingSpaces;
			}
		}

		length = total;
		trailingWhitespaceLength = trailing;
		newlineLength = newline;
	}

	private static bool SpanEndsInNewLine(RenderSegmentSpan segmentSpan)
	{
		var segment = segmentSpan.Segment;

		return segment is { Inline: Run, LineBreakAfter: true } &&
			segment.Text.TrimEnd().Length <= segmentSpan.GlyphsStart + segmentSpan.GlyphsLength;
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

	public override TextLine Collapse(double collapsingWidth, TextTrimming collapsingStyle, TextCollapsingSymbol? collapsingSymbol)
		// TODO Uno (R2): real ellipsis truncation is net-new on the Skia engine. For now return the
		// uncollapsed line (HasCollapsed stays false); the paragraph still reports trimming via the
		// control's MaxLines/width detection and ParsedText renders the text clipped to bounds.
		=> this;

	public override bool HasCollapsed => false;

	// Conservative: assume clusters may span multiple characters (surrogate pairs,
	// combining marks). Caret navigation re-derives clusters from the segment data.
	public override bool HasMultiCharacterClusters => true;

	// Gets the character hit corresponding to the specified distance from the beginning of the line.
	public override CharacterHit GetCharacterHitFromDistance(double distance)
	{
		var point = new Point(m_start + distance, _lineTop + _renderLine.Height / 2.0);
		var index = _parsedText.GetIndexAtUnadjusted(point, ignoreEndingSpace: false, extendedSelection: false);

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
		return _parsedText.GetRectForUnadjustedIndex(index).X - m_start;
	}

	// Returns the leading edge of the nearest preceding cluster. If there is no previous character,
	// the return value is exactly equal to characterHit. On input a non-zero trailing length means
	// characterHit references the trailing edge of the indicated cluster.
	public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
	{
		if (characterHit.TrailingLength != 0)
		{
			return new CharacterHit(characterHit.FirstCharacterIndex, 0);
		}

		if (characterHit.FirstCharacterIndex <= _startIndex)
		{
			return characterHit;
		}

		return new CharacterHit(characterHit.FirstCharacterIndex - 1, 0);
	}

	// Returns the trailing edge of the nearest following cluster. If there is no next character, the
	// return value is exactly equal to characterHit. On output the trailing length points exactly at
	// the trailing edge of the returned cluster.
	public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
	{
		var start = characterHit.TrailingLength == 0
			? characterHit.FirstCharacterIndex
			: characterHit.FirstCharacterIndex + characterHit.TrailingLength;

		if (start >= LineEndIndex)
		{
			return characterHit;
		}

		return new CharacterHit(start, 1);
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

		for (var i = start; i < end; i++)
		{
			var rect = _parsedText.GetRectForUnadjustedIndex(i);
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
