#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Text;
using HarfBuzzSharp;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Buffer = HarfBuzzSharp.Buffer;
using GlyphInfo = HarfBuzzSharp.GlyphInfo;

namespace Microsoft.UI.Xaml.Documents;

// Excerpt from https://www.unicode.org/reports/tr9/tr9-6.html:
// The Bidirectional Algorithm takes a stream of text as input, and proceeds in three main phases:
//  * Separation of the input text into paragraphs. The rest of the algorithm affects only the text between paragraph separators.
// 	* Resolution of the embedding levels of the text. In this phase, the directional character types, plus the explicit format codes, are used to produce resolved embedding levels.
// 	* Reordering the text for display on a line-by-line basis using the resolved embedding levels, once the text has been broken into lines.
// The algorithm only reorders text within a paragraph; characters in one paragraph have no effect on characters in a different paragraph. Paragraphs are divided by the Paragraph Separator or appropriate Newline Function (see Section 4.3, Directionality and Unicode Technical Report #13, “Unicode Newline Guidelines,” found on the CD-ROM or the up-to-date version on the Unicode web site on the handling of CR, LF, and CRLF). Paragraphs may also be determined by higher-level protocols: for example, the text in two different cells of a table will be in different paragraphs.

// TODO: character spacing
// TODO: what happens if text has no drawable glyphs but is not empty? Can this happen? The HarfBuzz docs imply that it can't
internal readonly partial struct UnicodeText : IParsedText
{
	// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
	private const float TabStopWidth = 48;
	private const byte UBIDI_DEFAULT_LTR = 0xfe;
	private const int UBIDI_LTR = 0;
	private const int UBIDI_RTL = 1;

	// A readonly snapshot of an Inline that is referenced by individual text runs after splitting. It's a class
	// and not a struct because we don't want to copy the same Inline for each run.
	private class ReadonlyInlineCopy
	{
		public Inline Inline { get; }
		public int StartIndex { get; }
		public int EndIndex { get; }
		public string Text { get; }
		public FlowDirection FlowDirection { get; }
		public FontDetails FontDetails { get; }
		public double FontSize { get; }
		public FontWeight FontWeight { get; }
		public FontStretch FontStretch { get; }
		public FontStyle FontStyle { get; }
		public Brush Foreground { get; }

		public ReadonlyInlineCopy(Inline inline, int startIndex, FlowDirection defaultFlowDirection, bool forceDefaultFlowDirection = false)
		{
			CI.Assert(inline is Run or LineBreak);
			Inline = inline;
			Text = inline.GetText();
			Foreground = inline.Foreground;
			FlowDirection = forceDefaultFlowDirection ? defaultFlowDirection : (inline as Run)?.FlowDirection ?? defaultFlowDirection;
			FontDetails = inline.FontInfo;
			FontSize = inline.FontSize;
			FontWeight = inline.FontWeight;
			FontStretch = inline.FontStretch;
			FontStyle = inline.FontStyle;
			StartIndex = startIndex;
			EndIndex = startIndex + Text.Length;
		}
	}

	// The same as HarfBuzz's GlyphPosition but with a float XAdvance instead of int
	private readonly record struct UnoGlyphPosition(GlyphPosition GlyphPosition, float XAdvance);

	// A BidiRun run split at line break boundaries
	private readonly record struct BidiRun(ReadonlyInlineCopy inline, int startInInline, int endInInline, bool rtl, FontDetails fontDetails);

	// FontDetails might be different from inline.FontDetails because of font fallback
	// glyphs are always ordered LTR even in RTL text
	private readonly record struct ShapedLineBrokenBidiRun(ReadonlyInlineCopy Inline, int startInInline, int endInInline, (GlyphInfo info, UnoGlyphPosition position)[] glyphs, FontDetails fontDetails, bool rtl);
	private record LayoutedGlyphDetails(GlyphInfo info, UnoGlyphPosition position, float xPosInRun, Cluster? cluster, LayoutedLineBrokenBidiRun parentRun)
	{
		public Cluster? cluster { get; set; } = cluster;
	}

	private record LayoutedLineBrokenBidiRun(ReadonlyInlineCopy inline, int startInInline, int endInInline, float xPosInLine, float width, LayoutedGlyphDetails[] glyphs, FontDetails fontDetails, bool rtl, LayoutedLine line, int indexInLine)
	{
		public float width { get; set; } = width;
		public LayoutedLine line { get; set; } = line;
	}
	// runs are always sorted LTR even in RTL text
	// Each line must have at least one run except the very last line.
	private record LayoutedLine(float lineHeight, float baselineOffset, int lineIndex, float xAlignmentOffset, float y, int startInText, int endInText, List<LayoutedLineBrokenBidiRun> runs);
	private record Cluster(int sourceTextStart, int sourceTextEnd, LayoutedLineBrokenBidiRun layoutedRun, int glyphInRunIndexStart, int glyphInRunIndexEnd);

	private static readonly SKPaint _spareDrawPaint = new();

	private readonly Size _size;
	private readonly TextAlignment _textAlignment;
	private readonly bool _rtl;
	private readonly List<ReadonlyInlineCopy> _inlines;
	private readonly List<LayoutedLine> _lines;
	private readonly Cluster[] _textIndexToGlyph;
	private readonly Size _desiredSize;
	private readonly string _text;
	private readonly List<int> _wordBoundaries;
	private readonly FontDetails _defaultFontDetails;

	bool IParsedText.IsBaseDirectionRightToLeft => _rtl;

	internal interface IFontCacheUpdateListener
	{
		void Invalidate();
	}

	internal UnicodeText(
		Size availableSize,
		Inline[] inlines, // only leaf nodes
		FontDetails defaultFontDetails, // only used for a final empty line, otherwise the FontDetails are read from the inline
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		FlowDirection flowDirection,
		TextAlignment? textAlignment, // null to determine from text. This will also infer the directionality of the text from the content
		TextWrapping textWrapping,
		IFontCacheUpdateListener fontListener,
		out Size desiredSize)
	{
		CI.Assert(maxLines >= 0);
		_size = availableSize;
		_defaultFontDetails = defaultFontDetails;

		string text;
		if (textAlignment is null)
		{
			// TODO: can we make this cleaner instead of implicitly assuming that this is a code path coming from TextBox?
			CI.Assert(inlines.Length == 1);
			var inline = (Run)inlines[0];
			var inlineText = inline.GetText();
			if (inlineText.Length == 0)
			{
				flowDirection = inline.FlowDirection;
			}
			else
			{
				var firstInlineText = inlines[0].GetText();
				using var _1 = ICU.CreateBiDiAndSetPara(firstInlineText, 0, firstInlineText.Length, UBIDI_DEFAULT_LTR, out var bidi);
				ICU.GetMethod<ICU.ubidi_getLogicalRun>()(bidi, 0, out _, out var level);
				CI.Assert(level is UBIDI_LTR or UBIDI_RTL);
				flowDirection = level is UBIDI_RTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
			}
			textAlignment = flowDirection is FlowDirection.LeftToRight ? TextAlignment.Left : TextAlignment.Right;
			var copy = new ReadonlyInlineCopy(inline, 0, flowDirection, true);
			var length = copy.Text.Length;
			_inlines = length == 0 ? [] : [copy];
			text = copy.Text;
		}
		else
		{
			_inlines = new();
			var lastEnd = 0;
			var builder = new StringBuilder();
			foreach (var inline in inlines)
			{
				var copy = new ReadonlyInlineCopy(inline, lastEnd, flowDirection);
				var length = copy.Text.Length;
				if (length != 0)
				{
					_inlines.Add(copy);
				}
				lastEnd = copy.EndIndex;
				builder.Append(copy.Text);
			}
			text = builder.ToString();
		}

		_rtl = flowDirection == FlowDirection.RightToLeft;

		if (_inlines.Count == 0)
		{
			_lines = new();
			desiredSize = new Size(0, GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, true, true).lineHeight);
			_textIndexToGlyph = [];
			_inlines = [];
			_wordBoundaries = new();
			_textAlignment = textAlignment.Value;
			_text = "";
			return;
		}

		var lineWidth = textWrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
		var unlayoutedLines = SplitTextIntoLines(_rtl, _inlines, lineWidth, textWrapping, fontListener);
		var lines = LayoutLines(unlayoutedLines, textAlignment.Value, lineStackingStrategy, lineHeight, (float)availableSize.Width, defaultFontDetails);

		_lines = maxLines == 0 || maxLines >= lines.Count ? lines : lines[..maxLines];
		_text = text = text[.._lines[^1].endInText];

		_textIndexToGlyph = new Cluster[_text.Length];
		CreateSourceTextFromAndToGlyphMapping(_lines, _textIndexToGlyph);

		_wordBoundaries = GetWordBreakingOpportunities(text);

		var desiredHeight = _lines.Sum(l => l.lineHeight);
		var desiredWidth = _lines.Max(l => l.runs.Sum(r => r.width));
		_desiredSize = desiredSize = new Size(desiredWidth, desiredHeight);
	}

	/// <returns>The runs of each run are sorted according to the visual order.</returns>
	private static List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)> SplitTextIntoLines(bool rtl, List<ReadonlyInlineCopy> inlines, float lineWidth, TextWrapping textWrapping, IFontCacheUpdateListener fontListener)
	{
		var logicallyOrderedRuns = new List<BidiRun>();
		var logicallyOrderedLineBreakingOpportunities = new List<(int indexInInline, ReadonlyInlineCopy inline)>();
		foreach (var inline in inlines)
		{
			logicallyOrderedRuns.AddRange(SplitTextIntoLogicallyOrderedBidiRuns(inline, fontListener));
			logicallyOrderedLineBreakingOpportunities.AddRange(GetLineBreakingOpportunities(inline.Text).Select(b => (b, inline)));
		}

		var linesWithLogicallyOrderedRuns = ApplyLineBreaking(lineWidth, logicallyOrderedRuns, logicallyOrderedLineBreakingOpportunities, rtl, textWrapping);
		var linesWithBidiReordering = ApplyBidiReordering(rtl, linesWithLogicallyOrderedRuns, fontListener);

		return linesWithBidiReordering;
	}

	private static List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)> ApplyBidiReordering(bool rtl, List<List<BidiRun>> linesWithLogicallyOrderedRuns, IFontCacheUpdateListener fontListener)
	{
		var shapedLines = new List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)>();
		for (var lineIndex = 0; lineIndex < linesWithLogicallyOrderedRuns.Count; lineIndex++)
		{
			var line = linesWithLogicallyOrderedRuns[lineIndex];
			if (line.Count == 0)
			{
				// Only the last line can be empty, otherwise it will have at least one piece of text or a line break.
				CI.Assert(lineIndex == linesWithLogicallyOrderedRuns.Count - 1);
				if (lineIndex == 0)
				{
					shapedLines.Add((new(), 0, 0));
				}
				else
				{
					shapedLines.Add((new(), shapedLines[^1].endInText, shapedLines[^1].endInText));
				}
				continue;
			}

			var lineRuns = new Deque<ShapedLineBrokenBidiRun>();

			foreach (var (inline, startInInline, endInInline) in GroupByInline(line))
			{
				var sameInlineRuns = new List<ShapedLineBrokenBidiRun>();

				using var _ = ICU.CreateBiDiAndSetPara(inline.Text, startInInline, endInInline, (byte)(inline.FlowDirection is FlowDirection.RightToLeft ? 1 : 0), out var bidi);

				var runCount = ICU.GetMethod<ICU.ubidi_countRuns>()(bidi, out var countRunsErrorCode);
				ICU.CheckErrorCode<ICU.ubidi_countRuns>(countRunsErrorCode);

				for (var runIndex = 0; runIndex < runCount; runIndex++)
				{
					var level = ICU.GetMethod<ICU.ubidi_getVisualRun>()(bidi, runIndex, out var logicalStart, out var length);
					CI.Assert(level is UBIDI_LTR or UBIDI_RTL);

					var sameInlineRunsLengthBeforeFontSplitting = sameInlineRuns.Count;
					var currentFontDetails = inline.FontDetails;
					var currentFontSplitStart = logicalStart;
					for (var i = logicalStart; i < logicalStart + length; i += char.IsSurrogate(inline.Text, startInInline + i) ? 2 : 1)
					{
						FontDetails newFontDetails;
						if (char.ConvertToUtf32(inline.Text, startInInline + i) is var codepoint && !inline.FontDetails.SKFont.ContainsGlyph(codepoint))
						{
							newFontDetails = GetFallbackFont(codepoint, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle, fontListener) ?? inline.FontDetails;
						}
						else
						{
							newFontDetails = inline.FontDetails;
						}

						if (newFontDetails != currentFontDetails)
						{
							if (i != logicalStart)
							{
								// the currentLineWidth parameter of ShapeRun is null and the tab width will be adjusted later in LayoutLines.
								sameInlineRuns.Add(new ShapedLineBrokenBidiRun(inline, startInInline + currentFontSplitStart, startInInline + i, ShapeRun(inline.Text[(startInInline + currentFontSplitStart)..(startInInline + i)], level is UBIDI_RTL, currentFontDetails, null, ignoreTrailingSpaces: false), currentFontDetails, level is UBIDI_RTL));
							}
							currentFontDetails = newFontDetails;
							currentFontSplitStart = i;
						}
					}
					// the currentLineWidth parameter of ShapeRun is null and the tab width will be adjusted later in LayoutLines.
					sameInlineRuns.Add(new ShapedLineBrokenBidiRun(inline, startInInline + currentFontSplitStart, startInInline + logicalStart + length, ShapeRun(inline.Text[(startInInline + currentFontSplitStart)..(startInInline + logicalStart + length)], level is UBIDI_RTL, currentFontDetails, null, ignoreTrailingSpaces: false), currentFontDetails, level is UBIDI_RTL));
					// swap runs if rtl since we always process characters in a single bidi run in logical order
					if (level is UBIDI_RTL)
					{
						for (var i = 0; i < (sameInlineRuns.Count - sameInlineRunsLengthBeforeFontSplitting) / 2; i++)
						{
							(sameInlineRuns[sameInlineRunsLengthBeforeFontSplitting + i], sameInlineRuns[^(1 + i)]) = (sameInlineRuns[^(1 + i)], sameInlineRuns[sameInlineRunsLengthBeforeFontSplitting + i]);
						}
					}
				}
				if (rtl)
				{
					for (int i = sameInlineRuns.Count - 1; i >= 0; i--)
					{
						lineRuns.AddToFront(sameInlineRuns[i]);
					}
				}
				else
				{
					for (int i = 0; i < sameInlineRuns.Count; i++)
					{
						lineRuns.AddToBack(sameInlineRuns[i]);
					}
				}
			}
			shapedLines.Add((lineRuns.ToList(), line[0].startInInline + line[0].inline.StartIndex, line[^1].endInInline + line[^1].inline.StartIndex));
		}

		return shapedLines;
	}

	private static FontDetails? GetFallbackFont(int codepoint, float fontSize, FontWeight fontWeight, FontStretch fontStretch, FontStyle fontStyle, IFontCacheUpdateListener fontListener)
	{
		var symbolsFontTask = FontDetailsCache.GetFont(FeatureConfiguration.Font.SymbolsFont, fontSize, fontWeight, fontStretch, fontStyle);
		if (symbolsFontTask.loadedTask.IsCompleted)
		{
			if (symbolsFontTask.loadedTask.IsCompletedSuccessfully
				&& symbolsFontTask.loadedTask.Result is { } symbolsFont
				&& symbolsFont.SKFont.ContainsGlyph(codepoint))
			{
				return symbolsFont;
			}
		}
		else
		{
			symbolsFontTask.loadedTask.ContinueWith(_ => NativeDispatcher.Main.Enqueue(fontListener.Invalidate));
		}

		var fallbackFontTask = FontDetailsCache.GetFontForCodepoint(codepoint, fontSize, fontWeight, fontStretch, fontStyle);
		if (fallbackFontTask.IsCompleted)
		{
			if (fallbackFontTask.IsCompletedSuccessfully
				&& fallbackFontTask.Result is { } fallbackFont)
			{
				return fallbackFont;
			}
		}
		else
		{
			fallbackFontTask.ContinueWith(_ => NativeDispatcher.Main.Enqueue(fontListener.Invalidate));
		}

		if (SKFontManager.Default.MatchCharacter(codepoint) is { } typeface)
		{
			return FontDetails.Create(typeface, fontSize);
		}

		return null;
	}

	private static IEnumerable<(ReadonlyInlineCopy Inline, int startInInline, int endInInline)> GroupByInline(List<BidiRun> line)
	{
		foreach (var group in line.GroupBy(r => r.inline))
		{
			var inline = group.Key;
			var groupAsArray = group.ToArray();
			var startInInline = groupAsArray[0].startInInline;
			var endInInline = groupAsArray[^1].endInInline;
			var i = startInInline;
			while (inline.Text.IndexOf('\t', i) is var tabIndex && tabIndex != -1 && tabIndex < endInInline)
			{
				if (i != tabIndex)
				{
					yield return (inline, i, tabIndex);
				}
				yield return (inline, tabIndex, tabIndex + 1);
				i = tabIndex + 1;
				if (i == endInInline)
				{
					break;
				}
			}

			if (i != endInInline)
			{
				yield return (inline, i, endInInline);
			}
		}
	}

	private static float MeasureContiguousRunSequence(float currentLineWidth, List<BidiRun> logicallyOrderedRuns, int firstRunIndex, int startingIndexInFirstRun, int endRunIndex, int endIndexInLastRun, bool rtl)
	{
		var backup1 = logicallyOrderedRuns[firstRunIndex];
		var backup2 = logicallyOrderedRuns[endRunIndex - 1];
		logicallyOrderedRuns[firstRunIndex] = logicallyOrderedRuns[firstRunIndex] with { startInInline = startingIndexInFirstRun };
		logicallyOrderedRuns[endRunIndex - 1] = logicallyOrderedRuns[endRunIndex - 1] with { endInInline = endIndexInLastRun };

		float usedWidth = currentLineWidth;
		for (var index = endRunIndex - 2; index >= firstRunIndex; index--)
		{
			var run = logicallyOrderedRuns[index];
			var start_ = index == firstRunIndex ? startingIndexInFirstRun : run.startInInline;
			var glyphs = ShapeRun(run.inline.Text[start_..run.endInInline], run.rtl, run.fontDetails, usedWidth, ignoreTrailingSpaces: false);
			usedWidth += RunWidth(glyphs, run.inline.FontDetails);
		}

		var lastRun = logicallyOrderedRuns[endRunIndex - 1];
		var start = firstRunIndex == endRunIndex - 1 ? startingIndexInFirstRun : lastRun.startInInline;
		var glyphsWithoutTrailingSpaces = ShapeRun(lastRun.inline.Text[start..endIndexInLastRun], lastRun.rtl, lastRun.fontDetails, usedWidth, ignoreTrailingSpaces: true);

		logicallyOrderedRuns[firstRunIndex] = backup1;
		logicallyOrderedRuns[endRunIndex - 1] = backup2;

		return usedWidth - currentLineWidth + RunWidth(glyphsWithoutTrailingSpaces, lastRun.inline.FontDetails);
	}

	private static IEnumerable<(int endRunIndex, int endIndexInLastRun)> SplitByLineBreakingOpportunities(List<BidiRun> logicallyOrderedRuns, List<(int indexInInline, ReadonlyInlineCopy inline)> logicallyOrderedLineBreakingOpportunities)
	{
		var current = 0;
		foreach (var lineBreakingOpportunity in logicallyOrderedLineBreakingOpportunities)
		{
			while (logicallyOrderedRuns[current].inline != lineBreakingOpportunity.inline || logicallyOrderedRuns[current].endInInline < lineBreakingOpportunity.indexInInline)
			{
				current++;
			}
			yield return (current + 1, lineBreakingOpportunity.indexInInline);
			if (logicallyOrderedRuns[current].endInInline == lineBreakingOpportunity.indexInInline)
			{
				current++;
			}
		}
	}

	// This is completely outside the Unicode standard. We're in a situation where a sequence of characters without
	// any line breaking opportunities doesn't fit in a line so we need to put the part that fits on the line and
	// the rest on the next line.
	private static (int endRunIndex, int endIndexInLastRun) SplitRunSequenceForWrapping(float lineWidth, bool rtl, List<BidiRun> logicallyOrderedRuns, int firstRunIndex, int startingIndexInFirstRun, int endRunIndex, int endIndexInLastRun)
	{
		for (int testEndRunIndex = endRunIndex; testEndRunIndex > firstRunIndex; testEndRunIndex--)
		{
			for (int testEndIndexInLastRun = testEndRunIndex == endRunIndex ? endIndexInLastRun - 1 : logicallyOrderedRuns[testEndRunIndex - 1].endInInline;
				 testEndIndexInLastRun > (testEndRunIndex - 1 == firstRunIndex ? startingIndexInFirstRun : logicallyOrderedRuns[testEndRunIndex - 1].startInInline);
				 testEndIndexInLastRun--)
			{
				if (char.IsHighSurrogate(logicallyOrderedRuns[testEndRunIndex - 1].inline.Text[testEndIndexInLastRun - 1]))
				{
					continue;
				}
				var widthWithoutTrailingSpaces = MeasureContiguousRunSequence(lineWidth, logicallyOrderedRuns, firstRunIndex, startingIndexInFirstRun, testEndRunIndex, testEndIndexInLastRun, rtl);
				if (widthWithoutTrailingSpaces <= lineWidth)
				{
					return (testEndRunIndex, testEndIndexInLastRun);
				}
			}
		}

		// nothing fits, default to having a single character in the first run
		return (firstRunIndex + 1, startingIndexInFirstRun + (char.IsSurrogate(logicallyOrderedRuns[firstRunIndex].inline.Text[startingIndexInFirstRun]) ? 2 : 1));
	}

	private static List<List<BidiRun>> ApplyLineBreaking(float lineWidth, List<BidiRun> logicallyOrderedRuns,
		List<(int indexInInline, ReadonlyInlineCopy inline)> logicallyOrderedLineBreakingOpportunities, bool rtl,
		TextWrapping textWrapping)
	{
		var lines = new List<List<BidiRun>>();

		(int firstRunIndex, int startingIndexInFirstRun) = (0, 0);
		(int endRunIndex, int endIndexInLastRun)? prevInSameLine = null;

		var lineBreakingOpporunities = SplitByLineBreakingOpportunities(logicallyOrderedRuns, logicallyOrderedLineBreakingOpportunities).ToList();
		for (var lineBreakingOpportunityIndex = 0; lineBreakingOpportunityIndex < lineBreakingOpporunities.Count; lineBreakingOpportunityIndex++)
		{
			var lineBreakingOpporunity = lineBreakingOpporunities[lineBreakingOpportunityIndex];

			var widthWithoutTrailingSpaces = MeasureContiguousRunSequence(0, logicallyOrderedRuns, firstRunIndex, startingIndexInFirstRun, lineBreakingOpporunity.endRunIndex, lineBreakingOpporunity.endIndexInLastRun, rtl);

			if (widthWithoutTrailingSpaces > lineWidth)
			{
				// If there was no text wrapping, then everything would fit on the same line
				CI.Assert(textWrapping is TextWrapping.Wrap or TextWrapping.WrapWholeWords);

				if (prevInSameLine is not null)
				{
					// after including the current run sequence on the line, things won't fit anymore
					// so it moves to the next line regardless of whether we're in Wrap or WrapWholeWords
					var line = new List<BidiRun>();
					line.AddRange(logicallyOrderedRuns.Slice(firstRunIndex, prevInSameLine.Value.endRunIndex - firstRunIndex));
					line[0] = line[0] with { startInInline = startingIndexInFirstRun };
					line[^1] = line[^1] with { endInInline = prevInSameLine.Value.endIndexInLastRun };
					lines.Add(line);
					(firstRunIndex, startingIndexInFirstRun) = (prevInSameLine.Value.endRunIndex - 1, prevInSameLine.Value.endIndexInLastRun);
					prevInSameLine = null;
					lineBreakingOpportunityIndex--; // retry the line breaking opportunity
				}
				else
				{
					// only one "non-line-breakable" sequence is on this line but it still won't fit
					if (textWrapping is TextWrapping.WrapWholeWords || logicallyOrderedRuns[firstRunIndex].inline.Text[startingIndexInFirstRun] == ' ')
					{
						// WrapWholeWords or nothing but spaces: move to the next line
						var line = new List<BidiRun>();
						line.AddRange(logicallyOrderedRuns.Slice(firstRunIndex, lineBreakingOpporunity.endRunIndex - firstRunIndex));
						line[0] = line[0] with { startInInline = startingIndexInFirstRun };
						line[^1] = line[^1] with { endInInline = lineBreakingOpporunity.endIndexInLastRun };
						lines.Add(line);
						(firstRunIndex, startingIndexInFirstRun) = (lineBreakingOpporunity.endRunIndex - 1, lineBreakingOpporunity.endIndexInLastRun);
					}
					else // Wrap
					{
						(int endRunIndex, int endIndexInLastRun) = SplitRunSequenceForWrapping(lineWidth, rtl, logicallyOrderedRuns, firstRunIndex, startingIndexInFirstRun, lineBreakingOpporunity.endRunIndex, lineBreakingOpporunity.endIndexInLastRun);
						var line = new List<BidiRun>();
						line.AddRange(logicallyOrderedRuns.Slice(firstRunIndex, endRunIndex - firstRunIndex));
						line[0] = line[0] with { startInInline = startingIndexInFirstRun };
						line[^1] = line[^1] with { endInInline = endIndexInLastRun };
						lines.Add(line);
						(firstRunIndex, startingIndexInFirstRun) = (endRunIndex - 1, endIndexInLastRun);
						if (endRunIndex != lineBreakingOpporunity.endRunIndex || endIndexInLastRun != lineBreakingOpporunity.endIndexInLastRun)
						{
							lineBreakingOpportunityIndex--; // retry the line breaking opportunity
						} // else we took the whole thing and couldn't split the sequence any further
					}
				}
			}
			else if (IsLineBreak(logicallyOrderedRuns[lineBreakingOpporunity.endRunIndex - 1].inline.Text, lineBreakingOpporunity.endIndexInLastRun))
			{
				var line = new List<BidiRun>();
				line.AddRange(logicallyOrderedRuns.Slice(firstRunIndex, lineBreakingOpporunity.endRunIndex - firstRunIndex));
				line[0] = line[0] with { startInInline = startingIndexInFirstRun };
				line[^1] = line[^1] with { endInInline = lineBreakingOpporunity.endIndexInLastRun };
				lines.Add(line);
				(firstRunIndex, startingIndexInFirstRun) = (lineBreakingOpporunity.endRunIndex - 1, lineBreakingOpporunity.endIndexInLastRun);
				prevInSameLine = null;
			}
			else
			{
				prevInSameLine = lineBreakingOpporunity;
			}

			if (firstRunIndex < logicallyOrderedRuns.Count - 1 && logicallyOrderedRuns[firstRunIndex].endInInline == startingIndexInFirstRun)
			{
				// The current line starts at the end of a run, so it actually means that it starts at the beginning of
				// the next run
				firstRunIndex++;
				startingIndexInFirstRun = logicallyOrderedRuns[firstRunIndex].startInInline;
			}
		}

		if (prevInSameLine is not null)
		{
			var line = new List<BidiRun>();
			line.AddRange(logicallyOrderedRuns.Slice(firstRunIndex, prevInSameLine.Value.endRunIndex - firstRunIndex));
			line[0] = line[0] with { startInInline = startingIndexInFirstRun };
			line[^1] = line[^1] with { endInInline = prevInSameLine.Value.endIndexInLastRun };
			(firstRunIndex, startingIndexInFirstRun) = (prevInSameLine.Value.endRunIndex - 1, prevInSameLine.Value.endIndexInLastRun);
			lines.Add(line);
		}

		if (IsLineBreak(logicallyOrderedRuns[firstRunIndex].inline.Text, startingIndexInFirstRun))
		{
			var line = new List<BidiRun>();
			lines.Add(line);
		}

#if DEBUG
		CI.Assert(lines.Count > 0);
		// all the lines have content except possibly the last line, because we only move to a new line when we hit a line break
		foreach (var line in lines)
		{
			CI.Assert(line.Count > 0 || line == lines[^1]);
		}
#endif

		return lines;
	}

	private static unsafe IEnumerable<int> GetLineBreakingOpportunities(string text)
	{
		var ret = new List<int>();
		if (OperatingSystem.IsBrowser())
		{
			var breaker = ICU.BrowserICUSymbols.init_line_breaker(text);
			if (breaker == IntPtr.Zero)
			{
				typeof(UnicodeText).LogError()?.Error($"Failed to create a break iterator for input text '{text}' of unicode codepoints [{string.Join(',', text.EnumerateRunes().Select(r => r.Value))}]. Falling back to naive space-based line breaking.");
				var lines = new List<int>();
				for (int i = 0; i < text.Length;)
				{
					var inc = text.Length > i + 1 && text[i] == '\r' && text[i + 1] == '\n' ? 2 : 1;
					if (char.IsWhiteSpace(text[i]))
					{
						lines.Add(i + inc);
					}

					i += inc;
				}

				if (lines.Count == 0 || lines[^1] != text.Length)
				{
					lines.Add(text.Length);
				}
				return lines;
			}

			for (var next = ICU.BrowserICUSymbols.next_line_breaking_opportunity(breaker); next != -1; next = ICU.BrowserICUSymbols.next_line_breaking_opportunity(breaker))
			{
				ret.Add(next);
			}
		}
		else
		{
			fixed (char* locale = &CultureInfo.CurrentUICulture.Name.GetPinnableReference())
			{
				fixed (char* textPtr = &text.GetPinnableReference())
				{
					var breakIterator = ICU.GetMethod<ICU.ubrk_open>()(/* Line */ 2, (IntPtr)locale, (IntPtr)textPtr, text.Length, out int status);
					ICU.CheckErrorCode<ICU.ubrk_open>(status);
					ICU.GetMethod<ICU.ubrk_first>()(breakIterator);
					while (ICU.GetMethod<ICU.ubrk_next>()(breakIterator) is var next && next != /* UBRK_DONE */ -1)
					{
						ret.Add(next);
					}
					ICU.GetMethod<ICU.ubrk_close>()(breakIterator);
				}
			}
		}
		return ret;
	}

	private static List<int> GetWordBreakingOpportunities(string text)
	{
		// libICU's BreakIterator does not return what we want here. It only returns what it considers proper words
		// like sequences of latin characters, but e.g. a sequence of symbols is ignored.
		var chunks = new List<int>();
		{
			// a chunk is possible (continuous letters/numbers or continuous non-letters/non-numbers) then possible spaces.
			// \r\n, \r, \n and \t are always their own chunks
			var length = text.Length;
			for (var i = 0; i < length;)
			{
				var start = i;
				var c = text[i];
				if (c is '\r' && i < (length - 1) && text[i + 1] == '\n')
				{
					i += 2;
				}
				else if (c is '\r' or '\t' or '\n')
				{
					i++;
				}
				else if (c == ' ')
				{
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}
				else if (char.IsLetterOrDigit(text[i]))
				{
					while (i < length && char.IsLetterOrDigit(text[i]))
					{
						i++;
					}
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}
				else
				{
					while (i < length && !char.IsLetterOrDigit(text[i]) && text[i] != ' ' && text[i] != '\r')
					{
						i++;
					}
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}

				chunks.Add(i);
			}
		}
		return chunks;
	}

	private static List<BidiRun> SplitTextIntoLogicallyOrderedBidiRuns(ReadonlyInlineCopy inline, IFontCacheUpdateListener fontListener)
	{
		using var _ = ICU.CreateBiDiAndSetPara(inline.Text, 0, inline.Text.Length, (byte)(inline.FlowDirection == FlowDirection.LeftToRight ? UBIDI_LTR : UBIDI_RTL), out var bidi);

		var runCount = ICU.GetMethod<ICU.ubidi_countRuns>()(bidi, out int countRunsErrorCode);
		ICU.CheckErrorCode<ICU.ubidi_countRuns>(countRunsErrorCode);

		var logicallyOrderedRuns = new List<BidiRun>(runCount);
		for (var runIndex = 0; runIndex < runCount; runIndex++)
		{
			// using bidi.GetLogicalRun instead returned weird results especially in rtl text.
			var level = ICU.GetMethod<ICU.ubidi_getVisualRun>()(bidi, runIndex, out var logicalStart, out var length);
			CI.Assert(level is UBIDI_LTR or UBIDI_RTL);

			var currentFontDetails = inline.FontDetails;
			var currentFontSplitStart = logicalStart;
			for (var i = logicalStart; i < logicalStart + length; i += char.IsSurrogate(inline.Text, i) ? 2 : 1)
			{
				FontDetails newFontDetails;
				if (char.ConvertToUtf32(inline.Text, i) is var codepoint && !inline.FontDetails.SKFont.ContainsGlyph(codepoint))
				{
					newFontDetails = GetFallbackFont(codepoint, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle, fontListener) ?? inline.FontDetails;
				}
				else
				{
					newFontDetails = inline.FontDetails;
				}

				var isTab = inline.Text[i] is '\t';
				if (newFontDetails != currentFontDetails || isTab)
				{
					if (currentFontSplitStart != i)
					{
						logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, i, level is UBIDI_RTL, currentFontDetails));
					}
					currentFontDetails = newFontDetails;
					currentFontSplitStart = i;
					if (isTab)
					{
						logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, i + 1, level is UBIDI_RTL, currentFontDetails));
						currentFontSplitStart = i + 1;
					}
				}
			}

			if (currentFontSplitStart != logicalStart + length)
			{
				logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, logicalStart + length, level is UBIDI_RTL, currentFontDetails));
			}
		}

		logicallyOrderedRuns.Sort((run, bidiRun) => run.startInInline - bidiRun.startInInline);

#if DEBUG
		CI.Assert(logicallyOrderedRuns[0].startInInline == 0);
		for (var i = 0; i < runCount - 1; i++)
		{
			CI.Assert(logicallyOrderedRuns[i].endInInline != logicallyOrderedRuns[i].startInInline && logicallyOrderedRuns[i].endInInline == logicallyOrderedRuns[i + 1].startInInline);
		}
		CI.Assert(logicallyOrderedRuns[^1].endInInline != logicallyOrderedRuns[^1].startInInline && logicallyOrderedRuns[^1].endInInline == inline.Text.Length);
#endif

		return logicallyOrderedRuns;
	}

	/// <param name="currentLineWidth">Only used for tab stop width calculation. Null to ignore this case.</param>
	private static (GlyphInfo info, UnoGlyphPosition position)[] ShapeRun(string textRun, bool rtl, FontDetails fontDetails, float? currentLineWidth, bool ignoreTrailingSpaces)
	{
		if (ignoreTrailingSpaces)
		{
			textRun = textRun[..^TrailingSpaceCount(textRun, 0, textRun.Length)];
		}

		CI.Assert(textRun.Length < 2 || !textRun[..^2].Contains("\r\n"));
		using var buffer = new Buffer();
		buffer.AddUtf16(textRun, 0, textRun is [.., '\r', '\n'] ? textRun.Length - 1 : textRun.Length);
		buffer.GuessSegmentProperties();
		buffer.Direction = rtl ? Direction.RightToLeft : Direction.LeftToRight;
		// TODO: ligatures
		fontDetails.Font.Shape(buffer, new Feature(new Tag('l', 'i', 'g', 'a'), 0));
		var positions = buffer.GetGlyphPositionSpan();
		var infos = buffer.GetGlyphInfoSpan();
		var count = buffer.Length;
		var ret = new (GlyphInfo, UnoGlyphPosition)[count];
		for (var i = 0; i < count; i++)
		{
			ret[i] = (infos[i], new UnoGlyphPosition(positions[i], positions[i].XAdvance));
		}

		// Fonts will give a width > 0 to \r, so we hardcode the width here.
		// TODO: make this cleaner somehow
		var isTab = textRun is "\t";
		if ((isTab || IsLineBreak(textRun, textRun.Length)) && infos[^1].Cluster == textRun.Length - (textRun is [.., '\r', '\n'] ? 2 : 1))
		{
			fontDetails.Font.TryGetGlyph(' ', out var spaceCodepoint);
			var tabWidth = TabStopWidth / fontDetails.TextScale.textScaleX;
			// 1 and not 0 to avoid issues related to newlines having zero width. This way, it's practically 0 but not == 0.
			var xAdvance = isTab ? tabWidth - (currentLineWidth / fontDetails.TextScale.textScaleX ?? 1) % tabWidth : 1;
			ret[^1] = (infos[^1] with { Codepoint = spaceCodepoint }, new UnoGlyphPosition(positions[^1] with { XAdvance = (int)xAdvance }, xAdvance));
		}

		if (ret is [])
		{
			// Even though textRun is nonempty and fontDetails contains a font that can shape all the characters in it,
			// Font.Shape may still decide to yield 0 glyphs.
			fontDetails.Font.TryGetGlyph(' ', out var spaceCodepoint);
			ret = [(new GlyphInfo { Codepoint = spaceCodepoint }, new UnoGlyphPosition(new GlyphPosition(), 0))];
		}
		return ret;
	}

	private List<LayoutedLine> LayoutLines(List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)> lines, TextAlignment textAlignment, LineStackingStrategy lineStackingStrategy, float lineHeight, float availableWidth, FontDetails defaultFontDetails)
	{
		var layoutedLines = new List<LayoutedLine>();
		float currentLineY = 0;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var lineRuns = lines[lineIndex].runs;
			var layoutedRuns = new List<LayoutedLineBrokenBidiRun>(lineRuns.Count);
			LayoutedLine layoutedLine;
			if (lineRuns.Count == 0)
			{
				// Only the last line can be empty. All other lines either have a line break character or actual content since you can't wrap to a new line without having any content on the initial line.
				CI.Assert(lineIndex == lines.Count - 1 && lineIndex != 0);
				var (currentLineHeight, baselineOffset) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, defaultFontDetails, false, true);
				layoutedLine = new LayoutedLine(currentLineHeight, baselineOffset, lineIndex, 0, currentLineY, line.startInText, line.endInText, new());
			}
			else
			{
				float currentLineX = 0;
				for (var runIndex = 0; runIndex < lineRuns.Count; runIndex++)
				{
					var run = lineRuns[runIndex];
					float runX = 0;
					var glyphs = new LayoutedGlyphDetails[run.glyphs.Length];
					var layoutedRun = new LayoutedLineBrokenBidiRun(run.Inline, run.startInInline, run.endInInline, currentLineX, default, glyphs, run.fontDetails, run.rtl, null!, runIndex);
					layoutedRuns.Add(layoutedRun);
					for (var i = 0; i < glyphs.Length; i++)
					{
						var glyph = run.glyphs[i];
						glyphs[i] = new LayoutedGlyphDetails(glyph.info, glyph.position, runX, default, layoutedRun);
						runX += GlyphWidth(glyph.position, run.fontDetails);
					}

					if (run.Inline.Text[run.startInInline] is '\t')
					{
						runX = TabStopWidth - currentLineX % TabStopWidth;
					}

					layoutedRun.width = runX;
					currentLineX += runX;
				}

				var lineWidth = currentLineX;
				var alignmentOffset = textAlignment switch
				{
					TextAlignment.Center when lineWidth <= availableWidth => (availableWidth - lineWidth) / 2,
					TextAlignment.Right when lineWidth <= availableWidth => availableWidth - lineWidth,
					_ => 0
				};

				var fontDetailsWithMaxHeight = lineRuns.MaxBy(r => r.fontDetails.LineHeight).fontDetails;
				var (currentLineHeight, baselineOffset) = GetLineHeightAndBaselineOffset(lineStackingStrategy, lineHeight, fontDetailsWithMaxHeight, lineIndex == 0, lineIndex == lines.Count - 1);
				layoutedLine = new LayoutedLine(currentLineHeight, baselineOffset, lineIndex, alignmentOffset, currentLineY, line.startInText, line.endInText, layoutedRuns);
			}
			layoutedLines.Add(layoutedLine);
			layoutedRuns.ForEach(r => r.line = layoutedLine);
			currentLineY += layoutedLine.lineHeight;
		}

		return layoutedLines;
	}

	// TODO: we're using harfbuzz clusters as the units for clustering/"atomization" but should we use Unicode's text segmentation algorithm instead?
	// and how are they different? It seems from the HarfBuzz docs that HarfBuzz clustering by default approximates Unicode's text segmentation
	// https://harfbuzz.github.io/working-with-harfbuzz-clusters.html
	// https://unicode.org/reports/tr29
	private static void CreateSourceTextFromAndToGlyphMapping(List<LayoutedLine> lines, Cluster[] textIndexToGlyphMap)
	{
		foreach (var line in lines)
		{
			foreach (var run in line.runs)
			{
				var runGlyphLength = run.glyphs.Length;
				var runStart = run.startInInline + run.inline.StartIndex;
				var runLength = run.endInInline - run.startInInline;

				Cluster? previousCluster = null;
				for (var index = run.rtl ? 0 : runGlyphLength - 1; (run.rtl && index < runGlyphLength) || (!run.rtl && index >= 0); index += run.rtl ? 1 : -1)
				{
					var glyphDetails = run.glyphs[index];
					if (((run.rtl && index < runGlyphLength - 1) || (!run.rtl && index > 0)) && glyphDetails.info.Cluster == run.glyphs[index + (run.rtl ? 1 : -1)].info.Cluster)
					{
						continue;
					}
					var (startGlyphIndex, endGlyphIndex) = run.rtl
						? (previousCluster?.glyphInRunIndexEnd ?? 0, index + 1)
						: (index, previousCluster?.glyphInRunIndexStart ?? runGlyphLength);
					var cluster = new Cluster(runStart + (int)glyphDetails.info.Cluster, previousCluster?.sourceTextStart ?? (runStart + runLength), run, startGlyphIndex, endGlyphIndex);
					previousCluster = cluster;
					for (var i = startGlyphIndex; i < endGlyphIndex; i++)
					{
						run.glyphs[i].cluster = cluster;
					}
					for (var i = cluster.sourceTextStart; i < cluster.sourceTextEnd; i++)
					{
						textIndexToGlyphMap[i] = cluster;
					}
				}

				CI.Assert(run.glyphs.All(g => g.cluster is not null));
			}
		}
	}

	public void Draw(in Visual.PaintingSession session, (int index, CompositionBrush brush, float thickness)? caret,
		(int selectionStart, int selectionEnd, CompositionBrush selectedTextBackgroundBrush, Brush selectedTextForegroundBrush)? selection)
	{
		// if selection is out of range, this means that the parent TextBlock/TextBox updated the text and the
		// selection but a new UnicodeText instance has not been created yet. In that case, skip rendering
		// the selection this frame and wait to be called again after measuring.
		(int selectionIndexStart, int selectionIndexEnd, Cluster selectionClusterStart, Cluster selectionClusterEnd, CompositionBrush background, Brush foreground)? selectionDetails = null;
		if (selection is { } s && s.selectionStart != s.selectionEnd && s.selectionStart <= _text.Length && s.selectionEnd <= _text.Length && _text.Length > 0)
		{
			selectionDetails = (s.selectionStart, s.selectionEnd, _textIndexToGlyph[s.selectionStart], _textIndexToGlyph[Math.Min(_textIndexToGlyph.Length - 1, s.selectionEnd)], s.selectedTextBackgroundBrush, s.selectedTextForegroundBrush);
		}

		for (var index = 0; index < _lines.Count; index++)
		{
			var line = _lines[index];
			var currentLineX = line.xAlignmentOffset;
			foreach (var run in line.runs)
			{
				// Ideally, we would want to get the path of the glyphs and then draw them using CompositionBrush.Paint, but this
				// does not work for Emojis which don't have a path and instead must be drawn directly with SKCanvas.DrawText
				// var path = new SKPath();
				// for (var i = 0; i < run.glyphs.Length; i++)
				// {
				// 	var glyph = run.glyphs[i];
				// 	var p = run.fontDetails.SKFont.GetGlyphPath((ushort)glyph.info.Codepoint);
				// 	p.Transform(SKMatrix.CreateTranslation(glyph.xPosInRun + glyph.position.XOffset * run.fontDetails.TextScale.textScaleX, glyph.position.YOffset * run.fontDetails.TextScale.textScaleY), p);
				// 	path.AddPath(p);
				// }
				// path.Transform(SKMatrix.CreateTranslation(currentLineX, line.y + line.baselineOffset), path);
				//
				// session.Canvas.Save();
				// session.Canvas.ClipPath(path, antialias: true);
				// run.inline.Foreground.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()).Paint(session.Canvas, session.Opacity, path.Bounds);
				// session.Canvas.Restore();

				using (var textBlobBuilder = new SKTextBlobBuilder())
				{
					var glyphs = new ushort[run.glyphs.Length];
					var positions = new SKPoint[run.glyphs.Length];
					for (var i = 0; i < run.glyphs.Length; i++)
					{
						var glyph = run.glyphs[i];
						glyphs[i] = (ushort)glyph.info.Codepoint;
						positions[i] = new SKPoint(glyph.xPosInRun + glyph.position.GlyphPosition.XOffset * run.fontDetails.TextScale.textScaleX, line.y + glyph.position.GlyphPosition.YOffset * run.fontDetails.TextScale.textScaleY);
					}

					void DrawText(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<SKPoint> positions, Visual.PaintingSession session, Brush brush)
					{
						textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
						var blob1 = textBlobBuilder.Build(); // Build resets the blob builder
						var paint = SetupPaint(brush, session.Opacity);
						session.Canvas.DrawText(blob1, currentLineX, line.baselineOffset, paint);
					}

					if (selectionDetails is { } sd && (sd.selectionClusterStart.sourceTextStart < run.endInInline + run.inline.StartIndex && (selection!.Value.selectionEnd == _text.Length || run.startInInline + run.inline.StartIndex < sd.selectionClusterEnd.sourceTextStart)))
					{
						int selectionLeft;
						int selectionRight; // the selection ends to the left of positions[selectionRight].X
						if (run.rtl)
						{
							selectionLeft = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length ? sd.selectionClusterEnd.glyphInRunIndexEnd : 0;
							selectionRight = sd.selectionClusterStart.layoutedRun == run ? sd.selectionClusterStart.glyphInRunIndexStart + 1 : run.glyphs.Length;
						}
						else
						{
							selectionLeft = sd.selectionClusterStart.layoutedRun == run ? sd.selectionClusterStart.glyphInRunIndexStart : 0;
							selectionRight = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length ? sd.selectionClusterEnd.glyphInRunIndexStart : run.glyphs.Length;
						}

						var leftX = positions[selectionLeft].X;
						var rightX = positions[selectionRight - 1].X + GlyphWidth(run.glyphs[selectionRight - 1].position, run.fontDetails);
						var selectionRect = new SKRect(currentLineX + leftX, line.y, currentLineX + rightX, line.y + line.lineHeight);
						sd.background.Paint(session.Canvas, session.Opacity, selectionRect);

						var glyphsSpan = glyphs.AsSpan();
						var positionsSpan = positions.AsSpan();
						if (selectionLeft > 0)
						{
							DrawText(glyphsSpan[..selectionLeft], positionsSpan[..selectionLeft], session, run.inline.Foreground);
						}
						DrawText(glyphsSpan[selectionLeft..selectionRight], positionsSpan[selectionLeft..selectionRight], session, sd.foreground);
						if (selectionRight < run.glyphs.Length)
						{
							DrawText(glyphsSpan[selectionRight..], positionsSpan[selectionRight..], session, run.inline.Foreground);
						}
					}
					else
					{
						DrawText(glyphs, positions, session, run.inline.Foreground);
					}

					currentLineX += run.width;
				}
			}
		}

		// if the caret index is out of range, this means that the parent TextBlock/TextBox updated the text and the
		// caret position but a new UnicodeText instance has not been created yet. In that case, skip care rendering
		// this frame and wait to be called again after measuring.
		if (caret is { } c && caret.Value.index <= _text.Length)
		{
			c.brush.Paint(session.Canvas, session.Opacity, GetCaretRectForIndex(c.index, c.thickness).ToSKRect());
		}
	}

	private static SKPaint SetupPaint(Brush foreground, float opacity)
	{
		var paint = _spareDrawPaint;
		paint.Reset();
		paint.IsStroke = false;
		paint.IsAntialias = true;

		if (foreground is SolidColorBrush scb)
		{
			var scbColor = scb.Color;
			paint.Color = new SKColor(
				red: scbColor.R,
				green: scbColor.G,
				blue: scbColor.B,
				alpha: (byte)(scbColor.A * scb.Opacity * opacity));
		}
		else if (foreground is GradientBrush gb)
		{
			var gbColor = gb.FallbackColorWithOpacity;
			paint.Color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}
		else if (foreground is XamlCompositionBrushBase xcbb)
		{
			var gbColor = xcbb.FallbackColorWithOpacity;
			paint.Color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}

		return paint;
	}

	private static float RunWidth((GlyphInfo info, UnoGlyphPosition position)[] glyphs, FontDetails details) => glyphs.Sum(g => GlyphWidth(g.position, details));
	private static float GlyphWidth(UnoGlyphPosition position, FontDetails details) => position.XAdvance * details.TextScale.textScaleX;

	public Rect GetCaretRectForIndex(int index, float caretThickness)
	{
		if (index == 0 && string.IsNullOrEmpty(_text))
		{
			var alignmentOffset = _textAlignment switch
			{
				TextAlignment.Left => 0,
				TextAlignment.Center => _desiredSize.Width / 2,
				TextAlignment.Right => _desiredSize.Width,
				_ => throw new ArgumentOutOfRangeException()
			};
			return new Rect(alignmentOffset, 0, caretThickness, _defaultFontDetails.LineHeight);
		}

		if (index == _text.Length)
		{
			var lastLine = _lines[^1];
			if (lastLine.runs.Count == 0)
			{
				// text ending in newline
				return new Rect(_rtl ? lastLine.xAlignmentOffset - caretThickness : lastLine.xAlignmentOffset, lastLine.y, caretThickness, lastLine.lineHeight);
			}
			else
			{
				var lastCluster = _textIndexToGlyph[^1];
				var lastGlyph = lastCluster.layoutedRun.glyphs[lastCluster.glyphInRunIndexEnd - 1];
				var lastGlyphX = lastGlyph.xPosInRun + lastCluster.layoutedRun.xPosInLine + lastCluster.layoutedRun.line.xAlignmentOffset;
				return new Rect(lastCluster.layoutedRun.rtl ? lastGlyphX : lastGlyphX + GlyphWidth(lastGlyph.position, lastCluster.layoutedRun.fontDetails) - caretThickness, lastCluster.layoutedRun.line.y, caretThickness, lastCluster.layoutedRun.line.lineHeight);
			}
		}
		else
		{
			var cluster = _textIndexToGlyph[index];
			var glyph = cluster.layoutedRun.glyphs[cluster.glyphInRunIndexStart];
			var glyphX = glyph.xPosInRun + cluster.layoutedRun.xPosInLine + cluster.layoutedRun.line.xAlignmentOffset;
			var rect = new Rect(cluster.layoutedRun.rtl ? glyphX + GlyphWidth(glyph.position, cluster.layoutedRun.fontDetails) - caretThickness : glyphX, cluster.layoutedRun.line.y, caretThickness, cluster.layoutedRun.line.lineHeight);

			// When the index is set to be right after a run that runs in the direction of the base direction,
			// and right at the start of a run that runs opposite to the base direction, the caret should be
			// at the end of the logically previous run, not at the start of the "current" run.
			var glyphRun = glyph.parentRun;
			var isFirstGlyphInRun = (!glyphRun.rtl && glyphRun.glyphs[0] == glyph) || (glyphRun.rtl && glyphRun.glyphs[^1] == glyph);
			if (isFirstGlyphInRun && glyphRun.indexInLine > 0)
			{
				if (_rtl && !glyphRun.rtl && glyphRun.indexInLine < glyphRun.line.runs.Count - 1 && glyphRun.line.runs[glyphRun.indexInLine + 1].rtl)
				{
					rect.X += glyphRun.width;
				}
				else if (!_rtl && glyphRun.rtl && glyphRun.indexInLine > 0 && !glyphRun.line.runs[glyphRun.indexInLine - 1].rtl)
				{
					rect.X -= glyphRun.width;
				}
			}

			return rect;
		}
	}

	public Rect GetRectForIndex(int index)
	{
		if (index == 0 && string.IsNullOrEmpty(_text))
		{
			var alignmentOffset = _textAlignment switch
			{
				TextAlignment.Left => 0,
				TextAlignment.Center => _desiredSize.Width / 2,
				TextAlignment.Right => _desiredSize.Width,
				_ => throw new ArgumentOutOfRangeException()
			};
			return new Rect(alignmentOffset, 0, 0, _defaultFontDetails.LineHeight);
		}

		index = Math.Min(index, _text.Length);

		if (index == _text.Length)
		{
			var lastRect = GetRectForIndex(index - 1);
			return _rtl ?
				new Rect(lastRect.Left, lastRect.Y, 0, lastRect.Height) :
				new Rect(lastRect.Right, lastRect.Y, 0, lastRect.Height);
		}
		var cluster = _textIndexToGlyph[index];
		var glyphs = cluster.layoutedRun.glyphs[cluster.glyphInRunIndexStart..cluster.glyphInRunIndexEnd];

		var x = glyphs[0].xPosInRun + cluster.layoutedRun.xPosInLine + cluster.layoutedRun.line.xAlignmentOffset;
		var y = cluster.layoutedRun.line.y;
		var width = glyphs.Sum(g => GlyphWidth(g.position, cluster.layoutedRun.fontDetails));
		var height = cluster.layoutedRun.line.lineHeight;
		return new Rect(x, y, width, height);
	}

	public int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection) =>
		GetIndexAndRunAt(p, ignoreEndingNewLine, extendedSelection).index;

	private (int index, LayoutedLineBrokenBidiRun? run) GetIndexAndRunAt(Point p, bool ignoreEndingNewLine, bool extendedSelection)
	{
		if (_lines.Count == 0)
		{
			return extendedSelection ? (0, null) : (-1, null);
		}

		if (p.Y < 0)
		{
			if (extendedSelection)
			{
				p.Y = 0;
			}
			else
			{
				return (-1, null);
			}
		}

		if (_desiredSize.Height < p.Y)
		{
			if (extendedSelection)
			{
				p.Y = _desiredSize.Height;
			}
			else
			{
				return (-1, null);
			}
		}

		foreach (var line in _lines)
		{
			if (line.y > p.Y || line.y + line.lineHeight < p.Y)
			{
				continue;
			}

			if (line.runs.Count == 0)
			{
				CI.Assert(line == _lines[^1]);
				return extendedSelection ? (0, _lines[^2].runs.MaxBy(r => r.indexInLine + r.inline.StartIndex)) : (-1, null);
			}

			{
				if (line.runs[0] is var firstRun && firstRun.xPosInLine + firstRun.line.xAlignmentOffset > p.X)
				{
					if (!extendedSelection)
					{
						return (-1, null);
					}
					else
					{
						var index = _rtl
							? firstRun.endInInline + firstRun.inline.StartIndex - (ignoreEndingNewLine ? TrailingCRLFCount(firstRun.inline.Text, firstRun.startInInline, firstRun.endInInline) : 0)
							: firstRun.startInInline + firstRun.inline.StartIndex;
						return (index, firstRun);
					}
				}
			}

			{
				if (line.runs[^1] is var lastRun && lastRun.xPosInLine + lastRun.line.xAlignmentOffset + lastRun.width < p.X)
				{
					if (!extendedSelection)
					{
						return (-1, null);
					}
					else
					{
						var index = _rtl
							? lastRun.inline.StartIndex + lastRun.startInInline
							: lastRun.inline.StartIndex + lastRun.endInInline - (ignoreEndingNewLine ? TrailingCRLFCount(lastRun.inline.Text, lastRun.startInInline, lastRun.endInInline) : 0);
						return (index, lastRun);
					}
				}
			}

			foreach (var run in line.runs)
			{
				if (run.xPosInLine + run.line.xAlignmentOffset > p.X || run.xPosInLine + run.line.xAlignmentOffset + run.width < p.X)
				{
					continue;
				}

				foreach (var glyph in run.glyphs)
				{
					var globalGlyphX = glyph.xPosInRun + run.xPosInLine + run.line.xAlignmentOffset;
					var width = GlyphWidth(glyph.position, run.fontDetails);
					if (globalGlyphX <= p.X && globalGlyphX + width >= p.X)
					{
						var closerToLeft = p.X - globalGlyphX < globalGlyphX + width - p.X;
						var index = (closerToLeft && !run.rtl) || (!closerToLeft && run.rtl) ? glyph.cluster!.sourceTextStart : glyph.cluster!.sourceTextEnd;
						return (index, run);
					}
				}
			}
		}

		CI.Assert(false, "This should be unreachable");
		return (-1, null);
	}

	public Hyperlink? GetHyperlinkAt(Point point)
	{
		var run = GetIndexAndRunAt(point, ignoreEndingNewLine: false, extendedSelection: false).run;
		DependencyObject? parent = run?.inline.Inline;
		while (parent is TextElement textElement)
		{
			if (parent is Hyperlink h)
			{
				return h;
			}
			parent = textElement.GetParent() as DependencyObject;
		}

		return null;
	}

	public (int start, int length) GetWordAt(int index, bool right)
	{
		if (index == 0)
		{
			if (_wordBoundaries.Count == 0)
			{
				return (0, 0);
			}
			else
			{
				return (0, _wordBoundaries[0]);
			}
		}
		else if (index == _text.Length)
		{
			if (right)
			{
				return (_text.Length, 0);
			}
			else if (_wordBoundaries.Count == 1)
			{
				return (0, _text.Length);
			}
			else
			{
				return (_wordBoundaries[^2], _text.Length);
			}
		}
		else
		{
			var prevBoundary = 0;
			foreach (var boundary in _wordBoundaries)
			{
				if (index < boundary || boundary == index && !right)
				{
					return (prevBoundary, boundary - prevBoundary);
				}
				prevBoundary = boundary;
			}
		}
		throw new UnreachableException();
	}

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
	{
		if (_lines.Count == 0)
		{
			return (0, 0, true, true, 0);
		}
		foreach (var line in _lines)
		{
			if (line.startInText <= index && (line.endInText > index || (line.lineIndex == _lines.Count - 1 && line.endInText == index)))
			{
				return (line.startInText, line.endInText - line.startInText, line.lineIndex == 0, line.lineIndex == _lines.Count - 1, line.lineIndex);
			}
		}
		throw new ArgumentOutOfRangeException("Given index is not within the range of text length.");
	}

	private static int TrailingSpaceCount(string str, int start, int end)
	{
		for (var i = end - 1; i >= start; i--)
		{
			if (str[i] != ' ')
			{
				return end - 1 - i;
			}
		}
		return 0;
	}

	private static int TrailingCRLFCount(string str, int start, int end)
	{
		if (str is [.., '\r', '\n'])
		{
			return 2;
		}
		else if (str[end - 1] == '\r' || str[end - 1] == '\n')
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}

	private static bool IsLineBreak(string text, int indexAfterLineBreakOpportunity)
	{
		// https://www.unicode.org/standard/reports/tr13/tr13-5.html

		switch (text[indexAfterLineBreakOpportunity - 1])
		{
			case '\u000A':
			case '\u000B':
			case '\u000C':
			case '\u000D':
			case '\u0085':
			case '\u2028':
			case '\u2029': // Paragraph separator (should apply paragraph formatting, i.e. paragraph spacing/indentation on new line, unlike other line
						   // breaks - could matter if/when Paragraph.TextIndent/RichTextBlock.TextIndent is implemented (UWP/WinUI conformance to this
						   // behavior was not tested).
				return true;
			default:
				return false;
		}
	}

	// This method assumes that the FontDetails with the biggest LineHeight is also the one with the biggest SKFontMetrics.Top.
	// If that assumption is wrong, we will need an additional lazy parameter for the latter.
	private static (float lineHeight, float baselineOffset) GetLineHeightAndBaselineOffset(LineStackingStrategy lineStackingStrategy, float lineHeight, FontDetails fontDetailsWithMaxHeightInLine, bool isFirstLine, bool isLastLine)
	{
		if (lineStackingStrategy is LineStackingStrategy.MaxHeight || !(lineHeight > 0))
		{
			return (Math.Max(lineHeight, fontDetailsWithMaxHeightInLine.LineHeight), -fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent);
		}
		else if (lineStackingStrategy is LineStackingStrategy.BaselineToBaseline)
		{
			if (isFirstLine)
			{
				return (Math.Min(fontDetailsWithMaxHeightInLine.LineHeight, Math.Max(-fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent, lineHeight)), -fontDetailsWithMaxHeightInLine.SKFontMetrics.Ascent);
			}
			else
			{
				if (isLastLine)
				{
					return (lineHeight + fontDetailsWithMaxHeightInLine.SKFontMetrics.Descent, lineHeight);
				}
				else
				{
					return (lineHeight, lineHeight);
				}
			}
		}
		else if (lineStackingStrategy is LineStackingStrategy.BlockLineHeight)
		{
			return (lineHeight, lineHeight - fontDetailsWithMaxHeightInLine.SKFontMetrics.Descent);
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(lineStackingStrategy));
		}
	}
}
