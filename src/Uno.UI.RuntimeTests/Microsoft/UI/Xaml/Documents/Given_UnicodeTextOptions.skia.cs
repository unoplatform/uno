#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

// The UnicodeTextOptions inputs, which RichTextBlock's paragraph layout relies on, checked on the engine directly.
[TestClass]
[RunsOnUIThread]
public class Given_UnicodeTextOptions
{
	private const string Latin = "The quick brown fox jumps over the lazy dog while the five boxing wizards jump quickly";
	private const string Hebrew = "אבג דהוז חטי כלמנ סעפ צקרש תאב גדהו";
	private const string ArabicWord = "المستشفياتالعربيةالكبيرة";

	private sealed class Listener : UnicodeText.IFontCacheUpdateListener
	{
		public void Invalidate()
		{
		}
	}

	private static readonly Listener _listener = new();

	private static Run R(string text, FlowDirection? flowDirection = null)
	{
		var run = new Run { Text = text, FontSize = 20 };
		if (flowDirection is { } direction)
		{
			run.FlowDirection = direction;
		}
		return run;
	}

	internal static UnicodeText Layout(
		IEnumerable<Inline> inlines,
		double width,
		FlowDirection flowDirection = FlowDirection.LeftToRight,
		TextTrimming trimming = TextTrimming.None,
		UnicodeTextOptions options = default,
		TextWrapping wrapping = TextWrapping.Wrap)
	{
		var list = inlines.ToList();
		return new UnicodeText(
			new Size(width, double.PositiveInfinity),
			list,
			list[0].FontInfo,
			0,
			0,
			LineStackingStrategy.MaxHeight,
			TextLineBounds.Full,
			flowDirection,
			null,
			wrapping,
			trimming,
			false,
			_listener,
			false,
			0,
			null,
			null,
			null,
			false,
			false,
			out _,
			options: options);
	}

	// Fallback fonts load asynchronously, and a layout made before one arrives shapes with the inline's font. Waits until
	// two layouts made apart give the same glyphs, so the layouts a test compares use the same fonts.
	internal static async Task WaitForFonts(IEnumerable<Inline> inlines, double width, FlowDirection flowDirection = FlowDirection.LeftToRight)
	{
		for (var attempt = 0; attempt < 50; attempt++)
		{
			var before = Glyphs(Layout(inlines, width, flowDirection));
			await WindowHelper.WaitForIdle();
			await Task.Delay(100);
			if (before == Glyphs(Layout(inlines, width, flowDirection)))
			{
				return;
			}
		}

		Assert.Fail("The fonts never settled.");

		static string Glyphs(UnicodeText text)
			=> string.Join("|", Enumerable.Range(0, text.LineCount).SelectMany(i => text.GetLineForTests(i).Clusters).Select(c => string.Join(",", c.Glyphs)));
	}

	private static string Describe(UnicodeText.ClusterForTests cluster)
		=> $"[{cluster.Start},{cluster.End}) rtl={cluster.RightToLeft} x={cluster.X:0.###} w={cluster.Width:0.###} glyphs={string.Join(",", cluster.Glyphs)}";

	internal static void AssertLinesEqual(UnicodeText.LineForTests expected, UnicodeText.LineForTests actual, string context)
	{
		Assert.AreEqual(expected.Start, actual.Start, $"{context}: start");
		Assert.AreEqual(expected.End, actual.End, $"{context}: end");
		Assert.AreEqual(expected.Width, actual.Width, 0.001, $"{context}: width");
		Assert.AreEqual(expected.WidthWithoutTrailingSpaces, actual.WidthWithoutTrailingSpaces, 0.001, $"{context}: width without trailing spaces");
		Assert.AreEqual(expected.Height, actual.Height, 0.001, $"{context}: height");
		Assert.AreEqual(expected.Baseline, actual.Baseline, 0.001, $"{context}: baseline");
		Assert.AreEqual(
			string.Join(" ", expected.Clusters.Select(Describe)),
			string.Join(" ", actual.Clusters.Select(Describe)),
			$"{context}: clusters in visual order");
	}

	private static int HardBreaksBefore(string text, int index)
		=> Enumerable.Range(0, index).Count(i => text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')));

	// The unwrapped lines before a resume index: one per hard break before it, plus the one the index ends, unless a hard
	// break ends right there.
	private static int PrefixLines(string text, int index)
		=> HardBreaksBefore(text, index) + (text[index - 1] is '\n' or '\r' ? 0 : 1);

	// Lays the text out resumed at each line start of the full layout and compares the lines from there on.
	private static void AssertEveryResumeMatches(Inline[] inlines, double width, FlowDirection flowDirection, TextWrapping wrapping = TextWrapping.Wrap, int minimumLines = 3)
	{
		var full = Layout(inlines, width, flowDirection, wrapping: wrapping);
		Assert.IsTrue(full.LineCount >= minimumLines, $"The text should wrap to at least {minimumLines} lines, not {full.LineCount}.");
		var text = string.Concat(inlines.Select(i => i is Run run ? run.Text : "\n"));

		for (var line = 1; line < full.LineCount; line++)
		{
			var resumeIndex = full.GetLineForTests(line).Start;
			var resumed = Layout(inlines, width, flowDirection, options: new() { ResumeIndex = resumeIndex }, wrapping: wrapping);

			// The text before the resume index stays on unwrapped lines, which end only at hard breaks and at the index.
			Assert.AreEqual(PrefixLines(text, resumeIndex), resumed.FirstResumedLine, $"Resumed at {resumeIndex}: the lines before it");
			Assert.AreEqual(resumeIndex, resumed.GetLineForTests(resumed.FirstResumedLine).Start, $"Resumed at {resumeIndex}: the first resumed line's start");
			Assert.AreEqual(full.IsBaseDirectionRightToLeft, resumed.IsBaseDirectionRightToLeft, $"Resumed at {resumeIndex}: the paragraph direction");
			Assert.AreEqual(full.LineCount - line, resumed.LineCount - resumed.FirstResumedLine, $"Resumed at {resumeIndex}: the resumed line count");
			for (var i = 0; line + i < full.LineCount; i++)
			{
				AssertLinesEqual(full.GetLineForTests(line + i), resumed.GetLineForTests(resumed.FirstResumedLine + i), $"Resumed at {resumeIndex}, line {line + i}");
			}
		}
	}

	[TestMethod]
	[DataRow("LTR")]
	[DataRow("Mixed")]
	[DataRow("Differing run")]
	[DataRow("Emergency break, Latin")]
	[DataRow("Line breaks")]
	[DataRow("Tabs")]
	[DataRow("NoWrap")]
	public async Task When_Resumed_Lines_Equal_Full_Layout_Lines(string row)
	{
		var wrapping = TextWrapping.Wrap;
		var (inlines, width, flowDirection) = row switch
		{
			"LTR" => (new Inline[] { R(Latin) }, 160.0, FlowDirection.LeftToRight),
			// A LineBreak in a RichTextBlock paragraph: hard breaks before the resume index, and after it.
			"Line breaks" => (new Inline[] { R("First part of the text"), new LineBreak(), R("then a second part of it"), new LineBreak(), R(Latin) }, 120.0, FlowDirection.LeftToRight),
			// Tabs before and after each resume index: a resumed line measures its tabs from its own start, as the full layout does.
			"Tabs" => (new Inline[] { R("Key\tvalue and words\tmore\tcolumns to wrap\there and\tthere again\tend") }, 150.0, FlowDirection.LeftToRight),
			"NoWrap" => (new Inline[] { R("One two three four"), new LineBreak(), R("five six"), new LineBreak(), R("seven eight nine ten") }, 50.0, FlowDirection.LeftToRight),
			// Two inlines, so the shaping runs and the line breaks don't coincide.
			"Mixed" => (new Inline[] { R("Uno Platform 7.0 שלום 42 world, "), R("עולם and 1234 more text אבג.") }, 120.0, FlowDirection.LeftToRight),
			// A run whose direction differs from the control's goes through ICU on a copy of the text with an embedding.
			"Differing run" => (new Inline[] { R("Start of the paragraph "), R("שלום world 42, עולם again here", FlowDirection.RightToLeft), R(" and the end of it") }, 110.0, FlowDirection.LeftToRight),
			// A word wider than the line breaks between clusters.
			"Emergency break, Latin" => (new Inline[] { R("ab Pneumonoultramicroscopicsilicovolcanoconiosis cd") }, 70.0, FlowDirection.LeftToRight),
			_ => throw new ArgumentOutOfRangeException(nameof(row)),
		};
		if (row == "NoWrap")
		{
			wrapping = TextWrapping.NoWrap;
		}
		await WaitForFonts(inlines, width, flowDirection);

		AssertEveryResumeMatches(inlines, width, flowDirection, wrapping);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)] // Needs a Hebrew fallback font.
	public async Task When_Resumed_Lines_Equal_Full_Layout_Lines_Rtl()
	{
		var inlines = new Inline[] { R(Hebrew + " " + Hebrew) };
		await WaitForFonts(inlines, 120, FlowDirection.RightToLeft);

		AssertEveryResumeMatches(inlines, 120, FlowDirection.RightToLeft);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)] // Needs an Arabic fallback font.
	public async Task When_Resumed_After_Emergency_Break_Arabic()
	{
		// One Arabic word wider than the line: the lines break inside it, and every glyph keeps the joining form it has in
		// the whole word, because the resumed layout shapes the whole paragraph.
		var inlines = new Inline[] { R(ArabicWord + ArabicWord) };
		await WaitForFonts(inlines, 60, FlowDirection.RightToLeft);

		AssertEveryResumeMatches(inlines, 60, FlowDirection.RightToLeft);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)] // Needs a Hebrew fallback font.
	public async Task When_Resumed_In_Rtl_Paragraph_Starting_With_Latin()
	{
		// The paragraph is right-to-left (its first strong character is Hebrew); the line the layout resumes at starts with
		// Latin. That line keeps the paragraph's level, so it reads as right-to-left text, aligned right.
		const string Token = "UnoPlatformRocks";
		var text = "שלום " + Token + " עולם";
		var inlines = new Inline[] { R(text) };
		await WaitForFonts(inlines, 1000, FlowDirection.LeftToRight);

		// Wide enough for the token alone, too narrow for the Hebrew word before it.
		var tokenStart = text.IndexOf(Token, StringComparison.Ordinal);
		var unwrapped = Layout(inlines, double.PositiveInfinity, FlowDirection.LeftToRight).GetLineForTests(0);
		var tokenWidth = unwrapped.Clusters.Where(c => c.Start >= tokenStart && c.End <= tokenStart + Token.Length).Sum(c => c.Width);
		var width = Math.Ceiling(tokenWidth) + 2;

		var full = Layout(inlines, width, FlowDirection.LeftToRight);
		Assert.IsTrue(full.IsBaseDirectionRightToLeft, "The paragraph should be right-to-left.");
		var tokenLine = Enumerable.Range(0, full.LineCount).Single(i => full.GetLineForTests(i).Start == tokenStart);
		var expected = full.GetLineForTests(tokenLine);
		Assert.AreEqual(width, expected.Clusters.Max(c => c.X + c.Width), 1, "The full layout's line should be aligned right.");

		var resumed = Layout(inlines, width, FlowDirection.LeftToRight, options: new() { ResumeIndex = tokenStart });
		Assert.IsTrue(resumed.IsBaseDirectionRightToLeft, "The resumed layout's paragraph should stay right-to-left.");
		AssertLinesEqual(expected, resumed.GetLineForTests(resumed.FirstResumedLine), "The resumed line");
	}

	[TestMethod]
	public async Task When_Resumed_Line_Starts_At_Resume_Index()
	{
		// The resume index ends a line even where the text would fit: the previous layout broke there at its own width.
		var inlines = new Inline[] { R("ab cd ef") };
		await WaitForFonts(inlines, 1000);

		var resumed = Layout(inlines, 1000, options: new() { ResumeIndex = 3 });

		Assert.AreEqual(2, resumed.LineCount);
		Assert.AreEqual(1, resumed.FirstResumedLine);
		Assert.AreEqual(0, resumed.GetLineForTests(0).Start);
		Assert.AreEqual(3, resumed.GetLineForTests(0).End);
		Assert.AreEqual(3, resumed.GetLineForTests(1).Start);
		Assert.AreEqual(8, resumed.GetLineForTests(1).End);
	}

	[TestMethod]
	public async Task When_Resumed_At_Text_End()
	{
		// A previous layout fit "abc" and its line break, but not the empty line after it: the resumed layout starts on
		// that ending line, which no engine line covers.
		var inlines = new Inline[] { R("abc"), new LineBreak() };
		await WaitForFonts(inlines, 200);

		var full = Layout(inlines, 200);
		var resumed = Layout(inlines, 200, options: new() { ResumeIndex = 4 });

		Assert.AreEqual(1, full.LineCount);
		Assert.AreEqual(2, full.VisualLineCount, "The full layout has an ending line.");
		Assert.AreEqual(resumed.LineCount, resumed.FirstResumedLine, "No engine line starts at the text end.");
		Assert.AreEqual(full.VisualLineCount, resumed.VisualLineCount);
		var expected = full.GetVisualLine(1);
		var actual = resumed.GetVisualLine(resumed.LineCount);
		Assert.AreEqual(expected.Start, actual.Start);
		Assert.AreEqual(expected.Length, actual.Length);
		Assert.AreEqual(expected.Bounds, actual.Bounds);
		Assert.AreEqual(expected.Baseline, actual.Baseline, 0.001);
	}

	[TestMethod]
	[DataRow(TextTrimming.CharacterEllipsis)]
	[DataRow(TextTrimming.WordEllipsis)]
	public async Task When_TrimmingWidth_Differs_From_WrappingWidth(TextTrimming trimming)
	{
		// The lines break at the wrapping width; each line wider than the trimming width ends with an ellipsis within it.
		var inlines = new Inline[] { R(Latin) };
		await WaitForFonts(inlines, 200);
		const float TrimmingWidth = 120;

		var baseLayout = Layout(inlines, 200, trimming: trimming);
		var twin = Layout(inlines, 200, trimming: trimming, options: new() { TrimmingWidth = TrimmingWidth });

		Assert.AreEqual(baseLayout.LineCount, twin.LineCount);
		var trimmed = 0;
		for (var i = 0; i < baseLayout.LineCount; i++)
		{
			var expected = baseLayout.GetLineForTests(i);
			var actual = twin.GetLineForTests(i);
			Assert.IsFalse(expected.HasEllipsis, $"Line {i} fits the wrapping width.");
			if (expected.WidthWithoutTrailingSpaces > TrimmingWidth)
			{
				trimmed++;
				Assert.IsTrue(actual.HasEllipsis, $"Line {i} ({expected.WidthWithoutTrailingSpaces}) is wider than {TrimmingWidth}.");
				Assert.AreEqual(expected.Start, actual.Start, $"Line {i}: start");
				Assert.AreEqual(expected.End, actual.End, $"Line {i}: end");
				Assert.IsTrue(actual.Width <= TrimmingWidth, $"Line {i}: {actual.Width} should fit {TrimmingWidth}.");
				Assert.IsTrue(actual.Width > TrimmingWidth / 2, $"Line {i}: {actual.Width} keeps what fits.");
			}
			else
			{
				AssertLinesEqual(expected, actual, $"Line {i}");
			}
		}
		Assert.IsTrue(trimmed >= 2, $"Only {trimmed} lines were trimmed.");

		// In a resumed layout, the unwrapped lines before the resume index are a previous layout's and are never trimmed.
		var resumeIndex = baseLayout.GetLineForTests(2).Start;
		var resumedTwin = Layout(inlines, 200, trimming: trimming, options: new() { TrimmingWidth = TrimmingWidth, ResumeIndex = resumeIndex });
		Assert.AreEqual(1, resumedTwin.FirstResumedLine);
		Assert.IsFalse(resumedTwin.GetLineForTests(0).HasEllipsis, "The line before the resume index");
		for (var i = 2; i < twin.LineCount; i++)
		{
			AssertLinesEqual(twin.GetLineForTests(i), resumedTwin.GetLineForTests(i - 1), $"Resumed line {i}");
		}
	}

	[TestMethod]
	public async Task When_ForcedEllipsisLine()
	{
		// A paragraph ellipsis: the line fits, but text after it won't be shown, so it ends with an ellipsis and is the last line.
		var inlines = new Inline[] { R(Latin) };
		await WaitForFonts(inlines, 200);

		var baseLayout = Layout(inlines, 200, trimming: TextTrimming.CharacterEllipsis);
		var twin = Layout(inlines, 200, trimming: TextTrimming.CharacterEllipsis, options: new() { ForcedEllipsisLine = 1 });

		Assert.IsTrue(baseLayout.LineCount >= 3);
		Assert.IsFalse(baseLayout.GetLineForTests(1).HasEllipsis, "The line fits.");
		Assert.AreEqual(2, twin.LineCount, "The lines after the forced one are dropped.");
		AssertLinesEqual(baseLayout.GetLineForTests(0), twin.GetLineForTests(0), "Line 0");
		var forced = twin.GetLineForTests(1);
		Assert.IsTrue(forced.HasEllipsis);
		Assert.AreEqual(baseLayout.GetLineForTests(1).Start, forced.Start);
		Assert.AreEqual(Latin.Length, forced.End, "The ellipsis stands for the rest of the text.");
		Assert.IsTrue(forced.Width <= 200, $"{forced.Width} should fit 200.");

		// Without trimming there is no ellipsis to force.
		var untrimmed = Layout(inlines, 200, options: new() { ForcedEllipsisLine = 1 });
		Assert.IsFalse(untrimmed.GetLineForTests(1).HasEllipsis);

		// In a resumed layout, the line counts from the start of the text, prefix lines included.
		var resumeIndex = baseLayout.GetLineForTests(1).Start;
		var fullTwin = Layout(inlines, 200, trimming: TextTrimming.CharacterEllipsis, options: new() { ForcedEllipsisLine = 2 });
		var resumedTwin = Layout(inlines, 200, trimming: TextTrimming.CharacterEllipsis, options: new() { ResumeIndex = resumeIndex, ForcedEllipsisLine = 2 });
		Assert.AreEqual(1, resumedTwin.FirstResumedLine);
		Assert.AreEqual(3, resumedTwin.LineCount);
		AssertLinesEqual(fullTwin.GetLineForTests(1), resumedTwin.GetLineForTests(1), "Resumed line 1");
		AssertLinesEqual(fullTwin.GetLineForTests(2), resumedTwin.GetLineForTests(2), "Resumed forced line 2");
		Assert.IsTrue(resumedTwin.GetLineForTests(2).HasEllipsis);

		// A line before the resume index belongs to a previous layout: forcing it would drop every resumed line.
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Layout(inlines, 200, trimming: TextTrimming.CharacterEllipsis, options: new() { ResumeIndex = resumeIndex, ForcedEllipsisLine = 0 }));
	}

	[TestMethod]
	public async Task When_Resumed_NoWrap_Line_Starts_At_Resume_Index()
	{
		// Without wrapping, the resume index still ends a line, and the rest stays on one line however wide.
		var inlines = new Inline[] { R("ab cd ef gh ij") };
		await WaitForFonts(inlines, 10);

		var resumed = Layout(inlines, 10, options: new() { ResumeIndex = 3 }, wrapping: TextWrapping.NoWrap);

		Assert.AreEqual(2, resumed.LineCount);
		Assert.AreEqual(1, resumed.FirstResumedLine);
		Assert.AreEqual(3, resumed.GetLineForTests(1).Start);
		Assert.AreEqual(14, resumed.GetLineForTests(1).End);
	}

	[TestMethod]
	public void When_ResumeIndex_Outside_Text_Throws()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Layout(new Inline[] { R("abc") }, 100, options: new() { ResumeIndex = 4 }));
	}

	[TestMethod]
	public async Task When_ResumeIndex_Inside_Cluster_Throws()
	{
		// A surrogate pair, a combining mark, a CRLF and a ligature candidate. Whether the mark or "ffi" makes one cluster
		// depends on the font, so the layout's own clusters say which indices are inside one. Inside a cluster or between
		// \r and \n the constructor throws; at every other index it resumes.
		const string Text = "a\uD83D\uDE00e\u0301\r\nffi b";
		var inlines = new Inline[] { R(Text) };
		await WaitForFonts(inlines, 1000);
		var full = Layout(inlines, 1000);
		var clusters = Enumerable.Range(0, full.LineCount).SelectMany(i => full.GetLineForTests(i).Clusters).ToList();
		var boundaries = clusters.SelectMany(c => new[] { c.Start, c.End }).ToHashSet();
		Assert.IsTrue(clusters.Any(c => c.End - c.Start > 1 && Text[c.Start] != '\r'), "The surrogate pair should be one cluster.");

		for (var index = 1; index < Text.Length; index++)
		{
			var inside = !boundaries.Contains(index) || (Text[index - 1] == '\r' && Text[index] == '\n');
			var resumeIndex = index;
			if (inside)
			{
				var exception = Assert.ThrowsExactly<ArgumentException>(() => Layout(inlines, 1000, options: new() { ResumeIndex = resumeIndex }), $"Index {index}");
				StringAssert.Contains(exception.Message, $"ResumeIndex {index} ");
			}
			else
			{
				Assert.AreEqual(index, Layout(inlines, 1000, options: new() { ResumeIndex = resumeIndex }).GetLineForTests(PrefixLines(Text, index)).Start, $"Index {index}");
			}
		}
	}
}
#endif
