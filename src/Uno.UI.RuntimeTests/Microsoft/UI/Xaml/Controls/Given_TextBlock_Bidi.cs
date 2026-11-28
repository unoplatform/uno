using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Visual order of bidi text, read from the colour of each run's ink. The expected orders are WinUI's.
[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_Bidi
{
	private const string LRE = "\u202A";
	private const string RLE = "\u202B";
	private const string PDF = "\u202C";
	private const string RLO = "\u202E";
	private const string RLM = "\u200F";
	private const string LRI = "\u2066";
	private const string RLI = "\u2067";
	private const string FSI = "\u2068";

	private static Run R(string text, Color color, FlowDirection? flowDirection = null)
	{
		var run = new Run { Text = text, Foreground = new SolidColorBrush(color) };
		if (flowDirection is { } direction)
		{
			run.FlowDirection = direction;
		}
		return run;
	}

	private static TextBlock CreateTextBlock(FlowDirection flowDirection, params Inline[] inlines)
	{
		var textBlock = new TextBlock { FontSize = 24, FlowDirection = flowDirection };
		foreach (var inline in inlines)
		{
			textBlock.Inlines.Add(inline);
		}
		return textBlock;
	}

	[TestMethod]
	public async Task When_Rtl_TextBlock_Latin_Keeps_Period_At_End()
	{
		var sut = CreateTextBlock(FlowDirection.RightToLeft, R("Hello world", Colors.Blue), R(".", Colors.Red));
		sut.Width = 300;

		try
		{
			var (_, bitmap) = await Show(sut);
			var text = InkX(bitmap, Colors.Blue);
			var period = InkX(bitmap, Colors.Red);

			// "Hello world." is left-to-right text, so the period stays at its end, as on WinUI.
			Assert.IsTrue(period.Min > text.Max, $"The period ({period}) should be drawn right of \"Hello world\" ({text}).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("RLO")]
	[DataRow("RLO with a differing run")]
	[DataRow("RLE with a trailing neutral")]
	[DataRow("User RLE straddling a differing run")]
	[DataRow("Nesting at the deepest level")]
	public async Task When_Bidi_Controls_In_Text(string row)
	{
		var sut = row switch
		{
			// RLO reverses "abcd" whatever its letters are, so "cd" is drawn left of "ab".
			"RLO" => CreateTextBlock(
				FlowDirection.LeftToRight,
				R("1", Colors.Black), R(RLO + "ab", Colors.Blue), R("cd" + PDF, Colors.Red)),
			"RLO with a differing run" => CreateTextBlock(
				FlowDirection.LeftToRight,
				R("1", Colors.Black), R(RLO + "ab", Colors.Blue), R("cd" + PDF, Colors.Red), R(" xy", Colors.Green, FlowDirection.RightToLeft)),
			// The trailing "!!" takes the direction of the right-to-left embedding, so it moves left of "ab".
			"RLE with a trailing neutral" => CreateTextBlock(
				FlowDirection.LeftToRight,
				R("x " + RLE, Colors.Black), R("ab", Colors.Blue), R("!!" + PDF, Colors.Red), R(" y", Colors.Black)),
			// The user's RLE opens before the run; the user's PDF inside the run closes the run's own embedding
			// first, so "ef" is drawn left of "ab" and "cd" right of it.
			"User RLE straddling a differing run" => CreateTextBlock(
				FlowDirection.LeftToRight,
				R("x ", Colors.Black), R(RLE + "ab", Colors.Red), R("cd" + PDF + " ef", Colors.Blue, FlowDirection.RightToLeft), R(" gh", Colors.Green)),
			// 125 nested embeddings put "ab" at level 126, one above the deepest explicit level.
			"Nesting at the deepest level" => CreateTextBlock(
				FlowDirection.LeftToRight,
				R(string.Concat(Enumerable.Range(0, 125).Select(i => i % 2 == 0 ? RLE : LRE)) + "ab", Colors.Blue),
				R("!!" + string.Concat(Enumerable.Repeat(PDF, 125)), Colors.Red),
				R(" xy", Colors.Green, FlowDirection.RightToLeft)),
			_ => throw new ArgumentOutOfRangeException(nameof(row)),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var blue = InkX(bitmap, Colors.Blue);
			var red = InkX(bitmap, Colors.Red);

			if (row == "User RLE straddling a differing run")
			{
				Assert.IsTrue(blue.Min < red.Min && blue.Max > red.Max, $"The run ({blue}) should be drawn on both sides of \"ab\" ({red}).");
			}
			else
			{
				Assert.IsTrue(red.Max < blue.Min, $"The red text ({red}) should be drawn left of the blue text ({blue}).");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("RLE")]
	[DataRow("RLO")]
	[DataRow("LRI")]
	[DataRow("RLI")]
	[DataRow("FSI")]
	public async Task When_Differing_Run_Leaves_Control_Open(string opener)
	{
		var control = opener switch
		{
			"RLE" => RLE,
			"RLO" => RLO,
			"LRI" => LRI,
			"RLI" => RLI,
			"FSI" => FSI,
			_ => throw new ArgumentOutOfRangeException(nameof(opener)),
		};
		var sut = CreateTextBlock(
			FlowDirection.LeftToRight,
			R("ab ", Colors.Black), R(control + "cd", Colors.Blue, FlowDirection.RightToLeft), R(" ef", Colors.Red), R(" gh", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var black = InkX(bitmap, Colors.Black);
			var blue = InkX(bitmap, Colors.Blue);
			var red = InkX(bitmap, Colors.Red);
			var green = InkX(bitmap, Colors.Green);
			var description = $"black {black}, blue {blue}, red {red}, green {green}";

			// Measured on WinUI. The run's own end closes the embedding the text left open instead of the run's, so
			// after an RLE or RLO the run's right-to-left embedding goes on to the end of the paragraph and " ef gh"
			// is drawn before "cd". After an isolate the text after the run keeps its order.
			if (opener is "RLE" or "RLO")
			{
				Assert.IsTrue(black.Max < red.Min && red.Max < green.Min && green.Max < blue.Min, $"Expected \"ab ef gh cd\": {description}.");
			}
			else
			{
				Assert.IsTrue(black.Max < blue.Min && blue.Max < red.Min && red.Max < green.Min, $"Expected \"ab cd ef gh\": {description}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Differing_Run_In_Paragraph_Of_Its_Own_Direction(FlowDirection controlDirection)
	{
		// The run is embedded although its paragraph already has its direction, as on WinUI: in a left-to-right control,
		// a paragraph that starts with a right-to-left mark draws "gh def abc". The mirror case reads naturally.
		var sut = controlDirection is FlowDirection.LeftToRight
			? CreateTextBlock(controlDirection, R(RLM + "abc ", Colors.Red), R("def", Colors.Blue, FlowDirection.RightToLeft), R(" gh", Colors.Green))
			: CreateTextBlock(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, FlowDirection.LeftToRight), R(" gh", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);
			var green = InkX(bitmap, Colors.Green);
			var description = $"red {red}, blue {blue}, green {green}";

			if (controlDirection is FlowDirection.LeftToRight)
			{
				Assert.IsTrue(green.Max < blue.Min && blue.Max < red.Min, $"Expected \"gh def abc\": {description}.");
			}
			else
			{
				Assert.IsTrue(red.Max < blue.Min && blue.Max < green.Min, $"Expected \"abc def gh\": {description}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight, false)]
	[DataRow(FlowDirection.RightToLeft, false)]
	[DataRow(FlowDirection.LeftToRight, true)]
	[DataRow(FlowDirection.RightToLeft, true)]
	public async Task When_Differing_Run_Wraps(FlowDirection controlDirection, bool trimmed)
	{
		// One number per colour, each its own run in the direction opposite to the control's, so that lines break (or the
		// line is trimmed) inside the embedding. Numbers, unlike Latin words, read right to left in a right-to-left
		// embedding. A right-to-left mark starts the right-to-left control's paragraph.
		var colors = new[] { Colors.Red, Colors.Blue, Colors.Green, Colors.Magenta, Colors.Orange, Colors.Purple };
		var runDirection = controlDirection is FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		var sut = CreateTextBlock(controlDirection, R(controlDirection is FlowDirection.LeftToRight ? "x " : RLM + "x ", Colors.Black));
		foreach (var (color, index) in colors.Select((color, index) => (color, index)))
		{
			sut.Inlines.Add(R(new string((char)('1' + index), 2) + " ", color, runDirection));
		}
		sut.Width = trimmed ? 150 : 110;
		sut.TextWrapping = trimmed ? TextWrapping.NoWrap : TextWrapping.Wrap;
		sut.TextTrimming = trimmed ? TextTrimming.CharacterEllipsis : TextTrimming.None;

		try
		{
			var (_, bitmap) = await Show(sut, height: 240);
			var words = colors
				.Select((color, index) => (index, x: InkX(bitmap, color), y: InkY(bitmap, color)))
				.Where(word => !word.x.IsEmpty)
				.ToArray();
			if (trimmed)
			{
				// The ellipsis takes the colour of the last word drawn.
				words = words[..^1];
			}
			// Digits have no descenders, so the bottom of their ink is their line's baseline.
			var lines = words
				.OrderBy(word => word.y.Bottom)
				.Aggregate(new List<List<(int index, InkSpan x, (int Top, int Bottom) y)>>(), (lines, word) =>
				{
					if (lines.Count == 0 || word.y.Bottom - lines[^1][0].y.Bottom > 8)
					{
						lines.Add([]);
					}
					lines[^1].Add(word);
					return lines;
				})
				.Select(line => line.OrderBy(word => word.index).ToArray())
				.ToArray();
			var description = string.Join("; ", words.Select(word => $"word {word.index}: x {word.x}, bottom {word.y.Bottom}"));

			Assert.IsTrue(trimmed ? lines.Length == 1 : lines.Length >= 2, $"Expected {(trimmed ? "one line" : "several lines")}: {description}.");
			Assert.IsTrue(lines.Any(line => line.Length >= 2), $"Expected a line holding several words: {description}.");
			foreach (var line in lines)
			{
				for (var i = 1; i < line.Length; i++)
				{
					// Within a line, the embedded numbers read in the run's direction.
					var inOrder = runDirection is FlowDirection.RightToLeft
						? line[i].x.Max < line[i - 1].x.Min
						: line[i - 1].x.Max < line[i].x.Min;
					Assert.IsTrue(inOrder, $"Words {line[i - 1].index} and {line[i].index} are out of order: {description}.");
				}
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	// WinUI keeps these spaces in the run's right-to-left embedding, between "ab" and the run (a 36 px gap here).
	// Uno resets spaces at the end of a line to the paragraph's level, as UAX #9 L1 says, and always has.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Differing_Run_Ends_With_Spaces()
	{
		var sut = CreateTextBlock(FlowDirection.LeftToRight, R("ab", Colors.Black), R("cd!!     ", Colors.Blue, FlowDirection.RightToLeft));

		try
		{
			var (_, bitmap) = await Show(sut);
			var text = InkX(bitmap, Colors.Black);
			var run = InkX(bitmap, Colors.Blue);

			Assert.IsTrue(run.Min - text.Max < 12, $"The right-to-left run ({run}) should follow \"ab\" ({text}), its trailing spaces at the end of the line.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Differing_Run_Spans_Paragraph_Separator()
	{
		// The run's left-to-right embedding covers its second paragraph too: the dots stay right of "cd" although
		// the paragraph starts with a right-to-left mark.
		var sut = CreateTextBlock(FlowDirection.RightToLeft, R("\n" + RLM + "cd......", Colors.Red, FlowDirection.LeftToRight));
		sut.Width = 300;

		try
		{
			var (_, bitmap) = await Show(sut);
			var ink = Letters(bitmap, Colors.Red);

			Assert.IsTrue(DotsOnTheRight(ink), $"The dots should be right of \"cd\": ink {ink.Ink}, letters {ink.Letters}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.LeftToRight)]
	public async Task When_Run_FlowDirection_Differs_From_Control(FlowDirection controlDirection)
	{
		var runDirection = controlDirection is FlowDirection.RightToLeft ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
		var sut = controlDirection is FlowDirection.RightToLeft
			? CreateTextBlock(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, runDirection), R(" jkl", Colors.Green), R(".", Colors.Magenta))
			: CreateTextBlock(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, runDirection), R("!!", Colors.Magenta, runDirection), R(" jkl", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);
			var green = InkX(bitmap, Colors.Green);
			var magenta = InkX(bitmap, Colors.Magenta);
			var description = $"red {red}, blue {blue}, green {green}, magenta {magenta}";

			if (controlDirection is FlowDirection.RightToLeft)
			{
				// Left-to-right text with an explicitly left-to-right run reads naturally, period last.
				Assert.IsTrue(red.Max < blue.Min && blue.Max < green.Min && green.Max < magenta.Min, $"Expected \"abc def jkl.\": {description}.");
			}
			else
			{
				// The right-to-left run is embedded: its trailing "!!" moves to its left.
				Assert.IsTrue(red.Max < magenta.Min && magenta.Max < blue.Min && blue.Max < green.Min, $"Expected \"abc !!def jkl\": {description}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Uno's text engine
	public async Task When_Bidi_Buffers_Outlive_SetPara()
	{
#if HAS_UNO
		// ICU keeps the pointers it gets in ubidi_setPara and reads them again for each line, and the engine keeps an
		// index map for that line loop. Between the two, overwrite the pooled arrays of those sizes (Uno's own pools)
		// that a buffer returned too early would come back as. The GC can move the text too, but a forced collection
		// didn't make the old code fail, so this test can't catch a text copy that is dropped.
		UnicodeText.ParagraphBidiResolvedForTests = length =>
		{
			var levels = Uno.Buffers.ArrayPool<byte>.Shared.Rent(length);
			Array.Clear(levels);
			Uno.Buffers.ArrayPool<byte>.Shared.Return(levels);
			var map = Uno.Buffers.ArrayPool<int>.Shared.Rent(length + 1);
			Array.Clear(map);
			Uno.Buffers.ArrayPool<int>.Shared.Return(map);
			GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		};

		try
		{
			var sut = CreateTextBlock(FlowDirection.LeftToRight, R("ab", Colors.Blue, FlowDirection.RightToLeft), R("!!", Colors.Red, FlowDirection.RightToLeft));
			var (_, bitmap) = await Show(sut);
			var blue = InkX(bitmap, Colors.Blue);
			var red = InkX(bitmap, Colors.Red);

			Assert.IsTrue(red.Max < blue.Min, $"\"!!\" ({red}) should be drawn left of \"ab\" ({blue}), as in the right-to-left embedding.");
		}
		finally
		{
			UnicodeText.ParagraphBidiResolvedForTests = null;
			WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}
}
