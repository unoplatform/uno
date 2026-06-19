// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextLine.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
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
		m_alignmentFollowsReadingOrder = false;
	}

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

	public override void Arrange(Rect bounds)
	{
		// TODO Uno (Stage 6): position embedded inline objects within the arranged
		// line bounds. Glyph layout is fixed once ParsedText has run.
	}

	public override void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth)
		=> throw new NotSupportedException(
			"TODO Uno (Stage 6): per-line drawing. Paragraph rendering currently goes through ParsedText.Draw.");

	public override TextLine Collapse(double collapsingWidth, TextTrimming collapsingStyle, TextCollapsingSymbol? collapsingSymbol)
		=> throw new NotSupportedException(
			"TODO Uno (Stage 3/R2): line collapsing (text trimming ellipsis) is not yet ported.");

	public override bool HasCollapsed => false;

	// Conservative: assume clusters may span multiple characters (surrogate pairs,
	// combining marks). Caret navigation re-derives clusters from the segment data.
	public override bool HasMultiCharacterClusters => true;

	public override CharacterHit GetCharacterHitFromDistance(double distance)
		=> throw new NotSupportedException("TODO Uno (Stage 6): caret hit-testing from distance.");

	public override double GetDistanceFromCharacterHit(CharacterHit characterHit)
		=> throw new NotSupportedException("TODO Uno (Stage 6): distance from caret hit.");

	public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
		=> throw new NotSupportedException("TODO Uno (Stage 6): caret navigation.");

	public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
		=> throw new NotSupportedException("TODO Uno (Stage 6): caret navigation.");

	public override TextBounds[] GetTextBounds(int firstCharacterIndex, int textLength)
		=> throw new NotSupportedException("TODO Uno (Stage 6): per-range text bounds.");

	public override FlowDirection GetDetectedParagraphDirection() => _parsedText.FlowDirection;

	public override FlowDirection GetParagraphDirection() => _paragraphProperties.FlowDirection;
}
