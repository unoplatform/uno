// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextLine.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextLine class contains formatting results for a line of text.
/// </summary>
internal abstract class TextLine
{
	// Arranges the content of the line.
	public abstract void Arrange(Rect bounds);

	// Creates rendering data for the line's contents.
	public abstract void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth);

	// Creates a new TextLine shortened to the specified constraining width.
	public abstract TextLine Collapse(
		double collapsingWidth,
		TextTrimming collapsingStyle,
		TextCollapsingSymbol? collapsingSymbol);

	// Returns true if the visual width of this line will be smaller than its
	// logical width, false otherwise.
	public abstract bool HasCollapsed { get; }

	// Returns false if the caller can rely on all unicode code points in the
	// line mapping to independent glyph runs. A conservative implementation
	// simply returns true.
	public abstract bool HasMultiCharacterClusters { get; }

	// Gets the character hit corresponding to the specified distance from the
	// beginning of the line.
	public abstract CharacterHit GetCharacterHitFromDistance(double distance);

	// Gets the distance from the beginning of the line to the specified
	// character hit.
	public abstract double GetDistanceFromCharacterHit(CharacterHit characterHit);

	// Gets the previous character hit for caret navigation.
	public abstract CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit);

	// Gets the next character hit for caret navigation.
	public abstract CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit);

	// Gets an array of bounding rectangles that represent the range of
	// characters within a text line.
	public abstract TextBounds[] GetTextBounds(int firstCharacterIndex, int textLength);

	public abstract FlowDirection GetDetectedParagraphDirection();
	public abstract FlowDirection GetParagraphDirection();

	// The width of the line, excluding trailing whitespace characters.
	protected double m_width;

	// The width of the line, including trailing whitespace characters.
	protected double m_widthIncludingTrailingWhitespace;

	// The distance from the start of a paragraph to the starting point of a line.
	protected double m_start;

	// The height of the line.
	protected double m_height;

	// The height of the text and any other content in the line.
	protected double m_textHeight;

	// The distance from the top of the line to the baseline.
	protected double m_baseline;

	// The distance from the top of the text and any other content in the line to the baseline.
	protected double m_textBaseline;

	// The distance that black pixels extend prior to the left leading alignment edge of the line.
	protected double m_overhangLeading;

	// The distance that black pixels extend following the right trailing alignment edge of the line.
	protected double m_overhangTrailing;

	// The total number of character positions of the current line.
	protected uint m_length;

	// The number of whitespace code points beyond the last non-blank character in a line.
	protected uint m_trailingWhitespaceLength;

	// The number of newline characters at the end of a line.
	protected uint m_newlineLength;

	// The number of characters following the last character of the line that
	// may trigger reformatting of the current line.
	protected uint m_dependentLength;

	// The state of the line when broken by line breaking process.
	protected TextLineBreak? m_pTextLineBreak;

	// The flag to indicate whether the detected text reading order is different from the set flow direction.
	protected bool m_alignmentFollowsReadingOrder;

	// Gets the width of the line, excluding trailing whitespace characters.
	// Excludes whitespace resulting from CharacterSpacing set on the last
	// character of the line.
	public double Width => m_width;

	// Gets the width of the line, including trailing whitespace characters.
	public double WidthIncludingTrailingWhitespace => m_widthIncludingTrailingWhitespace;

	// Gets the distance from the start of a paragraph to the starting point of a line.
	public double Start => m_start;

	// Gets the height of the line.
	public double Height => m_height;

	// Gets the height of the text and any other content in the line.
	public double TextHeight => m_textHeight;

	// Gets the distance from the top of the line to the baseline.
	public double Baseline => m_baseline;

	// Gets the distance from the top of the text and any other content in the line to the baseline.
	public double TextBaseline => m_textBaseline;

	// Gets the distance that black pixels extend prior to the left leading alignment edge of the line.
	public double OverhangLeading => m_overhangLeading;

	// Gets the distance that black pixels extend following the right trailing alignment edge of the line.
	public double OverhangTrailing => m_overhangTrailing;

	// Gets the total number of character positions of the current line. This includes trailing whitespaces and newline characters.
	public uint Length => m_length;

	// Gets the number of whitespace characters beyond the last non-blank character in a line.
	public uint TrailingWhitespaceLength => m_trailingWhitespaceLength;

	// Gets the number of newline characters at the end of a line.
	public uint NewlineLength => m_newlineLength;

	// Gets the number of characters following the last character of the line that may trigger reformatting of the current line.
	public uint DependentLength => m_dependentLength;

	// Gets the state of the line when broken by line breaking process.
	public TextLineBreak? TextLineBreak => m_pTextLineBreak;

	// Gets the flag that indicates TextReadingOrder is different.
	public bool AlignmentFollowsReadingOrder => m_alignmentFollowsReadingOrder;
}
