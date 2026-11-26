using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.TextInkHelper;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// RichTextBlock in right-to-left and with bidi text, read from the colour of the ink. The expected values are WinUI's:
// the control's flow space is mirrored for right-to-left, alignment is relative to its FlowDirection, and the reading
// order comes from the content.
[TestClass]
[RunsOnUIThread]
public class Given_RichTextBlock_Bidi
{
	private const string Hebrew = "שלום עולם";
	private const string RLO = "‮";
	private const string RLE = "‫";
	private const string PDF = "‬";

#if HAS_UNO
	private const bool IsNativeWinUI = false;
#else
	private const bool IsNativeWinUI = true;
#endif

	private static Run R(string text, Color color, FlowDirection? flowDirection = null)
	{
		var run = new Run { Text = text, Foreground = new SolidColorBrush(color) };
		if (flowDirection is { } direction)
		{
			run.FlowDirection = direction;
		}
		return run;
	}

	private static Paragraph P(params Inline[] inlines)
	{
		var paragraph = new Paragraph();
		foreach (var inline in inlines)
		{
			paragraph.Inlines.Add(inline);
		}
		return paragraph;
	}

	private static RichTextBlock Rtb(FlowDirection flowDirection, params Inline[] inlines)
	{
		var rtb = new RichTextBlock { Width = 300, FontSize = 20, FlowDirection = flowDirection };
		rtb.Blocks.Add(P(inlines));
		return rtb;
	}

	private static void AssertNear(double expected, double actual, double tolerance, string message)
		=> Assert.AreEqual(expected, actual, tolerance, $"{message} (expected {expected}, actual {actual})");

	// ---------------------------------------------------------------- reading order and alignment (round-1 rows)

	[TestMethod]
	[DataRow("two runs")]
	[DataRow("one run")]
	public async Task When_Reading_Order_From_Content(string row)
	{
		// Row 02: a right-to-left control with left-to-right text reads "Hello world." with the period at the end, aligned
		// to the control's start, its right edge.
		var sut = row == "one run"
			? Rtb(FlowDirection.RightToLeft, R("Hello world......", Colors.Blue))
			: Rtb(FlowDirection.RightToLeft, R("Hello world", Colors.Blue), R(".", Colors.Red));

		try
		{
			var (_, bitmap) = await Show(sut);
			if (row == "one run")
			{
				var ink = Letters(bitmap, Colors.Blue);
				Assert.IsFalse(ink.Ink.IsEmpty, "The text should be drawn");
				Assert.IsTrue(DotsOnTheRight(ink), $"The dots should be drawn right of \"Hello world\": ink {ink.Ink}, letters {ink.Letters}.");
				AssertNear(300, ink.Ink.Max + 1, 4, "The line should end at the control's right edge");
				return;
			}

			var text = DrawnX(bitmap, Colors.Blue);
			var period = DrawnX(bitmap, Colors.Red);

			Assert.IsTrue(period.Min > text.Max, $"The period ({period}) should be drawn right of \"Hello world\" ({text}).");
			AssertNear(300, period.Max + 1, 4, "The line should end at the control's right edge");
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
		// Row 15: a run whose direction differs from the control's is embedded in its own direction.
		var sut = controlDirection is FlowDirection.RightToLeft
			? Rtb(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, FlowDirection.LeftToRight), R(" jkl", Colors.Green), R(".", Colors.Magenta))
			: Rtb(controlDirection, R("abc ", Colors.Red), R("def", Colors.Blue, FlowDirection.RightToLeft), R("!!", Colors.Magenta, FlowDirection.RightToLeft), R(" jkl", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = DrawnX(bitmap, Colors.Red);
			var blue = DrawnX(bitmap, Colors.Blue);
			var green = DrawnX(bitmap, Colors.Green);
			var magenta = DrawnX(bitmap, Colors.Magenta);
			var description = $"red {red}, blue {blue}, green {green}, magenta {magenta}";

			if (controlDirection is FlowDirection.RightToLeft)
			{
				// The paragraph, detected from its first strong character, stays left-to-right: the runs read in order and
				// the period stays last, at the control's start.
				Assert.IsTrue(red.Max < blue.Min && blue.Max < green.Min && green.Max < magenta.Min, $"Expected \"abc def jkl.\": {description}.");
				AssertNear(300, magenta.Max + 1, 4, "The line should end at the control's right edge");
			}
			else
			{
				// The right-to-left run's trailing "!!" moves to its left.
				Assert.IsTrue(red.Max < magenta.Min && magenta.Max < blue.Min && blue.Max < green.Min, $"Expected \"abc !!def jkl\": {description}.");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(TextAlignment.Left, "right")]
	[DataRow(TextAlignment.Right, "left")]
	[DataRow(TextAlignment.Center, "center")]
	[DataRow(TextAlignment.DetectFromContent, "left")]
	public async Task When_TextAlignment_Relative_To_FlowDirection(TextAlignment alignment, string side)
	{
		// Rows 02, 13 and 11: Left and Right are relative to the control's FlowDirection, whatever the text's direction,
		// and DetectFromContent aligns to the start of the left-to-right text.
		var sut = Rtb(FlowDirection.RightToLeft, R("Hello world.", Colors.Red));
		sut.TextAlignment = alignment;

		try
		{
			var (_, bitmap) = await Show(sut);
			AssertSide(DrawnX(bitmap, Colors.Red), side, 300);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs a Hebrew fallback font, which only Windows is known to have here.
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Left, "left")]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Right, "right")]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Center, "center")]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.DetectFromContent, "right")]
	[DataRow(FlowDirection.RightToLeft, TextAlignment.Left, "right")]
	[DataRow(FlowDirection.RightToLeft, TextAlignment.Justify, "right")]
	public async Task When_TextAlignment_Relative_To_FlowDirection_Hebrew(FlowDirection flowDirection, TextAlignment alignment, string side)
	{
		// Rows 04, 08, 09, 10, 06 and 20.
		var sut = Rtb(flowDirection, R(Hebrew, Colors.Red));
		sut.TextAlignment = alignment;

		try
		{
			var (_, bitmap) = await Show(sut);
			AssertSide(DrawnX(bitmap, Colors.Red), side, 300);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static void AssertSide(InkSpan ink, string side, double width)
	{
		Assert.IsFalse(ink.IsEmpty, "The text should be drawn");
		switch (side)
		{
			case "left":
				Assert.IsTrue(ink.Min < 6, $"The text ({ink}) should be drawn at the left edge");
				break;
			case "right":
				AssertNear(width, ink.Max + 1, 4, $"The text ({ink}) should be drawn at the right edge");
				break;
			case "center":
				AssertNear(width / 2, (ink.Min + ink.Max + 1) / 2.0, 4, $"The text ({ink}) should be centered");
				break;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs a Hebrew fallback font, which only Windows is known to have here.
	public async Task When_Mixed_Bidi_Text()
	{
		// Row 12: "abc אבג def" in a left-to-right control; the Hebrew word reads right to left inside the line.
		var sut = Rtb(
			FlowDirection.LeftToRight,
			R("abc ", Colors.Red), R("א", Colors.Magenta), R("בג", Colors.Green), R(" def", Colors.Blue));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = DrawnX(bitmap, Colors.Red);
			var magenta = DrawnX(bitmap, Colors.Magenta);
			var green = DrawnX(bitmap, Colors.Green);
			var blue = DrawnX(bitmap, Colors.Blue);

			Assert.IsTrue(red.Max < green.Min && green.Max < magenta.Min && magenta.Max < blue.Min, $"Expected red ({red}), green ({green}), magenta ({magenta}), blue ({blue}) from left to right.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs an Arabic fallback font, which only Windows is known to have here.
	public async Task When_Digits_In_Rtl_Text()
	{
		// Row 07: "سعر 25 دولار" in a right-to-left control: the words read right to left, the number left to right.
		var sut = Rtb(
			FlowDirection.RightToLeft,
			R("سعر ", Colors.Red), R("2", Colors.Blue), R("5", Colors.Magenta), R(" دولار", Colors.Green));

		try
		{
			var (_, bitmap) = await Show(sut);
			var red = DrawnX(bitmap, Colors.Red);
			var blue = DrawnX(bitmap, Colors.Blue);
			var magenta = DrawnX(bitmap, Colors.Magenta);
			var green = DrawnX(bitmap, Colors.Green);

			Assert.IsTrue(green.Max < blue.Min && blue.Max < magenta.Min && magenta.Max < red.Min, $"Expected green ({green}), blue ({blue}), magenta ({magenta}), red ({red}) from left to right.");
			AssertNear(300, red.Max + 1, 4, "The line should start at the control's right edge");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.RightToLeft, TextAlignment.Left)]
	[DataRow(FlowDirection.RightToLeft, TextAlignment.Justify)]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Left)]
	public async Task When_Measured_Without_Width_Limit(FlowDirection flowDirection, TextAlignment alignment)
	{
		// A horizontal StackPanel measures its child with an infinite width: the text still lies within the control and
		// its character rects on its ink.
		var run = R("Hello world", Colors.Red);
		var sut = new RichTextBlock { FontSize = 20, FlowDirection = flowDirection, TextAlignment = alignment };
		sut.Blocks.Add(P(run));
		var panel = new StackPanel { Orientation = Orientation.Horizontal };
		panel.Children.Add(sut);

		try
		{
			var (host, bitmap) = await Show(panel);
			var ink = DrawnX(bitmap, Colors.Red);
			var bounds = sut.TransformToVisual(host).TransformBounds(new Rect(0, 0, sut.ActualWidth, sut.ActualHeight));
			Assert.IsTrue(ink.Min >= bounds.Left - 2 && ink.Max <= bounds.Right + 2, $"The ink ({ink}) should lie in the control ({bounds})");

			var first = run.ContentStart!.GetCharacterRect(LogicalDirection.Forward);
			var inHost = sut.TransformToVisual(host).TransformBounds(new Rect(first.X, first.Y, Math.Max(1, first.Width), first.Height));
			Assert.IsTrue(double.IsFinite(first.X) && inHost.Left >= ink.Min - 4 && inHost.Right <= ink.Max + 4, $"The first character's rect ({inHost}) should lie on the ink ({ink})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.LeftToRight)]
	public async Task When_Justify_Rows(FlowDirection flowDirection)
	{
		// Rows J1 and J4: the stretched lines span the width; the last line isn't stretched and sits at the control's start.
		var sut = new RichTextBlock { Width = 240, FontSize = 20, FlowDirection = flowDirection, TextAlignment = TextAlignment.Justify, TextWrapping = TextWrapping.Wrap };
		// Two words fit a line but not three, whatever the font, so the third line holds the last word alone.
		sut.Blocks.Add(P(R("abcdefghijk abcdefghijk abcdefghijk abcdefghijk abcdefghijk", Colors.Red)));

		try
		{
			var (_, bitmap) = await Show(sut, 280, 140);
			var lines = GetLineBands(sut, bitmap);
			Assert.AreEqual(3, lines.Count, "The text should wrap to 3 lines");

			for (var i = 0; i < lines.Count - 1; i++)
			{
				Assert.IsTrue(lines[i].Min < 6 && lines[i].Max + 1 > 234, $"Line {i} ({lines[i]}) should be stretched across the width");
			}

			var last = lines[^1];
			if (flowDirection == FlowDirection.RightToLeft)
			{
				AssertNear(240, last.Max + 1, 4, $"The last line ({last}) should sit at the right edge");
				Assert.IsTrue(last.Min > 110, $"The last line ({last}) should not be stretched");
			}
			else
			{
				Assert.IsTrue(last.Min < 6 && last.Max < 130, $"The last line ({last}) should sit at the left edge, unstretched");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The red ink of each line, from the lines' character rects.
	private static List<InkSpan> GetLineBands(RichTextBlock sut, RawBitmap bitmap)
	{
		var lines = new List<InkSpan>();
		var tops = new SortedSet<int>();
		for (var position = sut.ContentStart; position is not null && position.Offset < sut.ContentEnd!.Offset; position = position.GetPositionAtOffset(1, LogicalDirection.Forward))
		{
			if (position.Parent is Run)
			{
				var rect = position.GetCharacterRect(LogicalDirection.Forward);
				if (tops.Add((int)Math.Round(rect.Y)))
				{
					lines.Add(InkX(bitmap, Colors.Red, (int)rect.Y + 3, (int)(rect.Y + rect.Height) - 3));
				}
			}
		}
		return lines;
	}

	[TestMethod]
	[DataRow("RLO")]
	[DataRow("RLE with a trailing neutral")]
	public async Task When_Bidi_Controls_In_Text(string row)
	{
		// As in TextBlock: bidi control characters in the text are honored.
		var sut = row switch
		{
			"RLO" => Rtb(FlowDirection.LeftToRight, R("1", Colors.Black), R(RLO + "ab", Colors.Blue), R("cd" + PDF, Colors.Red)),
			_ => Rtb(FlowDirection.LeftToRight, R("x " + RLE, Colors.Black), R("ab", Colors.Blue), R("!!" + PDF, Colors.Red), R(" y", Colors.Black)),
		};

		try
		{
			var (_, bitmap) = await Show(sut);
			var blue = DrawnX(bitmap, Colors.Blue);
			var red = DrawnX(bitmap, Colors.Red);
			Assert.IsTrue(red.Max < blue.Min, $"The red text ({red}) should be drawn left of the blue text ({blue}).");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Left)]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Right)]
	[DataRow(FlowDirection.LeftToRight, TextAlignment.Center)]
	[DataRow(FlowDirection.RightToLeft, TextAlignment.Left)]
	public async Task When_Point_Beside_Link_Line_Resolves_To_Link(FlowDirection flowDirection, TextAlignment alignment)
	{
		// A point beside a line maps to the nearest text position, which is how a click there reaches the link on WinUI
		// (measured: a click beside a line holding only a link raises its Click, on either side).
		var linkRun = new Run { Text = "Link" };
		var hyperlink = new Hyperlink { Foreground = new SolidColorBrush(Colors.Red) };
		hyperlink.Inlines.Add(linkRun);
		var sut = Rtb(flowDirection, hyperlink);
		sut.TextAlignment = alignment;

		try
		{
			var (host, bitmap) = await Show(sut);
			var ink = DrawnX(bitmap, Colors.Red);
			var toRtb = host.TransformToVisual(sut);
			foreach (var x in new[] { ink.Min / 2.0, (ink.Max + 300) / 2.0 })
			{
				if (x < ink.Min - 10 || x > ink.Max + 10)
				{
					var position = sut.GetPositionFromPoint(toRtb.TransformPoint(new Point(x, 12)));
					Assert.AreSame(linkRun, position?.Parent, $"The point at x={x} (link ink {ink}) should resolve into the link's run");
				}
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight, true, 2, "link")]
	[DataRow(FlowDirection.LeftToRight, true, 480, "plain")]
	[DataRow(FlowDirection.LeftToRight, false, 2, "plain")]
	[DataRow(FlowDirection.LeftToRight, false, 480, "link")]
	[DataRow(FlowDirection.RightToLeft, true, 2, "plain")]
	[DataRow(FlowDirection.RightToLeft, true, 480, "link")]
	[DataRow(FlowDirection.RightToLeft, false, 2, "link")]
	[DataRow(FlowDirection.RightToLeft, false, 480, "plain")]
	public async Task When_Point_Beside_Line_Resolves_To_Nearest_Run(FlowDirection flowDirection, bool linkFirst, double x, string expected)
	{
		// Beside "Link more text" or "Some text Link", a point resolves to the run at that end of the text, the link's or
		// the plain one's, so a click there activates the link only on its side. x is in the control's flow space.
		var linkRun = new Run { Text = "Link" };
		var hyperlink = new Hyperlink();
		hyperlink.Inlines.Add(linkRun);
		var plain = new Run { Text = linkFirst ? " more text" : "Some text " };
		var sut = new RichTextBlock { Width = 500, FontSize = 28, FlowDirection = flowDirection };
		sut.Blocks.Add(linkFirst ? P(hyperlink, plain) : P(plain, hyperlink));

		try
		{
			await Show(sut, 600, 100);
			var position = sut.GetPositionFromPoint(new Point(x, 15));
			Assert.AreSame(expected == "link" ? linkRun : plain, position?.Parent, $"The point at x={x} should resolve into the {expected} run");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// ---------------------------------------------------------------- RTL changes beyond the glyphs (F9)

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Rtl_Padding_And_Paragraph_Margin(bool paragraphMargin)
	{
		// The left padding or margin is the start one in right-to-left, so on the right (WinUI ink 216..259).
		var sut = Rtb(FlowDirection.RightToLeft, R("Hello", Colors.Red));
		if (paragraphMargin)
		{
			sut.Blocks[0].Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			sut.Padding = new Thickness(40, 0, 0, 0);
		}

		try
		{
			var (_, bitmap) = await Show(sut);
			var ink = DrawnX(bitmap, Colors.Red);
			AssertNear(260, ink.Max + 1, 4, $"The text ({ink}) should end at the start padding");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Grid CreateColumns()
	{
		var columns = new Grid { Width = 40, Height = 20 };
		columns.ColumnDefinitions.Add(new ColumnDefinition());
		columns.ColumnDefinitions.Add(new ColumnDefinition());
		var first = new Border { Background = new SolidColorBrush(Colors.Lime) };
		var second = new Border { Background = new SolidColorBrush(Colors.Magenta) };
		Grid.SetColumn(second, 1);
		columns.Children.Add(first);
		columns.Children.Add(second);
		return columns;
	}

	private static TextBlock CreateNestedTextBlock()
	{
		var nested = new TextBlock { FontSize = 20 };
		nested.Inlines.Add(R("ab", Colors.Lime));
		nested.Inlines.Add(R("c", Colors.Magenta));
		return nested;
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Rtl_Hosted_Child_Is_Mirrored(FlowDirection flowDirection)
	{
		// A hosted element inherits the control's direction, so a two-column Grid puts its first column on the right in
		// right-to-left (WinUI: lime 254..273 right of magenta 234..253).
		var sut = Rtb(flowDirection, R("ab ", Colors.Black), new InlineUIContainer { Child = CreateColumns() }, R(" cd", Colors.Black));

		try
		{
			var (_, bitmap) = await Show(sut);
			var lime = DrawnX(bitmap, Colors.Lime);
			var magenta = DrawnX(bitmap, Colors.Magenta);
			Assert.IsFalse(lime.IsEmpty || magenta.IsEmpty, "The child should be drawn");
			Assert.AreEqual(flowDirection == FlowDirection.RightToLeft, lime.Min > magenta.Max, $"Column 0 ({lime}) vs column 1 ({magenta})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	// A WriteableBitmap doesn't render on WinUI here (twice), so WinUI's side is not observable; the same flip as the Grid's
	// applies.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Rtl_Hosted_Image_Is_Mirrored(FlowDirection flowDirection)
	{
		// An image inherits the control's direction, so it is mirrored in right-to-left: its left half is drawn on the right.
		var pixels = new byte[20 * 10 * 4];
		for (var i = 0; i < 20 * 10; i++)
		{
			var color = i % 20 < 10 ? Colors.Lime : Colors.Magenta;
			(pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2], pixels[i * 4 + 3]) = (color.B, color.G, color.R, 255);
		}
		var bitmap = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(20, 10);
		await new System.IO.MemoryStream(pixels).AsInputStream().ReadAsync(bitmap.PixelBuffer, bitmap.PixelBuffer.Length, Windows.Storage.Streams.InputStreamOptions.None);
		bitmap.Invalidate();
		var image = new Image { Width = 40, Height = 20, Stretch = Stretch.Fill, Source = bitmap };
		var sut = Rtb(flowDirection, R("ab ", Colors.Black), new InlineUIContainer { Child = image }, R(" cd", Colors.Black));

		try
		{
			var (host, _) = await Show(sut);
			await Task.Delay(300);
			var shot = await Capture(host);
			var lime = DrawnX(shot, Colors.Lime);
			var magenta = DrawnX(shot, Colors.Magenta);
			Assert.IsFalse(lime.IsEmpty || magenta.IsEmpty, "The image should be drawn");
			Assert.AreEqual(flowDirection == FlowDirection.RightToLeft, lime.Min > magenta.Max, $"The image's left half ({lime}) vs its right half ({magenta})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_Rtl_Nested_TextBlock_Reads_Correctly(FlowDirection flowDirection)
	{
		// The nested TextBlock reads "abc" left to right in either direction (WinUI: "ab" 255..274 left of "c" 276..284).
		var sut = Rtb(flowDirection, R("x ", Colors.Black), new InlineUIContainer { Child = CreateNestedTextBlock() }, R(" y", Colors.Black));

		try
		{
			var (_, bitmap) = await Show(sut);
			var lime = DrawnX(bitmap, Colors.Lime);
			var magenta = DrawnX(bitmap, Colors.Magenta);
			Assert.IsTrue(lime.Max < magenta.Min, $"\"ab\" ({lime}) should be drawn left of \"c\" ({magenta})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.RightToLeft)]
	public async Task When_InlineUIContainer_In_Rtl_Paragraph(FlowDirection flowDirection)
	{
		// Rows O1 and O2: the object sits between the runs it separates, where its character rects say.
		var child = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Blue) };
		var sut = Rtb(flowDirection, R("abc ", Colors.Red), new InlineUIContainer { Child = child }, R(" def", Colors.Green));

		try
		{
			var (host, bitmap) = await Show(sut);
			var red = DrawnX(bitmap, Colors.Red);
			var blue = DrawnX(bitmap, Colors.Blue);
			var green = DrawnX(bitmap, Colors.Green);
			Assert.IsTrue(red.Max < blue.Min && blue.Max < green.Min, $"Expected red ({red}), the child ({blue}), green ({green}) from left to right");

			var childBounds = child.TransformToVisual(host).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
			AssertNear(blue.Min, childBounds.Left, 1, "The child's bounds should be where it is drawn");
			if (flowDirection == FlowDirection.RightToLeft)
			{
				AssertNear(300, green.Max + 1, 4, "The line should end at the control's right edge");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs a Hebrew fallback font, which only Windows is known to have here.
	[DataRow(FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.LeftToRight)]
	public async Task When_InlineUIContainer_In_Rtl_Hebrew_Paragraph(FlowDirection flowDirection)
	{
		// Rows O3 and O4: in right-to-left text, the first word is on the right, then the object, then the second word.
		var child = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Blue) };
		var sut = Rtb(flowDirection, R("שלום ", Colors.Red), new InlineUIContainer { Child = child }, R(" עולם", Colors.Green));

		try
		{
			var (host, bitmap) = await Show(sut);
			var red = DrawnX(bitmap, Colors.Red);
			var blue = DrawnX(bitmap, Colors.Blue);
			var green = DrawnX(bitmap, Colors.Green);
			Assert.IsTrue(green.Max < blue.Min && blue.Max < red.Min, $"Expected green ({green}), the child ({blue}), red ({red}) from left to right");

			var childBounds = child.TransformToVisual(host).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
			AssertNear(blue.Min, childBounds.Left, 1, "The child's bounds should be where it is drawn");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// ---------------------------------------------------------------- overflow direction (F4, N1)

	private static (RichTextBlock Master, RichTextBlockOverflow Overflow, StackPanel Panel) CreateOverflow(FlowDirection master, FlowDirection overflow, params Inline[] overflowContent)
	{
		var sut = new RichTextBlock { Width = 300, Height = 30, FontSize = 20, FlowDirection = master };
		sut.Blocks.Add(P(R("First.", Colors.Gray)));
		sut.Blocks.Add(P(overflowContent));
		var target = new RichTextBlockOverflow { Width = 300, Height = 60, FlowDirection = overflow };
		sut.OverflowContentTarget = target;
		var panel = new StackPanel();
		panel.Children.Add(sut);
		panel.Children.Add(target);
		return (sut, target, panel);
	}

	[TestMethod]
	[DataRow(FlowDirection.RightToLeft, FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.LeftToRight, FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.RightToLeft, FlowDirection.RightToLeft)]
	public async Task When_Overflow_FlowDirection_Differs_From_Master(FlowDirection masterDirection, FlowDirection overflowDirection)
	{
		// The overflow lays out and mirrors by the master's direction, whatever its own (WinUI: RTL/LTR ink 197..294, the
		// period at 297, the 'H' rect at 194.7, and the overflow's origin mapped to 300).
		var (_, overflow, panel) = CreateOverflow(masterDirection, overflowDirection, R("Hello world", Colors.Red), R(".", Colors.Blue));

		try
		{
			var (host, bitmap) = await Show(panel);
			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			var red = DrawnX(bitmap, Colors.Red, top);
			var blue = DrawnX(bitmap, Colors.Blue, top);
			Assert.IsTrue(red.Max < blue.Min, $"The period ({blue}) should stay at the end of \"Hello world\" ({red})");

			var masterRtl = masterDirection == FlowDirection.RightToLeft;
			if (masterRtl)
			{
				AssertNear(300, blue.Max + 1, 4, "The overflow's text should be aligned to the master's start, the right edge");
			}
			else
			{
				Assert.IsTrue(red.Min < 6, $"The overflow's text ({red}) should be aligned to the master's start, the left edge");
			}

			// The overflow element is flipped by the master's direction.
			var origin = overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).X;
			AssertNear(masterRtl ? 300 : 0, origin, 1, "The overflow's origin");

			// Its character rects are where the text is drawn.
			var hRect = overflow.TransformToVisual(host).TransformBounds(overflow.ContentStart!.GetPositionAtOffset(2, LogicalDirection.Forward)!.GetCharacterRect(LogicalDirection.Forward));
			AssertNear(red.Min, hRect.X, 4, "The rect of 'H' should be at the ink's left edge");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	// WinUI mirrors the children of an overflow whose own direction differs from its master's by the master's
	// direction, and draws a nested TextBlock with mirrored glyphs there. Each child renders by its own FlowDirection
	// here, as everywhere else.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[DataRow(FlowDirection.RightToLeft, FlowDirection.LeftToRight)]
	[DataRow(FlowDirection.LeftToRight, FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.RightToLeft, FlowDirection.RightToLeft)]
	[DataRow(FlowDirection.LeftToRight, FlowDirection.LeftToRight)]
	public async Task When_Overflow_Hosts_Children_In_Their_Own_Direction(FlowDirection masterDirection, FlowDirection overflowDirection)
	{
		var columns = CreateColumns();
		var nested = CreateNestedTextBlock();
		var (_, overflow, panel) = CreateOverflow(
			masterDirection,
			overflowDirection,
			R("x ", Colors.Black), new InlineUIContainer { Child = columns }, R(" y ", Colors.Black), new InlineUIContainer { Child = nested });

		try
		{
			var (host, bitmap) = await Show(panel);
			var columnsBounds = columns.TransformToVisual(host).TransformBounds(new Rect(0, 0, columns.ActualWidth, columns.ActualHeight));
			var nestedBounds = nested.TransformToVisual(host).TransformBounds(new Rect(0, 0, nested.ActualWidth, nested.ActualHeight));

			var firstColumn = InkIn(bitmap, Colors.Lime, columnsBounds);
			var secondColumn = InkIn(bitmap, Colors.Magenta, columnsBounds);
			Assert.AreEqual(
				columns.FlowDirection == FlowDirection.RightToLeft,
				firstColumn.Min > secondColumn.Max,
				$"The Grid ({columns.FlowDirection}) should put column 0 ({firstColumn}) on its start side of column 1 ({secondColumn})");

			var ab = InkIn(bitmap, Colors.Lime, nestedBounds);
			var c = InkIn(bitmap, Colors.Magenta, nestedBounds);
			Assert.IsTrue(ab.Max < c.Min, $"The nested TextBlock should read \"abc\" (\"ab\" {ab}, \"c\" {c})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The ink of a colour inside a rect, so each child's colours are read on their own.
	private static InkSpan InkIn(RawBitmap bitmap, Color color, Rect rect)
	{
		int min = int.MaxValue, max = -1;
		for (var y = Math.Max(0, (int)Math.Ceiling(rect.Top)); y < Math.Min(bitmap.Height, (int)Math.Floor(rect.Bottom)); y++)
		{
			for (var x = Math.Max(0, (int)Math.Ceiling(rect.Left)); x < Math.Min(bitmap.Width, (int)Math.Floor(rect.Right)); x++)
			{
				var pixel = bitmap.GetPixel(x, y);
				if (pixel.A > 128 && Math.Abs(pixel.R - color.R) + Math.Abs(pixel.G - color.G) + Math.Abs(pixel.B - color.B) < 90)
				{
					(min, max) = (Math.Min(min, x), Math.Max(max, x));
				}
			}
		}
		return new InkSpan(min, max);
	}

	// The position before a run's first character.
	private static TextPointer FirstPosition(RichTextBlock sut, Run run)
	{
		for (var position = sut.ContentStart; position is not null && position.Offset < sut.ContentEnd!.Offset; position = position.GetPositionAtOffset(1, LogicalDirection.Forward))
		{
			if (ReferenceEquals(position.Parent, run))
			{
				return position;
			}
		}

		throw new InvalidOperationException("The run has no position");
	}

	[TestMethod]
	[DataRow("objects")]
	[DataRow("runs with their own direction")]
	public async Task When_Master_FlowDirection_Changes_Updates_Overflow(string content)
	{
		// When no inherited FlowDirection reaches the content, the master's own change must re-lay out and re-flip the
		// overflow.
		var master = new RichTextBlock { Width = 300, Height = 30, FontSize = 20 };
		if (content == "objects")
		{
			master.Blocks.Add(P(new InlineUIContainer { Child = new Border { Width = 280, Height = 20, Background = new SolidColorBrush(Colors.Gray) } }));
			master.Blocks.Add(P(new InlineUIContainer { Child = new Border { Width = 40, Height = 20, Background = new SolidColorBrush(Colors.Red) } }));
		}
		else
		{
			master.Blocks.Add(P(R("abc", Colors.Gray, FlowDirection.LeftToRight)));
			master.Blocks.Add(P(R("WWW", Colors.Red, FlowDirection.LeftToRight)));
		}

		var overflow = new RichTextBlockOverflow { Width = 300, Height = 60 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			var (host, bitmap) = await Show(panel);
			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			Assert.IsTrue(DrawnX(bitmap, Colors.Red, top).Min < 6, "Precondition: the overflow's content starts at the left edge");

			master.FlowDirection = FlowDirection.RightToLeft;
			bitmap = await Capture(host);

			var red = DrawnX(bitmap, Colors.Red, top);
			AssertNear(300, red.Max + 1, 3, $"The overflow's content ({red}) should move to the right edge");
			AssertNear(300, overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).X, 1, "The overflow should be mirrored");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Overflow_Resumes_At_Trailing_LineBreak()
	{
		// The master holds "abc" and its line break; the empty line after the break is the overflow's first.
		var master = new RichTextBlock { Width = 300, Height = 30, FontSize = 20 };
		master.Blocks.Add(P(R("abc", Colors.Red), new LineBreak()));
		master.Blocks.Add(P(R("next", Colors.Blue)));
		var overflow = new RichTextBlockOverflow { Width = 300, Height = 100 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			var (host, bitmap) = await Show(panel, 340, 160);
			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			Assert.IsFalse(InkX(bitmap, Colors.Red, 0, top - 1).IsEmpty, "The master should show \"abc\"");
			Assert.IsTrue(InkX(bitmap, Colors.Red, top).IsEmpty, "The overflow should not repeat \"abc\"");

			var (blueTop, _) = DrawnY(bitmap, Colors.Blue);
			Assert.IsTrue(blueTop > top + 22, $"\"next\" (top {blueTop}) should follow the empty line at the overflow's top ({top})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Overflow_Resumes_At_Object_Only_Line()
	{
		// The master fits "ab"; the object that wrapped after it takes no text position, and the overflow must still
		// show it, then the next paragraph.
		var master = new RichTextBlock { Width = 100, Height = 30, FontSize = 20 };
		master.Blocks.Add(P(R("ab ", Colors.Black), new InlineUIContainer { Child = new Border { Width = 90, Height = 20, Background = new SolidColorBrush(Colors.Red) } }));
		master.Blocks.Add(P(R("next", Colors.Blue)));
		var overflow = new RichTextBlockOverflow { Width = 100, Height = 100 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			var (host, bitmap) = await Show(panel, 140, 160);
			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			Assert.IsTrue(InkX(bitmap, Colors.Red, 0, top - 1).IsEmpty, "Precondition: the object wraps to the overflow");
			var (redTop, redBottom) = DrawnY(bitmap, Colors.Red);
			Assert.IsTrue(redTop >= top, $"The object (top {redTop}) should be drawn in the overflow (top {top})");
			var (blueTop, _) = DrawnY(bitmap, Colors.Blue);
			Assert.IsTrue(blueTop > redBottom, $"\"next\" (top {blueTop}) should follow the object (bottom {redBottom})");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Needs an Arabic fallback font, which only Windows is known to have here.
	public async Task When_Paragraph_Split_Across_Overflow_Keeps_Bidi()
	{
		// An Arabic paragraph whose overflow starts with Latin text: the overflow's line keeps the paragraph's right-to-left
		// level, so the trailing period is drawn left of "UnoPlatform".
		var master = new RichTextBlock { Width = 160, Height = 30, FontSize = 20, FlowDirection = FlowDirection.RightToLeft };
		master.Blocks.Add(P(R("مرحبا بكم ", Colors.Green), R("UnoPlatform", Colors.Red), R(".", Colors.Blue)));
		var overflow = new RichTextBlockOverflow { Width = 160, Height = 60 };
		master.OverflowContentTarget = overflow;
		var panel = new StackPanel();
		panel.Children.Add(master);
		panel.Children.Add(overflow);

		try
		{
			var (host, bitmap) = await Show(panel, 200, 120);
			var top = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
			Assert.IsTrue(InkX(bitmap, Colors.Red, 0, top - 1).IsEmpty, "Precondition: \"UnoPlatform\" should wrap to the overflow");

			var red = DrawnX(bitmap, Colors.Red, top);
			var blue = DrawnX(bitmap, Colors.Blue, top);
			Assert.IsTrue(blue.Max < red.Min, $"The period ({blue}) should be drawn left of \"UnoPlatform\" ({red}) in the right-to-left paragraph");
			AssertNear(160, red.Max + 1, 4, "The line should start at the right edge");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// ---------------------------------------------------------------- foreground (N5)

	[TestMethod]
	[DataRow("control")]
	[DataRow("run")]
	public async Task When_Foreground_Changes_After_Load(string owner)
	{
		// A new brush only repaints, as in WinUI: the runs inherit the control's, or the run has its own.
		var run = new Run { Text = "Hello world" };
		var sut = new RichTextBlock { Width = 300, FontSize = 20 };
		sut.Blocks.Add(P(run));
		if (owner == "run")
		{
			run.Foreground = new SolidColorBrush(Colors.Red);
		}
		else
		{
			sut.Foreground = new SolidColorBrush(Colors.Red);
		}

		try
		{
			var (host, bitmap) = await Show(sut);
			Assert.IsFalse(InkX(bitmap, Colors.Red).IsEmpty, "Precondition: red ink");

			var layoutUpdates = 0;
			void OnLayoutUpdated(object? sender, object e) => layoutUpdates++;
			sut.LayoutUpdated += OnLayoutUpdated;
			try
			{
				if (owner == "run")
				{
					run.Foreground = new SolidColorBrush(Colors.Blue);
				}
				else
				{
					sut.Foreground = new SolidColorBrush(Colors.Blue);
				}

				bitmap = await Capture(host);
			}
			finally
			{
				sut.LayoutUpdated -= OnLayoutUpdated;
			}

			Assert.IsTrue(InkX(bitmap, Colors.Red).IsEmpty, "The red ink should be gone");
			Assert.IsFalse(InkX(bitmap, Colors.Blue).IsEmpty, "The text should be drawn with the new brush");
			Assert.AreEqual(0, layoutUpdates, "A new brush should not run layout");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Foreground_Brush_Colour_Animates()
	{
		// A brush whose colour changes in place only repaints: the engine reads the colour when it draws.
		var brush = new SolidColorBrush(Colors.Red);
		var sut = new RichTextBlock { Width = 300, FontSize = 20, Foreground = brush };
		sut.Blocks.Add(P(new Run { Text = "Hello world" }));

		try
		{
			var (host, bitmap) = await Show(sut);
			Assert.IsFalse(InkX(bitmap, Colors.Red).IsEmpty, "Precondition: red ink");

			var layoutUpdates = 0;
			void OnLayoutUpdated(object? sender, object e) => layoutUpdates++;
			sut.LayoutUpdated += OnLayoutUpdated;
			try
			{
				brush.Color = Colors.Blue;
				bitmap = await Capture(host);
			}
			finally
			{
				sut.LayoutUpdated -= OnLayoutUpdated;
			}

			Assert.IsTrue(InkX(bitmap, Colors.Red).IsEmpty, "The red ink should be gone");
			Assert.IsFalse(InkX(bitmap, Colors.Blue).IsEmpty, "The text should be drawn in the brush's new colour");
			Assert.AreEqual(0, layoutUpdates, "A colour change should not run layout");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// ---------------------------------------------------------------- highlight band and stacking

	[TestMethod]
	[DataRow(LineStackingStrategy.MaxHeight, 0.0)]
	[DataRow(LineStackingStrategy.BlockLineHeight, 40.0)]
	[DataRow(LineStackingStrategy.BaselineToBaseline, 40.0)]
	public async Task When_Painted_Lines_Lie_In_Line_Bounds(LineStackingStrategy strategy, double lineHeight)
	{
		// Each line's ink and its highlight lie in the band its character rects give, which is the line's advance.
		var sut = new RichTextBlock { Width = 300, FontSize = 20, LineStackingStrategy = strategy, LineHeight = lineHeight };
		sut.Blocks.Add(P(R("Hello", Colors.Red), new LineBreak(), R("World", Colors.Red)));
		var highlighter = new TextHighlighter { Background = new SolidColorBrush(Colors.Yellow), Foreground = new SolidColorBrush(Colors.Red) };
		highlighter.Ranges.Add(new TextRange { StartIndex = 0, Length = 11 });
		sut.TextHighlighters.Add(highlighter);

		try
		{
			var (host, bitmap) = await Show(sut, 340, 160);
			var toHost = sut.TransformToVisual(host);
			var bands = new List<Rect>();
			foreach (var run in sut.Blocks.OfType<Paragraph>().Single().Inlines.OfType<Run>())
			{
				bands.Add(toHost.TransformBounds(FirstPosition(sut, run).GetCharacterRect(LogicalDirection.Forward)));
			}

			Assert.IsTrue(bands[1].Y > bands[0].Y, "Precondition: two lines");
			for (var i = 0; i < bands.Count; i++)
			{
				var band = bands[i];
				var ink = InkX(bitmap, Colors.Red, (int)band.Top + 1, (int)band.Bottom - 1);
				Assert.IsFalse(ink.IsEmpty, $"Line {i}'s ink should lie in its band {band}");

				// The highlight fills the band: just inside its top and bottom edges.
				var top = InkX(bitmap, Colors.Yellow, (int)Math.Ceiling(band.Top) + 1, (int)Math.Ceiling(band.Top) + 1);
				var bottom = InkX(bitmap, Colors.Yellow, (int)Math.Floor(band.Bottom) - 2, (int)Math.Floor(band.Bottom) - 2);
				Assert.IsFalse(top.IsEmpty || bottom.IsEmpty, $"Line {i}'s highlight should fill its band {band} (top {top}, bottom {bottom})");
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
	public async Task When_Selection_Highlight_Fills_Line_Advance(FlowDirection flowDirection)
	{
		// The selection covers each line's whole advance, as its bounds do, so two lines' highlights touch.
		var sut = Rtb(flowDirection, R("Hello", Colors.Red), new LineBreak(), R("World", Colors.Red));
		sut.LineHeight = 40;
		sut.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
		sut.SelectionHighlightColor = new SolidColorBrush(Colors.Yellow);

		try
		{
			var (host, _) = await Show(sut, 340, 120);
			sut.Focus(FocusState.Programmatic);
			sut.SelectAll();
			var bitmap = await Capture(host);

			// Rows 1 and 79 are inside the two 40 px advances; row 39/40 is where they meet.
			foreach (var y in new[] { 1, 38, 41, 78 })
			{
				Assert.IsFalse(InkX(bitmap, Colors.Yellow, y, y).IsEmpty, $"The selection should fill row {y}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// ---------------------------------------------------------------- flyout (PopupPanel parity)

	[TestMethod]
	public async Task When_Rtl_RichTextBlock_In_Rtl_Flyout()
	{
		// The flyout's popup panel never mirrors itself; its right-to-left content does, and so does the RichTextBlock in it:
		// its text starts at its right edge.
		var sut = Rtb(FlowDirection.RightToLeft, R("Hello", Colors.Red));
		sut.Width = 200;
		var button = new Button { Content = "Open", FlowDirection = FlowDirection.RightToLeft };
		var flyout = new Flyout { Content = sut, Placement = FlyoutPlacementMode.Bottom };

		try
		{
			await Show(button);
			flyout.ShowAt(button);
			await WindowHelper.WaitForLoaded(sut);
			await WindowHelper.WaitForIdle();

			var toWindow = sut.TransformToVisual(null);
			var bounds = toWindow.TransformBounds(new Rect(0, 0, sut.Width, sut.ActualHeight));
			var first = toWindow.TransformBounds(sut.ContentStart!.GetPositionAtOffset(2, LogicalDirection.Forward)!.GetCharacterRect(LogicalDirection.Forward));
			var last = toWindow.TransformBounds(sut.ContentStart!.GetPositionAtOffset(7, LogicalDirection.Forward)!.GetCharacterRect(LogicalDirection.Forward));

			Assert.IsTrue(first.X < last.X, $"\"Hello\" reads left to right ('H' at {first.X}, its end at {last.X})");
			AssertNear(bounds.Right, last.X, 4, $"The text should end at the RichTextBlock's right edge ({bounds})");
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}
}
