using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Documents
{
	partial class InlineCollection
	{
		private readonly List<RenderLine> _renderLines = new();

		private double _lastMeasuredWidth;
		private Size _lastDesiredSize;
		private Size _lastArrangedSize;

		/// <summary>
		/// Measures a block-level inline collection, i.e. one that belongs to a TextBlock or Paragraph.
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

			double lineHeight = parent.LineHeight;
			var lineStackingStrategy = lineHeight == 0 ? LineStackingStrategy.MaxHeight : parent.LineStackingStrategy;

			_renderLines.Clear();

			List<RenderSegmentSpan> lineSegmentSpans = new();
			bool previousLineWrapped = false;

			float availableWidth = wrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
			float widestLineWidth = 0;

			float x = 0;

			foreach (var inline in _collection.SelectMany(InlineExtensions.Enumerate))
			{
				if (inline is LineBreak lineBreak)
				{
					Segment breakSegment = new(lineBreak);
					RenderSegmentSpan breakSegmentSpan = new(breakSegment, 0, 0, 0, 0);
					lineSegmentSpans.Add(breakSegmentSpan);

					MoveToNextLine(currentLineWrapped: false);
				}
				else if (inline is Run run)
				{
					// TODO: rethink character spacing implementation to properly handle trailing spacing on a line
					// float characterSpacing = (float)run.FontSize * run.CharacterSpacing / 1000;

					foreach (var segment in run.Segments)
					{
						// TODO: After bidi is implemented, consider that adjacent segments may not have a word break or new line between them but may just
						// switch direction and thus must appear together without wrapping. We don't need to worry about this for now since every segment
						// ends in either a word break or line break

						// Exclude leading spaces at the start of the line only if the previous line ended because it was wrapped and not because of a line break

						int startGlyph = x == 0 && previousLineWrapped ? segment.LeadingSpaces : 0;

					BeginSegmentFitting:

						if (maxLines > 0 && _renderLines.Count == maxLines)
						{
							goto MaxLinesHit;
						}

						var gri = GetGlyphsRenderInfo(segment, startGlyph);
						float remainingWidth = availableWidth - x;

						// Check if whole segment fits

						if (gri.Width <= remainingWidth)
						{
							// Add in as many trailing spaces as possible

							float widthWithoutTrailingSpaces = gri.Width;
							int end = segment.LineBreakAfter ? segment.Glyphs.Count - 1 : segment.Glyphs.Count;

							while (startGlyph + gri.Length < end && (gri.Width + segment.Glyphs[gri.Length].AdvanceX) is var newWidth && newWidth <= remainingWidth)
							{
								gri.Width = newWidth;
								gri.Length++;
							}

							RenderSegmentSpan segmentSpan = new(segment, startGlyph, gri.Length, gri.Width, widthWithoutTrailingSpaces);
							lineSegmentSpans.Add(segmentSpan);
							x += gri.Width;

							if (segment.LineBreakAfter)
							{
								MoveToNextLine(currentLineWrapped: false);
							}
							else if (gri.Length < end)
							{
								MoveToNextLine(currentLineWrapped: true);
							}

							continue;
						}

						if (startGlyph == 0 && segment.LeadingSpaces > 0)
						{
							// Render as many leading spaces as possible

							int spaces = 0;
							float width = 0;

							if (x == 0)
							{
								// minimum 1 space if this is the start of the line

								spaces = 1;
								width = segment.Glyphs[0].AdvanceX;
							}

							while (spaces < segment.LeadingSpaces && (width + segment.Glyphs[spaces].AdvanceX) is var newWidth && newWidth < remainingWidth)
							{
								width = newWidth;
								spaces++;
							}

							if (width > 0)
							{
								RenderSegmentSpan segmentSpan = new(segment, 0, spaces, width, 0);
								lineSegmentSpans.Add(segmentSpan);
								x += width;

								startGlyph = segment.LeadingSpaces;
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

							RenderSegmentSpan segmentSpan = new(segment, startGlyph, gri.Length, gri.Width, gri.Width);
							lineSegmentSpans.Add(segmentSpan);
							x += gri.Width;

							MoveToNextLine(currentLineWrapped: !segment.LineBreakAfter);
						}
						else // wrapping == TextWrapping.Wrap
						{
							// Put as much of the segment on this line as possible then continue fitting the rest of the segment on the next line

							int length = 1;
							float width = segment.Glyphs[startGlyph].AdvanceX;

							while ((width + segment.Glyphs[startGlyph + length].AdvanceX) is var newWidth && newWidth < remainingWidth)
							{
								width = newWidth;
								length++;
							}

							RenderSegmentSpan segmentSpan = new(segment, startGlyph, length, width, width);
							lineSegmentSpans.Add(segmentSpan);
							x += width;
							startGlyph += length;

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
				double height = lineStackingStrategy switch
				{
					LineStackingStrategy.MaxHeight => lineHeight == 0 ?
						_renderLines.Sum(r => r.GetMaxStackHeight()) :
						_renderLines.Sum(r => Math.Max(r.GetMaxStackHeight(), lineHeight)),
					LineStackingStrategy.BlockLineHeight => _renderLines.Count * lineHeight,
					// BaselineToBaseline
					_ => _renderLines[0].GetMaxAboveBaselineHeight() + (_renderLines.Count - 1) * lineHeight,
				};

				_lastDesiredSize = new Size(widestLineWidth, height);
			}

			return _lastDesiredSize;

			// Local functions

			void MoveToNextLine(bool currentLineWrapped)
			{
				_renderLines.Add(new RenderLine(lineSegmentSpans));
				lineSegmentSpans.Clear();

				if (x > widestLineWidth)
				{
					widestLineWidth = x;
				}

				x = 0;
				previousLineWrapped = currentLineWrapped;
			}

			// Gets glyph rendering info for a segment, excluding any trailing spaces.

			static (int Length, float Width) GetGlyphsRenderInfo(Segment segment, int start)
			{
				var glyphs = segment.Glyphs;
				int end = segment.LineBreakAfter ? glyphs.Count - 1 : glyphs.Count;
				end -= segment.TrailingSpaces;

				float width = 0;

				for (int i = start; i < end; i++)
				{
					width += glyphs[i].AdvanceX;
				}

				return (end - start, width);
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

			float lineHeight = (float)parent.LineHeight;
			var lineStackingStrategy = lineHeight == 0 ? LineStackingStrategy.MaxHeight : parent.LineStackingStrategy;

			bool firstLine = true;

			float y = 0;

			foreach (var line in _renderLines)
			{
				// TODO: (Performance) Stop rendering when the lines exceed the available height

				float x = alignment switch
				{
					TextAlignment.Left => 0,
					TextAlignment.Center => ((float)_lastArrangedSize.Width - line.WidthWithoutTrailingSpaces) / 2,
					TextAlignment.Right => ((float)_lastArrangedSize.Width - line.Width),
					_ => 0, //Justify (not supported yet)
				};

				float baselineOffsetY; // baseline offset for the current line

				switch (lineStackingStrategy)
				{
					case LineStackingStrategy.MaxHeight:
						float maxStackHeight = (float)line.GetMaxStackHeight();

						if (lineHeight == 0)
						{
							y += maxStackHeight;
							baselineOffsetY = -(float)line.GetMaxBelowBaselineHeight();
						}
						else
						{
							if (lineHeight < maxStackHeight)
							{
								y += maxStackHeight;
								baselineOffsetY = -(float)line.GetMaxBelowBaselineHeight();

							}
							else
							{
								y += lineHeight;
								baselineOffsetY = GetBaselineOffsetY(line, lineHeight, maxStackHeight);
							}
						}

						break;

					case LineStackingStrategy.BlockLineHeight:
						y += lineHeight;
						baselineOffsetY = GetBaselineOffsetY(line, lineHeight, (float)line.GetMaxStackHeight());
						break;

					default: // LineStackingStrategy.BaselineToBaseline:
						if (firstLine)
						{
							y = (float)line.GetMaxAboveBaselineHeight();
							baselineOffsetY = 0;
						}
						else
						{
							y += lineHeight;
							baselineOffsetY = GetBaselineOffsetY(line, lineHeight, (float)line.GetMaxStackHeight());
						}

						break;
				}

				for (int s = 0; s < line.SegmentSpans.Count; s++)
				{
					var segmentSpan = line.SegmentSpans[s];

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
						float width = s == line.SegmentSpans.Count - 1 ? segmentSpan.WidthWithoutTrailingSpaces : segmentSpan.Width;

						if ((decorations & TextDecorations.Underline) != 0)
						{
							// TODO: what should default thickness/position be if metrics does not contain it?
							float yPos = y + baselineOffsetY + (metrics.UnderlinePosition ?? 0);
							DrawDecoration(canvas, x, yPos, width, metrics.UnderlineThickness ?? 1, paint);
						}

						if ((decorations & TextDecorations.Strikethrough) != 0)
						{
							// TODO: what should default thickness/position be if metrics does not contain it?
							float yPos = y + baselineOffsetY + (metrics.StrikeoutPosition ?? -paint.TextSize / 2);
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
							glyphs[i] = glyphInfo.GlyphId;
							positions[i] = new SKPoint(x + glyphInfo.OffsetX, glyphInfo.OffsetY);
							x += glyphInfo.AdvanceX;
						}
					}
					else // RightToLeft
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
								glyphs[j] = glyphInfo.GlyphId;
								positions[j] = new SKPoint(x + glyphInfo.OffsetX, glyphInfo.OffsetY);
								x += glyphInfo.AdvanceX;
							}
						}
					}

					using var textBlob = textBlobBuilder.Build();
					canvas.DrawText(textBlob, 0, y + baselineOffsetY, paint);
				}

				firstLine = false;
			}

			// Local functions:

			// Gets the offset of the baseline for a render line based on a custom line height. Scales the default baseline offset by the ratio of the default
			// line height to the custom line height.

			static float GetBaselineOffsetY(RenderLine line, float lineHeight, float maxStackHeight)
			{
				if (maxStackHeight == 0)
				{
					return 0;
				}

				var defaultBaselineOffsetY = -(float)line.GetMaxBelowBaselineHeight();
				return defaultBaselineOffsetY * lineHeight / maxStackHeight;
			}

			static void DrawDecoration(SKCanvas canvas, float x, float y, float width, float thickness, SKPaint paint)
			{
				paint.StrokeWidth = thickness;
				paint.IsStroke = true;
				canvas.DrawLine(x, y, x + width, y, paint);
				paint.IsStroke = false;
			}
		}
	}
}
