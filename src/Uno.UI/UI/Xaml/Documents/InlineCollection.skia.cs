using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Xaml.Media;
using Uno.UI.Composition;

#nullable enable

namespace Windows.UI.Xaml.Documents
{
	partial class InlineCollection
	{
		internal const float CaretThicknessAsRatioOfLineHeight = 0.05f;

		// This is safe as a static field.
		// 1) It's only accessed from UI thread.
		// 2) Once we call SKTextBlobBuilder.Build(), the instance is reset to its initial state.
		// See https://api.skia.org/classSkTextBlobBuilder.html#abf5e20208fd5656981191a3778ee5fef:
		// > Resets SkTextBlobBuilder to its initial empty state, allowing it to be reused to build a new set of runs.
		// The reset to the initial state happens here:
		// https://github.com/google/skia/blob/d29cc3fe182f6e8a8539004a6a4ee8251677a6fd/src/core/SkTextBlob.cpp#L652-L656
		private static SKTextBlobBuilder _textBlobBuilder = new();

		private readonly List<RenderLine> _renderLines = new();

		private bool _invalidationPending;
		private double _lastMeasuredWidth;
		private float _lastDefaultLineHeight;
		private Size _lastDesiredSize;
		private Size _lastArrangedSize;

		// these should only be used by TextBox.
		internal (int startLine, int startIndex, int endLine, int endIndex)? Selection { get; set; }
		internal (bool atEndOfSelection, Windows.UI.Color color)? Caret { get; set; }
		internal bool RenderSelectionAndCaret { get; set; }

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

			_lastMeasuredWidth = availableSize.Width;

			var parent = (IBlock)_collection.GetParent();
			var wrapping = parent.TextWrapping;
			int maxLines = parent.MaxLines;

			float lineHeight = (float)parent.LineHeight;
			var lineStackingStrategy = lineHeight == 0 ? LineStackingStrategy.MaxHeight : parent.LineStackingStrategy;

			_renderLines.Clear();

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
					RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0, 0, 0);
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

					BeginSegmentFitting:

						if (maxLines > 0 && _renderLines.Count == maxLines)
						{
							goto MaxLinesHit;
						}

						float remainingWidth = availableWidth - x;
						(int length, float width) = GetSegmentRenderInfo(segment, start, characterSpacing);

						// Check if whole segment fits

						if (width <= remainingWidth)
						{
							// Add in as many trailing spaces as possible

							float widthWithoutTrailingSpaces = width;
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

							RenderSegmentSpan segmentSpan = new(segment, start, length, trailingSpaces, characterSpacing, width, widthWithoutTrailingSpaces);
							lineSegmentSpans.Add(segmentSpan);
							x += width;

							if (segment.LineBreakAfter)
							{
								MoveToNextLine(currentLineWrapped: false);
							}
							else if (length < end)
							{
								MoveToNextLine(currentLineWrapped: true);
							}

							continue;
						}

						// Whole segment does not fit so tack on as many leading spaces as possible

						if (start == 0 && segment.LeadingSpaces > 0)
						{
							int spaces = 0;
							width = 0;

							if (x == 0)
							{
								// minimum 1 space if this is the start of the line

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

							if (width > 0)
							{
								RenderSegmentSpan segmentSpan = new(segment, 0, spaces, 0, characterSpacing, width, 0);
								lineSegmentSpans.Add(segmentSpan);
								x += width;

								start = segment.LeadingSpaces;
							}
						}

						if (x > 0)
						{
							// There is content on this line and the segment did not fit so wrap to the next line and retry adding the segment

							MoveToNextLine(currentLineWrapped: true);
							goto BeginSegmentFitting;
						}

						// There is no content on the line so wrap the segment according to the wrapping mode.

						if (wrapping == TextWrapping.WrapWholeWords)
						{
							// Put the whole segment on the line and move to the next line.

							RenderSegmentSpan segmentSpan = new(segment, start, length, segment.TrailingSpaces, characterSpacing, width, width);
							lineSegmentSpans.Add(segmentSpan);
							x += width;

							MoveToNextLine(currentLineWrapped: !segment.LineBreakAfter);
						}
						else // wrapping == TextWrapping.Wrap
						{
							// Put as much of the segment on this line as possible then continue fitting the rest of the segment on the next line

							length = 1;
							width = GetGlyphWidthWithSpacing(segment.Glyphs[start], characterSpacing);

							while (start + length < segment.Glyphs.Count
								&& (width + GetGlyphWidthWithSpacing(segment.Glyphs[start + length], characterSpacing)) is var newWidth
								&& newWidth < remainingWidth)
							{
								width = newWidth;
								length++;
							}

							RenderSegmentSpan segmentSpan = new(segment, start, length, 0, characterSpacing, width, width);
							lineSegmentSpans.Add(segmentSpan);
							x += width;
							start += length;

							MoveToNextLine(currentLineWrapped: true);
							goto BeginSegmentFitting;
						}
					}
				}
			}

			if (lineSegmentSpans.Count != 0)
			{
				MoveToNextLine(false);
			}

		MaxLinesHit:

			if (_renderLines.Count == 0)
			{
				_lastDesiredSize = new Size(0, defaultLineHeight);
			}
			else
			{
				_lastDesiredSize = new Size(widestLineWidth, height);
			}

			return _lastDesiredSize;

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
				var renderLine = new RenderLine(lineSegmentSpans, lineStackingStrategy, lineHeight, _renderLines.Count == 0, currentLineWrapped);
				_renderLines.Add(renderLine);
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

		/// <summary>
		/// Renders a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal void Draw(in DrawingSession session)
		{
			if (_renderLines.Count == 0)
			{
				return;
			}

			var canvas = session.Surface.Canvas;
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
					var segmentSpan = line.RenderOrderedSegmentSpans[s];

					var currentCharacterCount = segmentSpan.GlyphsLength;

					if (segmentSpan.GlyphsLength == 0)
					{
						continue;
					}

					var segment = segmentSpan.Segment;
					var inline = segment.Inline;
					var fontInfo = inline.FontInfo;
					var paint = inline.Paint;

					if (segment.FallbackFont is FontDetails fallback)
					{
						paint = segment.Paint!;
						fontInfo = fallback;
					}

					if (inline.Foreground is SolidColorBrush scb)
					{
						paint.Color = new SKColor(
							red: scb.Color.R,
							green: scb.Color.G,
							blue: scb.Color.B,
							alpha: (byte)(scb.Color.A * scb.Opacity * session.Filters.Opacity));
					}

					var decorations = inline.TextDecorations;
					const TextDecorations allDecorations = TextDecorations.Underline | TextDecorations.Strikethrough;

					if ((decorations & allDecorations) != 0)
					{
						var metrics = fontInfo.SKFontMetrics;
						float width = s == line.RenderOrderedSegmentSpans.Count - 1 ? segmentSpan.WidthWithoutTrailingSpaces : segmentSpan.Width;

						if ((decorations & TextDecorations.Underline) != 0)
						{
							// TODO: what should default thickness/position be if metrics does not contain it?
							float yPos = y + baselineOffsetY + (metrics.UnderlinePosition ?? 0);
							DrawDecoration(canvas, x, yPos, width, metrics.UnderlineThickness ?? 1, paint);
						}

						if ((decorations & TextDecorations.Strikethrough) != 0)
						{
							// TODO: what should default thickness/position be if metrics does not contain it?
							float yPos = y + baselineOffsetY + (metrics.StrikeoutPosition ?? fontInfo.SKFontSize / -2);
							DrawDecoration(canvas, x, yPos, width, metrics.StrikeoutThickness ?? 1, paint);
						}
					}

					var run = _textBlobBuilder.AllocatePositionedRunFast(fontInfo.SKFont, segmentSpan.GlyphsLength);
					var glyphs = run.GetGlyphSpan(segmentSpan.GlyphsLength);
					var positions = run.GetPositionSpan(segmentSpan.GlyphsLength);

					if (segment.Direction == FlowDirection.LeftToRight)
					{
						for (int i = 0; i < segmentSpan.GlyphsLength; i++)
						{
							var glyphInfo = segment.Glyphs[segmentSpan.GlyphsStart + i];

							if (glyphInfo.AdvanceX > 0)
							{
								x += segmentSpan.CharacterSpacing;
							}

							glyphs[i] = glyphInfo.GlyphId;
							positions[i] = new SKPoint(x + glyphInfo.OffsetX - segmentSpan.CharacterSpacing, glyphInfo.OffsetY);
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

								glyphs[j] = glyphInfo.GlyphId;
								positions[j] = new SKPoint(x + glyphInfo.OffsetX, glyphInfo.OffsetY);
								x += glyphInfo.AdvanceX;
							}
						}
					}

					// Warning: this is only tested and currently used by single-line single-run skia-based TextBoxes
					{
						if (RenderSelectionAndCaret && Selection is { } bg && bg.startLine <= lineIndex && lineIndex <= bg.endLine)
						{
							var spanStartingIndex = characterCountSoFar;

							float left;
							if (bg.startLine == lineIndex)
							{
								if (bg.startIndex - spanStartingIndex < 0)
								{
									if (positions.Length > 0)
									{
										left = positions[0].X;
									}
									else
									{
										// no glyphs, so we're at the start
										left = x;
									}
								}
								else if (bg.startIndex - spanStartingIndex < positions.Length)
								{
									left = positions[bg.startIndex - spanStartingIndex].X;
								}
								else if (bg.startIndex - spanStartingIndex < currentCharacterCount) // positions.Length + TrailingSpaces
								{
									// x is set to the end of the glyph sequence (no accounting for spaces yet)
									left = x + justifySpaceOffset * (currentCharacterCount - (bg.startIndex - spanStartingIndex));
								}
								else
								{
									left = x + justifySpaceOffset * segmentSpan.TrailingSpaces;
								}
							}
							else
							{
								if (positions.Length > 0)
								{
									left = positions[0].X;
								}
								else
								{
									// no glyphs, so we're at the start
									left = x;
								}
							}

							float right;
							if (bg.endLine == lineIndex)
							{
								if (bg.endIndex - spanStartingIndex < 0)
								{
									if (positions.Length > 0)
									{
										right = positions[0].X;
									}
									else
									{
										// no glyphs, so we're at the start
										right = x;
									}
								}
								else if (bg.endIndex - spanStartingIndex < positions.Length)
								{
									right = positions[bg.endIndex - spanStartingIndex].X;
								}
								else if (bg.endIndex - spanStartingIndex < currentCharacterCount) // positions.Length + TrailingSpaces
								{
									// x is set to the end of the glyph sequence
									right = x + justifySpaceOffset * (currentCharacterCount - (bg.endIndex - spanStartingIndex));
								}
								else
								{
									right = x + justifySpaceOffset * segmentSpan.TrailingSpaces;
								}
							}
							else
							{
								right = x + justifySpaceOffset * segmentSpan.TrailingSpaces;
							}

							canvas.DrawRect(new SKRect(left, y - line.Height, right, y), new SKPaint
							{
								Color = ((TextBlock)parent).SelectionHighlightColor.Color.ToSKColor(),
								Style = SKPaintStyle.Fill
							});
						}
					}

					using var textBlob = _textBlobBuilder.Build();
					canvas.DrawText(textBlob, 0, y + baselineOffsetY, paint);

					// Warning: this is only tested and currently used by single-line single-run skia-based TextBoxes
					{
						var spanStartingIndex = characterCountSoFar;
						if (RenderSelectionAndCaret && Caret is { } caret && Selection is { } selection)
						{
							var (l, i) = caret.atEndOfSelection ? (selection.endLine, selection.endIndex) : (selection.startLine, selection.startIndex);

							float caretLocation = -1f;

							if (l == lineIndex && i >= spanStartingIndex && i <= characterCountSoFar + currentCharacterCount)
							{
								if (i >= spanStartingIndex + positions.Length)
								{
									caretLocation = x + justifySpaceOffset * (i - (spanStartingIndex + positions.Length)) - line.Height * 0.05f;
								}
								else
								{
									caretLocation = positions[i - spanStartingIndex].X;
								}
							}

							if (caretLocation != -1f)
							{
								canvas.DrawRect(new SKRect(caretLocation, y - line.Height, caretLocation + line.Height * 0.05f, y), new SKPaint
								{
									Color = new SKColor(caret.color.R, caret.color.G, caret.color.B, caret.color.A),
									Style = SKPaintStyle.Fill
								});
							}
						}
					}

					x += justifySpaceOffset * segmentSpan.TrailingSpaces;
					characterCountSoFar += currentCharacterCount;
				}
			}

			static void DrawDecoration(SKCanvas canvas, float x, float y, float width, float thickness, SKPaint paint)
			{
				paint.StrokeWidth = thickness;
				paint.IsStroke = true;
				canvas.DrawLine(x, y, x + width, y, paint);
				paint.IsStroke = false;
			}
		}

		// Warning: this is only tested and currently used by single-line single-run skia-based TextBoxes
		internal int GetIndexForTextBlock(Point p)
		{
			var line = GetRenderLineAt(p.Y, true)?.line;

			if (line is not { })
			{
				return 0;
			}

			var characterCount = 0;
			foreach (var currentLine in _renderLines.TakeWhile(l => l != line))
			{
				foreach (var SegmentSpan in currentLine.SegmentSpans)
				{
					characterCount += SegmentSpan.Segment.Glyphs.Count;
				}
			}

			var (span, x) = GetRenderSegmentSpanAt(p, true)!.Value;

			foreach (var currentSpan in line.SegmentSpans.TakeWhile(s => !s.Equals(span)))
			{
				characterCount += currentSpan.Segment.Glyphs.Count;
			}

			var segment = span.Segment;
			var run = (Run)segment.Inline;
			var characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

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

			return characterCount;
		}

		// Warning: this is only tested and currently used by single-line single-run skia-based TextBoxes
		internal Rect GetRectForTextBlockIndex(int index)
		{
			var characterCount = 0;
			float y = 0, x = 0;
			var parent = (IBlock)_collection.GetParent();

			for (var lineIndex = 0; lineIndex < _renderLines.Count; lineIndex++)
			{
				var line = _renderLines[lineIndex];
				(x, var justifySpaceOffset) = line.GetOffsets((float)_lastArrangedSize.Width, parent.TextAlignment);

				var spans = line.RenderOrderedSegmentSpans;
				for (var spanIndex = 0; spanIndex < spans.Count; spanIndex++)
				{
					var span = spans[spanIndex];
					var glyphCount = span.Segment.Glyphs.Count;

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
					}

					characterCount += glyphCount;
					x += span.Width;
				}

				y += line.Height;
			}

			// width and height default to 0 if there's nothing there
			return new Rect(x, y, 0, _renderLines.Count > 0 ? _renderLines[^1].Height : 0);
		}

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
	}
}
