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
	public Task When_Differing_Run_Wraps(FlowDirection controlDirection, bool trimmed)
		=> AssertDifferingRunWraps(controlDirection, trimmed, "");

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight, false)]
	[DataRow(FlowDirection.RightToLeft, false)]
	[DataRow(FlowDirection.LeftToRight, true)]
	[DataRow(FlowDirection.RightToLeft, true)]
	public Task When_Differing_Run_With_Information_Separators_Wraps(FlowDirection controlDirection, bool trimmed)
		// Every number but the first ends with separators, which split the runs' embeddings (measured on WinUI).
		=> AssertDifferingRunWraps(controlDirection, trimmed, "\u001C\u001D\u001E");

	private static async Task AssertDifferingRunWraps(FlowDirection controlDirection, bool trimmed, string separators)
	{
		// One number per colour, each its own run in the direction opposite to the control's, so that lines break (or the
		// line is trimmed) inside the embedding. Numbers, unlike Latin words, read right to left in a right-to-left
		// embedding. A right-to-left mark starts the right-to-left control's paragraph.
		var colors = new[] { Colors.Red, Colors.Blue, Colors.Green, Colors.Magenta, Colors.Orange, Colors.Purple };
		var runDirection = controlDirection is FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		var sut = CreateTextBlock(controlDirection, R(controlDirection is FlowDirection.LeftToRight ? "x " : RLM + "x ", Colors.Black));
		foreach (var (color, index) in colors.Select((color, index) => (color, index)))
		{
			sut.Inlines.Add(R(new string((char)('1' + index), 2) + separators[..Math.Min(index, separators.Length)] + " ", color, runDirection));
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
					// Within a line, the embedded numbers read in the run's direction, and those after a separator in the
					// paragraph's (the control's, here).
					var direction = separators.Length > 0 && line[i - 1].index > 0 ? controlDirection : runDirection;
					var inOrder = direction is FlowDirection.RightToLeft
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

	// U+001C-U+001E are paragraph separators to the bidi algorithm (class B), but no line ends at them. As on WinUI, the
	// text around one stays on one line and in one bidi paragraph, and the character itself draws nothing.
	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1C, FlowDirection.RightToLeft)]
	[DataRow(0x1D, FlowDirection.LeftToRight)]
	[DataRow(0x1D, FlowDirection.RightToLeft)]
	[DataRow(0x1E, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_Between_Latin_Words(int separator, FlowDirection flowDirection)
	{
		// The "é" keeps the left-to-right layout off the ASCII shortcut, so it goes through ICU too.
		var sut = CreateTextBlock(flowDirection, R("abc", Colors.Red), R(((char)separator).ToString(), Colors.Green), R("déf", Colors.Blue));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);
			var description = $"red {red}, blue {blue}";

			Assert.IsTrue(red.Max < blue.Min, $"Expected \"abc déf\": {description}.");
			AssertOneLine(bitmap, Colors.Red, Colors.Blue);
			Assert.IsTrue(InkX(bitmap, Colors.Green).IsEmpty, "The separator should draw nothing.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1C, FlowDirection.RightToLeft)]
	[DataRow(0x1D, FlowDirection.LeftToRight)]
	[DataRow(0x1D, FlowDirection.RightToLeft)]
	[DataRow(0x1E, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs a Hebrew font.
	public async Task When_Information_Separator_Between_Hebrew_Words(int separator, FlowDirection flowDirection)
	{
		var sut = CreateTextBlock(flowDirection, R("אבג", Colors.Red), R(((char)separator).ToString(), Colors.Green), R("דהו", Colors.Blue));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);

			Assert.IsTrue(blue.Max < red.Min, $"The second word ({blue}) should be drawn left of the first ({red}).");
			AssertOneLine(bitmap, Colors.Red, Colors.Blue);
			Assert.IsTrue(InkX(bitmap, Colors.Green).IsEmpty, "The separator should draw nothing.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs a Hebrew font.
	public async Task When_Information_Separator_Before_Hebrew(int separator, FlowDirection flowDirection)
	{
		// One paragraph, whose first strong character is Latin, so "def" stays right of the Hebrew word. Had the separator
		// started a right-to-left paragraph, "def" would be drawn left of it.
		var sut = CreateTextBlock(flowDirection, R("abc", Colors.Red), R(((char)separator).ToString(), Colors.Green), R("אבג ", Colors.Blue), R("def", Colors.Magenta));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = InkX(bitmap, Colors.Red);
			var blue = InkX(bitmap, Colors.Blue);
			var magenta = InkX(bitmap, Colors.Magenta);
			var description = $"red {red}, blue {blue}, magenta {magenta}";

			Assert.IsTrue(red.Max < blue.Min && blue.Max < magenta.Min, $"Expected \"abc גבא def\": {description}.");
			AssertOneLine(bitmap, Colors.Red, Colors.Magenta);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1D, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.LeftToRight)]
	[DataRow(0x1C, FlowDirection.RightToLeft)]
	[DataRow(0x1D, FlowDirection.RightToLeft)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_In_Differing_Run(int separator, FlowDirection controlDirection)
	{
		// Measured on WinUI: the separator takes the paragraph's level, as a paragraph separator would, so the run's
		// embedding is split there and "!!" no longer moves left of "ab" in a right-to-left run. The paragraph goes on: its
		// first strong character, in "xy", sets the direction of all of it.
		var runDirection = controlDirection is FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		var sut = CreateTextBlock(controlDirection, R("xy ", Colors.Black), R("ab", Colors.Blue, runDirection), R((char)separator + "!!", Colors.Red, runDirection));

		try
		{
			var (_, bitmap) = await Show(sut);
			var blue = InkX(bitmap, Colors.Blue);
			var red = InkX(bitmap, Colors.Red);
			var description = $"blue {blue}, red {red}";

			Assert.IsTrue(blue.Max < red.Min, $"Expected \"!!\" right of \"ab\": {description}.");
			AssertOneLine(bitmap, Colors.Blue, Colors.Red);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("LF", 0xE0041, FlowDirection.LeftToRight)]
	[DataRow("LF", 0xE0041, FlowDirection.RightToLeft)]
	[DataRow("CR", 0xE0041, FlowDirection.LeftToRight)]
	[DataRow("CRLF", 0xE0041, FlowDirection.LeftToRight)]
	[DataRow("NEL", 0xE0041, FlowDirection.LeftToRight)]
	[DataRow("PS", 0xE0041, FlowDirection.LeftToRight)]
	[DataRow("LF", 0x0301, FlowDirection.LeftToRight)]
	[DataRow("LF", 0xFE0F, FlowDirection.RightToLeft)]
	public async Task When_Continuation_Follows_Line_Break(string lineBreak, int continuation, FlowDirection flowDirection)
	{
		// A tag character (U+E0041) or a combining mark joins the character before it when shaped, even a line break.
		// As on WinUI, the line still ends at the break.
		var separator = lineBreak switch
		{
			"LF" => "\n",
			"CR" => "\r",
			"CRLF" => "\r\n",
			"NEL" => "\u0085",
			"PS" => ((char)0x2029).ToString(),
			_ => throw new ArgumentOutOfRangeException(nameof(lineBreak)),
		};
		var sut = new TextBlock { FontSize = 24, FlowDirection = flowDirection, Foreground = new SolidColorBrush(Colors.Red), Text = "ab" + separator + char.ConvertFromUtf32(continuation) + "cd" };

		try
		{
			var (_, bitmap) = await Show(sut);

			Assert.AreEqual(2, InkLines(bitmap, Colors.Red), "Expected two lines.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(0x1C, FlowDirection.LeftToRight)]
	[DataRow(0x1E, FlowDirection.RightToLeft)]
	public async Task When_Information_Separator_Starts_Line(int separator, FlowDirection flowDirection)
	{
		var sut = CreateTextBlock(flowDirection, R("abc\n" + (char)separator, Colors.Red), R("déf", Colors.Blue));

		try
		{
			var (_, bitmap) = await Show(sut);
			var (redTop, _) = InkY(bitmap, Colors.Red);
			var (blueTop, _) = InkY(bitmap, Colors.Blue);

			Assert.AreEqual(1, InkLines(bitmap, Colors.Red), "\"abc\" should take one line.");
			Assert.AreEqual(1, InkLines(bitmap, Colors.Blue), "\"déf\" should take one line.");
			Assert.IsTrue(blueTop > redTop + 20, $"\"déf\" (top {blueTop}) should be on the line after \"abc\" (top {redTop}).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Information_Separators_With_MaxLines(FlowDirection flowDirection)
	{
		var sut = new TextBlock
		{
			FontSize = 24,
			Width = 120,
			FlowDirection = flowDirection,
			TextWrapping = TextWrapping.Wrap,
			MaxLines = 1,
			Foreground = new SolidColorBrush(Colors.Red),
			Text = "abc\u001Edéf ghi\u001Cjkl mno\u001Dpqr",
		};

		try
		{
			var (_, bitmap) = await Show(sut);

			Assert.AreEqual(1, InkLines(bitmap, Colors.Red), "Expected one line.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	// WinUI ends the override at the separator, as at a paragraph separator, so "d!" is drawn right of "c" there. ICU reads
	// the separator as a segment separator, which doesn't end it.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Override_Spans_Information_Separator()
	{
		var sut = CreateTextBlock(
			FlowDirection.LeftToRight,
			R(RLO + "a", Colors.Red), R("b", Colors.Blue), R("\u001C", Colors.Green), R("c", Colors.Magenta), R("d!" + PDF, Colors.Orange));

		try
		{
			var (_, bitmap) = await Show(sut);
			var c = InkX(bitmap, Colors.Magenta);
			var d = InkX(bitmap, Colors.Orange);

			Assert.IsTrue(d.Max < c.Min, $"The override should reverse \"cd!\": c {c}, d! {d}.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The number of lines one colour's ink takes: bands of rows with ink, apart by more than a few rows.
	private static int InkLines(RawBitmap bitmap, Color color)
	{
		var lines = 0;
		var lastRow = -100;
		for (var y = 0; y < bitmap.Height; y++)
		{
			if (!InkX(bitmap, color, y, y).IsEmpty)
			{
				if (y - lastRow > 6)
				{
					lines++;
				}
				lastRow = y;
			}
		}
		return lines;
	}

	// Text of the two colours sits on one line: their ink starts at the same height.
	private static void AssertOneLine(RawBitmap bitmap, Color first, Color second)
	{
		var (firstTop, firstBottom) = InkY(bitmap, first);
		var (secondTop, secondBottom) = InkY(bitmap, second);
		Assert.IsTrue(
			Math.Abs(firstBottom - secondBottom) < 6 && firstBottom - firstTop < 40 && secondBottom - secondTop < 40,
			$"Expected one line: rows {firstTop}..{firstBottom} and {secondTop}..{secondBottom}.");
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
