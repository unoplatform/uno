#if __SKIA__
#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents;

// The paragraph's bidi runs, which the shaping walks with the run, script and font breaks in the order of the text
// (unoplatform/uno#25979).
public partial class Given_UnicodeTextOptions
{
	private static (Inline[] Inlines, FlowDirection FlowDirection, string Directions) BidiRunsSample(string row) => row switch
	{
		// The direction of each character: R for right to left, L for left to right. Each run has the control's
		// direction, as in a TextBlock, where it inherits it.
		"Arabic ending in a number" => ([R("نیا چاند 1", FlowDirection.RightToLeft)], FlowDirection.RightToLeft, "RRRRRRRRRL"),
		"Arabic around a number, left-to-right paragraph" => ([R("abc نیا 1 چاند")], FlowDirection.LeftToRight, "LLLLRRRRLRRRRR"),
		"Arabic date" => ([R("منگل 3 نومبر 2026 · 14:58", FlowDirection.RightToLeft)], FlowDirection.RightToLeft, "RRRRRLRRRRRRRLLLLRRRLLLLL"),
		"Several direction changes" => ([R("نیا 1 چاند abc پیر 9", FlowDirection.RightToLeft)], FlowDirection.RightToLeft, "RRRRLRRRRRRLLLRRRRRL"),
		"Bracket before a number" => ([R("(1", FlowDirection.RightToLeft)], FlowDirection.RightToLeft, "RL"),
		// ICU reads a copy with an embedding around the run.
		"Right-to-left run in a left-to-right control" => ([R("نیا چاند 1", FlowDirection.RightToLeft)], FlowDirection.LeftToRight, "RRRRRRRRRL"),
		_ => throw new ArgumentOutOfRangeException(nameof(row)),
	};

	[TestMethod]
	[DataRow("Arabic ending in a number")]
	[DataRow("Arabic date")]
	[DataRow("Arabic around a number, left-to-right paragraph")]
	[DataRow("Several direction changes")]
	[DataRow("Bracket before a number")]
	[DataRow("Right-to-left run in a left-to-right control")]
	public void When_Bidi_Runs_Are_In_Text_Order(string row)
	{
		var (inlines, flowDirection, directions) = BidiRunsSample(row);
		var runs = Layout(inlines, 1000, flowDirection).BidiRunsForTests;
		var description = string.Join(", ", runs.Select(run => $"{run.direction} to {run.end}"));

		var start = 0;
		foreach (var (end, direction) in runs)
		{
			Assert.IsTrue(end > start, $"The runs should follow the text: {description}.");
			for (var index = start; index < end; index++)
			{
				Assert.AreEqual(directions[index] == 'R', direction is FlowDirection.RightToLeft, $"The direction at {index}: {description}.");
			}
			start = end;
		}
		Assert.AreEqual(directions.Length, start, $"The runs should cover the text: {description}.");
	}

	// Waits until a font has glyphs for all of the text, which a fallback font that is still downloading can take longer
	// than WaitForFonts waits for; false when none has them.
	internal static async Task<bool> WaitForGlyphs(Inline[] inlines, double width, FlowDirection flowDirection)
	{
		for (var attempt = 0; attempt < 50; attempt++)
		{
			var layout = Layout(inlines, width, flowDirection);
			if (!Enumerable.Range(0, layout.LineCount).SelectMany(line => layout.GetLineForTests(line).Clusters).Any(cluster => cluster.Glyphs.Contains((ushort)0)))
			{
				await WaitForFonts(inlines, width, flowDirection);
				return true;
			}
			await WindowHelper.WaitForIdle();
			await Task.Delay(200);
		}
		return false;
	}

	// The glyphs of the clusters inside a word, in the order of the text.
	private static string WordGlyphs(UnicodeText layout, int start, int length)
		=> string.Join("|", Enumerable.Range(0, layout.LineCount)
			.SelectMany(line => layout.GetLineForTests(line).Clusters)
			.Where(cluster => cluster.Start >= start && cluster.End <= start + length)
			.OrderBy(cluster => cluster.Start)
			.Select(cluster => string.Join(",", cluster.Glyphs)));

	private static async Task AssertWordsShapedAsAlone(string text, UnicodeText layout, string context)
	{
		foreach (var word in new[] { "نیا", "چاند", "پیر", "منگل", "نومبر" })
		{
			Inline[] alone = [R(word, FlowDirection.RightToLeft)];
			if (!await WaitForGlyphs(alone, 1000, FlowDirection.RightToLeft))
			{
				// Without a font that has the glyphs, every letter is the missing glyph, whatever the shaping.
				Assert.Inconclusive($"No font here has glyphs for \"{word}\".");
			}
			var expected = WordGlyphs(Layout(alone, 1000, FlowDirection.RightToLeft), 0, word.Length);
			for (var start = text.IndexOf(word, StringComparison.Ordinal); start >= 0; start = text.IndexOf(word, start + 1, StringComparison.Ordinal))
			{
				Assert.AreEqual(expected, WordGlyphs(layout, start, word.Length), $"{context}: the glyphs of \"{word}\" at {start}");
			}
		}
	}

	[TestMethod]
	[DataRow("Arabic ending in a number")]
	[DataRow("Arabic date")]
	[DataRow("Several direction changes")]
	[DataRow("Right-to-left run in a left-to-right control")]
	public async Task When_Rtl_Words_Next_To_Ltr_Runs_Shape_As_Alone(string row)
	{
		var (inlines, flowDirection, _) = BidiRunsSample(row);
		var text = string.Concat(inlines.Cast<Run>().Select(run => run.Text));
		if (!await WaitForGlyphs(inlines, 1000, flowDirection))
		{
			Assert.Inconclusive("No font here has glyphs for all of the text.");
		}

		await AssertWordsShapedAsAlone(text, Layout(inlines, 1000, flowDirection), row);
	}

	[TestMethod]
	public async Task When_Resumed_Rtl_Words_Next_To_Ltr_Runs_Shape_As_Alone()
	{
		// One "نیا چاند 1" per line, and the layout resumed at each line: the shaping still covers the whole paragraph.
		Inline[] line = [R("نیا چاند 1", FlowDirection.RightToLeft)];
		if (!await WaitForGlyphs(line, 1000, FlowDirection.RightToLeft))
		{
			Assert.Inconclusive("No font here has glyphs for all of the text.");
		}
		var width = Layout(line, 1000, FlowDirection.RightToLeft).GetLineForTests(0).Width + 10;
		const string Text = "نیا چاند 1 نیا چاند 1 نیا چاند 1";
		Inline[] inlines = [R(Text, FlowDirection.RightToLeft)];
		var full = Layout(inlines, width, FlowDirection.RightToLeft);
		Assert.AreEqual(3, full.LineCount, "One sample per line");

		await AssertWordsShapedAsAlone(Text, full, "Full layout");
		for (var index = 1; index < full.LineCount; index++)
		{
			var resumeIndex = full.GetLineForTests(index).Start;
			var resumed = Layout(inlines, width, FlowDirection.RightToLeft, options: new() { ResumeIndex = resumeIndex });
			await AssertWordsShapedAsAlone(Text, resumed, $"Resumed at {resumeIndex}");
		}
	}
}
#endif
