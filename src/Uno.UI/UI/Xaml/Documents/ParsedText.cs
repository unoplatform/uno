#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI;
using WinUIColor = Windows.UI.Color;

// Aliased rather than imported: the RichTextServices namespace also declares a TextFormatting type,
// which would collide with the TextFormatting namespace imported above.
using ObjectRun = Microsoft.UI.Xaml.Documents.RichTextServices.ObjectRun;
using ObjectRunMetrics = Microsoft.UI.Xaml.Documents.RichTextServices.ObjectRunMetrics;

namespace Microsoft.UI.Xaml.Documents;

internal readonly struct ParsedText : IParsedText
{
	private static readonly SKPaint _spareDrawPaint = new();
	private static readonly SKPaint _spareBackplatePaint = new() { IsAntialias = true };
	private static readonly SKPaint _spareSelectionPaint = new() { IsAntialias = true };
	// This is safe as a static field.
	// 1) It's only accessed from UI thread.
	// 2) Once we call SKTextBlobBuilder.Build(), the instance is reset to its initial state.
	// See https://api.skia.org/classSkTextBlobBuilder.html#abf5e20208fd5656981191a3778ee5fef:
	// > Resets SkTextBlobBuilder to its initial empty state, allowing it to be reused to build a new set of runs.
	// The reset to the initial state happens here:
	// https://github.com/google/skia/blob/d29cc3fe182f6e8a8539004a6a4ee8251677a6fd/src/core/SkTextBlob.cpp#L652-L656
	private static readonly SKTextBlobBuilder _textBlobBuilder = new();

	public static readonly object InitialSelection = new SelectionDetails(0, 0, 0, 0);
	public static readonly ParsedText Empty = new([], [], Size.Empty, TextAlignment.Left, TextWrapping.NoWrap, 20, FlowDirection.LeftToRight);

	private readonly List<RenderLine> _renderLines;
	private readonly Size _availableSize;
	private readonly TextAlignment _textAlignment;
	private readonly TextWrapping _textWrapping;
	private readonly string _text;
	private readonly float _defaultLineHeight; // used when the text is empty
	private readonly FlowDirection _flowDirection;
	private readonly Inline[] _inlines;

	private ParsedText(Inline[] inlines, List<RenderLine> renderLines, Size availableSize, TextAlignment textTextAlignment, TextWrapping textWrapping, float defaultLineHeight, FlowDirection flowDirection)
	{
		_inlines = inlines;
		_renderLines = renderLines;
		_availableSize = availableSize;
		_textAlignment = textTextAlignment;
		_textWrapping = textWrapping;
		_defaultLineHeight = defaultLineHeight;
		_flowDirection = flowDirection;
		_text = string.Concat(inlines.Select(InlineExtensions.GetText));
	}

	/// <summary>
	/// Measures a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
	/// </summary>
	internal static ParsedText ParseText(
		Size availableSize,
		Inline[] inlines, // traversed pre-orderly
		float defaultLineHeight,
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		TextLineBounds textLineBounds,
		TextAlignment textAlignment,
		TextWrapping textWrapping,
		FlowDirection flowDirection,
		out Size desiredSize,
		Func<InlineUIContainer, (ObjectRun Run, ObjectRunMetrics Metrics)?>? formatInlineObject = null)
		=> ParseText(availableSize, inlines, defaultLineHeight, maxLines, lineHeight, lineStackingStrategy, textLineBounds, textAlignment, textWrapping, flowDirection, out desiredSize, resumeCharIndex: 0, out _, formatInlineObject);

	/// <summary>
	/// Measures a block-level inline collection whose layout resumes at <paramref name="resumeCharIndex"/>, where a
	/// previous link of a linked chain broke at its own width.
	/// </summary>
	/// <param name="resumeLineIndex">Index of the first line laid out from <paramref name="resumeCharIndex"/>.</param>
	internal static ParsedText ParseText(
		Size availableSize,
		Inline[] inlines, // traversed pre-orderly
		float defaultLineHeight,
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		TextLineBounds textLineBounds,
		TextAlignment textAlignment,
		TextWrapping textWrapping,
		FlowDirection flowDirection,
		out Size desiredSize,
		int resumeCharIndex,
		out int resumeLineIndex,
		Func<InlineUIContainer, (ObjectRun Run, ObjectRunMetrics Metrics)?>? formatInlineObject = null)
	{
		lineStackingStrategy = lineHeight == 0 ? LineStackingStrategy.MaxHeight : lineStackingStrategy;

		var renderLines = new List<RenderLine>();
		List<RenderSegmentSpan> lineSegmentSpans = new();
		bool previousLineWrapped = false;

		float wrappingWidth = textWrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
		float widestLineWidth = 0, widestLineHeight = 0;

		// Like Line Services resuming at cpFirst, the text before resumeCharIndex belongs to a previous link. It is kept
		// as unwrapped, never-painted lines so line character intervals stay paragraph-relative.
		bool inPrefix = resumeCharIndex > 0;
		float availableWidth = inPrefix ? float.PositiveInfinity : wrappingWidth;
		int charIndex = 0;
		int resumeLine = 0;

		float x = 0;
		float height = 0;

		foreach (var inline in inlines)
		{
			EndPrefixIfReached();

			if (inline is LineBreak lineBreak)
			{
				// A <LineBreak/> is one flat character (CLineBreak::GetRun yields a single \x2028), matching the
				// "\n" InlineExtensions.GetText already put in _text. It renders no glyph, so CharacterLength stays 0.
				Segment breakSegment = new(lineBreak, lineBreakLength: 1);
				RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0, 0, 0, 0, 0, 0);
				lineSegmentSpans.Add(breakSegmentSpan);

				if (inPrefix)
				{
					charIndex += breakSegmentSpan.CharacterLengthWithNewLine;
				}

				MoveToNextLine(currentLineWrapped: false);
			}
			else if (inline is InlineUIContainer container)
			{
				if (inPrefix)
				{
					// An object takes no character position; one before the break was laid out by the previous link.
					continue;
				}

				// Only containers the caller formats occupy space. Formatting outside a block-layout
				// host (so with no embedded element host to measure against) leaves them zero-sized.
				if (formatInlineObject?.Invoke(container) is not { } inlineObject)
				{
					continue;
				}

				if (maxLines > 0 && renderLines.Count == maxLines)
				{
					goto MaxLinesHit;
				}

				var objectWidth = inlineObject.Metrics.Width;

				if (x > 0 && objectWidth > availableWidth - x)
				{
					// The object doesn't fit in what's left of the line, and there is content before it,
					// so wrap it whole onto the next line. An object never breaks internally.
					MoveToNextLine(currentLineWrapped: true);
				}

				Segment objectSegment = new(container, flowDirection, inlineObject.Run, inlineObject.Metrics);
				RenderSegmentSpan objectSegmentSpan = new(objectSegment, 0, 0, 0, 0, 0, objectWidth, objectWidth, 0, 0);
				lineSegmentSpans.Add(objectSegmentSpan);
				x += objectWidth;
			}
			else if (inline is Run run)
			{
				float characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

				foreach (var segment in run.Segments)
				{
					// TODO: After bidi is implemented, consider that adjacent segments may not have a word break or new line between them but may just
					// switch direction and thus must appear together without wrapping. We don't need to worry about this for now since every segment
					// ends in either a word break or line break

					// Exclude leading spaces at the start of the line only if the previous line ended because it was wrapped and not because of a line break

					EndPrefixIfReached();

					int start = x == 0 && previousLineWrapped ? segment.LeadingSpaces : 0;
					int skippedLeadingSpaces = start;

					if (inPrefix)
					{
						var glyphCount = segment.LineBreakAfter ? segment.Glyphs.Count - 1 : segment.Glyphs.Count;
						RenderSegmentSpan wholeSpan = new(segment, 0, glyphCount, 0, 0, characterSpacing, 0, 0, 0, segment.ContentLength);
						var wholeLength = wholeSpan.CharacterLengthWithNewLine;

						if (charIndex + wholeLength <= resumeCharIndex)
						{
							lineSegmentSpans.Add(wholeSpan);
							charIndex += wholeLength;

							if (segment.LineBreakAfter)
							{
								MoveToNextLine(currentLineWrapped: false);
							}

							EndPrefixIfReached();
							continue;
						}

						// The previous link wrapped inside this segment, so the continuation starts at that character's glyph.
						var resumeGlyph = segment.GetGlyphIndex(resumeCharIndex - charIndex);
						var prefixSpan = RenderSegmentSpan.Create(segment, 0, resumeGlyph, 0, 0, characterSpacing, 0, 0, 0, resumeGlyph);
						lineSegmentSpans.Add(prefixSpan);
						charIndex += prefixSpan.CharacterLength;
						EndPrefixIfReached();

						start = resumeGlyph;
						skippedLeadingSpaces = 0;
					}

				BeginSegmentFitting:

					if (maxLines > 0 && renderLines.Count == maxLines)
					{
						goto MaxLinesHit;
					}

					if (segment.IsTab)
					{
						segment.AdjustTabWidth(x);
					}

					float remainingWidth = availableWidth - x;
					(int segmentLengthWithoutTrailingSpaces, float widthWithoutTrailingSpaces) = GetSegmentRenderInfo(segment, start, characterSpacing);

					// Check if whole segment fits

					if (widthWithoutTrailingSpaces <= remainingWidth)
					{
						// Add in as many trailing spaces as possible

						int length = segmentLengthWithoutTrailingSpaces;
						float width = widthWithoutTrailingSpaces;
						int end = segment.LineBreakAfter ? segment.Glyphs.Count - 1 : segment.Glyphs.Count;
						int trailingSpaces = 0;

						while (start + length < end &&
							(width + GetGlyphWidthWithSpacing(segment.Glyphs[length], characterSpacing)) is var newWidth &&
							newWidth <= remainingWidth)
						{
							width = newWidth;
							length++;
							trailingSpaces++;
						}

						var fullGlyphsStart = start == skippedLeadingSpaces ? 0 : start;
						var segmentSpan = RenderSegmentSpan.Create(segment, start, length, Math.Max(0, segment.LeadingSpaces - start), trailingSpaces, characterSpacing, width, widthWithoutTrailingSpaces, fullGlyphsStart, end - fullGlyphsStart);
						lineSegmentSpans.Add(segmentSpan);
						x += width;

						if (segment.LineBreakAfter)
						{
							MoveToNextLine(currentLineWrapped: false);
						}
						else if (start + length < end)
						{
							// equivalently condition would be `trailingSpaces < segment.TrailingSpaces`
							global::System.Diagnostics.CI.Assert(end - (start + length) == segment.TrailingSpaces - trailingSpaces);

							// We could fit the segment, but not all of the trailing spaces
							// These remaining trailing spaces will never be rendered, that's
							// how WinUI does it.
							MoveToNextLine(currentLineWrapped: true);
						}

						continue;
					}

					// Whole segment does not fit so tack on as many leading spaces as possible

					if (start == 0 && segment.LeadingSpaces > 0)
					{
						int spaces = 0;
						float width = 0;

						if (x == 0)
						{
							// minimum 1 space if this is the start of the line
							// if we didn't add even a single space, then we're not making any progress

							spaces = 1;
							width = GetGlyphWidthWithSpacing(segment.Glyphs[0], characterSpacing);
						}

						while (spaces < segment.LeadingSpaces &&
							(width + GetGlyphWidthWithSpacing(segment.Glyphs[spaces], characterSpacing)) is var newWidth &&
							newWidth < remainingWidth)
						{
							width = newWidth;
							spaces++;
						}

						// The remaining leading spaces that didn't fit won't be rendered at all, and will not continue
						// on the next line like one would intuitively assume. This matches WinUI. This is similar
						// to the case of trailing spaces that don't fit.

						if (width > 0)
						{
							var segmentSpan = RenderSegmentSpan.Create(segment, 0, spaces, spaces, 0, characterSpacing, widthWithoutTrailingSpaces, 0, 0, segment.LeadingSpaces);
							lineSegmentSpans.Add(segmentSpan);
							x += width;

							start = segment.LeadingSpaces;
						}
					}

					// By this point, we must have at least dealt with all the leading spaces. We either drew
					// all the leading spaces or we drew as many as we could and we're discarding the rest.

					if (x > 0)
					{
						// There is content on this line and the segment did not fit so wrap to the next line and retry adding the segment

						// But only if there's actually content ahead. This is explicitly to handle a content
						// of just too many spaces. We don't want to add a new line even if only some of
						// the spaces fit as the remainder of the spaces will just not render.
						// This is most definitely not perfect, as it won't catch cases of having more empty inlines
						// after, but it should be good enough for the majority of cases.
						if (inlines[^1] == run && run.Segments[^1] == segment && start == segment.LeadingSpaces && segment.LeadingSpaces == segment.Glyphs.Count)
						{
							continue;
						}

						MoveToNextLine(currentLineWrapped: true);
						goto BeginSegmentFitting;
					}

					// There is no content on the line so wrap the segment according to the wrapping mode.

					if (textWrapping == TextWrapping.WrapWholeWords)
					{
						// Put the whole segment on the line and move to the next line.

						var fullGlyphsStart = start == skippedLeadingSpaces ? 0 : start;
						var fullGlyphsEnd = segment.LineBreakAfter ? segment.Glyphs.Count - 1 : segment.Glyphs.Count;
						var segmentSpan = RenderSegmentSpan.Create(segment, start, segmentLengthWithoutTrailingSpaces - segment.LeadingSpaces, 0, segment.TrailingSpaces, characterSpacing, widthWithoutTrailingSpaces, widthWithoutTrailingSpaces, fullGlyphsStart, fullGlyphsEnd - fullGlyphsStart);
						lineSegmentSpans.Add(segmentSpan);
						x += widthWithoutTrailingSpaces;

						MoveToNextLine(currentLineWrapped: !segment.LineBreakAfter);
					}
					else // wrapping == TextWrapping.Wrap
					{
						// Put as much of the segment on this line as possible then continue fitting the rest of the segment on the next line

						var length = 1;
						var width = GetGlyphWidthWithSpacing(segment.Glyphs[start], characterSpacing);

						while (start + length < segment.Glyphs.Count
							&& (width + GetGlyphWidthWithSpacing(segment.Glyphs[start + length], characterSpacing)) is var newWidth
							&& newWidth < remainingWidth)
						{
							width = newWidth;
							length++;
						}

						// Line Services never breaks inside a cluster: back off to the cluster's start, or take the whole
						// cluster when it is all that is on the line.
						while (length > 1 && IsInsideCluster(segment, start + length))
						{
							length--;
							width -= GetGlyphWidthWithSpacing(segment.Glyphs[start + length], characterSpacing);
						}

						while (IsInsideCluster(segment, start + length))
						{
							width += GetGlyphWidthWithSpacing(segment.Glyphs[start + length], characterSpacing);
							length++;
						}

						// We've already dealt with leading spaces.
						// We can't fit the segment content (excluding trailing spaces),
						// so this span definitely doesn't include any trailing spaces either.

						var fullGlyphsStart = start == skippedLeadingSpaces ? 0 : start;
						var segmentSpan = RenderSegmentSpan.Create(segment, start, length, 0, 0, characterSpacing, widthWithoutTrailingSpaces, widthWithoutTrailingSpaces, fullGlyphsStart, start + length - fullGlyphsStart);
						lineSegmentSpans.Add(segmentSpan);
						x += width;
						start += length;

						MoveToNextLine(currentLineWrapped: true);
						goto BeginSegmentFitting;
					}
				}
			}
		}

		if (lineSegmentSpans.Count == 0 && !previousLineWrapped)
		{
			// We ended on a <LineBreak /> or a Run ending in \r or \n. We usually just wait for the following content to
			// fill the line and then create a RenderLine. In this case, there is no following content, so we must
			// create the RenderLine now.

			lineHeight = defaultLineHeight;
			lineStackingStrategy = LineStackingStrategy.BlockLineHeight;

			// this bit isn't strictly necessary but it maintains the invariant that RenderLines always have a span.
			// lineBreakLength stays 0: the newline it stands for was already counted by the preceding Run's
			// LineBreakLength (or there is no newline at all), so it must not be counted twice.
			Segment breakSegment = new(new LineBreak());
			RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			lineSegmentSpans.Add(breakSegmentSpan);

			MoveToNextLine(false);
		}

		if (lineSegmentSpans.Count != 0)
		{
			// every line gets finalized in MoveToNextLine, so it must be called for the last line too.
			MoveToNextLine(false);
		}

	MaxLinesHit:

		if (inPrefix || resumeLine >= renderLines.Count)
		{
			// A break at or past the content's end: there is nothing left to continue with but the last line.
			resumeLine = Math.Max(0, renderLines.Count - 1);
		}

		resumeLineIndex = resumeLine;
		desiredSize = renderLines.Count == 0 ? new Size(0, defaultLineHeight) : new Size(widestLineWidth, height);
		return new(inlines, renderLines, availableSize, textAlignment, textWrapping, defaultLineHeight, flowDirection);

		// Local functions

		// Gets rendering info for a segment, excluding any trailing spaces.

		static (int Length, float Width) GetSegmentRenderInfo(Segment segment, int startGlyph, float characterSpacing)
		{
			var glyphs = segment.Glyphs;
			int end = segment.LineBreakAfter ? glyphs.Count - 1 : glyphs.Count;
			end -= segment.TrailingSpaces;

			float width = 0;

			for (int i = startGlyph; i < end; i++)
			{
				width += GetGlyphWidthWithSpacing(glyphs[i], characterSpacing);
			}

			return (end - startGlyph, width);
		}

		// True when the glyph at glyphIndex continues the cluster of the glyph before it.
		static bool IsInsideCluster(Segment segment, int glyphIndex)
			=> glyphIndex > 0 && glyphIndex < segment.Glyphs.Count && segment.Glyphs[glyphIndex].Cluster == segment.Glyphs[glyphIndex - 1].Cluster;

		void MoveToNextLine(bool currentLineWrapped)
		{
			var renderLine = new RenderLine(lineSegmentSpans, lineStackingStrategy, lineHeight, renderLines.Count == 0, currentLineWrapped, textLineBounds);
			renderLines.Add(renderLine);
			lineSegmentSpans.Clear();

			if (x > widestLineWidth)
			{
				widestLineWidth = x;
				widestLineHeight = lineHeight;
			}

			x = 0;
			height += renderLine.Height;
			previousLineWrapped = currentLineWrapped;
		}

		void EndPrefixIfReached()
		{
			if (!inPrefix || charIndex < resumeCharIndex)
			{
				return;
			}

			if (lineSegmentSpans.Count > 0)
			{
				// The previous link's line wrapped here; after a hard break the line has already moved on.
				MoveToNextLine(currentLineWrapped: true);
			}

			inPrefix = false;
			availableWidth = wrappingWidth;
			resumeLine = renderLines.Count;
		}
	}

	#region IParsedText

	// compositionRange is accepted to satisfy the IParsedText contract, but
	// composition underline rendering is implemented by UnicodeText (the actual
	// renderer used by TextBlock/TextBox). ParsedText is only the initial empty
	// fallback and never has an active composition to render.
	/// <param name="firstLine">Index of the first line to paint. Lines before it still advance the
	/// character index so highlight and selection ranges stay in the paragraph's own space.</param>
	/// <param name="lineCount">Number of lines to paint, for a paragraph split across a page break.</param>
	public void Draw(UIElement owner, in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret,
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange,
		int firstLine = 0,
		int lineCount = int.MaxValue)
	{
		var useHighContrastAdjustment = owner.UseHighContrastAdjustment();
		var effectiveOpacity = useHighContrastAdjustment && session.Opacity > 0
			? 1f
			: session.Opacity;
		var (highContrastForeground, highContrastBackground, highContrastSelectionForeground, highContrastSelectionBackground) =
			useHighContrastAdjustment
				? owner.GetHighContrastTextColors()
				: default;

		// Resolved once for the whole draw: the merged regions and their line spans depend only on the
		// highlighters, not on the segment being painted.
		var highlightRegions = new List<(HighlightRegion Region, SelectionDetails Details)>();
		foreach (var region in MergeHighlighters(highlighters))
		{
			// HighlightRegion is inclusive, CalculateSelection takes an exclusive end.
			highlightRegions.Add((region, CalculateSelection(region.StartIndex, region.EndIndex + 1)));
		}

		if (_renderLines.Count == 0)
		{
			// empty, so caret is at the beginning
			if (caret is not null)
			{
				var caretRect = new SKRect(0, 0, caret.Value.thickness, _defaultLineHeight);
				caret.Value.brush.Paint(session.Canvas, effectiveOpacity, caretRect);
			}

			return;
		}

		var canvas = session.Canvas;
		var alignment = ResolvedTextAlignment;
		if (_flowDirection == FlowDirection.RightToLeft)
		{
			alignment = alignment switch
			{
				TextAlignment.Left => TextAlignment.Right,
				TextAlignment.Right => TextAlignment.Left,
				_ => alignment,
			};
		}

		var characterCountSoFar = 0;

		float y = 0;

		var lastLine = lineCount == int.MaxValue ? int.MaxValue : firstLine + lineCount;

		for (var lineIndex = 0; lineIndex < _renderLines.Count && lineIndex < lastLine; lineIndex++)
		{
			var line = _renderLines[lineIndex];
			// TODO: (Performance) Stop rendering when the lines exceed the available height

			if (lineIndex < firstLine)
			{
				// Not on this page, but its characters still count towards the index space.
				foreach (var skipped in line.SegmentSpans)
				{
					characterCountSoFar += skipped.CharacterLengthWithNewLine;
				}

				continue;
			}

			(float x, float justifySpaceOffset) = line.GetOffsets((float)_availableSize.Width, alignment);
			var lineStartX = x;
			if (line is { CollapsingSymbol: { } leadingSymbol, CollapsingSymbolLeads: true })
			{
				x += leadingSymbol.Width;
			}

			y += line.Height;
			float baselineOffsetY = line.BaselineOffsetY;

			for (int s = 0; s < line.RenderOrderedSegmentSpans.Count; s++)
			{
				var xBeforeGlyphOffsets = x;
				var segmentSpan = line.RenderOrderedSegmentSpans[s];

				var segment = segmentSpan.Segment;

				if (segment.IsInlineObject)
				{
					// Inline objects do not render content directly. They are rendering through UIElement tree render walk.
					x += segmentSpan.Width;
					continue;
				}

				var inline = segment.Inline;
				var fontInfo = segment.FallbackFont ?? inline.FontInfo;

				var paint = _spareDrawPaint;

				paint.Reset();

				paint.IsStroke = false;
				paint.IsAntialias = true;

				if (useHighContrastAdjustment)
				{
					paint.Color = ToSkColor(highContrastForeground, effectiveOpacity);
				}
				else if (inline.Foreground is SolidColorBrush scb)
				{
					var scbColor = scb.Color;
					paint.Color = new SKColor(
						red: scbColor.R,
						green: scbColor.G,
						blue: scbColor.B,
						alpha: (byte)(scbColor.A * scb.Opacity * effectiveOpacity));
				}
				else if (inline.Foreground is GradientBrush gb)
				{
					var gbColor = gb.FallbackColorWithOpacity;
					paint.Color = new SKColor(
						red: gbColor.R,
						green: gbColor.G,
						blue: gbColor.B,
						alpha: (byte)(gbColor.A * effectiveOpacity));
				}
				else if (inline.Foreground is XamlCompositionBrushBase xcbb)
				{
					var gbColor = xcbb.FallbackColorWithOpacity;
					paint.Color = new SKColor(
						red: gbColor.R,
						green: gbColor.G,
						blue: gbColor.B,
						alpha: (byte)(gbColor.A * effectiveOpacity));
				}

				// TODO: Consider using a stackalloc for small values of GlyphsLength.
				// Note that using a stackalloc will require refactoring this code to a separate method to avoid having a stackalloc in a loop.
				var glyphs = ArrayPool<ushort>.Shared.Rent(segmentSpan.GlyphsLength);
				var positions = ArrayPool<SKPoint>.Shared.Rent(segmentSpan.GlyphsLength);

				// The pool can rent us arrays of size larger than the requested size.
				// So, we pass these spans around to make sure nothing tries to read beyond the correct size.
				var glyphsSpan = glyphs.AsSpan().Slice(0, segmentSpan.GlyphsLength);
				var positionsSpan = positions.AsSpan().Slice(0, segmentSpan.GlyphsLength);

				if (segment.Direction == FlowDirection.LeftToRight)
				{
					for (int i = 0; i < segmentSpan.GlyphsLength; i++)
					{
						var glyphInfo = segment.Glyphs[segmentSpan.GlyphsStart + i];

						if (glyphInfo.AdvanceX > 0)
						{
							x += segmentSpan.CharacterSpacing;
						}

						glyphsSpan[i] = glyphInfo.GlyphId;
						positionsSpan[i] = new SKPoint(x + glyphInfo.OffsetX - segmentSpan.CharacterSpacing, glyphInfo.OffsetY);
						x += glyphInfo.AdvanceX;
					}
				}
				else // FlowDirection.RightToLeft
				{
					// Enumerate clusters in reverse order to draw left-to-right

					for (int i = segmentSpan.GlyphsLength - 1; i >= 0; i--)
					{
						int cluster = segment.Glyphs[segmentSpan.GlyphsStart + i].Cluster;
						int clusterGlyphCount = 1;

						while (i > 0 && segment.Glyphs[segmentSpan.GlyphsStart + i - 1].Cluster == cluster)
						{
							i--;
							clusterGlyphCount++;
						}

						for (int j = i; j < i + clusterGlyphCount; j++)
						{
							var glyphInfo = segment.Glyphs[segmentSpan.GlyphsStart + j];

							if (glyphInfo.AdvanceX > 0)
							{
								x += segmentSpan.CharacterSpacing;
							}

							glyphsSpan[j] = glyphInfo.GlyphId;
							positionsSpan[j] = new SKPoint(x + glyphInfo.OffsetX, glyphInfo.OffsetY);
							x += glyphInfo.AdvanceX;
						}
					}
				}

				// Skia doesn't have the concept of a Z-axis here. Drawings are drawn on top of one another,
				// so we need to draw from the bottom layer to the top layer (from inside the screen to outside)
				// 1. Selection never covers anything, so it goes first
				// 2. Text and text decorations don't generally overlap, so they're interchangeable.
				// 3. The caret goes on top so that it's always fully showing without anything covering it.
				// Note that carets and text decorations never occur at the same time for now (TextBox has a caret but no
				// decorations, TextBlock doesn't have a caret), but a RichTextBox can have both, so that should be kept in mind

				if (useHighContrastAdjustment)
				{
					var backplateWidth = s == line.RenderOrderedSegmentSpans.Count - 1
						? segmentSpan.WidthWithoutTrailingSpaces
						: segmentSpan.Width;
					DrawBackplate(canvas, xBeforeGlyphOffsets, y, line.Height, backplateWidth, highContrastBackground, effectiveOpacity);
				}

				if (highlightRegions.Count > 0)
				{
					// Backgrounds first: merged regions never overlap, so they compose without repainting
					// one another, and the text is then drawn once across the whole segment.
					foreach (var (region, details) in highlightRegions)
					{
						HandleSelection(
							details,
							lineIndex,
							characterCountSoFar,
							positionsSpan,
							x,
							justifySpaceOffset,
							segmentSpan,
							segment,
							fontInfo,
							y,
							line,
							canvas,
							(region.BackgroundBrush ?? DefaultHighlighterBackground).GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()),
							effectiveOpacity,
							useHighContrastAdjustment ? ToSkColor(highContrastSelectionBackground, effectiveOpacity) : null);
					}

					RenderHighlightedText(
						highlightRegions,
						lineIndex,
						characterCountSoFar,
						segmentSpan,
						fontInfo,
						positionsSpan,
						glyphsSpan,
						canvas,
						y + baselineOffsetY,
						paint,
						useHighContrastAdjustment ? ToSkColor(highContrastSelectionForeground, effectiveOpacity) : null);
				}
				else
				{
					RenderText(null, lineIndex, characterCountSoFar, segmentSpan, fontInfo, positionsSpan, glyphsSpan, canvas, y + baselineOffsetY, paint);
				}

				// START decorations
				var decorations = inline.TextDecorations;
				const TextDecorations allDecorations = TextDecorations.Underline | TextDecorations.Strikethrough;

				if ((decorations & allDecorations) != 0)
				{
					var metrics = fontInfo.SKFontMetrics;
					float width = s == line.RenderOrderedSegmentSpans.Count - 1 ? segmentSpan.WidthWithoutTrailingSpaces : segmentSpan.Width;

					// We don't need to see where selection starts and ends in this case, as the decoration color doesn't
					// get affected by the selection.

					if ((decorations & TextDecorations.Underline) != 0)
					{
						// TODO: what should default thickness/position be if metrics does not contain it?
						float yPos = y + baselineOffsetY + (metrics.UnderlinePosition ?? 0);
						DrawDecoration(canvas, xBeforeGlyphOffsets, yPos, width, metrics.UnderlineThickness ?? 1, paint);
					}

					if ((decorations & TextDecorations.Strikethrough) != 0)
					{
						// TODO: what should default thickness/position be if metrics does not contain it?
						float yPos = y + baselineOffsetY + (metrics.StrikeoutPosition ?? fontInfo.SKFontSize / -2);
						DrawDecoration(canvas, xBeforeGlyphOffsets, yPos, width, metrics.StrikeoutThickness ?? 1, paint);
					}
				}
				// END decorations

				if (caret is not null)
				{
					HandleCaret(caret.Value.index, caret.Value.thickness, canvas, caret.Value.brush, effectiveOpacity, characterCountSoFar, segmentSpan, positionsSpan, x, justifySpaceOffset, y, line);
				}

				x += justifySpaceOffset * segmentSpan.TrailingSpaces;
				characterCountSoFar += segmentSpan.CharacterLengthWithNewLine;

				ArrayPool<SKPoint>.Shared.Return(positions);
				ArrayPool<ushort>.Shared.Return(glyphs);
			}

			// A collapsed line paints its ellipsis at its logical end, still last so it takes the kept glyphs' paint.
			if (line.CollapsingSymbol is { } collapsingSymbol)
			{
				DrawCollapsingSymbol(collapsingSymbol, canvas, line.CollapsingSymbolLeads ? lineStartX : x, y + baselineOffsetY);
			}
		}

		static void DrawDecoration(SKCanvas canvas, float x, float y, float width, float thickness, SKPaint paint)
		{
			paint.StrokeWidth = thickness;
			paint.IsStroke = true;
			canvas.DrawLine(x, y, x + width, y, paint);
			paint.IsStroke = false;
		}

		static void DrawBackplate(
			SKCanvas canvas,
			float x,
			float y,
			float lineHeight,
			float width,
			WinUIColor background,
			float opacity)
		{
			if (width <= 0)
			{
				return;
			}

			_spareBackplatePaint.Color = ToSkColor(background, opacity);
			canvas.DrawRect(
				new SKRect(
					MathF.Round(x, MidpointRounding.AwayFromZero),
					y - lineHeight,
					MathF.Round(x + width, MidpointRounding.AwayFromZero),
					y),
				_spareBackplatePaint);
		}
	}

	/// <remarks>
	/// Takes a UTF-16 index into the paragraph text, the same space as the RichTextServices TextLine character
	/// indices. An index inside a cluster gets the whole cluster's box.
	/// </remarks>
	public Rect GetRectForIndex(int index)
	{
		var characterCount = 0;
		float y = 0, x = 0;

		foreach (var line in _renderLines)
		{
			(x, var justifySpaceOffset) = line.GetOffsets((float)_availableSize.Width, ResolvedTextAlignment);

			var spans = line.RenderOrderedSegmentSpans;
			foreach (var span in spans)
			{
				var spanLength = span.CharacterLengthWithNewLine;

				if (index < characterCount + spanLength)
				{
					// we found the right span
					var segment = span.Segment;

					// A <LineBreak/> (or inline-object) span has no glyphs; the caret sits at the span's left edge.
					if (segment.Inline is not Run run)
					{
						return new Rect(x, y, 0, line.Height);
					}

					var characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;
					var offset = index - characterCount;

					for (var i = 0; i < span.GlyphsLength;)
					{
						var next = GetNextCluster(span, i, characterSpacing, out var clusterWidth);

						if (offset < span.GetCharacterOffset(next))
						{
							return new Rect(x, y, clusterWidth, line.Height);
						}

						x += clusterWidth;
						i = next;
					}

					// we should have returned by now, so this is a case of a trailing \r and/or non-rendered trailing spaces, which are not counted in GlyphsLength
					return new Rect(x, y, 0, line.Height);
				}

				characterCount += spanLength;
				x += span.Width;
			}

			if (line != _renderLines[^1])
			{
				y += line.Height;
			}
		}

		// width and height default to 0 if there's nothing there
		return new Rect(x, y, 0, _renderLines.Count > 0 ? _renderLines[^1].Height : 0);
	}

	public TextGeometryPositionInfo GetGeometryPosition(int adjustedIndex)
	{
		var index = Math.Clamp(adjustedIndex, 0, _text.Length);
		var characterRect = GetRectForIndex(index);
		var kind = TextGeometryPositionKind.Caret;
		if (index == _text.Length)
		{
			kind |= TextGeometryPositionKind.FinalEndOfParagraph | TextGeometryPositionKind.TrailingEdge;
		}
		else
		{
			kind |= TextGeometryPositionKind.Text | TextGeometryPositionKind.LeadingEdge;
		}
		if (_flowDirection == FlowDirection.RightToLeft)
		{
			kind |= TextGeometryPositionKind.RightToLeft;
		}

		return new TextGeometryPositionInfo(
			characterRect,
			characterRect with { Width = 0 },
			kind);
	}

	public double GetBaselineForIndex(int adjustedIndex)
	{
		if (_renderLines.Count == 0)
		{
			return _defaultLineHeight;
		}

		var lineIndex = GetLineAt(Math.Clamp(adjustedIndex, 0, _text.Length)).lineIndex;
		var lineBottom = 0f;
		for (var i = 0; i <= lineIndex; i++)
		{
			lineBottom += _renderLines[i].Height;
		}

		return lineBottom + _renderLines[lineIndex].BaselineOffsetY;
	}

	public int VisualLineCount => Math.Max(1, _renderLines.Count);

	public TextVisualLineInfo GetVisualLine(int lineIndex)
	{
		if ((uint)lineIndex >= (uint)VisualLineCount)
		{
			throw new ArgumentOutOfRangeException(nameof(lineIndex));
		}

		if (_renderLines.Count == 0)
		{
			return new TextVisualLineInfo(
				0,
				0,
				0,
				new Rect(0, 0, 0, _defaultLineHeight),
				_defaultLineHeight,
				true,
				true);
		}

		var intervals = GetLineIntervals();
		var line = _renderLines[lineIndex];
		var top = 0f;
		for (var i = 0; i < lineIndex; i++)
		{
			top += _renderLines[i].Height;
		}
		var (x, _) = line.GetOffsets((float)_availableSize.Width, ResolvedTextAlignment);
		return new TextVisualLineInfo(
			intervals[lineIndex].start,
			intervals[lineIndex].length,
			lineIndex,
			new Rect(x, top, line.Width, line.Height),
			top + line.Height + line.BaselineOffsetY,
			lineIndex == 0,
			lineIndex == _renderLines.Count - 1);
	}

	public Hyperlink GetHyperlinkAt(Point point)
	{
		var start = 0;
		var hyperlinks = new List<(int start, int end, Hyperlink hyperlink)>();

		// Only leaves carry text, and _inlines may be either the leaf list (RichTextBlock feeds the
		// formatter through ISkiaParagraphSource.GetLeafInlines) or a pre-order walk. Deriving the
		// ranges from the leaves and walking up to the containing Hyperlink handles both.
		foreach (var inline in _inlines)
		{
			if (inline is Span)
			{
				// Container - its leaves contribute the text.
				continue;
			}

			var length = inline.GetText().Length;

			if (FindContainingHyperlink(inline) is { } hyperlink)
			{
				hyperlinks.Add((start, start + length, hyperlink));
			}

			start += length;
		}
		var characterIndex = ((IParsedText)this).GetIndexAt(point, ignoreEndingNewLine: false, extendedSelection: false);
		return hyperlinks.FirstOrDefault(h => h.start <= characterIndex && h.end > characterIndex)
			.hyperlink;
	}

	// Nearest Hyperlink ancestor of a leaf inline, or null when the leaf is not inside one.
	private static Hyperlink? FindContainingHyperlink(Inline inline)
	{
		for (DependencyObject? current = inline; current is not null; current = current.GetParent() as DependencyObject)
		{
			if (current is Hyperlink hyperlink)
			{
				return hyperlink;
			}

			if (current is Microsoft.UI.Xaml.Controls.TextBlock or Microsoft.UI.Xaml.Controls.RichTextBlock)
			{
				break;
			}
		}

		return null;
	}

	public (int start, int length) GetWordAt(int index, bool right)
	{
		var chunks = new List<(int start, int length)>();
		{
			// a chunk is possible (continuous letters/numbers or continuous non-letters/non-numbers) then possible spaces.
			// \r\n, \r, \n and \t are always their own chunks
			var length = _text.Length;
			for (var i = 0; i < length;)
			{
				var start = i;
				var c = _text[i];
				if (c is '\r' && i < (length - 1) && _text[i + 1] == '\n')
				{
					i += 2;
				}
				else if (c is '\r' or '\t' or '\n')
				{
					i++;
				}
				else if (c == ' ')
				{
					while (i < length && _text[i] == ' ')
					{
						i++;
					}
				}
				else if (char.IsLetterOrDigit(_text[i]))
				{
					while (i < length && char.IsLetterOrDigit(_text[i]))
					{
						i++;
					}
					while (i < length && _text[i] == ' ')
					{
						i++;
					}
				}
				else
				{
					while (i < length && !char.IsLetterOrDigit(_text[i]) && _text[i] != ' ' && _text[i] != '\r')
					{
						i++;
					}
					while (i < length && _text[i] == ' ')
					{
						i++;
					}
				}

				chunks.Add((start, i - start));
			}
		}

		{
			var i = 0;
			foreach (var chunk in chunks)
			{
				if (chunk.start < index && chunk.start + chunk.length > index
					|| chunk.start == index && right
					|| chunk.start + chunk.length == index && !right)
				{
					return chunk;
				}

				i += chunk.length;
			}
			return chunks.Count > 0 ? chunks[^1] : (0, 0);
		}
	}

	/// <summary>
	/// The parameters here use the possibly-negative length format
	/// </summary>
	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
	{
		global::System.Diagnostics.CI.Assert(index >= 0 && index <= _text.Length);
		if (_text.Length == 0)
		{
			return (0, 0, true, true, 0);
		}

		var lines = GetLineIntervals();
		global::System.Diagnostics.CI.Assert(lines.Count > 0);

		for (var i = 0; i < lines.Count; i++)
		{
			var line = lines[i];
			if (line.start <= index && index < line.start + line.length)
			{
				return (line.start, line.length, i == 0, i == lines.Count - 1, i);
			}
		}

		// end == Text.Length
		return (lines[^1].start, lines[^1].length, lines.Count == 1, true, lines.Count - 1);
	}

	public bool IsBaseDirectionRightToLeft => false;

	internal int LineCount => _renderLines.Count;

	// True when the paragraph carries no text at all; SkiaTextLine uses it to stand in for
	// WinUI's EndOfParagraphRun (ParagraphTextSource::GetTextRun).
	internal bool IsEmpty => _text.Length == 0;

	// Bridge accessors used by the RichTextServices Skia formatter (SkiaTextLine /
	// SkiaTextFormatter) to vend per-line metrics over the parsed layout.
	internal IReadOnlyList<RenderLine> RenderLines => _renderLines;

	// Top of a line within the paragraph. A page that starts partway in draws its first line at the
	// top, so hit-testing has to add back the height of the lines it skipped.
	internal float GetLineTop(int lineIndex)
	{
		var top = 0f;
		for (var i = 0; i < lineIndex && i < _renderLines.Count; i++)
		{
			top += _renderLines[i].Height;
		}

		return top;
	}

	// Text trimming collapses a line in place: the formatter hands the same ParsedText back for every
	// line of the paragraph, so the collapsed line has to replace the original for Draw to pick it up.
	internal void ReplaceRenderLine(int index, RenderLine line) => _renderLines[index] = line;

	public float FirstLineBaseline => _renderLines.Count > 0 ? _renderLines[0].Height + _renderLines[0].BaselineOffsetY : _defaultLineHeight;

	internal Size AvailableSize => _availableSize;

	internal TextAlignment TextAlignment => ResolvedTextAlignment;

	// WinUI resolves DetectFromContent to a concrete edge in one place, against the paragraph's
	// detected reading order (ParagraphNode::CalculateLineOffset), and nothing downstream ever
	// sees the enum value. Resolving it here keeps every consumer on that contract.
	private TextAlignment ResolvedTextAlignment
		=> _textAlignment == TextAlignment.DetectFromContent
			? (IsBaseDirectionRightToLeft ? TextAlignment.Right : TextAlignment.Left)
			: _textAlignment;

	internal FlowDirection FlowDirection => _flowDirection;

	// TextStore::Initialize - with TextReadingOrder.DetectFromContent (the RichTextBlock default) the paragraph
	// reads in its content's direction, falling back to FlowDirection.
	internal bool IsReadingOrderRightToLeft() => UnicodeText.IsRightToLeftParagraph(_text, _flowDirection);

	#endregion

	private void HandleSelection(SelectionDetails selection, int lineIndex,
		int characterCountSoFar, Span<SKPoint> positions, float x, float justifySpaceOffset,
		RenderSegmentSpan segmentSpan, Segment segment, FontDetails fontInfo, float y, RenderLine line, SKCanvas canvas,
		CompositionBrush brush, float opacity, SKColor? colorOverride)
	{
		if (selection is { } bg && bg.StartLine <= lineIndex && lineIndex <= bg.EndLine)
		{
			var spanStartingIndex = characterCountSoFar;

			// x at this point is set to the right of the rightmost character ignoring spaces.

			float left;
			if (bg.StartIndex < spanStartingIndex)
			{
				// the selection starts from a previous span, so this span is selected from the very beginning
				left = positions.Length > 0 ? positions[0].X : x;
			}
			else if (segmentSpan.GetRenderedGlyphIndex(bg.StartIndex - spanStartingIndex) is var startGlyph && startGlyph < positions.Length)
			{
				// part or all of this span is selected
				left = positions[startGlyph].X;
			}
			else
			{
				// this span is not a part of the selection, so we select nothing by making the left edge to the far right
				left = x + justifySpaceOffset * segmentSpan.TrailingSpaces;
			}

			float right;
			if (bg.EndIndex - spanStartingIndex < 0)
			{
				// this span is not a part of the selection, so we select nothing by making the left edge to the far left
				right = positions.Length > 0 ? positions[0].X : x;
			}
			else if (segmentSpan.GetRenderedGlyphIndex(bg.EndIndex - spanStartingIndex) is var endGlyph && endGlyph < positions.Length)
			{
				// part or all of this span is selected
				right = positions[endGlyph].X;
			}
			else
			{
				// the selection ends after this span, so this span is selected to the very end
				right = x + justifySpaceOffset * segmentSpan.TrailingSpaces;

				var selectionNotEmpty = bg.StartIndex != bg.EndIndex;
				// CharacterLength doesn't include CRLF, so we specifically check if EndIndex goes past it to know if CRLF is included or not.
				var newLineIncludedInSelection = segmentSpan.EndsInNewLine && bg.EndIndex - spanStartingIndex > segmentSpan.CharacterLength;
				if (selectionNotEmpty && newLineIncludedInSelection)
				{
					// fontInfo.SKFontSize / 3 is a heuristic width of a selected \r, which normally doesn't have a width
					right += (segment.LineBreakAfter ? fontInfo.SKFontSize / 3 : 0);
				}
			}

			if (Math.Abs(left - right) > 0.01)
			{
				var rect = new SKRect(left, y - line.Height, right, y);
				if (colorOverride is { } color)
				{
					_spareSelectionPaint.Color = color;
					canvas.DrawRect(rect, _spareSelectionPaint);
				}
				else
				{
					brush.Paint(canvas, opacity, rect);
				}
			}
		}
	}

	private void RenderText(SelectionDetails? selection, int lineIndex, int characterCountSoFar,
		RenderSegmentSpan segmentSpan, FontDetails fontInfo, Span<SKPoint> positions, Span<ushort> glyphs,
		SKCanvas canvas, float y, SKPaint paint, SKColor? selectionColor = null)
	{
		if (selection is not { } bg || bg.StartLine > lineIndex || lineIndex > bg.EndLine)
		{
			if (segmentSpan.GlyphsLength > 0)
			{
				DrawText(
					fontInfo,
					positions,
					glyphs,
					canvas,
					y,
					paint);
			}
		}
		else
		{
			// Offsets before the span clamp to its first glyph and offsets past it to its end.
			var startOfSelection = segmentSpan.GetRenderedGlyphIndex(bg.StartIndex - characterCountSoFar);
			var endOfSelection = segmentSpan.GetRenderedGlyphIndex(bg.EndIndex - characterCountSoFar);

			if (startOfSelection > 0) // pre selection
			{
				DrawText(
					fontInfo,
					positions.Slice(0, startOfSelection),
					glyphs.Slice(0, startOfSelection),
					canvas,
					y,
					paint);
			}

			if (endOfSelection - startOfSelection > 0) // selection
			{
				var color = paint.Color;
				paint.Color = selectionColor ?? new SKColor(255, 255, 255, 255);
				DrawText(
					fontInfo,
					positions.Slice(startOfSelection, endOfSelection - startOfSelection),
					glyphs.Slice(startOfSelection, endOfSelection - startOfSelection),
					canvas,
					y,
					paint);
				paint.Color = color;
			}

			if (segmentSpan.GlyphsLength - endOfSelection > 0) // post selection
			{
				DrawText(
					fontInfo,
					positions.Slice(endOfSelection, segmentSpan.GlyphsLength - endOfSelection),
					glyphs.Slice(endOfSelection, segmentSpan.GlyphsLength - endOfSelection),
					canvas,
					y,
					paint);
			}
		}

	}

	private static void DrawCollapsingSymbol(CollapsedLineSymbol symbol, SKCanvas canvas, float x, float y)
	{
		var positions = new SKPoint[symbol.Glyphs.Length];
		var pen = x;

		for (var i = 0; i < symbol.Glyphs.Length; i++)
		{
			positions[i] = new SKPoint(pen, 0);
			pen += symbol.Advances[i];
		}

		DrawText(symbol.Font, positions, symbol.Glyphs, canvas, y, _spareDrawPaint);
	}

	private static void DrawText(FontDetails fontInfo, Span<SKPoint> positions, Span<ushort> glyphs,
		SKCanvas canvas, float y, SKPaint paint)
	{
		_textBlobBuilder.AddPositionedRun(glyphs, fontInfo.SKFont, positions);
		// Roughly equivalent to:
		//   using var textBlob = _textBlobBuilder.Build();
		//   canvas.DrawText(textBlob, 0f, y, paint);
		var textBlobHandle = UnoSkiaApi.sk_textblob_builder_make(_textBlobBuilder.Handle);
		UnoSkiaApi.sk_canvas_draw_text_blob(canvas.Handle, textBlobHandle, 0f, y, paint.Handle);
		UnoSkiaApi.sk_textblob_unref(textBlobHandle);
	}

	// TextHighlightRenderer::GetDefaultHighlighterBrushes — the fallback when a highlighter's brush is
	// unset or not a SolidColorBrush.
	private static SolidColorBrush DefaultHighlighterBackground =>
		ResourceResolver.ResolveResourceStatic<SolidColorBrush>("TextControlHighlighterBackground")
		?? new SolidColorBrush(Microsoft.UI.Colors.Yellow);

	// TextHighlightRenderer::IterateMergedHighlighters — collapse every highlighter's ranges into
	// non-overlapping regions where a later entry wins. The caller appends the selection last, so it
	// takes precedence wherever it overlaps an app highlighter.
	private static List<HighlightRegion> MergeHighlighters(IEnumerable<TextHighlighter> highlighters)
	{
		TextHighlightMerge merge = new();

		foreach (var highlighter in highlighters)
		{
			if (highlighter?.Ranges is not { Count: > 0 } ranges)
			{
				continue;
			}

			// WinUI resolves highlighter brushes to SolidColorBrush and falls back to the theme default.
			var foreground = highlighter.Foreground as SolidColorBrush;
			var background = highlighter.Background as SolidColorBrush;

			foreach (var range in ranges)
			{
				if (range.Length <= 0)
				{
					continue;
				}

				// HighlightRegion ranges are inclusive; TextRange carries a length.
				merge.AddRegion(new HighlightRegion(range.StartIndex, range.StartIndex + range.Length - 1, foreground, background));
			}
		}

		return new List<HighlightRegion>(merge.Regions);
	}

	// Renders one segment split across the merged highlight regions: unhighlighted runs keep the base
	// colour, each highlighted run takes its region's foreground. Regions are disjoint and ordered, so a
	// single left-to-right pass covers the segment without overdrawing it.
	private void RenderHighlightedText(List<(HighlightRegion Region, SelectionDetails Details)> regions,
		int lineIndex, int characterCountSoFar, RenderSegmentSpan segmentSpan, FontDetails fontInfo,
		Span<SKPoint> positions, Span<ushort> glyphs, SKCanvas canvas, float y, SKPaint paint,
		SKColor? highContrastSelectionColor)
	{
		var glyphsLength = segmentSpan.GlyphsLength;
		var baseColor = paint.Color;
		var cursor = 0;

		foreach (var (region, details) in regions)
		{
			if (details.StartLine > lineIndex || lineIndex > details.EndLine)
			{
				continue;
			}

			var start = segmentSpan.GetRenderedGlyphIndex(details.StartIndex - characterCountSoFar);
			var end = segmentSpan.GetRenderedGlyphIndex(details.EndIndex - characterCountSoFar);

			if (end <= start)
			{
				continue;
			}

			if (start > cursor)
			{
				DrawText(fontInfo, positions.Slice(cursor, start - cursor), glyphs.Slice(cursor, start - cursor), canvas, y, paint);
			}

			paint.Color = highContrastSelectionColor
				?? (region.ForegroundBrush is { } fg ? ToSkColor(fg.Color, baseColor.Alpha / 255f) : new SKColor(255, 255, 255, 255));
			DrawText(fontInfo, positions.Slice(start, end - start), glyphs.Slice(start, end - start), canvas, y, paint);
			paint.Color = baseColor;

			cursor = end;
		}

		if (cursor < glyphsLength)
		{
			DrawText(fontInfo, positions.Slice(cursor, glyphsLength - cursor), glyphs.Slice(cursor, glyphsLength - cursor), canvas, y, paint);
		}
	}

	private static SKColor ToSkColor(WinUIColor color, float opacity) =>
		new(
			color.R,
			color.G,
			color.B,
			(byte)(color.A * opacity));

	private void HandleCaret(int caretIndex, float caretThickness, SKCanvas canvas,
		CompositionBrush caretBrush, float opacity, int characterCountSoFar, RenderSegmentSpan segmentSpan,
		Span<SKPoint> positions, float x, float justifySpaceOffset, float y, RenderLine line)
	{
		{
			float caretLocation = float.MinValue;

			var offset = caretIndex - characterCountSoFar;
			if (offset >= 0 && offset <= segmentSpan.CharacterLength)
			{
				var glyph = segmentSpan.GetRenderedGlyphIndex(offset);

				// In case of non-rendered trailing spaces, the caret should theoretically be beyond the width of the TextBox,
				// but we still render the caret at the end of the visible area like WinUI does.
				caretLocation = glyph < positions.Length
					? positions[glyph].X
					: x + justifySpaceOffset * (offset > segmentSpan.GetCharacterOffset(positions.Length) ? segmentSpan.TrailingSpaces : 0);
			}

			if (Math.Round(caretLocation + caretThickness) > _availableSize.Width)
			{
				// WinUI draws the caret one-pixel early if the text takes (almost) all the available width.
				// Try this and move the caret to the end (after the l):
				// new TextBox
				// {
				// 	Width = 94,
				// 	TextWrapping = TextWrapping.Wrap,
				// 	Text = "abcdefghijkl",
				// 	FontFamily = new FontFamily("/Assets/Roboto-Regular.ttf#Roboto")
				// }
				// and try again with
				// 	Width = 95,
				// Notice how the caret is drawn one pixel later in the second case even though the text is the exact same.
				caretLocation -= caretThickness;
			}
			if (caretLocation != float.MinValue)
			{
				var caretRect = new SKRect(caretLocation, y - line.Height, caretLocation + caretThickness, y);
				caretBrush.Paint(canvas, opacity, caretRect);
			}
		}
	}

	internal List<(int start, int length)> GetLineIntervals()
	{
		var lineIntervals = new List<(int start, int length)>(_renderLines.Count);

		var start = 0;
		foreach (var line in _renderLines)
		{
			var length = GetCharacterLength(line);
			lineIntervals.Add((start, length));
			start += length;
		}

		return lineIntervals;
	}

	private SelectionDetails CalculateSelection(int start, int end)
	{
		// TODO: we're passing twice to look for the start and end lines. Could easily be done in 1 pass
		var startLine = GetRenderLineAt(GetRectForIndex(start).GetCenter().Y, true)?.index ?? 0;
		var endLine = GetRenderLineAt(GetRectForIndex(end).GetCenter().Y, true)?.index ?? 0;
		return new SelectionDetails(startLine, start, endLine, end);
	}

	/// <param name="extendedSelection">returns the most appropriate match even if y is completely outside the textblock</param>
	private (RenderLine line, int index)? GetRenderLineAt(double y, bool extendedSelection)
	{
		if (_renderLines.Count == 0)
		{
			return null;
		}

		RenderLine line;
		float lineY = 0;
		int i = 0;

		do
		{
			line = _renderLines[i++];
			lineY += line.Height;

			if (y <= lineY && (extendedSelection || y >= lineY - line.Height))
			{
				return (line, i - 1);
			}
		} while (i < _renderLines.Count);

		return extendedSelection ? (line, i - 1) : null;
	}

	private (RenderSegmentSpan span, float x)? GetRenderSegmentSpanAt(Point point, bool extendedSelection)
	{
		var line = GetRenderLineAt(point.Y, extendedSelection)?.line ?? null;

		if (line == null)
		{
			return null;
		}

		RenderSegmentSpan span;
		(float spanX, float justifySpaceOffset) = line.GetOffsets((float)_availableSize.Width, ResolvedTextAlignment);
		int i = 0;

		do
		{
			span = line.RenderOrderedSegmentSpans[i++];
			spanX += span.Width;

			if (point.X <= spanX && (extendedSelection || point.X >= spanX - span.Width))
			{
				return (span, spanX - span.Width);
			}

			spanX += justifySpaceOffset * span.TrailingSpaces;
		} while (i < line.RenderOrderedSegmentSpans.Count);

		return extendedSelection ? (span, spanX - span.Width) : null;
	}

	/// <remarks>Returns a UTF-16 index into the paragraph text, always at a cluster boundary.</remarks>
	public int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection)
	{
		var line = GetRenderLineAt(p.Y, extendedSelection)?.line;

		if (line is not { })
		{
			return -1;
		}

		var characterCount = _renderLines
			.TakeWhile(l => l != line) // all previous lines
			.Sum(GetCharacterLength);

		var (span, x) = GetRenderSegmentSpanAt(p, true)!.Value; // never null because we already found a line

		characterCount += line.SegmentSpans
			.TakeWhile(s => !s.Equals(span)) // all previous spans in line
			.Sum(s => s.CharacterLengthWithNewLine);

		var segment = span.Segment;
		if (segment.Inline is not Run run)
		{
			return characterCount;
		}

		var characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

		// Only rendered glyphs can be found with a pointer. Non-rendered spaces don't matter here.
		for (var i = 0; i < span.GlyphsLength;)
		{
			var next = GetNextCluster(span, i, characterSpacing, out var clusterWidth);
			if (p.X < x + clusterWidth / 2) // the point is closer to the left side of the cluster.
			{
				return characterCount + span.GetCharacterOffset(i);
			}

			x += clusterWidth;
			i = next;
		}

		characterCount += span.GetCharacterOffset(span.GlyphsLength);

		if (ignoreEndingNewLine
			&& span == line.SegmentSpans[^1]
			&& line != _renderLines[^1]
			&& _textWrapping != TextWrapping.NoWrap
			&& span.GlyphsStart + span.GlyphsLength > 0
			&& char.IsWhiteSpace(segment.Text[segment.GetCharacterOffset(span.GlyphsStart + span.GlyphsLength - 1)]))
		{
			// in cases like clicking at the end of a line that ends in a wrapping space, we actually want the character right before the space
			characterCount--;
		}

		return characterCount;
	}

	// The caret stop containing a UTF-16 index: a surrogate pair, a combining sequence or a CRLF is a single stop.
	internal (int Start, int Length) GetCaretStop(int index)
	{
		if (index < 0)
		{
			return (index, 1);
		}

		var characterCount = 0;
		foreach (var line in _renderLines)
		{
			foreach (var span in line.SegmentSpans)
			{
				var spanLength = span.CharacterLengthWithNewLine;
				if (index < characterCount + spanLength)
				{
					var offset = index - characterCount;
					if (offset >= span.CharacterLength)
					{
						return (characterCount + span.CharacterLength, spanLength - span.CharacterLength);
					}

					var (start, end) = span.Segment.GetClusterRange(span.CharacterStart + offset);
					start = Math.Max(start, span.CharacterStart);
					end = Math.Min(end, span.CharacterStart + span.CharacterLength);
					return (characterCount + start - span.CharacterStart, end - start);
				}

				characterCount += spanLength;
			}
		}

		return (index, 1);
	}

	// The UTF-16 length of a line, including the line break it ends with.
	private static int GetCharacterLength(RenderLine line)
	{
		var length = 0;
		foreach (var span in line.SegmentSpans)
		{
			length += span.CharacterLengthWithNewLine;
		}

		return length;
	}

	// Returns the rendered glyph index following the cluster that starts at glyphIndex, and that cluster's width.
	private static int GetNextCluster(RenderSegmentSpan span, int glyphIndex, float characterSpacing, out float clusterWidth)
	{
		var glyphs = span.Segment.Glyphs;
		var cluster = glyphs[span.GlyphsStart + glyphIndex].Cluster;
		clusterWidth = 0;

		do
		{
			clusterWidth += GetGlyphWidthWithSpacing(glyphs[span.GlyphsStart + glyphIndex], characterSpacing);
			glyphIndex++;
		}
		while (glyphIndex < span.GlyphsLength && glyphs[span.GlyphsStart + glyphIndex].Cluster == cluster);

		return glyphIndex;
	}

	private static float GetGlyphWidthWithSpacing(GlyphInfo glyph, float characterSpacing)
	{
		return glyph.AdvanceX > 0 ? glyph.AdvanceX + characterSpacing : glyph.AdvanceX;
	}

	private record SelectionDetails(int StartLine, int StartIndex, int EndLine, int EndIndex)
	{
		public virtual bool Equals(SelectionDetails? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return StartLine == other.StartLine && StartIndex == other.StartIndex && EndLine == other.EndLine && EndIndex == other.EndIndex;
		}
		public override int GetHashCode() => HashCode.Combine(StartLine, StartIndex, EndLine, EndIndex);
	}
}
