#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Text;
using HarfBuzzSharp;
using Icu;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents.TextFormatting;
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


internal readonly struct UnicodeText : IParsedText
{
	// A readonly snapshot of an Inline that is referenced by individual text runs after splitting. It's a class
	// and not a struct because we don't want to copy the same Inline for each run.
	private class ReadonlyInlineCopy
	{
		public int StartIndex { get; }
		public int EndIndex { get; }
		public string Text { get; }
		public FlowDirection FlowDirection { get; }
		public FontDetails FontDetails { get; }
		public double FontSize { get; }
		public FontWeight FontWeight { get; }
		public FontStretch FontStretch { get; }
		public FontStyle FontStyle { get; }

		public ReadonlyInlineCopy(Inline inline, int startIndex, FlowDirection defaultFlowDirection)
		{
			Debug.Assert(inline is Run or LineBreak);
			Text = inline.GetText();
			FlowDirection = (inline as Run)?.FlowDirection ?? defaultFlowDirection;
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
	private readonly record struct ShapedLineBrokenBidiRun(ReadonlyInlineCopy Inline, int inlineIndex, int inInlineIndexStart, int inInlineIndexEnd, (GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails fontDetails, bool rtl);
	private record LayoutedGlyphDetails(GlyphInfo info, GlyphPosition position, float xPosInRun, Cluster? cluster, LayoutedLineBrokenBidiRun parentRun)
	{
		public Cluster? cluster { get; set; } = cluster;
	}

	private record LayoutedLineBrokenBidiRun(ReadonlyInlineCopy inline, int inlineIndex, int inInlineIndexStart, int inInlineIndexEnd, float x, float y, float width, LayoutedGlyphDetails[] glyphs, FontDetails fontDetails, bool rtl, LayoutedLine line, int indexInLine)
	{
		public float width { get; set; } = width;
		public LayoutedLine line { get; set; } = line;
	}
	// runs are always sorted LTR even in RTL text
	// Each line must have at least one run except the very last line.
	private record LayoutedLine(float lineHeight, int lineIndex, float y, List<LayoutedLineBrokenBidiRun> runs);
	private record Cluster(int sourceTextStart, int sourceTextEnd, LayoutedLineBrokenBidiRun layoutedRun, int glyphInRunIndexStart, int glyphInRunIndexEnd);

	private readonly Size _size;
	private readonly TextAlignment _textAlignment;
	private readonly bool _rtl;
	private readonly List<ReadonlyInlineCopy> _inlines;
	private readonly List<LayoutedLine> _lines;
	private readonly Cluster[] _textIndexToGlyph;
	private readonly Size _desiredSize;
	private readonly int _textLength;

	private static readonly FieldInfo _biDiFieldInfo = typeof(BiDi).GetField("_biDi", BindingFlags.NonPublic | BindingFlags.Instance)!;
	private static readonly ubidi_setLineDelegate _setLine;
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void ubidi_setLineDelegate(IntPtr bidi, int start, int limit, IntPtr lineBiDi, out ErrorCode errorCode);

	static UnicodeText()
	{
		Wrapper.Verbose = true;
		Wrapper.Init();

		var icuNetAssembly = typeof(BiDi).Assembly;
		var nativeMethodsType = icuNetAssembly.GetType("Icu.NativeMethods")!;
		var getMethod = nativeMethodsType.GetMethod("GetMethod", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(ubidi_setLineDelegate));
		var icuCommonLibHandle = nativeMethodsType.GetProperty("IcuCommonLibHandle", BindingFlags.Static | BindingFlags.NonPublic)!;
		_setLine = (ubidi_setLineDelegate)getMethod.Invoke(null, [icuCommonLibHandle.GetValue(null), "ubidi_setLine", false])!;
	}

	internal UnicodeText(
		Size availableSize,
		Inline[] inlines, // only leaf nodes
		float defaultLineHeight,
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		TextAlignment textAlignment,
		TextWrapping textWrapping,
		FlowDirection flowDirection,
		out Size desiredSize)
	{
		_rtl = flowDirection == FlowDirection.RightToLeft;
		_size = availableSize;
		_textAlignment = textAlignment;

		_inlines = new();
		var acc = 0;
		foreach (var inline in inlines)
		{
			var copy = new ReadonlyInlineCopy(inline, acc, flowDirection);
			var length = copy.Text.Length;
			if (length != 0)
			{
				_inlines.Add(copy);
				acc += length;
			}
		}
		_textLength = acc;

		if (_inlines.Count == 0)
		{
			_lines = new();
			desiredSize = new Size(0, defaultLineHeight);
			_textIndexToGlyph = [];
			_inlines = [];
			return;
		}
		// TODO: multiple inlines
		var firstInline = _inlines[0];
		var text = firstInline.Text;
		if (text.Length == 0)
		{
			_lines = new();
			desiredSize = new Size(0, defaultLineHeight);
			_textIndexToGlyph = [];
			return;
		}

		var lineWidth = textWrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
		var unlayoutedLines = SplitTextIntoLines(firstInline, 0, lineWidth, lineWidth);
		_lines = LayoutLines(unlayoutedLines);
		_textIndexToGlyph = new Cluster[inlines.Sum(i => i.GetText().Length)];
		CreateSourceTextFromAndToGlyphMapping(_lines, _textIndexToGlyph);

		var desiredHeight = _lines.Sum(l => l.lineHeight);
		var desiredWidth = _lines.Max(l => l.runs.Sum(r => r.width));
		_desiredSize = desiredSize = new Size(desiredWidth, desiredHeight);
	}

	/// <returns>The runs of each run are sorted according to the visual order.</returns>
	private static List<List<ShapedLineBrokenBidiRun>> SplitTextIntoLines(ReadonlyInlineCopy inline, int textElementIndex, float firstLineWidth, float lineWidth)
	{
		var text = inline.Text;
		var bidi = new BiDi();
		bidi.SetPara(text, (byte)(inline.FlowDirection == FlowDirection.LeftToRight ? 0 : 1), null);

		var logicallyOrderedRuns = SplitTextIntoLogicallyOrderedBidiRuns(inline, bidi);
		var lineBreakingOpportunities = GetLineBreakingOpportunities(text);
		var lineEnds = ApplyLineBreaking(inline, firstLineWidth, lineWidth, lineBreakingOpportunities, logicallyOrderedRuns);
		var lines = ApplyBidiReordering(inline, textElementIndex, lineEnds, bidi);

		return lines;
	}

	private static List<List<ShapedLineBrokenBidiRun>> ApplyBidiReordering(ReadonlyInlineCopy inline, int textElementIndex, List<int> lineEnds, BiDi bidi)
	{
		var ret = new List<List<ShapedLineBrokenBidiRun>>();
		for (var lineIndex = 0; lineIndex < lineEnds.Count; lineIndex++)
		{
			var lineEnd = lineEnds[lineIndex];
			var lineStart = lineIndex > 0 ? lineEnds[lineIndex - 1] : 0;
			// The delegate for SetLine is incorrectly implemented and causes a segfault so we roll our own
			using var lineBidi = new BiDi();
			_setLine((IntPtr)_biDiFieldInfo.GetValue(bidi)!, lineStart, lineEnd, (IntPtr)_biDiFieldInfo.GetValue(lineBidi)!, out var error);
			if (error != ErrorCode.ZERO_ERROR)
			{
				throw new InvalidOperationException("LibICU's ubidi_setLine returned an error: " + error);
			}

			var runs = new List<ShapedLineBrokenBidiRun>();
			var runCount = lineBidi.CountRuns();
			for (var runIndex = 0; runIndex < runCount; runIndex++)
			{
				var beginningIndexInRunList = runs.Count;
				var level = lineBidi.GetVisualRun(runIndex, out var logicalStart, out var length);
				Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);

				var currentFontDetails = inline.FontDetails;
				var currentFontSplitStart = logicalStart;
				for (var i = logicalStart; i < logicalStart + length; i += char.IsSurrogate(inline.Text, lineStart + i) ? 2 : 1)
				{
					FontDetails newFontDetails;
					if (char.ConvertToUtf32(inline.Text, lineStart + i) is var codepoint && !inline.FontDetails.SKFont.ContainsGlyph(codepoint))
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
							runs.Add(new ShapedLineBrokenBidiRun(inline, textElementIndex, lineStart + currentFontSplitStart, lineStart + i, ShapeRun(inline.Text[(lineStart + currentFontSplitStart)..(lineStart + i)], level is BiDi.BiDiDirection.RTL, currentFontDetails.Font), currentFontDetails, level is BiDi.BiDiDirection.RTL));
						}
						currentFontDetails = newFontDetails;
						currentFontSplitStart = i;
					}
				}
				runs.Add(new ShapedLineBrokenBidiRun(inline, textElementIndex, lineStart + currentFontSplitStart, lineStart + logicalStart + length, ShapeRun(inline.Text[(lineStart + currentFontSplitStart)..(lineStart + logicalStart + length)], level is BiDi.BiDiDirection.RTL, currentFontDetails.Font), currentFontDetails, level is BiDi.BiDiDirection.RTL));
				// swap runs if rtl
				if (level is BiDi.BiDiDirection.RTL)
				{
					for (var i = 0; i < (runs.Count - beginningIndexInRunList) / 2; i++)
					{
						(runs[beginningIndexInRunList + i], runs[^(1 + i)]) = (runs[^(1 + i)], runs[beginningIndexInRunList + i]);
					}
				}
			}
			ret.Add(runs);
		}

		return ret;
	}

	private static List<int> ApplyLineBreaking(ReadonlyInlineCopy inline, float firstLineWidth, float lineWidth, List<int> lineBreakingOpportunities, List<BidiRun> logicallyOrderedRuns)
	{
		var lineEnds = new List<int>();
		var currentLineEnd = -1;
		var nextLineBreakingOpportunityIndex = 0;
		var nextLineBreakingOpportunity = lineBreakingOpportunities[0];
		var remainingLineWidth = firstLineWidth;
		var stack = new Stack<BidiRun>();
		for (var index = 0; stack.Count > 0 || index < logicallyOrderedRuns.Count;)
		{
			var bidiRun = stack.Count > 0 ? stack.Pop() : logicallyOrderedRuns[index++];
			var testLineBreakingOpportunityIndex = nextLineBreakingOpportunityIndex;
			// TODO: could we get away with something faster?
			while (testLineBreakingOpportunityIndex < bidiRun.endInInline && testLineBreakingOpportunityIndex < lineBreakingOpportunities.Count - 1)
			{
				var testLineBreakingOpportunity = lineBreakingOpportunities[testLineBreakingOpportunityIndex];
				if (IsLineBreak(bidiRun.inline.Text, testLineBreakingOpportunity))
				{
					stack.Push(bidiRun with { startInInline = testLineBreakingOpportunity });
					bidiRun = bidiRun with { endInInline = testLineBreakingOpportunity };
					break;
				}
				else
				{
					testLineBreakingOpportunityIndex++;
				}
			}

			var glyphs = ShapeRun(bidiRun.inline.Text[bidiRun.startInInline..bidiRun.endInInline], bidiRun.rtl, bidiRun.fontDetails.Font);
			var runWidth = RunWidth(glyphs, inline.FontDetails);
			if (remainingLineWidth >= runWidth)
			{
				currentLineEnd = bidiRun.endInInline;
				remainingLineWidth -= runWidth;

				while (nextLineBreakingOpportunity <= bidiRun.endInInline && nextLineBreakingOpportunityIndex < lineBreakingOpportunities.Count - 1)
				{
					var nextLineBreakingOpportunityIsLineBreak = IsLineBreak(bidiRun.inline.Text, nextLineBreakingOpportunity);
					if (nextLineBreakingOpportunityIsLineBreak)
					{
						currentLineEnd = nextLineBreakingOpportunity;
						MoveToNextLine(lineWidth, lineEnds, ref currentLineEnd, ref remainingLineWidth);
					}

					nextLineBreakingOpportunityIndex++;
					nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];

					if (nextLineBreakingOpportunityIsLineBreak)
					{
						break;
					}
				}
			}
			else if (currentLineEnd != -1 && bidiRun.endInInline <= nextLineBreakingOpportunity)
			{
				MoveToNextLine(lineWidth, lineEnds, ref currentLineEnd, ref remainingLineWidth);
				stack.Push(bidiRun);
			}
			else
			{
				// TODO: end-of-line space hanging

				// Find the maximal substring of this bidi run that can fit on the line
				var partOnThisLine = bidiRun with { endInInline = nextLineBreakingOpportunity };
				var partOnThisLineGlyphs = ShapeRun(bidiRun.inline.Text[partOnThisLine.startInInline..partOnThisLine.endInInline], partOnThisLine.rtl, partOnThisLine.fontDetails.Font);
				var partOnThisLineWidth = RunWidth(partOnThisLineGlyphs, inline.FontDetails);
				if (currentLineEnd != -1 && partOnThisLineWidth > remainingLineWidth)
				{
					MoveToNextLine(lineWidth, lineEnds, ref currentLineEnd, ref remainingLineWidth);
					stack.Push(bidiRun);
				}
				else
				{
					nextLineBreakingOpportunityIndex++;
					nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];

					while (true)
					{
						var attemptPartOnThisLine = bidiRun with { endInInline = nextLineBreakingOpportunity };
						var attemptPartOnThisLineGlyphs = ShapeRun(bidiRun.inline.Text[attemptPartOnThisLine.startInInline..attemptPartOnThisLine.endInInline], attemptPartOnThisLine.rtl, attemptPartOnThisLine.fontDetails.Font);
						var attemptPartOnThisLineWidth = RunWidth(attemptPartOnThisLineGlyphs, inline.FontDetails);
						if (attemptPartOnThisLineWidth > remainingLineWidth)
						{
							break;
						}
						else
						{
							partOnThisLine = attemptPartOnThisLine;
							nextLineBreakingOpportunityIndex++;
							nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];
						}
					}

					currentLineEnd = partOnThisLine.endInInline;
					MoveToNextLine(lineWidth, lineEnds, ref currentLineEnd, ref remainingLineWidth);
					stack.Push(bidiRun with { startInInline = partOnThisLine.endInInline });
				}
			}
		}

		if (currentLineEnd != -1)
		{
			lineEnds.Add(currentLineEnd);
		}

		return lineEnds;

		static void MoveToNextLine(float lineWidth, List<int> lineEnds, ref int currentLineEnd, ref float remainingLineWidth)
		{
			lineEnds.Add(currentLineEnd);
			currentLineEnd = -1;
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

	private static List<BidiRun> SplitTextIntoLogicallyOrderedBidiRuns(ReadonlyInlineCopy inline, BiDi bidi)
	{
		var runCount = bidi.CountRuns();
		var logicallyOrderedRuns = new List<BidiRun>(runCount);
		// TODO: paragraphs
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

				if (newFontDetails != currentFontDetails)
				{
					if (i != logicalStart)
					{
						logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, i, level == BiDi.BiDiDirection.RTL, currentFontDetails));
					}
					currentFontDetails = newFontDetails;
					currentFontSplitStart = i;
				}
			}
			logicallyOrderedRuns.Add(new BidiRun(inline, currentFontSplitStart, logicalStart + length, level == BiDi.BiDiDirection.RTL, currentFontDetails));
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

	private static (GlyphInfo info, GlyphPosition position)[] ShapeRun(string textRun, bool rtl, Font font)
	{
		Debug.Assert(textRun.Length < 2 || !textRun[..^2].Contains("\r\n"));
		using var buffer = new Buffer();
		buffer.AddUtf16(textRun, 0, textRun is [.., '\r', '\n'] ? textRun.Length - 1 : textRun.Length);
		buffer.GuessSegmentProperties();
		buffer.Direction = rtl ? Direction.RightToLeft : Direction.LeftToRight;
		// TODO: ligatures
		font.Shape(buffer, new Feature(new Tag('l', 'i', 'g', 'a'), 0));
		var positions = buffer.GetGlyphPositionSpan();
		var infos = buffer.GetGlyphInfoSpan();
		var count = buffer.Length;
		var ret = new (GlyphInfo, GlyphPosition)[count];
		for (var i = 0; i < count; i++)
		{
			ret[i] = (infos[i], positions[i]);
		}

		if (IsLineBreak(textRun, textRun.Length) && infos[^1].Cluster == textRun.Length - 2)
		{
			ret[^1] = (infos[^1] with { Codepoint = 0 }, positions[^1] with { XAdvance = 0 });
		}
		return ret;
	}

	private static List<LayoutedLine> LayoutLines(List<List<ShapedLineBrokenBidiRun>> lines)
	{
		var layoutedLines = new List<LayoutedLine>();
		float currentLineY = 0;
		for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
		{
			var line = lines[lineIndex];
			var layoutedRuns = new List<LayoutedLineBrokenBidiRun>(line.Count);
			float currentLineX = 0;
			for (var runIndex = 0; runIndex < line.Count; runIndex++)
			{
				var run = line[runIndex];
				float runX = 0;
				var glyphs = new LayoutedGlyphDetails[run.glyphs.Length];
				var layoutedRun = new LayoutedLineBrokenBidiRun(run.Inline, run.inlineIndex, run.inInlineIndexStart, run.inInlineIndexEnd, currentLineX, currentLineY, default, glyphs, run.fontDetails, run.rtl, null!, runIndex);
				layoutedRuns.Add(layoutedRun);
				for (var i = 0; i < glyphs.Length; i++)
				{
					var glyph = run.glyphs[i];
					glyphs[i] = new LayoutedGlyphDetails(glyph.info, glyph.position, runX, default, layoutedRun);
					runX += GlyphWidth(glyph.position, run.fontDetails);
				}

				layoutedRun.width = runX;
				currentLineX += runX;
			}

			// TODO: line stacking strategy
			var lineHeight = line.Max(r => r.fontDetails.LineHeight);
			var layoutedLine = new LayoutedLine(lineHeight, lineIndex, currentLineY, layoutedRuns);
			layoutedLines.Add(layoutedLine);
			layoutedRuns.ForEach(r => r.line = layoutedLine);
			currentLineY += lineHeight;
		}

		return layoutedLines;
	}

	// TODO: we're using harfbuzz clusters as the units for clustering/"atomization" but should we use Unicode's text segmentation algorithm instead?
	// and how are they different? It seems from the HarfBuzz docs that HarfBuzz clustering by default approximates Unicode's text segmentation
	// https://harfbuzz.github.io/working-with-harfbuzz-clusters.html
	// https://unicode.org/reports/tr29
	private void CreateSourceTextFromAndToGlyphMapping(List<LayoutedLine> lines, Cluster[] textIndexToGlyphMap)
	{
		foreach (var line in lines)
		{
			foreach (var run in line.runs)
			{
				var runGlyphLength = run.glyphs.Length;
				var runStart = run.inInlineIndexStart + run.inline.StartIndex;
				var runLength = run.inInlineIndexEnd - run.inInlineIndexStart;

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

	public void Draw(in Visual.PaintingSession session, (int index, CompositionBrush brush)? caret,
		(int selectionStart, int selectionEnd, CompositionBrush brush)? selection, float caretThickness)
	{
		for (var index = 0; index < _lines.Count; index++)
		{
			var line = _lines[index];
			float currentLineX = 0;
			foreach (var run in line.runs)
			{
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

					textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
					session.Canvas.DrawText(textBlobBuilder.Build(), currentLineX, line.lineHeight - run.fontDetails.SKFontMetrics.Descent, new SKPaint { Color = SKColors.Red });
					currentLineX += run.width;
				}
			}
		}
	}

	private static float RunWidth((GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails details) => glyphs.Sum(g => GlyphWidth(g.position, details));
	private static float GlyphWidth(GlyphPosition position, FontDetails details) => position.XAdvance * details.TextScale.textScaleX;

	public Rect GetRectForIndex(int index)
	{
		var cluster = _textIndexToGlyph[index];
		var glyphs = cluster.layoutedRun.glyphs[cluster.glyphInRunIndexStart..cluster.glyphInRunIndexEnd];

		var x = glyphs[0].xPosInRun + cluster.layoutedRun.x;
		var y = cluster.layoutedRun.line.y;
		var width = glyphs.Sum(g => GlyphWidth(g.position, cluster.layoutedRun.fontDetails));
		var height = cluster.layoutedRun.line.lineHeight;
		return new Rect(x, y, width, height);
	}

	public int GetIndexAt(Point p, bool ignoreEndingSpace, bool extendedSelection)
	{
		// TODO: ignoreEndingSpace
		if (_lines.Count == 0 || p.Y < 0)
		{
			return extendedSelection ? 0 : -1;
		}

		if (_desiredSize.Height < p.Y)
		{
			return extendedSelection ? _textLength : -1;
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
				return extendedSelection ? _textLength : -1;
			}

			if (line.runs[0] is var firstRun && firstRun.x > p.X)
			{
				if (!extendedSelection)
				{
					return -1;
				}
				else
				{
					return _rtl
						? firstRun.inInlineIndexEnd + firstRun.inline.StartIndex - TrailingWhiteSpaceCount(firstRun.inline.Text, firstRun.inInlineIndexStart, firstRun.inInlineIndexEnd)
						: firstRun.inInlineIndexStart + firstRun.inline.StartIndex;
				}
			}
			if (line.runs[^1] is var lastRun && lastRun.x + lastRun.width < p.X)
			{
				if (!extendedSelection)
				{
					return -1;
				}
				else
				{
					return _rtl
						? lastRun.inline.StartIndex + lastRun.inInlineIndexStart
						: lastRun.inline.StartIndex + lastRun.inInlineIndexEnd - TrailingWhiteSpaceCount(firstRun.inline.Text, firstRun.inInlineIndexStart, firstRun.inInlineIndexEnd);
				}
			}

			foreach (var run in line.runs)
			{
				if (run.x > p.X || run.x + run.width < p.X)
				{
					continue;
				}

				foreach (var glyph in run.glyphs)
				{
					var globalGlyphX = glyph.xPosInRun + run.x;
					var width = GlyphWidth(glyph.position, run.fontDetails);
					if (globalGlyphX <= p.X && globalGlyphX + width >= p.X)
					{
						var closerToLeft = p.X - globalGlyphX < globalGlyphX + width - p.X;
						return (closerToLeft && !run.rtl) || (!closerToLeft && run.rtl) ? glyph.cluster!.sourceTextStart : glyph.cluster!.sourceTextEnd;
					}
				}
			}
		}

		Debug.Assert(false, "This should be unreachable");
		return -1;
	}

	public Hyperlink GetHyperlinkAt(Point point) => throw new System.NotImplementedException();

	public (int start, int length) GetWordAt(int index, bool right) => throw new System.NotImplementedException();

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index) => throw new System.NotImplementedException();

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
}
