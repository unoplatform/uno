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

	private readonly Size _size;
	private readonly TextAlignment _textAlignment;
	private readonly bool _rtl;
	private readonly List<(float lineHeight, List<ShapedLineBrokenBidiRun> runs)> _lines;

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
			return;
		}
		// TODO: multiple inlines
		var textElement = new ReadonlyTextElementCopy((Run)inlines[0]);
		var text = textElement.Text;
		if (text.Length == 0)
		{
			_lines = new();
			desiredSize = new Size(0, defaultLineHeight);
			return;
		}

		var lineWidth = textWrapping == TextWrapping.NoWrap ? float.PositiveInfinity : (float)availableSize.Width;
		_lines = SplitTextIntoLines(textElement, lineWidth, lineWidth);

		var desiredHeight = _lines.Max(l => l.lineHeight);
		var desiredWidth = _lines.Sum(l => l.runs.Sum(r => RunWidth(r.glyphs, r.fontDetails)));
		desiredSize = new Size(desiredWidth, desiredHeight);

		// Console.WriteLine($"{desiredWidth}x{desiredHeight} ----------------------------------");
		// for (var index = 0; index < _lines.Count; index++)
		// {
		// 	var line = _lines[index];
		// 	Console.WriteLine($"LINE {{index}} height {line.lineHeight} ----------");
		// 	foreach (var (start, end, glyphs, fontDetails, rtl, visualOrderMajor, visualOrderMinor) in line.runs)
		// 	{
		// 		Console.WriteLine($"Run {visualOrderMajor}.{visualOrderMinor} rtl={rtl}: [{text[start..end]}]");
		// 	}
		// }
		// Console.WriteLine("END ORDERED LINE BROKEN RUNS ----------------------------------");
	}

	/// <returns>The runs of each run are sorted according to the visual order.</returns>
	private static List<(float lineHeight, List<ShapedLineBrokenBidiRun> runs)> SplitTextIntoLines(ReadonlyTextElementCopy textElement, float firstLineWidth, float lineWidth)
	{
		var text = textElement.Text;
		var bidi = new BiDi();
		bidi.SetPara(text, textElement.FlowDirection == FlowDirection.LeftToRight ? BiDi.DEFAULT_LTR : BiDi.DEFAULT_RTL, null);
		var runCount = bidi.CountRuns();
		var logicallyOrderedRuns = new BidiRun[runCount];
		// TODO: paragraphs
		for (var i = 0; i < runCount; i++)
		{
			var level = bidi.GetVisualRun(i, out var logicalStart, out var length);
			Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);
			logicallyOrderedRuns[i] = new BidiRun(textElement, logicalStart, logicalStart + length, level == BiDi.BiDiDirection.RTL);
		}
#if DEBUG
		Debug.Assert(logicallyOrderedRuns[0].start == 0);
		for (int i = 0; i < runCount - 1; i++)
		{
			Debug.Assert(logicallyOrderedRuns[i].end != logicallyOrderedRuns[i].start && logicallyOrderedRuns[i].end == logicallyOrderedRuns[i + 1].start);
		}
		Debug.Assert(logicallyOrderedRuns[^1].end != logicallyOrderedRuns[^1].start && logicallyOrderedRuns[^1].end == text.Length);
#endif
		logicallyOrderedRuns = logicallyOrderedRuns.OrderBy(r => r.start).ToArray();

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
			if (bidiRun.end <= nextLineBreakingOpportunity || remainingLineWidth >= runWidth)
			{
				currentLineEnd = bidiRun.end;
				remainingLineWidth -= runWidth;
				while (nextLineBreakingOpportunity <= bidiRun.end && nextLineBreakingOpportunityIndex < lineBreakingOpportunities.Count - 1)
				{
					nextLineBreakingOpportunityIndex++;
					nextLineBreakingOpportunity = lineBreakingOpportunities[nextLineBreakingOpportunityIndex];
				}
			}
			else if (bidiRun.end > nextLineBreakingOpportunity)
			{
				// TODO: end-of-line space hanging

				// Find the maximal substring of this bidi run that can fit on the line
				var partOnThisLine = bidiRun with { end = nextLineBreakingOpportunity };
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

		if (currentLineEnd != -1)
		{
			lineEnds.Add(currentLineEnd);
		}

		var ret = new List<(float lineHeight, List<ShapedLineBrokenBidiRun>)>();
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
			ret.Add((textElement.FontDetails.LineHeight, runs));
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

	public void Draw(in Visual.PaintingSession session, (int index, CompositionBrush brush)? caret,
		(int selectionStart, int selectionEnd, CompositionBrush brush)? selection, float caretThickness)
	{
		float currentLineY = 0;
		for (var index = 0; index < _lines.Count; index++)
		{
			var line = _lines[index];
			currentLineY += line.lineHeight;
			float currentLineX = 0;
			foreach (var run in line.runs)
			{
				using (var textBlobBuilder = new SKTextBlobBuilder())
				{
					var glyphs = new ushort[run.glyphs.Length];
					var positions = new SKPoint[run.glyphs.Length];
					float x = 0;
					for (var i = 0; i < run.glyphs.Length; i++)
					{
						var glyph = run.glyphs[i];
						glyphs[i] = (ushort)glyph.info.Codepoint;
						positions[i] = new SKPoint(x + glyph.position.XOffset * run.fontDetails.TextScale.textScaleX, glyph.position.YOffset * run.fontDetails.TextScale.textScaleY);
						x += glyph.position.XAdvance * run.fontDetails.TextScale.textScaleX;
					}

					textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
					session.Canvas.DrawText(textBlobBuilder.Build(), currentLineX, currentLineY, new SKPaint { Color = SKColors.Red });
					currentLineX += x;
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
