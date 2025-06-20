#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Documents
{
	partial class InlineCollection
	{
		// The caret thickness is actually always 1-pixel wide regardless of how big the text is
		internal const float CaretThickness = 1;

		// This is safe as a static field.
		// 1) It's only accessed from UI thread.
		// 2) Once we call SKTextBlobBuilder.Build(), the instance is reset to its initial state.
		// See https://api.skia.org/classSkTextBlobBuilder.html#abf5e20208fd5656981191a3778ee5fef:
		// > Resets SkTextBlobBuilder to its initial empty state, allowing it to be reused to build a new set of runs.
		// The reset to the initial state happens here:
		// https://github.com/google/skia/blob/d29cc3fe182f6e8a8539004a6a4ee8251677a6fd/src/core/SkTextBlob.cpp#L652-L656
		private static readonly SKTextBlobBuilder _textBlobBuilder = new();

		private List<RenderLine> _renderLines = new();

		private bool _invalidationPending;
		private double _lastMeasuredWidth;
		private float _lastDefaultLineHeight;
		private Size _lastDesiredSize;
		private Size _lastArrangedSize;
		private List<(int start, int length)> _lineIntervals;
		private bool _lineIntervalsValid;

		private SelectionDetails _selection = new(0, 0, 0, 0);
		private bool _renderSelection;
		private bool _renderCaret;
		internal bool RenderSelection
		{
			get => _renderSelection;
			set
			{
				if (_renderSelection != value)
				{
					_renderSelection = value;
					((IBlock)_collection.GetParent()).Invalidate(false);
				}
			}
		}
		internal bool RenderCaret
		{
			get => _renderCaret;
			set
			{
				if (_renderCaret != value)
				{
					_renderCaret = value;
					((IBlock)_collection.GetParent()).Invalidate(false);
				}
			}
		}

		internal event Action? DrawingStarted;
		internal event Action<(Rect rect, SKCanvas canvas)>? SelectionFound;
		internal event Action? DrawingFinished;

		/// <summary>
		/// The second argument is whether this caret is the one at the start (false) or the end (true)
		/// of the selection.
		/// </summary>
		internal event Action<(Rect rect, SKCanvas canvas, float opacity, bool endCaret)>? CaretFound;

		internal (int start, int end) Selection
		{
			set
			{
				var parent = ((IBlock)_collection.GetParent());
				var text = parent.GetText();
				var unadjustedValue = (start: CountRunes(text[..value.start]), end: CountRunes(text[..value.end])); // unadjust for surrogate pairs

				// TODO: we're passing twice to look for the start and end lines. Could easily be done in 1 pass
				if (_selection is null || (unadjustedValue.start, unadjustedValue.end) != (_selection.StartIndex, _selection.EndIndex))
				{
					// GetRectForIndex expects an adjusted index, so no need to unadjust
					var startLine = GetRenderLineAt(GetRectForIndex(value.start).GetCenter().Y, true)?.index ?? 0;
					var endLine = GetRenderLineAt(GetRectForIndex(value.end).GetCenter().Y, true)?.index ?? 0;
					_selection = new SelectionDetails(startLine, unadjustedValue.start, endLine, unadjustedValue.end);
					parent.Invalidate(false);
				}

				static int CountRunes(string s)
				{
					var enumerator = s.EnumerateRunes();
					int count = 0;
					// Avoid LINQ's Count() because it will box the enumerator.
					while (enumerator.MoveNext())
					{
						count++;
					}

					return count;
				}
			}
		}

		/// <summary>
		/// Measures a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal Size Measure(Size availableSize, float defaultLineHeight)
		{
			_lastDefaultLineHeight = defaultLineHeight;
			if (!_invalidationPending &&
				availableSize.Width <= _lastMeasuredWidth &&
				availableSize.Width >= _lastDesiredSize.Width)
			{
				return _lastDesiredSize;
			}
			_invalidationPending = false;
			_lineIntervalsValid = false;
			_lastMeasuredWidth = availableSize.Width;

			var parent = (IBlock)_collection.GetParent();
			_renderLines = ParseText(
				availableSize,
				defaultLineHeight,
				parent.TextWrapping,
				parent.MaxLines,
				(float)parent.LineHeight,
				parent.LineStackingStrategy,
				out _lastDesiredSize);
			return _lastDesiredSize;
		}

		/// <summary>
		/// Measures a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal List<RenderLine> ParseText(
			Size availableSize,
			float defaultLineHeight,
			TextWrapping wrapping,
			int maxLines,
			float lineHeight,
			LineStackingStrategy lineStackingStrategy,
			out Size desiredSize)
		{
			lineStackingStrategy = lineHeight == 0 ? LineStackingStrategy.MaxHeight : lineStackingStrategy;

			var renderLines = new List<RenderLine>();
			List<RenderSegmentSpan> lineSegmentSpans = new();
			bool previousLineWrapped = false;

			float availableWidth = wrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
			float widestLineWidth = 0, widestLineHeight = 0;

			float x = 0;
			float height = 0;

			foreach (var inline in PreorderTree)
			{
				if (inline is LineBreak lineBreak)
				{
					Segment breakSegment = new(lineBreak);
					RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0, 0, 0, 0, 0);
					lineSegmentSpans.Add(breakSegmentSpan);

					MoveToNextLine(currentLineWrapped: false);
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

						int start = x == 0 && previousLineWrapped ? segment.LeadingSpaces : 0;
						int skippedLeadingSpaces = start;

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

							var fullGlyphsLength = start == skippedLeadingSpaces ? segment.Glyphs.Count : segment.Glyphs.Count - start;
							if (segment.LineBreakAfter)
							{
								fullGlyphsLength--;
							}
							RenderSegmentSpan segmentSpan = new(segment, start, length, Math.Max(0, segment.LeadingSpaces - start), trailingSpaces, characterSpacing, width, widthWithoutTrailingSpaces, fullGlyphsLength);
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
								RenderSegmentSpan segmentSpan = new(segment, 0, spaces, spaces, 0, characterSpacing, widthWithoutTrailingSpaces, 0, segment.LeadingSpaces);
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
							if (PreorderTree[^1] == run && run.Segments[^1] == segment && start == segment.LeadingSpaces && segment.LeadingSpaces == segment.Glyphs.Count)
							{
								continue;
							}

							MoveToNextLine(currentLineWrapped: true);
							goto BeginSegmentFitting;
						}

						// There is no content on the line so wrap the segment according to the wrapping mode.

						if (wrapping == TextWrapping.WrapWholeWords)
						{
							// Put the whole segment on the line and move to the next line.

							var fullGlyphsLength = start == skippedLeadingSpaces ? segment.Glyphs.Count : segment.Glyphs.Count - start;
							if (segment.LineBreakAfter)
							{
								fullGlyphsLength--;
							}
							RenderSegmentSpan segmentSpan = new(segment, start, segmentLengthWithoutTrailingSpaces - segment.LeadingSpaces, 0, segment.TrailingSpaces, characterSpacing, widthWithoutTrailingSpaces, widthWithoutTrailingSpaces, fullGlyphsLength);
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

							// We've already dealt with leading spaces.
							// We can't fit the segment content (excluding trailing spaces),
							// so this span definitely doesn't include any trailing spaces either.

							RenderSegmentSpan segmentSpan = new(segment, start, length, 0, 0, characterSpacing, widthWithoutTrailingSpaces, widthWithoutTrailingSpaces, start == skippedLeadingSpaces ? length + skippedLeadingSpaces : length);
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

				// this bit isn't strictly necessary but it maintains the invariant that RenderLines always have a span
				Segment breakSegment = new(new LineBreak());
				RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0, 0, 0, 0, 0);
				lineSegmentSpans.Add(breakSegmentSpan);

				MoveToNextLine(false);
			}

			if (lineSegmentSpans.Count != 0)
			{
				// every line gets finalized in MoveToNextLine, so it must be called for the last line too.
				MoveToNextLine(false);
			}

		MaxLinesHit:

			desiredSize = renderLines.Count == 0 ? new Size(0, defaultLineHeight) : new Size(widestLineWidth, height);
			return renderLines;

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

			void MoveToNextLine(bool currentLineWrapped)
			{
				var renderLine = new RenderLine(lineSegmentSpans, lineStackingStrategy, lineHeight, renderLines.Count == 0, currentLineWrapped);
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
		}

		private static float GetGlyphWidthWithSpacing(GlyphInfo glyph, float characterSpacing)
		{
			return glyph.AdvanceX > 0 ? glyph.AdvanceX + characterSpacing : glyph.AdvanceX;
		}

		internal Size Arrange(Size finalSize)
		{
			_lastArrangedSize = finalSize;

			if (!_invalidationPending &&
				finalSize.Width <= _lastMeasuredWidth &&
				finalSize.Width >= _lastDesiredSize.Width)
			{
				return _lastDesiredSize;
			}

			return Measure(finalSize, _lastDefaultLineHeight);
		}

		internal void InvalidateMeasure()
		{
			// Mark invalidation as pending, but temporarily keep
			// the least last measured width, last desired size, and
			// last arranged size, so that asynchronous rendering can still
			// use them to render properly.
			_invalidationPending = true;
		}

		private static SKPaint _spareDrawPaint = new SKPaint();

		/// <summary>
		/// Renders a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal void Draw(in Visual.PaintingSession session)
		{
			DrawingStarted?.Invoke();

			if (_renderLines.Count == 0)
			{
				DrawingFinished?.Invoke();
				// empty, so caret is at the beginning
				if (RenderCaret)
				{
					CaretFound?.Invoke((new Rect(new Point(0, 0), new Point(CaretThickness, _lastDefaultLineHeight)), session.Canvas, session.Opacity, false));
					CaretFound?.Invoke((new Rect(new Point(0, 0), new Point(CaretThickness, _lastDefaultLineHeight)), session.Canvas, session.Opacity, true));
				}
				DrawingFinished?.Invoke();

				return;
			}

			var canvas = session.Canvas;
			var parent = (IBlock)_collection.GetParent();
			var alignment = parent.TextAlignment;
			if (parent.FlowDirection == FlowDirection.RightToLeft)
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

			for (var lineIndex = 0; lineIndex < _renderLines.Count; lineIndex++)
			{
				var line = _renderLines[lineIndex];
				// TODO: (Performance) Stop rendering when the lines exceed the available height

				(float x, float justifySpaceOffset) = line.GetOffsets((float)_lastArrangedSize.Width, alignment);

				y += line.Height;
				float baselineOffsetY = line.BaselineOffsetY;

				for (int s = 0; s < line.RenderOrderedSegmentSpans.Count; s++)
				{
					var xBeforeGlyphOffsets = x;
					var segmentSpan = line.RenderOrderedSegmentSpans[s];

					var segment = segmentSpan.Segment;
					var inline = segment.Inline;
					var fontInfo = segment.FallbackFont ?? inline.FontInfo;

					var paint = _spareDrawPaint;

					paint.Reset();

					paint.IsStroke = false;
					paint.IsAntialias = true;

					if (inline.Foreground is SolidColorBrush scb)
					{
						var scbColor = scb.Color;
						paint.Color = new SKColor(
							red: scbColor.R,
							green: scbColor.G,
							blue: scbColor.B,
							alpha: (byte)(scbColor.A * scb.Opacity * session.Opacity));
					}
					else if (inline.Foreground is GradientBrush gb)
					{
						var gbColor = gb.FallbackColorWithOpacity;
						paint.Color = new SKColor(
							red: gbColor.R,
							green: gbColor.G,
							blue: gbColor.B,
							alpha: (byte)(gbColor.A * session.Opacity));
					}
					else if (inline.Foreground is XamlCompositionBrushBase xcbb)
					{
						var gbColor = xcbb.FallbackColorWithOpacity;
						paint.Color = new SKColor(
							red: gbColor.R,
							green: gbColor.G,
							blue: gbColor.B,
							alpha: (byte)(gbColor.A * session.Opacity));
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

					HandleSelection(lineIndex, characterCountSoFar, positionsSpan, x, justifySpaceOffset, segmentSpan, segment, fontInfo, y, line, canvas);

					RenderText(lineIndex, characterCountSoFar, segmentSpan, fontInfo, positionsSpan, glyphsSpan, canvas, y + baselineOffsetY, paint);

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

					HandleCaret(canvas, session.Opacity, characterCountSoFar, lineIndex, segmentSpan, positionsSpan, x, justifySpaceOffset, y, line);

					x += justifySpaceOffset * segmentSpan.TrailingSpaces;
					characterCountSoFar += segmentSpan.FullGlyphsLength + (SpanEndsInNewLine(segmentSpan) ? segment.LineBreakLength : 0);

					ArrayPool<SKPoint>.Shared.Return(positions);
					ArrayPool<ushort>.Shared.Return(glyphs);
				}
			}

			DrawingFinished?.Invoke();

			static void DrawDecoration(SKCanvas canvas, float x, float y, float width, float thickness, SKPaint paint)
			{
				paint.StrokeWidth = thickness;
				paint.IsStroke = true;
				canvas.DrawLine(x, y, x + width, y, paint);
				paint.IsStroke = false;
			}
		}

		private void HandleSelection(int lineIndex, int characterCountSoFar, Span<SKPoint> positions, float x, float justifySpaceOffset, RenderSegmentSpan segmentSpan, Segment segment, FontDetails fontInfo, float y, RenderLine line, SKCanvas canvas)
		{
			if (RenderSelection && _selection is { } bg && bg.StartLine <= lineIndex && lineIndex <= bg.EndLine)
			{
				var spanStartingIndex = characterCountSoFar;

				// x at this point is set to the right of the rightmost character ignoring spaces.

				float left;
				if (bg.StartIndex < spanStartingIndex)
				{
					// the selection starts from a previous span, so this span is selected from the very beginning
					left = positions.Length > 0 ? positions[0].X : x;
				}
				else if (bg.StartIndex - spanStartingIndex < positions.Length)
				{
					// part or all of this span is selected
					left = positions[bg.StartIndex - spanStartingIndex].X;
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
				else if (bg.EndIndex - spanStartingIndex < positions.Length)
				{
					// part or all of this span is selected
					right = positions[bg.EndIndex - spanStartingIndex].X;
				}
				else
				{
					// the selection ends after this span, so this span is selected to the very end
					right = x + justifySpaceOffset * segmentSpan.TrailingSpaces;

					var selectionNotEmpty = bg.StartIndex != bg.EndIndex;
					// positions.Length doesn't include CRLF, so we specifically check if EndIndex goes past position.Length to know if CRLF is included or not.
					var newLineIncludedInSelection = SpanEndsInNewLine(segmentSpan) && bg.EndIndex - spanStartingIndex > positions.Length;
					if (selectionNotEmpty && newLineIncludedInSelection)
					{
						// fontInfo.SKFontSize / 3 is a heuristic width of a selected \r, which normally doesn't have a width
						right += (segment.LineBreakAfter ? fontInfo.SKFontSize / 3 : 0);
					}
				}

				if (Math.Abs(left - right) > 0.01)
				{
					SelectionFound?.Invoke((new Rect(new Point(left, y - line.Height), new Point(right, y)), canvas));
				}
			}
		}

		private void RenderText(int lineIndex, int characterCountSoFar, RenderSegmentSpan segmentSpan, FontDetails fontInfo, Span<SKPoint> positions, Span<ushort> glyphs, SKCanvas canvas, float y, SKPaint paint)
		{
			if (!RenderSelection || _selection is not { } bg || bg.StartLine > lineIndex || lineIndex > bg.EndLine)
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
				var spanStartingIndex = characterCountSoFar;
				int startOfSelection;
				int endOfSelection;

				if (bg.StartIndex < spanStartingIndex)
				{
					// the selection starts from a previous span, so this span is selected from the very beginning
					startOfSelection = 0;
				}
				else if (bg.StartIndex - spanStartingIndex < segmentSpan.GlyphsLength)
				{
					// part or all of this span is selected
					startOfSelection = bg.StartIndex - spanStartingIndex;
				}
				else
				{
					// this span is not a part of the selection
					startOfSelection = segmentSpan.GlyphsLength;
				}

				if (bg.EndIndex - spanStartingIndex < 0)
				{
					// this span is not a part of the selection
					endOfSelection = 0;
				}
				else if (bg.EndIndex - spanStartingIndex < segmentSpan.GlyphsLength)
				{
					// part or all of this span is selected
					endOfSelection = bg.EndIndex - spanStartingIndex;
				}
				else
				{
					// the selection ends after this span, so this span is selected to the very end
					endOfSelection = segmentSpan.GlyphsLength;
				}

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
					paint.Color = new SKColor(255, 255, 255, 255); // selection is always white
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

			static void DrawText(FontDetails fontInfo, Span<SKPoint> positions, Span<ushort> glyphs,
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
		}

		private void HandleCaret(SKCanvas canvas, float opacity, int characterCountSoFar, int lineIndex, RenderSegmentSpan segmentSpan,
			Span<SKPoint> positions, float x, float justifySpaceOffset, float y, RenderLine line)
		{
			var spanStartingIndex = characterCountSoFar;
			if (RenderCaret)
			{
				foreach (var (l, i, caretAtSelectionEnd) in (ReadOnlySpan<(int, int, bool)>)[(_selection.StartLine, _selection.StartIndex, false), (_selection.EndLine, _selection.EndIndex, true)])
				{
					float caretLocation = float.MinValue;

					if (l == lineIndex && i >= spanStartingIndex && i <= spanStartingIndex + segmentSpan.GlyphsLength)
					{
						if (i >= spanStartingIndex + positions.Length)
						{
							caretLocation = x + justifySpaceOffset * (i - (spanStartingIndex + positions.Length));
						}
						else
						{
							caretLocation = positions[i - spanStartingIndex].X;
						}
					}
					else if (l == lineIndex && i >= spanStartingIndex && i <= spanStartingIndex + segmentSpan.FullGlyphsLength)
					{
						// In case of non-rendered trailing spaces, the caret should theoretically be beyond the width of the TextBox,
						// but we still render the caret at the end of the visible area like WinUI does.
						caretLocation = x + justifySpaceOffset * segmentSpan.TrailingSpaces;
					}

					if (Math.Round(caretLocation + CaretThickness) > _lastArrangedSize.Width)
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
						caretLocation -= CaretThickness;
					}

					if (caretLocation != float.MinValue)
					{
						CaretFound?.Invoke((new Rect(new Point(caretLocation, y - line.Height), new Point(caretLocation + CaretThickness, y)), canvas, opacity, caretAtSelectionEnd));
					}
				}
			}
		}

		/// <remarks>Adjusted for surrogate pairs</remarks>
		internal int GetIndexAt(Point p, bool ignoreEndingSpace, bool extendedSelection)
			=> AdjustIndexForSurrogatePairs(GetIndexAtUnadjusted(p, ignoreEndingSpace, extendedSelection));

		private int GetIndexAtUnadjusted(Point p, bool ignoreEndingSpace, bool extendedSelection)
		{
			var line = GetRenderLineAt(p.Y, extendedSelection)?.line;

			if (line is not { })
			{
				return -1;
			}

			var characterCount = _renderLines
				.TakeWhile(l => l != line) // all previous lines
				.Sum(currentLine => currentLine.SegmentSpans.Sum(GlyphsLengthWithCR)); // all characters in line

			var (span, x) = GetRenderSegmentSpanAt(p, true)!.Value; // never null because we already found a line

			characterCount += line.SegmentSpans
				.TakeWhile(s => !s.Equals(span)) // all previous spans in line
				.Sum(GlyphsLengthWithCR); // all characters in span

			var segment = span.Segment;
			if (segment.Inline is not Run run)
			{
				return characterCount;
			}

			var characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

			// The rest of the function uses GlyphsLength and not FullGlyphsLength as we can only really find a rendered glyph with a pointer.
			// Non-rendered spaces don't matter here.
			var glyphStart = span.GlyphsStart;
			var glyphEnd = glyphStart + span.GlyphsLength;
			for (var i = glyphStart; i < glyphEnd; i++)
			{
				var glyph = segment.Glyphs[i];
				var glyphWidth = GetGlyphWidthWithSpacing(glyph, characterSpacing);
				if (p.X < x + glyphWidth / 2) // the point is closer to the left side of the glyph.
				{
					return characterCount;
				}

				x += glyphWidth;
				characterCount++;
			}

			if (ignoreEndingSpace
				&& span == line.SegmentSpans[^1]
				&& line != _renderLines[^1]
				&& ((IBlock)_collection.GetParent()).TextWrapping != TextWrapping.NoWrap
				&& span.GlyphsStart + span.GlyphsLength > 0
				&& char.IsWhiteSpace(segment.Text[span.GlyphsStart + span.GlyphsLength - 1]))
			{
				// in cases like clicking at the end of a line that ends in a wrapping space, we actually want the character right before the space
				characterCount--;
			}

			return characterCount;
		}

		// Warning: this is only tested and currently used by TextBox
		/// <remarks>Takes an already adjusted-for-surrogate-pairs index</remarks>
		internal Rect GetRectForIndex(int adjustedIndex)
		{
			var characterCount = 0;
			float y = 0, x = 0;
			var parent = (IBlock)_collection.GetParent();
			var index = parent.GetText()[..adjustedIndex].EnumerateRunes().Count(); // unadjust

			foreach (var line in _renderLines)
			{
				(x, var justifySpaceOffset) = line.GetOffsets((float)_lastArrangedSize.Width, parent.TextAlignment);

				var spans = line.RenderOrderedSegmentSpans;
				foreach (var span in spans)
				{
					var glyphCount = GlyphsLengthWithCR(span);

					if (index < characterCount + glyphCount)
					{
						// we found the right span
						var segment = span.Segment;
						var run = (Run)segment.Inline;
						var characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

						var glyphStart = span.GlyphsStart;
						var glyphEnd = glyphStart + span.GlyphsLength;
						for (var i = glyphStart; i < glyphEnd; i++)
						{
							var glyph = segment.Glyphs[i];
							var glyphWidth = GetGlyphWidthWithSpacing(glyph, characterSpacing);

							if (index == characterCount)
							{
								return new Rect(x, y, glyphWidth, line.Height);
							}

							x += glyphWidth;
							characterCount++;
						}

						// we should have returned by now, so this is a case of a trailing \r and/or non-rendered trailing spaces, which are not counted in GlyphsLength
						return new Rect(x, y, 0, line.Height);
					}

					characterCount += glyphCount;
					x += span.Width;
				}

				y += line.Height;
			}

			// width and height default to 0 if there's nothing there
			return new Rect(x, y, 0, _renderLines.Count > 0 ? _renderLines[^1].Height : 0);
		}

		/// <param name="extendedSelection">returns the most appropriate match even if y is completely outside the textblock</param>
		internal (RenderLine line, int index)? GetRenderLineAt(double y, bool extendedSelection)
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

		internal (RenderSegmentSpan span, float x)? GetRenderSegmentSpanAt(Point point, bool extendedSelection)
		{
			var parent = (IBlock)_collection.GetParent();

			var line = GetRenderLineAt(point.Y, extendedSelection)?.line ?? null;

			if (line == null)
			{
				return null;
			}

			RenderSegmentSpan span;
			(float spanX, float justifySpaceOffset) = line.GetOffsets((float)_lastArrangedSize.Width, parent.TextAlignment);
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

		// equivalent to AdjustSelectionForSurrogatePairs for a single index
		/// <remarks>does nothing on invalid index input (i.e. keeps it invalid)</remarks>
		private int AdjustIndexForSurrogatePairs(int index)
		{
			if (index < 0)
			{
				return index;
			}

			var text = ((IBlock)_collection.GetParent()).GetText();
			var count = 0;
			for (int i = 0, j = 0; i < text.Length && j < index; i++, j++, count += 1)
			{
				if (i < text.Length - 1 && char.IsSurrogatePair(text[i], text[i + 1]))
				{
					// Notice how j didn't move here
					count += 1;
					i++;
				}
			}

			return count;
		}

		// Warning: this is only tested and currently used by TextBox
		internal List<(int start, int length)> GetLineIntervals()
		{
			if (_lineIntervalsValid)
			{
				return _lineIntervals;
			}

			_lineIntervalsValid = true;

			_lineIntervals = new List<(int start, int length)>(_renderLines.Count);

			var start = 0;
			foreach (var line in _renderLines)
			{
				var length = line.SegmentSpans.Sum(GlyphsLengthWithCR);
				_lineIntervals.Add((start, length));
				start += length;
			}

			return _lineIntervals;
		}

		internal void GetHyperlinkPositions(IList<(int start, int, Hyperlink hyperlink)> hyperlinks)
		{
			var start = 0;
			foreach (var inline in PreorderTree)
			{
				switch (inline)
				{
					case Hyperlink hyperlink:
						hyperlinks.Add((start, start + hyperlink.GetText().Length, hyperlink));
						break;
					case Span:
						break;
					default: // Leaf node
						start += inline.GetText().Length;
						break;
				}
			}
		}

		// RenderSegmentSpan.FullGlyphsLength includes spaces, but not \r
		private static int GlyphsLengthWithCR(RenderSegmentSpan span)
			=> span.FullGlyphsLength + (SpanEndsInNewLine(span) ? span.Segment.LineBreakLength : 0);

		private static bool SpanEndsInNewLine(RenderSegmentSpan segmentSpan)
		{
			var segment = segmentSpan.Segment;

			return segment is { Inline: Run, LineBreakAfter: true } &&
				segment.Text.TrimEnd().Length <= segmentSpan.GlyphsStart + segmentSpan.GlyphsLength; // last in segment
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
}
