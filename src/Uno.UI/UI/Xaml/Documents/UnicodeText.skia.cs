#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
	private readonly record struct LineBrokenBidiRun(ReadonlyTextElementCopy textElement, int start, int end, bool rtl);
	// FontDetails might be different from textElement.FontDetails because of font fallback
	private readonly record struct ShapedLineBrokenBidiRun(ReadonlyTextElementCopy textElement, int start, int end, (GlyphInfo info, GlyphPosition position)[] glyphs, FontDetails fontDetails, bool rtl);

	private readonly Size _size;
	private readonly TextAlignment _textAlignment;
	private readonly bool _rtl;
	private readonly List<(float lineHeight, List<ShapedLineBrokenBidiRun> runs)> _lines;

	static UnicodeText()
	{
		Wrapper.Verbose = true;
		Wrapper.Init();
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

		var logicallyOrderedLineBrokenRuns = SplitTextIntoBidiRunsBrokenAtLineBoundaries(textElement);
		var shapedLogicallyOrderedLineBrokenRuns = ShapedLogicallyOrderedLineBrokenRuns(logicallyOrderedLineBrokenRuns);
		var lines = textWrapping == TextWrapping.NoWrap
			? [shapedLogicallyOrderedLineBrokenRuns]
			: SplitIntoLines(text, shapedLogicallyOrderedLineBrokenRuns, textElement.FontDetails, (float)availableSize.Width, (float)availableSize.Width, textWrapping == TextWrapping.Wrap);

		// TODO: line stacking strategy
		_lines = lines.Select(runs =>
		{
			return (runs.Max(r => r.fontDetails.LineHeight), runs);
		}).ToList();

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

	private static List<LineBrokenBidiRun> SplitTextIntoBidiRunsBrokenAtLineBoundaries(ReadonlyTextElementCopy textElement)
	{
		var text = textElement.Text;
		var bidi = new BiDi();
		bidi.SetPara(text, textElement.FlowDirection == FlowDirection.LeftToRight ? BiDi.DEFAULT_LTR : BiDi.DEFAULT_RTL, null);
		var runCount = bidi.CountRuns();
		var logicallyOrderedRuns = new LineBrokenBidiRun[runCount];
		for (var i = 0; i < runCount; i++)
		{
			var level = bidi.GetVisualRun(i, out var logicalStart, out var length);
			Debug.Assert(level is BiDi.BiDiDirection.RTL or BiDi.BiDiDirection.LTR);
			logicallyOrderedRuns[i] = new LineBrokenBidiRun(textElement, logicalStart, logicalStart + length, level == BiDi.BiDiDirection.RTL);
		}
#if DEBUG
		Debug.Assert(logicallyOrderedRuns[0].start == 0);
		for (int i = 0; i < runCount - 1; i++)
		{
			Debug.Assert(logicallyOrderedRuns[i].end != logicallyOrderedRuns[i].start && logicallyOrderedRuns[i].end == logicallyOrderedRuns[i + 1].start);
		}
		Debug.Assert(logicallyOrderedRuns[^1].end != logicallyOrderedRuns[^1].start && logicallyOrderedRuns[^1].end == text.Length);
#endif
		Array.Sort(logicallyOrderedRuns);

		var lineBoundaries =
			// TODO: locale?
			BreakIterator.GetBoundaries(BreakIterator.UBreakIteratorType.LINE, new Locale("en", "US"), text)
				.SelectMany<Boundary, Boundary>(boundary =>
				{
					// make sequences of spaces their own run
					var beginningSpaces = 0;
					for (int i = boundary.Start; i < boundary.End; i++)
					{
						if (text[i] == ' ')
						{
							beginningSpaces++;
						}
						else
						{
							break;
						}
					}

					if (beginningSpaces == boundary.End - boundary.Start)
					{
						return [boundary];
					}

					var endingSpaces = 0;
					for (int i = boundary.End - 1; i >= boundary.Start; i--)
					{
						if (text[i] == ' ')
						{
							endingSpaces++;
						}
						else
						{
							break;
						}
					}

					return beginningSpaces == 0
						? endingSpaces == 0
							? [new Boundary(boundary.Start, boundary.End)]
							: [new Boundary(boundary.Start, boundary.End - endingSpaces), new Boundary(boundary.End - endingSpaces, boundary.End)]
						: endingSpaces == 0
							? [new Boundary(boundary.Start, boundary.Start + beginningSpaces), new Boundary(boundary.Start + beginningSpaces, boundary.End)]
							: [new Boundary(boundary.Start, boundary.Start + beginningSpaces), new Boundary(boundary.Start + beginningSpaces, boundary.End - endingSpaces), new Boundary(boundary.End - endingSpaces, boundary.End)];
				})
				.ToArray();

#if DEBUG
		Debug.Assert(lineBoundaries[0].Start == 0);
		for (int i = 0; i < lineBoundaries.Length - 1; i++)
		{
			Debug.Assert(lineBoundaries[i].End != lineBoundaries[i].Start && lineBoundaries[i].End == lineBoundaries[i + 1].Start);
		}
		Debug.Assert(lineBoundaries[^1].End != lineBoundaries[^1].Start && lineBoundaries[^1].End == text.Length);
#endif

		// We take the intersecion of line boundary runs and bidi runs. a Line boundary can have multiple bidi runs and
		// a bidi run can have multiple line boundaries, so neither of the run types is a superset of the other.
		Debug.Assert(lineBoundaries[0].Start == 0 && logicallyOrderedRuns[0].start == 0);
		var logicallyOrderedLineBrokenRuns = new List<LineBrokenBidiRun>();
		var currentLineBoundaryIndex = 0;
		var currentBidiRunIndex = 0;
		var lineRun = lineBoundaries[currentLineBoundaryIndex];
		var currentBidiRun = logicallyOrderedRuns[currentBidiRunIndex];
		while (currentBidiRunIndex < logicallyOrderedRuns.Length && currentLineBoundaryIndex < lineBoundaries.Length)
		{
			if (lineRun.End >= currentBidiRun.end)
			{
				logicallyOrderedLineBrokenRuns.Add(currentBidiRun);
				if (lineRun.End == currentBidiRun.end)
				{
					currentLineBoundaryIndex++;
					if (currentLineBoundaryIndex < lineBoundaries.Length)
					{
						lineRun = lineBoundaries[currentLineBoundaryIndex];
					}
				}
				currentBidiRunIndex++;
				if (currentBidiRunIndex < logicallyOrderedRuns.Length)
				{
					currentBidiRun = logicallyOrderedRuns[currentBidiRunIndex];
				}
			}
			else
			{
				logicallyOrderedLineBrokenRuns.Add(currentBidiRun with { end = lineRun.End });
				currentBidiRun = currentBidiRun with { start = lineRun.End };
				currentLineBoundaryIndex++;
				if (currentLineBoundaryIndex < lineBoundaries.Length)
				{
					lineRun = lineBoundaries[currentLineBoundaryIndex];
				}
			}
		}
		Debug.Assert(currentLineBoundaryIndex == lineBoundaries.Length && currentBidiRunIndex == logicallyOrderedRuns.Length);

		return logicallyOrderedLineBrokenRuns;
	}

	private static List<ShapedLineBrokenBidiRun> ShapedLogicallyOrderedLineBrokenRuns(List<LineBrokenBidiRun> logicallyOrderedLineBrokenRuns)
	{
		var list = new List<ShapedLineBrokenBidiRun>();
		foreach (var run in logicallyOrderedLineBrokenRuns)
		{
			var currentFontDetails = run.textElement.FontDetails;
			var currentFontFallbackSplitStart = run.start;
			for (int i = run.start; i < run.end; i += char.IsSurrogate(run.textElement.Text, i) ? 2 : 1)
			{
				// TODO: Should the fallback font be used for the rest of the run, or only for the individual codepoint that's
				// missing from the primary font?
				FontDetails newFontDetails;
				if (char.ConvertToUtf32(run.textElement.Text, i) is var codepoint && !run.textElement.FontDetails.SKFont.ContainsGlyph(codepoint))
				{
					newFontDetails = SKFontManager.Default.MatchCharacter(codepoint) is { } fallbackTypeface
						? FontDetailsCache.GetFont(fallbackTypeface.FamilyName, (float)run.textElement.FontSize, run.textElement.FontWeight, run.textElement.FontStretch, run.textElement.FontStyle).details
						: currentFontDetails;
				}
				else
				{
					newFontDetails = run.textElement.FontDetails;
				}

				if (newFontDetails != currentFontDetails)
				{
					if (currentFontFallbackSplitStart != run.start)
					{
						list.Add(new ShapedLineBrokenBidiRun(run.textElement, currentFontFallbackSplitStart, i, ShapeRun(run.textElement.Text[currentFontFallbackSplitStart..i], run.rtl, currentFontDetails.Font), currentFontDetails, run.rtl));
					}
					currentFontDetails = newFontDetails;
					currentFontFallbackSplitStart = i;
				}
			}
			list.Add(new ShapedLineBrokenBidiRun(run.textElement, currentFontFallbackSplitStart, run.end, ShapeRun(run.textElement.Text[run.start..run.end], run.rtl, currentFontDetails.Font), currentFontDetails, run.rtl));
		}

		return list;

		static (GlyphInfo info, GlyphPosition position)[] ShapeRun(string textRun, bool rtl, Font font)
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
	}

	private static List<List<ShapedLineBrokenBidiRun>> SplitIntoLines(string text, List<ShapedLineBrokenBidiRun> shapedLogicallyOrderedLineBrokenRuns, FontDetails runInlineFontInfo, float firstLineWidth, float lineWidth, bool breakMidWordWhenWordLargerThanEntireLine)
	{
		var ret = new List<List<ShapedLineBrokenBidiRun>>();
		var currentLineList = new List<ShapedLineBrokenBidiRun>();
		var remainingLineWidth = firstLineWidth;
		for (int i = 0; i < shapedLogicallyOrderedLineBrokenRuns.Count;)
		{
			var run = shapedLogicallyOrderedLineBrokenRuns[i];
			var runWidth = RunWidth(run.glyphs, run.fontDetails);
			if (runWidth > remainingLineWidth)
			{
				var isSpaceRun = text[run.start] == ' ';
				var wordIsLargerThanEntireLine = remainingLineWidth == lineWidth; // TODO: breakMidWordWhenWordLargerThanEntireLine
				if (wordIsLargerThanEntireLine || isSpaceRun)
				{
					currentLineList.Add(run);
					i++;
				}
				ret.Add(currentLineList);
				currentLineList = new List<ShapedLineBrokenBidiRun>();
				remainingLineWidth = lineWidth;
			}
			else
			{
				currentLineList.Add(run);
				i++;
				remainingLineWidth -= runWidth;
			}
		}

		if (currentLineList.Count != 0)
		{
			ret.Add(currentLineList);
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
			// var runs = line.runs.Order(new RunOrderComparer());
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
