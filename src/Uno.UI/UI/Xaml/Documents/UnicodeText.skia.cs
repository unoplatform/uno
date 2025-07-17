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
	// A readonly snapshot of a TextElement that is referenced by individual text runs after splitting. It's a class
	// and not a struct because we don't want to copy the same TextElement for each run.
	private class ReadonlyTextElementCopy
	{
		public string Text { get; }
		public FlowDirection FlowDirection { get; }
		public FontDetails FontDetails { get; }
		public double FontSize { get; }
		public FontWeight FontWeight { get; }
		public FontStretch FontStretch { get; }
		public FontStyle FontStyle { get; }

		public ReadonlyTextElementCopy(Run run)
		{
			Text = run.Text;
			FlowDirection = run.FlowDirection;
			FontDetails = run.FontInfo;
			FontSize = run.FontSize;
			FontWeight = run.FontWeight;
			FontStretch = run.FontStretch;
			FontStyle = run.FontStyle;
		}
	}
	// A BidiRun run split at line break boundaries
	private readonly record struct BidiRun(ReadonlyTextElementCopy textElement, int start, int end, bool rtl);
	// FontDetails might be different from textElement.FontDetails because of font fallback
	private readonly record struct ShapedLineBrokenBidiRun(ReadonlyTextElementCopy textElement, int start, int end, (GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails fontDetails, bool rtl);
	private record LayoutedGlyphDetails(GlyphInfo info, GlyphPosition position, float xPosInRun, float y, int sourceTextStart, int sourceTextEnd, LayoutedLineBrokenBidiRun parentRun);
	private record LayoutedLineBrokenBidiRun(ReadonlyTextElementCopy textElement, int start, int end, float x, float y, float width, LayoutedGlyphDetails[] glyphs, FontDetails fontDetails, bool rtl, int indexInLine)
	{
		public float width { get; set; } = width;
	}
	private record LayoutedLine(float lineHeight, int lineIndex, List<LayoutedLineBrokenBidiRun> runs);

	private readonly Size _size;
	private readonly TextAlignment _textAlignment;
	private readonly bool _rtl;
	private readonly List<LayoutedLine> _lines;
	private readonly LayoutedGlyphDetails[] _textIndexToGlyph;

	private static readonly ubidi_setLineDelegate SetLine;
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
		SetLine = (ubidi_setLineDelegate)getMethod.Invoke(null, [icuCommonLibHandle.GetValue(null), "ubidi_setLine", false])!;
	}

	internal UnicodeText(
		Size availableSize,
		Inline[] inlines, // traversed pre-orderly
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

		if (inlines.Length == 0)
		{
			_lines = new();
			desiredSize = new Size(0, defaultLineHeight);
			_textIndexToGlyph = [];
			return;
		}
		// TODO: multiple inlines
		var textElement = new ReadonlyTextElementCopy((Run)inlines[0]);
		var text = textElement.Text;
		if (text.Length == 0)
		{
			_lines = new();
			desiredSize = new Size(0, defaultLineHeight);
			_textIndexToGlyph = [];
			return;
		}

		var lineWidth = textWrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
		var unlayoutedLines = SplitTextIntoLines(textElement, lineWidth, lineWidth);
		_lines = LayoutLines(unlayoutedLines);
		_textIndexToGlyph = CreateSourceTextFromAndToGlyphMapping(_lines);

		var desiredHeight = _lines.Sum(l => l.lineHeight);
		var desiredWidth = _lines.Max(l => l.runs.Sum(r => r.width));
		desiredSize = new Size(desiredWidth, desiredHeight);
	}

	/// <returns>The runs of each run are sorted according to the visual order.</returns>
	private static List<List<ShapedLineBrokenBidiRun>> SplitTextIntoLines(ReadonlyTextElementCopy textElement, float firstLineWidth, float lineWidth)
	{
		var text = textElement.Text;
		var bidi = new BiDi();
		bidi.SetPara(text, (byte)(textElement.FlowDirection == FlowDirection.LeftToRight ? 0 : 1), null);
		var runCount = bidi.CountRuns();
		var logicallyOrderedRuns = new BidiRun[runCount];
		// TODO: paragraphs
		for (var i = 0; i < runCount; i++)
		{
			var level = bidi.GetVisualRun(i, out var logicalStart, out var length);
			Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);
			logicallyOrderedRuns[i] = new BidiRun(textElement, logicalStart, logicalStart + length, level == BiDi.BiDiDirection.RTL);
		}
		logicallyOrderedRuns = logicallyOrderedRuns.OrderBy(r => r.start).ToArray();
#if DEBUG
		Debug.Assert(logicallyOrderedRuns[0].start == 0);
		for (int i = 0; i < runCount - 1; i++)
		{
			Debug.Assert(logicallyOrderedRuns[i].end != logicallyOrderedRuns[i].start && logicallyOrderedRuns[i].end == logicallyOrderedRuns[i + 1].start);
		}
		Debug.Assert(logicallyOrderedRuns[^1].end != logicallyOrderedRuns[^1].start && logicallyOrderedRuns[^1].end == text.Length);
#endif

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

		var lineEnds = new List<int>();
		var currentLineEnd = -1;
		var nextLineBreakingOpportunityIndex = 0;
		var nextLineBreakingOpportunity = lineBreakingOpportunities[0];
		var remainingLineWidth = firstLineWidth;
		for (var index = 0; index < logicallyOrderedRuns.Length; index++)
		{
			var bidiRun = logicallyOrderedRuns[index];
			var glyphs = ShapeRun(bidiRun.textElement.Text[bidiRun.start..bidiRun.end], bidiRun.rtl,
				textElement.FontDetails.Font);
			var runWidth = RunWidth(glyphs, textElement.FontDetails);
			if (remainingLineWidth >= runWidth)
			{
				currentLineEnd = bidiRun.end;
				remainingLineWidth -= runWidth;
				while (nextLineBreakingOpportunity <= bidiRun.end && nextLineBreakingOpportunityIndex < lineBreakingOpportunities.Count - 1)
				{
					nextLineBreakingOpportunityIndex++;
					nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];
				}
			}
			else if (currentLineEnd != -1 && bidiRun.end <= nextLineBreakingOpportunity)
			{
				lineEnds.Add(currentLineEnd);
				currentLineEnd = -1;
				remainingLineWidth = lineWidth;
				index--;
			}
			else
			{
				// TODO: end-of-line space hanging

				// Find the maximal substring of this bidi run that can fit on the line
				var partOnThisLine = bidiRun with { end = nextLineBreakingOpportunity };
				var partOnThisLineGlyphs = ShapeRun(bidiRun.textElement.Text[partOnThisLine.start..partOnThisLine.end], partOnThisLine.rtl, textElement.FontDetails.Font);
				var partOnThisLineWidth = RunWidth(partOnThisLineGlyphs, textElement.FontDetails);
				if (partOnThisLineWidth > remainingLineWidth)
				{
					lineEnds.Add(currentLineEnd);
					currentLineEnd = -1;
					remainingLineWidth = lineWidth;
					index--;
				}
				else
				{
					nextLineBreakingOpportunityIndex++;
					nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];

					while (true)
					{
						var attemptPartOnThisLine = bidiRun with { end = nextLineBreakingOpportunity };
						var attemptPartOnThisLineGlyphs = ShapeRun(bidiRun.textElement.Text[attemptPartOnThisLine.start..attemptPartOnThisLine.end], attemptPartOnThisLine.rtl, textElement.FontDetails.Font);
						var attemptPartOnThisLineWidth = RunWidth(attemptPartOnThisLineGlyphs, textElement.FontDetails);
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

					currentLineEnd = partOnThisLine.end;
					lineEnds.Add(currentLineEnd);
					currentLineEnd = -1;
					remainingLineWidth = lineWidth;

					logicallyOrderedRuns[index] = bidiRun with { start = partOnThisLine.end };
					index--;
				}
			}
		}

		if (currentLineEnd != -1)
		{
			lineEnds.Add(currentLineEnd);
		}

		var ret = new List<List<ShapedLineBrokenBidiRun>>();
		for (var lineIndex = 0; lineIndex < lineEnds.Count; lineIndex++)
		{
			var lineEnd = lineEnds[lineIndex];
			var lineStart = lineIndex > 0 ? lineEnds[lineIndex - 1] : 0;
			// The delegate for SetLine is incorrectly implemented and causes a segfault so we roll our own
			using var lineBidi = new BiDi();
			SetLine((IntPtr)typeof(BiDi).GetField("_biDi", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bidi)!, lineStart, lineEnd, (IntPtr)typeof(BiDi).GetField("_biDi", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(lineBidi)!, out var error);
			if (error != ErrorCode.ZERO_ERROR)
			{
				throw new InvalidOperationException("LibICU's ubidi_setLine returned an error: " + error);
			}
			var runs = new List<ShapedLineBrokenBidiRun>();
			var c = lineBidi.CountRuns();
			for (var i = 0; i < c; i++)
			{
				var level = lineBidi.GetVisualRun(i, out var logicalStart, out var length);
				Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);
				runs.Add(new ShapedLineBrokenBidiRun(textElement, lineStart + logicalStart, lineStart + logicalStart + length, ShapeRun(textElement.Text[(lineStart + logicalStart)..(lineStart + logicalStart + length)], level is BiDi.BiDiDirection.RTL, textElement.FontDetails.Font), textElement.FontDetails, level is BiDi.BiDiDirection.RTL));
			}
			ret.Add(runs);
		}

		return ret;
	}

	private static (GlyphInfo info, GlyphPosition position)[] ShapeRun(string textRun, bool rtl, Font font)
	{
		using var buffer = new Buffer();
		buffer.AddUtf16(textRun);
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
				var layoutedRun = new LayoutedLineBrokenBidiRun(run.textElement, run.start, run.end, currentLineX, currentLineY, default, glyphs, run.fontDetails, run.rtl, runIndex);
				layoutedRuns.Add(layoutedRun);
				for (var i = 0; i < glyphs.Length; i++)
				{
					var glyph = run.glyphs[i];
					glyphs[i] = new LayoutedGlyphDetails(glyph.info, glyph.position, runX, currentLineY, default, default, layoutedRun);
					runX += glyph.position.XAdvance * run.fontDetails.TextScale.textScaleX;
				}

				layoutedRun.width = runX;
				currentLineX += runX;
			}

			// TODO: line stacking strategy
			var lineHeight = line.Max(r => r.fontDetails.LineHeight);
			currentLineY += lineHeight;
			layoutedLines.Add(new LayoutedLine(lineHeight, lineIndex, layoutedRuns));
		}

		return layoutedLines;
	}

	private LayoutedGlyphDetails[] CreateSourceTextFromAndToGlyphMapping(List<LayoutedLine> lines)
	{
		return [];
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
						positions[i] = new SKPoint(glyph.xPosInRun + glyph.position.XOffset * run.fontDetails.TextScale.textScaleX, glyph.y + glyph.position.YOffset * run.fontDetails.TextScale.textScaleY);
					}

					textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
					session.Canvas.DrawText(textBlobBuilder.Build(), currentLineX, line.lineHeight, new SKPaint { Color = SKColors.Red });
					currentLineX += run.width;
				}
			}
		}
	}

	private static float RunWidth((GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails details) => glyphs.Sum(g => g.position.XAdvance * details.TextScale.textScaleX);

	public Rect GetRectForIndex(int adjustedIndex) => throw new System.NotImplementedException();

	public int GetIndexAt(Point p, bool ignoreEndingSpace, bool extendedSelection) => throw new System.NotImplementedException();

	public Hyperlink GetHyperlinkAt(Point point) => throw new System.NotImplementedException();

	public (int start, int length) GetWordAt(int index, bool right) => throw new System.NotImplementedException();

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index) => throw new System.NotImplementedException();
}
