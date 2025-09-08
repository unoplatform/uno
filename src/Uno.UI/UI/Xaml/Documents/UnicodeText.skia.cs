#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Text;
using HarfBuzzSharp;
using Icu;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
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
internal readonly struct UnicodeText : IParsedText
{
	// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
	private const float TabStopWidth = 48;

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
			Debug.Assert(inline is Run or LineBreak);
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

	// A BidiRun run split at line break boundaries
	private readonly record struct BidiRun(ReadonlyInlineCopy inline, int startInInline, int endInInline, bool rtl, FontDetails fontDetails);

	// FontDetails might be different from inline.FontDetails because of font fallback
	// glyphs are always ordered LTR even in RTL text
	private readonly record struct ShapedLineBrokenBidiRun(ReadonlyInlineCopy Inline, int startInInline, int endInInline, (GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails fontDetails, bool rtl);
	private record LayoutedGlyphDetails(GlyphInfo info, GlyphPosition position, float xPosInRun, Cluster? cluster, LayoutedLineBrokenBidiRun parentRun)
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
		out Size desiredSize)
	{
		Debug.Assert(maxLines >= 0);
		_size = availableSize;
		_defaultFontDetails = defaultFontDetails;

		string text;
		if (textAlignment is null)
		{
			// TODO: can we make this cleaner instead of implicitly assuming that this is a code path coming from TextBox?
			Debug.Assert(inlines.Length == 1);
			var inline = (Run)inlines[0];
			var inlineText = inline.GetText();
			if (inlineText.Length == 0)
			{
				flowDirection = inline.FlowDirection;
			}
			else
			{
				using var bidi = new BiDi();
				bidi.SetPara(inlines[0].GetText(), BiDi.DEFAULT_LTR, null);
				bidi.GetLogicalRun(0, out var level);
				Debug.Assert(level is (int)BiDi.BiDiDirection.RTL or (int)BiDi.BiDiDirection.LTR);
				flowDirection = level is (int)BiDi.BiDiDirection.RTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
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
		var unlayoutedLines = SplitTextIntoLines(_rtl, _inlines, lineWidth);
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
	private static List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)> SplitTextIntoLines(bool rtl, List<ReadonlyInlineCopy> inlines, float lineWidth)
	{
		var logicallyOrderedRuns = new List<BidiRun>();
		var logicallyOrderedLineBreakingOpportunities = new List<(int indexInInline, ReadonlyInlineCopy inline)>();
		foreach (var inline in inlines)
		{
			var text = inline.Text;
			var bidi = new BiDi();
			bidi.SetPara(text, (byte)(inline.FlowDirection == FlowDirection.LeftToRight ? 0 : 1), null);
			logicallyOrderedRuns.AddRange(SplitTextIntoLogicallyOrderedBidiRuns(inline, bidi));
			logicallyOrderedLineBreakingOpportunities.AddRange(GetLineBreakingOpportunities(text).Select(b => (b, inline)));
		}

		var linesWithLogicallyOrderedRuns = ApplyLineBreaking(lineWidth, logicallyOrderedRuns, logicallyOrderedLineBreakingOpportunities);
		var linesWithBidiReordering = ApplyBidiReordering(rtl, linesWithLogicallyOrderedRuns);

		return linesWithBidiReordering;
	}

	private static List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)> ApplyBidiReordering(bool rtl, List<List<BidiRun>> linesWithLogicallyOrderedRuns)
	{
		var shapedLines = new List<(List<ShapedLineBrokenBidiRun> runs, int startInText, int endInText)>();
		for (var lineIndex = 0; lineIndex < linesWithLogicallyOrderedRuns.Count; lineIndex++)
		{
			var line = linesWithLogicallyOrderedRuns[lineIndex];
			if (line.Count == 0)
			{
				// Only the last line can be empty, otherwise it will have at least one piece of piece or a line break.
				Debug.Assert(lineIndex == linesWithLogicallyOrderedRuns.Count - 1);
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

				var text = inline.Text[startInInline..endInInline];
				var bidi = new BiDi();
				bidi.SetPara(text, (byte)(inline.FlowDirection is FlowDirection.RightToLeft ? 1 : 0), null);
				var runCount = bidi.CountRuns();
				for (var runIndex = 0; runIndex < runCount; runIndex++)
				{
					var level = bidi.GetVisualRun(runIndex, out var logicalStart, out var length);
					Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);

					var sameInlineRunsLengthBeforeFontSplitting = sameInlineRuns.Count;
					var currentFontDetails = inline.FontDetails;
					var currentFontSplitStart = logicalStart;
					for (var i = logicalStart; i < logicalStart + length; i += char.IsSurrogate(inline.Text, startInInline + i) ? 2 : 1)
					{
						FontDetails newFontDetails;
						if (char.ConvertToUtf32(inline.Text, startInInline + i) is var codepoint && !inline.FontDetails.SKFont.ContainsGlyph(codepoint))
						{
							newFontDetails = SKFontManager.Default.MatchCharacter(codepoint) is { } fallbackTypeface
								? FontDetailsCache.GetFont(fallbackTypeface.FamilyName, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle).details
								: inline.FontDetails;
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
								sameInlineRuns.Add(new ShapedLineBrokenBidiRun(inline, startInInline + currentFontSplitStart, startInInline + i, ShapeRun(inline.Text[(startInInline + currentFontSplitStart)..(startInInline + i)], level is BiDi.BiDiDirection.RTL, currentFontDetails, null), currentFontDetails, level is BiDi.BiDiDirection.RTL));
							}
							currentFontDetails = newFontDetails;
							currentFontSplitStart = i;
						}
					}
					// the currentLineWidth parameter of ShapeRun is null and the tab width will be adjusted later in LayoutLines.
					sameInlineRuns.Add(new ShapedLineBrokenBidiRun(inline, startInInline + currentFontSplitStart, startInInline + logicalStart + length, ShapeRun(inline.Text[(startInInline + currentFontSplitStart)..(startInInline + logicalStart + length)], level is BiDi.BiDiDirection.RTL, currentFontDetails, null), currentFontDetails, level is BiDi.BiDiDirection.RTL));
					// swap runs if rtl since we always process characters in a single bidi run in logical order
					if (level is BiDi.BiDiDirection.RTL)
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

	private static IEnumerable<(ReadonlyInlineCopy Inline, int startInInline, int endInInline)> GroupByInline(List<BidiRun> line)
	{
		foreach (var group in line.GroupBy(r => r.inline))
		{
			var inline = group.Key;
			var groupAsArray = group.ToArray();
			var startInInline = groupAsArray[0].startInInline;
			var endInInline = groupAsArray[^1].endInInline;
			var i = startInInline;
			while (inline.Text.IndexOf('\t', i) is var tabIndex && tabIndex != -1)
			{
				if (i != tabIndex)
				{
					yield return (inline, i, tabIndex);
				}
				yield return (inline, tabIndex, tabIndex + 1);
				i = tabIndex + 1;
			}

			if (i != endInInline)
			{
				yield return (inline, i, endInInline);
			}
		}
	}

	private static List<List<BidiRun>> ApplyLineBreaking(float lineWidth, List<BidiRun> logicallyOrderedRuns, List<(int indexInInline, ReadonlyInlineCopy inline)> logicallyOrderedLineBreakingOpportunities)
	{
		var lines = new List<List<BidiRun>>();
		var currentLine = new List<BidiRun>();

		var nextLineBreakingOpportunityIndex = 0;
		var nextLineBreakingOpportunity = logicallyOrderedLineBreakingOpportunities[0];

		var remainingLineWidth = lineWidth;
		var stack = new Stack<BidiRun>();
		for (var index = 0; stack.Count > 0 || index < logicallyOrderedRuns.Count;)
		{
			var bidiRun = stack.Count > 0 ? stack.Pop() : logicallyOrderedRuns[index++];
			// Each inline must end in a line breaking opportunity
			if (bidiRun.inline != nextLineBreakingOpportunity.inline || bidiRun.startInInline >= nextLineBreakingOpportunity.indexInInline)
			{
				nextLineBreakingOpportunityIndex++;
				nextLineBreakingOpportunity = logicallyOrderedLineBreakingOpportunities[nextLineBreakingOpportunityIndex];
				stack.Push(bidiRun);
				continue;
			}

			Debug.Assert(nextLineBreakingOpportunityIndex < logicallyOrderedLineBreakingOpportunities.Count && (bidiRun.inline.StartIndex < nextLineBreakingOpportunity.inline.StartIndex || (bidiRun.inline == nextLineBreakingOpportunity.inline && bidiRun.startInInline < nextLineBreakingOpportunity.indexInInline)));

			// TODO: end-of-line space hanging

			if (bidiRun.inline != nextLineBreakingOpportunity.inline || bidiRun.endInInline < nextLineBreakingOpportunity.indexInInline)
			{
				var glyphsOfEntireBidiRun = ShapeRun(bidiRun.inline.Text[bidiRun.startInInline..bidiRun.endInInline], bidiRun.rtl, bidiRun.fontDetails, lineWidth is float.PositiveInfinity ? 0 : lineWidth - remainingLineWidth);
				var bidiRunWidth = RunWidth(glyphsOfEntireBidiRun, bidiRun.inline.FontDetails);
				// no line breaking opportunity in run
				if (currentLine.Count == 0 || bidiRunWidth <= remainingLineWidth)
				{
					currentLine.Add(bidiRun);
					remainingLineWidth -= bidiRunWidth;
				}
				else
				{
					MoveToNextLine(lines, ref currentLine, ref remainingLineWidth, lineWidth);
					stack.Push(bidiRun);
				}
				continue;
			}

			Debug.Assert(nextLineBreakingOpportunityIndex < logicallyOrderedLineBreakingOpportunities.Count && bidiRun.inline == nextLineBreakingOpportunity.inline && bidiRun.startInInline < nextLineBreakingOpportunity.indexInInline && bidiRun.endInInline >= nextLineBreakingOpportunity.indexInInline);

			var biggestFittingPrefixEnd = -1;
			float biggestFittingPrefixWidth = -1;
			{
				var testOpportunityIndex = nextLineBreakingOpportunityIndex;
				var testOpportunity = logicallyOrderedLineBreakingOpportunities[testOpportunityIndex];
				while (testOpportunity.inline == bidiRun.inline && testOpportunity.indexInInline <= bidiRun.endInInline)
				{
					var testGlyphs = ShapeRun(bidiRun.inline.Text[bidiRun.startInInline..testOpportunity.indexInInline], bidiRun.rtl, bidiRun.fontDetails, lineWidth is float.PositiveInfinity ? 0 : lineWidth - remainingLineWidth);
					var testWidth = RunWidth(testGlyphs, bidiRun.inline.FontDetails);
					if (testWidth <= remainingLineWidth)
					{
						if (testWidth > biggestFittingPrefixWidth)
						{
							biggestFittingPrefixEnd = testOpportunity.indexInInline;
							biggestFittingPrefixWidth = testWidth;
						}
						if (IsLineBreak(bidiRun.inline.Text, testOpportunity.indexInInline))
						{
							break;
						}
						testOpportunityIndex++;
						if (testOpportunityIndex < logicallyOrderedLineBreakingOpportunities.Count)
						{
							testOpportunity = logicallyOrderedLineBreakingOpportunities[testOpportunityIndex];
						}
						else
						{
							break;
						}
					}
					else
					{
						break;
					}
				}
			}

			if (biggestFittingPrefixEnd == -1 && currentLine.Count == 0)
			{
				biggestFittingPrefixEnd = nextLineBreakingOpportunity.indexInInline;
				var g = ShapeRun(bidiRun.inline.Text[bidiRun.startInInline..nextLineBreakingOpportunity.indexInInline], bidiRun.rtl, bidiRun.fontDetails, lineWidth is float.PositiveInfinity ? 0 : lineWidth - remainingLineWidth);
				biggestFittingPrefixWidth = RunWidth(g, bidiRun.inline.FontDetails);
			}

			if (biggestFittingPrefixEnd != -1)
			{
				remainingLineWidth -= biggestFittingPrefixWidth;
				currentLine.Add(bidiRun with { endInInline = biggestFittingPrefixEnd });
				if (bidiRun.endInInline != biggestFittingPrefixEnd)
				{
					stack.Push(bidiRun with { startInInline = biggestFittingPrefixEnd });
				}
				if (remainingLineWidth < 0 || IsLineBreak(bidiRun.inline.Text, biggestFittingPrefixEnd))
				{
					MoveToNextLine(lines, ref currentLine, ref remainingLineWidth, lineWidth);
				}
			}
			else
			{
				stack.Push(bidiRun);
				MoveToNextLine(lines, ref currentLine, ref remainingLineWidth, lineWidth);
			}
		}

		lines.Add(currentLine);

#if DEBUG
		Debug.Assert(lines.Count > 0);
		// all the lines have content except possibly the last line, because we only move to a new line when we hit a line break
		foreach (var line in lines)
		{
			Debug.Assert(line.Count > 0 || line == lines[^1]);
		}
#endif

		return lines;

		static void MoveToNextLine(List<List<BidiRun>> lines, ref List<BidiRun> currentLine, ref float remainingLineWidth, float lineWidth)
		{
			lines.Add(currentLine);
			currentLine = new List<BidiRun>();
			remainingLineWidth = lineWidth;
		}
	}

	private static List<int> GetLineBreakingOpportunities(string text)
	{
		// TODO: locale?
		var lineBoundaries = BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.LINE, new Locale("en", "US"), text).ToArray();
#if DEBUG
		Debug.Assert(lineBoundaries[0].Start == 0);
		for (int i = 0; i < lineBoundaries.Length - 1; i++)
		{
			Debug.Assert(lineBoundaries[i].End != lineBoundaries[i].Start && lineBoundaries[i].End == lineBoundaries[i + 1].Start);
		}
		Debug.Assert(lineBoundaries[^1].End != lineBoundaries[^1].Start && lineBoundaries[^1].End == text.Length);
#endif
		// The line breaking opportunity is right before the "line boundary"
		var lineBreakingOpportunities = lineBoundaries.Select(b => b.End).ToList();
		return lineBreakingOpportunities;
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

	private static List<BidiRun> SplitTextIntoLogicallyOrderedBidiRuns(ReadonlyInlineCopy inline, BiDi bidi)
	{
		var runCount = bidi.CountRuns();
		var logicallyOrderedRuns = new List<BidiRun>(runCount);
		for (var runIndex = 0; runIndex < runCount; runIndex++)
		{
			// using bidi.GetLogicalRun instead returned weird results especially in rtl text.
			var level = bidi.GetVisualRun(runIndex, out var logicalStart, out var length);
			Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);

			var currentFontDetails = inline.FontDetails;
			var currentFontSplitStart = logicalStart;
			for (var i = logicalStart; i < logicalStart + length; i += char.IsSurrogate(inline.Text, i) ? 2 : 1)
			{
				FontDetails newFontDetails;
				if (char.ConvertToUtf32(inline.Text, i) is var codepoint && !inline.FontDetails.SKFont.ContainsGlyph(codepoint))
				{
					newFontDetails = SKFontManager.Default.MatchCharacter(codepoint) is { } fallbackTypeface
						? FontDetailsCache.GetFont(fallbackTypeface.FamilyName, (float)inline.FontSize, inline.FontWeight, inline.FontStretch, inline.FontStyle).details
						: inline.FontDetails;
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
						logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, i, level == BiDi.BiDiDirection.RTL, currentFontDetails));
					}
					currentFontDetails = newFontDetails;
					currentFontSplitStart = i;
					if (isTab)
					{
						logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, i + 1, level == BiDi.BiDiDirection.RTL, currentFontDetails));
						currentFontSplitStart = i + 1;
					}
				}
			}

			if (currentFontSplitStart != logicalStart + length)
			{
				logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, logicalStart + length, level == BiDi.BiDiDirection.RTL, currentFontDetails));
			}
		}

		logicallyOrderedRuns.Sort((run, bidiRun) => run.startInInline - bidiRun.startInInline);

#if DEBUG
		Debug.Assert(logicallyOrderedRuns[0].startInInline == 0);
		for (var i = 0; i < runCount - 1; i++)
		{
			Debug.Assert(logicallyOrderedRuns[i].endInInline != logicallyOrderedRuns[i].startInInline && logicallyOrderedRuns[i].endInInline == logicallyOrderedRuns[i + 1].startInInline);
		}
		Debug.Assert(logicallyOrderedRuns[^1].endInInline != logicallyOrderedRuns[^1].startInInline && logicallyOrderedRuns[^1].endInInline == inline.Text.Length);
#endif

		return logicallyOrderedRuns;
	}

	/// <param name="currentLineWidth">Only used for tab stop width calculation. Null to ignore this case.</param>
	private static (GlyphInfo info, GlyphPosition position)[] ShapeRun(string textRun, bool rtl, FontDetails fontDetails, float? currentLineWidth)
	{
		Debug.Assert(textRun.Length < 2 || !textRun[..^2].Contains("\r\n"));
		using var buffer = new Buffer();
		buffer.AddUtf16(textRun, 0, textRun is [.., '\r', '\n'] ? textRun.Length - 1 : textRun.Length);
		buffer.GuessSegmentProperties();
		buffer.Direction = rtl ? Direction.RightToLeft : Direction.LeftToRight;
		// TODO: ligatures
		fontDetails.Font.Shape(buffer, new Feature(new Tag('l', 'i', 'g', 'a'), 0));
		var positions = buffer.GetGlyphPositionSpan();
		var infos = buffer.GetGlyphInfoSpan();
		var count = buffer.Length;
		var ret = new (GlyphInfo, GlyphPosition)[count];
		for (var i = 0; i < count; i++)
		{
			ret[i] = (infos[i], positions[i]);
		}

		// Fonts will give a width > 0 to \r, so we hardcore the width here.
		// TODO: make this cleaner somehow
		var isTab = textRun is "\t";
		if ((isTab || IsLineBreak(textRun, textRun.Length)) && infos[^1].Cluster == textRun.Length - (textRun is [.., '\r', '\n'] ? 2 : 1))
		{
			fontDetails.Font.TryGetGlyph(' ', out var codepoint);
			var tabWidth = TabStopWidth / fontDetails.TextScale.textScaleX;
			ret[^1] = (infos[^1] with { Codepoint = codepoint }, positions[^1] with { XAdvance = (int)(isTab ? tabWidth - (currentLineWidth ?? 0) % tabWidth : 0) });
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
				Debug.Assert(lineIndex == lines.Count - 1 && lineIndex != 0);
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

				Debug.Assert(run.glyphs.All(g => g.cluster is not null));
			}
		}
	}

	public void Draw(in Visual.PaintingSession session, (int index, CompositionBrush brush, float thickness)? caret,
		(int selectionStart, int selectionEnd, CompositionBrush brush)? selection)
	{
		// if selection is out of range, this means that the parent TextBlock/TextBox updated the text and the
		// selection but a new UnicodeText instance has not been created yet. In that case, skip rendering
		// the selection this frame and wait to be called again after measuring.
		(int selectionIndexStart, int selectionIndexEnd, Cluster selectionClusterStart, Cluster selectionClusterEnd, CompositionBrush brush)? selectionDetails = null;
		if (selection is { } s && s.selectionStart != s.selectionEnd && s.selectionStart <= _text.Length && s.selectionEnd <= _text.Length && _text.Length > 0)
		{
			selectionDetails = (s.selectionStart, s.selectionEnd, _textIndexToGlyph[s.selectionStart], _textIndexToGlyph[Math.Min(_textIndexToGlyph.Length - 1, s.selectionEnd)], s.brush);
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
						positions[i] = new SKPoint(glyph.xPosInRun + glyph.position.XOffset * run.fontDetails.TextScale.textScaleX, line.y + glyph.position.YOffset * run.fontDetails.TextScale.textScaleY);
					}

					if (selectionDetails is { } sd && (sd.selectionClusterStart.sourceTextStart < run.endInInline + run.inline.StartIndex && run.startInInline + run.inline.StartIndex < sd.selectionClusterEnd.sourceTextStart))
					{
						if (run.rtl)
						{
							var leftX = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length ? positions[sd.selectionClusterEnd.glyphInRunIndexEnd - 1].X + GlyphWidth(run.glyphs[sd.selectionClusterEnd.glyphInRunIndexEnd - 1].position, run.fontDetails) : positions[0].X;
							var rightX = sd.selectionClusterStart.layoutedRun == run ? positions[sd.selectionClusterStart.glyphInRunIndexStart].X + GlyphWidth(run.glyphs[sd.selectionClusterStart.glyphInRunIndexStart].position, run.fontDetails) : positions[^1].X + GlyphWidth(run.glyphs[^1].position, run.fontDetails);
							var selectionRect = new SKRect(currentLineX + leftX, line.y, currentLineX + rightX, line.y + line.lineHeight);
							sd.brush.Paint(session.Canvas, session.Opacity, selectionRect);
						}
						else
						{
							var leftX = sd.selectionClusterStart.layoutedRun == run ? positions[sd.selectionClusterStart.glyphInRunIndexStart].X : positions[0].X;
							var rightX = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length
								? positions[sd.selectionClusterEnd.glyphInRunIndexStart].X
								: positions[^1].X + GlyphWidth(run.glyphs[^1].position, run.fontDetails);
							var selectionRect = new SKRect(currentLineX + leftX, line.y, currentLineX + rightX, line.y + line.lineHeight);
							sd.brush.Paint(session.Canvas, session.Opacity, selectionRect);
						}
					}

					textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);

					var paint = SetupPaint(run.inline.Foreground, session.Opacity);
					session.Canvas.DrawText(textBlobBuilder.Build(), currentLineX, line.baselineOffset, paint);
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

	private static float RunWidth((GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails details) => glyphs.Sum(g => GlyphWidth(g.position, details));
	private static float GlyphWidth(GlyphPosition position, FontDetails details) => position.XAdvance * details.TextScale.textScaleX;

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

	public int GetIndexAt(Point p, bool ignoreEndingSpace, bool extendedSelection) =>
		GetIndexAndRunAt(p, ignoreEndingSpace, extendedSelection).index;

	private (int index, LayoutedLineBrokenBidiRun? run) GetIndexAndRunAt(Point p, bool ignoreEndingSpace, bool extendedSelection)
	{
		if (_lines.Count == 0)
		{
			return extendedSelection ? (0, null) : (-1, null);
		}

		if (p.Y < 0)
		{
			return extendedSelection ? (0, _lines[0].runs.MinBy(r => r.indexInLine + r.inline.StartIndex)) : (-1, null);
		}

		if (_desiredSize.Height < p.Y)
		{
			var lastRun = _lines[^1].runs.Count == 0 ? _lines[^2].runs.MaxBy(r => r.indexInLine + r.inline.StartIndex)! : _lines[^1].runs.MaxBy(r => r.indexInLine + r.inline.StartIndex)!;
			var index = _rtl
				? lastRun.inline.StartIndex + lastRun.startInInline
				: lastRun.inline.StartIndex + lastRun.endInInline - (ignoreEndingSpace ? TrailingWhiteSpaceCount(lastRun.inline.Text, lastRun.startInInline, lastRun.endInInline) : 0);
			return (index, lastRun);
		}

		foreach (var line in _lines)
		{
			if (line.y > p.Y || line.y + line.lineHeight < p.Y)
			{
				continue;
			}

			if (line.runs.Count == 0)
			{
				Debug.Assert(line == _lines[^1]);
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
							? firstRun.endInInline + firstRun.inline.StartIndex - (ignoreEndingSpace ? TrailingWhiteSpaceCount(firstRun.inline.Text, firstRun.startInInline, firstRun.endInInline) : 0)
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
							: lastRun.inline.StartIndex + lastRun.endInInline - (ignoreEndingSpace ? TrailingWhiteSpaceCount(lastRun.inline.Text, lastRun.startInInline, lastRun.endInInline) : 0);
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

		Debug.Assert(false, "This should be unreachable");
		return (-1, null);
	}

	public Hyperlink? GetHyperlinkAt(Point point)
	{
		var run = GetIndexAndRunAt(point, ignoreEndingSpace: false, extendedSelection: false).run;
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
		foreach (var line in _lines)
		{
			if (line.startInText <= index && (line.endInText > index || (line.lineIndex == _lines.Count - 1 && line.endInText == index)))
			{
				return (line.startInText, line.endInText - line.startInText, line.lineIndex == 0, line.lineIndex == _lines.Count - 1, line.lineIndex);
			}
		}
		throw new ArgumentOutOfRangeException("Given index is not within the range of text length.");
	}

	private static int TrailingWhiteSpaceCount(string str, int start, int end)
	{
		for (var i = end - 1; i >= start; i--)
		{
			if (str[i] != ' ' && str[i] != '\t' && str[i] != '\r' && str[i] != '\n')
			{
				return end - 1 - i;
			}
		}
		return end - start;
	}

	private static bool IsLineBreak(string text, int indexAfterLineBreakOpportunity)
	{
		// https://www.unicode.org/standard/reports/tr13/tr13-5.html

		if (indexAfterLineBreakOpportunity >= 2 && text[indexAfterLineBreakOpportunity - 2] == '\r' || text[indexAfterLineBreakOpportunity - 1] == '\n')
		{
			return true;
		}

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
