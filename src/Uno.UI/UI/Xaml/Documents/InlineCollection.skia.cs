using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;

#nullable enable

namespace Microsoft.UI.Xaml.Documents
{
	partial class InlineCollection
	{
		private readonly List<RenderLine> _renderLines = new();

		private double _lastMeasuredWidth;
		private Size _lastDesiredSize;
		private Size _lastArrangedSize;

		/// <summary>
		/// Measures a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal Size Measure(Size availableSize)
		{
			if (availableSize.Width <= _lastMeasuredWidth && availableSize.Width >= _lastDesiredSize.Width)
			{
				return _lastDesiredSize;
			}

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
			float widestLineWidth = 0;

			float x = 0;
			float height = 0;

			foreach (var inline in _collection.SelectMany(InlineExtensions.Enumerate))
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
				_lastDesiredSize = new Size(0, 0);
			}
			else
			{
				_lastDesiredSize = new Size(widestLineWidth, height);
			}

			return _lastDesiredSize;

			// Local functions

			static float GetGlyphWidthWithSpacing(GlyphInfo glyph, float characterSpacing)
			{
				return glyph.AdvanceX > 0 ? glyph.AdvanceX + characterSpacing : glyph.AdvanceX;
			}

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
				}

				x = 0;
				height += renderLine.Height;
				previousLineWrapped = currentLineWrapped;
			}
		}

		internal Size Arrange(Size finalSize)
		{
			_lastArrangedSize = finalSize;

			if (finalSize.Width <= _lastMeasuredWidth && finalSize.Width >= _lastDesiredSize.Width)
			{
				return _lastDesiredSize;
			}

			return Measure(finalSize);
		}

		internal void InvalidateMeasure()
		{
			_lastMeasuredWidth = 0;
			_lastDesiredSize = new();
			_lastArrangedSize = new();
		}

		/// <summary>
		/// Renders a block-level inline collection, i.e. one that belongs to a TextBlock (or Paragraph, in the future).
		/// </summary>
		internal void Render(SKSurface surface, Compositor compositor)
		{
			if (_renderLines.Count == 0)
			{
				return;
			}

			var canvas = surface.Canvas;
			var parent = (IBlock)_collection.GetParent();
			var alignment = parent.TextAlignment;

			float y = 0;

			foreach (var line in _renderLines)
			{
				// TODO: (Performance) Stop rendering when the lines exceed the available height

				(float x, float justifySpaceOffset) = line.GetOffsets((float)_lastArrangedSize.Width, alignment);

				y += line.Height;
				float baselineOffsetY = line.BaselineOffsetY;

				for (int s = 0; s < line.RenderOrderedSegmentSpans.Count; s++)
				{
					var segmentSpan = line.RenderOrderedSegmentSpans[s];

					if (segmentSpan.GlyphsLength == 0)
					{
						continue;
					}

					var segment = segmentSpan.Segment;
					var paint = segment.Inline.Paint;

					if (segment.Inline.Foreground is SolidColorBrush scb)
					{
						paint.Color = new SKColor(
							red: scb.Color.R,
							green: scb.Color.G,
							blue: scb.Color.B,
							alpha: (byte)(scb.Color.A * compositor.CurrentOpacity));
					}

					var decorations = segment.Inline.TextDecorations;
					const TextDecorations allDecorations = TextDecorations.Underline | TextDecorations.Strikethrough;

					if ((decorations & allDecorations) != 0)
					{
						var metrics = paint.FontMetrics;
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
							float yPos = y + baselineOffsetY + (metrics.StrikeoutPosition ?? paint.TextSize / -2);
							DrawDecoration(canvas, x, yPos, width, metrics.StrikeoutThickness ?? 1, paint);
						}
					}

					using var textBlobBuilder = new SKTextBlobBuilder();
					var run = textBlobBuilder.AllocatePositionedRun(paint.ToFont(), segmentSpan.GlyphsLength);
					var glyphs = run.GetGlyphSpan();
					var positions = run.GetPositionSpan();

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

					using var textBlob = textBlobBuilder.Build();
					canvas.DrawText(textBlob, 0, y + baselineOffsetY, paint);

					x += justifySpaceOffset * segmentSpan.TrailingSpaces;
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

		internal RenderLine? GetRenderLineAt(double y, bool extendedSelection)
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
					return line;
				}
			} while (i < _renderLines.Count);

			return extendedSelection ? line : null;
		}

		internal RenderSegmentSpan? GetRenderSegmentSpanAt(Point point, bool extendedSelection)
		{
			var parent = (IBlock)_collection.GetParent();

			var line = GetRenderLineAt(point.Y, extendedSelection);

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
					return span;
				}

				spanX += justifySpaceOffset * span.TrailingSpaces;
			} while (i < line.RenderOrderedSegmentSpans.Count);

			return extendedSelection ? span : null;
		}
	}
}
