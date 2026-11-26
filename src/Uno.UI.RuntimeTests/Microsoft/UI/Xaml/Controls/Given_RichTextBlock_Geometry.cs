using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Geometry and hit-testing that match native WinUI; each test also runs on WinUI.
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Geometry
	{
		private const string WrappingText = "Hello world this is a wrapped paragraph of text.";

#if HAS_UNO
		private const bool IsNativeWinUI = false;
#else
		private const bool IsNativeWinUI = true;
#endif

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow("LTR-Left")]
		[DataRow("LTR-Center")]
		[DataRow("LTR-Right")]
		[DataRow("LTR-Left-Padding")]
		[DataRow("LTR-Left-TwoParagraphs")]
		[DataRow("LTR-Left-Wrapped")]
		[DataRow("LTR-Right-TrailingSpaces")]
		[DataRow("LTR-Justify-Wrapped")]
		[DataRow("RTL-Left")]
		[DataRow("RTL-Left-Padding")]
		[DataRow("RTL-Left-TwoParagraphs")]
		[DataRow("RTL-Left-Wrapped")]
		[DataRow("RTL-Right")]
		[DataRow("RTL-Center")]
		public async Task When_CharacterRect_On_Ink(string row)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20, Foreground = new SolidColorBrush(Colors.Red) };
			var text = "Hello world.";
			var rtl = row.StartsWith("RTL", StringComparison.Ordinal);
			var alignment = row.Split('-')[1];
			rtb.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
			rtb.TextAlignment = alignment switch
			{
				"Center" => TextAlignment.Center,
				"Right" => TextAlignment.Right,
				"Justify" => TextAlignment.Justify,
				_ => TextAlignment.Left,
			};

			// The text's last character, after which a position's rect has no ink to match (trailing spaces).
			var lastInkOffset = int.MaxValue;
			switch (row)
			{
				case "LTR-Left-Padding":
				case "RTL-Left-Padding":
					// Wraps, so its lines run up to the end padding.
					rtb.Padding = new Thickness(10, 0, 30, 0);
					text = WrappingText;
					break;
				case "LTR-Left-Wrapped":
				case "RTL-Left-Wrapped":
					rtb.Width = 150;
					text = WrappingText;
					break;
				case "LTR-Right-TrailingSpaces":
					text = "Hello   ";
					lastInkOffset = 2 + "Hello".Length;
					break;
				case "LTR-Justify-Wrapped":
					rtb.Width = 150;
					text = "Hello world this is a justified paragraph of text.";
					break;
			}

			rtb.Blocks.Add(CreateParagraph(new Run { Text = text }));
			if (row.EndsWith("TwoParagraphs", StringComparison.Ordinal))
			{
				rtb.Blocks.Add(CreateParagraph(new Run { Text = "Second one" }));
			}

			try
			{
				var host = await Show(rtb);
				var bitmap = await UITestHelper.ScreenShot(host);

				// Group the rects of every position in a run by line, and compare each line's extent with its ink.
				var toHost = rtb.TransformToVisual(host);
				// The explicit Width: ActualWidth is the width of the text, as in WinUI.
				var bounds = toHost.TransformBounds(new Rect(0, 0, rtb.Width, rtb.ActualHeight));
				var lines = new SortedDictionary<int, (double Left, double Right, double Top, double Height)>();
				var end = rtb.ContentEnd!.Offset;
				for (var position = rtb.ContentStart; position is not null && position.Offset <= end; position = position.GetPositionAtOffset(1, LogicalDirection.Forward))
				{
					if (position.Parent is Run && position.Offset <= lastInkOffset)
					{
						var rect = toHost.TransformBounds(position.GetCharacterRect(LogicalDirection.Forward));
						var key = (int)Math.Round(rect.Y);
						lines[key] = lines.TryGetValue(key, out var line)
							? (Math.Min(line.Left, rect.X), Math.Max(line.Right, rect.X), line.Top, line.Height)
							: (rect.X, rect.X, rect.Y, rect.Height);
					}

					if (position.Offset == end)
					{
						break;
					}
				}

				if (text != "Hello world." && row != "LTR-Right-TrailingSpaces")
				{
					Assert.IsTrue(lines.Count > 1, $"The text should wrap ({lines.Count} line)");
				}
				else
				{
					Assert.AreEqual(row.EndsWith("TwoParagraphs", StringComparison.Ordinal) ? 2 : 1, lines.Count, "Lines with character rects");
				}

				var lineIndex = 0;
				foreach (var line in lines.Values)
				{
					var isLastLine = ++lineIndex == lines.Count;
					// Stay inside the line so a neighbouring line's ink doesn't count.
					var ink = GetInkBounds(bitmap, Colors.Red, (int)line.Top + 2, (int)(line.Top + line.Height) - 2);
					Assert.IsTrue(ink.Right >= 0, $"No ink in the line at y={line.Top}");

					// Where the line is: rects and ink could otherwise move together. Ink edges are pixel columns,
					// so the ink spans [Left, Right + 1). 4 px covers the side bearings of 'H', 'S', 'w' and '.' at 20 px.
					// Alignment is relative to the control's FlowDirection: Left is the start, the right edge in right-to-left.
					var startsAtRight = rtl == (alignment is "Left" or "Justify");
					switch (row)
					{
						case "LTR-Center":
						case "RTL-Center":
							Assert.AreEqual(bounds.Left + bounds.Width / 2, (ink.Left + ink.Right + 1) / 2.0, 4, "The ink should be centered in the control");
							break;
						case "LTR-Left-Padding":
							Assert.AreEqual(bounds.Left + 10, ink.Left, 4, $"The line at y={line.Top} should start at the left padding");
							Assert.IsTrue(ink.Right + 1 <= bounds.Right - 30, $"The line at y={line.Top} should end before the right padding (ink ends at {ink.Right + 1}, padding starts at {bounds.Right - 30})");
							break;
						case "RTL-Left-Padding":
							// The left padding is the start one, on the right.
							Assert.AreEqual(bounds.Right - 10, ink.Right + 1, 4, $"The line at y={line.Top} should end at the start padding");
							Assert.IsTrue(ink.Left >= bounds.Left + 30, $"The line at y={line.Top} should start after the end padding (ink starts at {ink.Left}, padding ends at {bounds.Left + 30})");
							break;
						case "LTR-Justify-Wrapped" when !isLastLine:
							Assert.AreEqual(bounds.Left, ink.Left, 4, $"The stretched line at y={line.Top} should start at the control's left edge");
							Assert.AreEqual(bounds.Right, ink.Right + 1, 4, $"The stretched line at y={line.Top} should end at the control's right edge");
							break;
						default:
							if (startsAtRight)
							{
								Assert.AreEqual(bounds.Right, ink.Right + 1, 4, $"The line at y={line.Top} should end at the control's right edge");
							}
							else
							{
								Assert.AreEqual(bounds.Left, ink.Left, 4, $"The line at y={line.Top} should start at the control's left edge");
							}
							break;
					}

					// WinUI's RichTextBlock reports these rects on the side opposite its ink, where TextBlock is right. The
					// rects follow the ink here, as TextBlock's do on WinUI.
					if (IsNativeWinUI && row is "RTL-Right" or "RTL-Center")
					{
						continue;
					}

					Assert.AreEqual(ink.Left, line.Left, 4, $"First rect of the line at y={line.Top} vs the ink's left edge");
					Assert.AreEqual(ink.Right + 1, line.Right, 4, $"Last rect of the line at y={line.Top} vs the ink's right edge");
				}
			}
			finally
			{
				await Clear();
			}
		}

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow("01-plain")]
		[DataRow("02-rtl")]
		[DataRow("16-link")]
		[DataRow("17-rtl-link")]
		[DataRow("19-link-right")]
		public async Task When_GetPositionFromPoint_Beside_Line(string row)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20 };
			if (row is "01-plain" or "02-rtl")
			{
				rtb.Blocks.Add(CreateParagraph(new Run { Text = "Hello world." }));
			}
			else
			{
				var link = new Hyperlink();
				link.Inlines.Add(new Run { Text = "Link" });
				rtb.Blocks.Add(CreateParagraph(link));
			}

			rtb.FlowDirection = row.Contains("rtl", StringComparison.Ordinal) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
			rtb.TextAlignment = row == "19-link-right" ? TextAlignment.Right : TextAlignment.Left;

			try
			{
				var host = await Show(rtb);

				// Points in the host, left and right of the line whichever side it is drawn on. Offsets count the
				// paragraph's and the run's start (and the link's), so "Hello world." spans 2 to 14 and "Link" 3 to 7.
				// A left-to-right line maps a point on its left to its start and one on its right to its end.
				var toRtb = host.TransformToVisual(rtb);
				var left = rtb.GetPositionFromPoint(toRtb.TransformPoint(new Point(-5, 12)))?.Offset;
				var right = rtb.GetPositionFromPoint(toRtb.TransformPoint(new Point(305, 12)))?.Offset;
				var (start, end) = row is "01-plain" or "02-rtl" ? (2, 14) : (3, 7);
				Assert.AreEqual(start, left, "Left of the line maps to its reading start");
				Assert.AreEqual(end, right, "Right of the line maps to its reading end");
			}
			finally
			{
				await Clear();
			}
		}

		// The Hebrew and Arabic text needs a fallback font, which only Windows is known to have here.
		[TestMethod]
		[RequiresScaling(1f)]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)]
		[DataRow("06-rtl-hebrew")]
		[DataRow("18-rtl-arabic-link")]
		public async Task When_GetPositionFromPoint_Beside_Rtl_Line(string row)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20, FlowDirection = FlowDirection.RightToLeft };
			if (row == "06-rtl-hebrew")
			{
				rtb.Blocks.Add(CreateParagraph(new Run { Text = "שלום עולם" }));
			}
			else
			{
				var link = new Hyperlink();
				link.Inlines.Add(new Run { Text = "رابط" });
				rtb.Blocks.Add(CreateParagraph(link));
			}

			try
			{
				var host = await Show(rtb);

				// A right-to-left line maps a point on its right to its start and one on its left to its end.
				var toRtb = host.TransformToVisual(rtb);
				var left = rtb.GetPositionFromPoint(toRtb.TransformPoint(new Point(-5, 12)))?.Offset;
				var right = rtb.GetPositionFromPoint(toRtb.TransformPoint(new Point(305, 12)))?.Offset;
				var (start, end) = row == "06-rtl-hebrew" ? (2, 11) : (3, 7);
				Assert.AreEqual(end, left, "Left of the line maps to its reading end");
				Assert.AreEqual(start, right, "Right of the line maps to its reading start");
			}
			finally
			{
				await Clear();
			}
		}

		// Expected values measured on native WinUI 3 (Windows 11, scale 1), ±1 px.
		[TestMethod]
		[DataRow("H1 plain", 27.2, 22.2)]
		[DataRow("H4 LineHeight40 MaxHeight", 40.0, 21.6)]
		[DataRow("H6 LineHeight40 B2B 2 lines", 67.2, 22.2)]
		[DataRow("H7 LineHeight10 Block 2 lines", 20.0, 8.1)]
		[DataRow("H8 empty", 27.2, 22.2)]
		[DataRow("H9 TextLineBounds Tight", 14.4, 14.4)]
		public async Task When_Line_Heights_Match_WinUI(string row, double height, double baseline)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20 };
			switch (row)
			{
				case "H4 LineHeight40 MaxHeight":
					rtb.LineHeight = 40;
					break;
				case "H6 LineHeight40 B2B 2 lines":
					rtb.LineHeight = 40;
					rtb.LineStackingStrategy = LineStackingStrategy.BaselineToBaseline;
					break;
				case "H7 LineHeight10 Block 2 lines":
					rtb.LineHeight = 10;
					rtb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
					break;
				case "H9 TextLineBounds Tight":
					rtb.TextLineBounds = TextLineBounds.Tight;
					break;
			}

			rtb.Blocks.Add(row switch
			{
				"H6 LineHeight40 B2B 2 lines" or "H7 LineHeight10 Block 2 lines" => CreateParagraph(new Run { Text = "Hello" }, new LineBreak(), new Run { Text = "World" }),
				"H8 empty" => CreateParagraph(),
				_ => CreateParagraph(new Run { Text = "Hello" }),
			});

			await AssertLineHeights(rtb, height, baseline);
		}

		// The CJK text needs a fallback font, which only Windows is known to have here.
		// Expected values measured on native WinUI 3 (Windows 11, scale 1), ±1 px.
		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Line_Height_With_Fallback_Font_Matches_WinUI()
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20 };
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "Hello 你好" }));

			await AssertLineHeights(rtb, 27.2, 22.2);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Object_Only_Paragraph_Keeps_Following_Paragraphs(bool twoWideObjects)
		{
			var rtb = new RichTextBlock { Width = twoWideObjects ? 100 : 200, FontSize = 20 };
			// Narrower than "Text", so text drawn over the object still shows beside it. Two wide objects take a line each.
			rtb.Blocks.Add(twoWideObjects
				? CreateParagraph(
					new InlineUIContainer { Child = new Border { Width = 90, Height = 20, Background = new SolidColorBrush(Colors.Red) } },
					new InlineUIContainer { Child = new Border { Width = 90, Height = 20, Background = new SolidColorBrush(Colors.Red) } })
				: CreateParagraph(new InlineUIContainer { Child = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Red) } }));
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "Text", Foreground = new SolidColorBrush(Colors.Blue) }));

			try
			{
				var bitmap = await UITestHelper.ScreenShot(await Show(rtb));

				var red = GetInkBounds(bitmap, Colors.Red);
				var blue = GetInkBounds(bitmap, Colors.Blue);
				Assert.IsTrue(red.Right >= 0, "The object is drawn");
				Assert.IsTrue(blue.Right >= 0, "The paragraph after the object-only paragraph is drawn");
				Assert.IsTrue(blue.Top > red.Bottom, $"The following paragraph is drawn below the object (text top {blue.Top}, object bottom {red.Bottom})");
				if (twoWideObjects)
				{
					// Both objects, a line each, then the paragraph after them (WinUI: 66.6).
					Assert.IsTrue(red.Bottom - red.Top > 30, $"Both objects are drawn, on two lines (object ink {red.Top}..{red.Bottom})");
					Assert.AreEqual(66.6, rtb.ActualHeight, 2, "Height");
				}
			}
			finally
			{
				await Clear();
			}
		}

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Object_At_Page_Break(bool objectFirstOnOverflow)
		{
			// The master shows one line of 160 px. "AAAA " takes about 60 px, so a 30 px object stays on it
			// and a 120 px one wraps to the next line, which is the overflow's first.
			var child = new Border { Width = objectFirstOnOverflow ? 120 : 30, Height = 16, Background = new SolidColorBrush(Colors.Red) };
			var master = new RichTextBlock { Width = 160, Height = 28, FontSize = 20 };
			master.Blocks.Add(CreateParagraph(new Run { Text = "AAAA " }, new InlineUIContainer { Child = child }, new Run { Text = " BBBB CCCC DDDD" }));
			var overflow = new RichTextBlockOverflow { Width = 160, Height = 100 };
			master.OverflowContentTarget = overflow;
			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				var host = await Show(panel);
				var bitmap = await UITestHelper.ScreenShot(host);

				var overflowTop = (int)overflow.TransformToVisual(host).TransformPoint(new Point(0, 0)).Y;
				var onMaster = GetInkBounds(bitmap, Colors.Red, 0, overflowTop - 1).Right >= 0;
				var onOverflow = GetInkBounds(bitmap, Colors.Red, overflowTop).Right >= 0;

				Assert.IsTrue(onMaster != onOverflow, $"The object is drawn exactly once (master: {onMaster}, overflow: {onOverflow})");
				Assert.AreEqual(objectFirstOnOverflow, onOverflow, "The object is drawn on the page holding its line");
			}
			finally
			{
				await Clear();
			}
		}

		// Measured on WinUI: a TextBlock too narrow for a character and the ellipsis still shows its first cluster and the
		// ellipsis ("W…" from "WW…" at 47 px wide; " …" from " Hello world", whose first cluster is the space), and
		// RichTextBlock trims the same way.
		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow("TextBlock", "W||||||||")]
		[DataRow("TextBlock", " Hello world")]
		[DataRow("RichTextBlock", "W||||||||")]
		[DataRow("RichTextBlock", " Hello world")]
		public async Task When_Trimmed_Without_Room_Keeps_First_Cluster(string control, string text)
		{
			var probe = new TextBlock { Text = "W", FontSize = 40 };
			await TextInkHelper.Show(probe, 400, 100);
			var glyph = probe.ActualWidth;
			probe.Text = "…";
			await WindowHelper.WaitForIdle();
			var ellipsis = probe.ActualWidth;
			await Clear();

			// Room for the first character and half the ellipsis, so not for a second character.
			var width = glyph + ellipsis / 2;
			FrameworkElement sut;
			if (control == "TextBlock")
			{
				sut = new TextBlock { Text = text, FontSize = 40, Width = width, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = new SolidColorBrush(Colors.Red) };
			}
			else
			{
				var rtb = new RichTextBlock { FontSize = 40, Width = width, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = new SolidColorBrush(Colors.Red) };
				rtb.Blocks.Add(CreateParagraph(new Run { Text = text }));
				sut = rtb;
			}

			try
			{
				var (_, bitmap) = await TextInkHelper.Show(sut, 400, 100);
				var (top, bottom) = TextInkHelper.DrawnY(bitmap, Colors.Red);
				if (text.StartsWith(' '))
				{
					// The space and the ellipsis: only the dots are drawn.
					Assert.IsTrue(bottom - top < 10, $"Only the ellipsis should be drawn (ink rows {top}..{bottom})");
					return;
				}

				Assert.IsTrue(bottom - top > 15, $"A character should be drawn (ink rows {top}..{bottom})");
				// "W…": no bar after the W, whose ink ends before the glyph's advance.
				var ink = TextInkHelper.Letters(bitmap, Colors.Red);
				Assert.IsTrue(ink.Letters.Max < glyph, $"Only the first character should be kept: letters {ink.Letters}, glyph {glyph:F1}");
			}
			finally
			{
				await Clear();
			}
		}

		// Measured on WinUI: an object-only paragraph's end is after its object, in either direction (flow coordinates).
		// Known limit: an object takes no position in Uno's layout tree, so the paragraph's one position is both its
		// start and its end, before the object.
		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow(FlowDirection.LeftToRight)]
		[DataRow(FlowDirection.RightToLeft)]
		public async Task When_Object_Only_Paragraph_Ends_After_Object(FlowDirection flowDirection)
		{
			var child = new Border { Width = 40, Height = 20, Background = new SolidColorBrush(Colors.Red) };
			var rtb = new RichTextBlock { Width = 200, FontSize = 20, FlowDirection = flowDirection };
			var paragraph = CreateParagraph(new InlineUIContainer { Child = child });
			rtb.Blocks.Add(paragraph);

			try
			{
				await Show(rtb);
				Assert.AreEqual(0, paragraph.ContentStart!.GetCharacterRect(LogicalDirection.Forward).X, 1, "The paragraph starts before the object");
				Assert.AreEqual(IsNativeWinUI ? 40 : 0, paragraph.ContentEnd!.GetCharacterRect(LogicalDirection.Forward).X, 1, "The paragraph's end");
			}
			finally
			{
				await Clear();
			}
		}

		// Measured on WinUI: default tab stops every four times the font size (80 px at 20 px), in any font.
		[TestMethod]
		[RequiresScaling(1f)]
		public async Task When_Tab_Uses_Default_Incremental_Tab()
		{
			var rtb = new RichTextBlock { Width = 400, FontSize = 20 };
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "a	b	c" }, new LineBreak(), new Run { Text = "abcdefgh	b" }));

			try
			{
				await Show(rtb);
				var x = Lefts(rtb);
				Assert.AreEqual(80, x[2], 1, "The first tab stop");
				Assert.AreEqual(160, x[4], 1, "The second tab stop");
				Assert.AreEqual(160, x[^1], 1, "A tab after text wider than a stop goes to the next one");
			}
			finally
			{
				await Clear();
			}
		}

		// Measured on WinUI: each character gets the spacing after it, except the paragraph's last.
		[TestMethod]
		[RequiresScaling(1f)]
		public async Task When_CharacterSpacing_Follows_Each_Character()
		{
			async Task<double[]> Measure(int spacing)
			{
				var rtb = new RichTextBlock { Width = 400, FontSize = 20, CharacterSpacing = spacing };
				var run = new Run { Text = "abcd" };
				rtb.Blocks.Add(CreateParagraph(run));
				await Show(rtb);
				var lefts = Lefts(rtb);
				var end = run.ContentEnd!.GetCharacterRect(LogicalDirection.Backward).X;
				await Clear();
				return [.. lefts, end];
			}

			var plain = await Measure(0);
			var spaced = await Measure(500);
			for (var i = 1; i < 4; i++)
			{
				Assert.AreEqual(plain[i] + 10 * i, spaced[i], 1, $"Character {i} should move by the spacing of those before it");
			}
			Assert.AreEqual(plain[^1] + 30, spaced[^1], 1, "The last character gets no spacing after it");
		}

		// Measured on WinUI with Calibri: "ffi" is one ligature, and the positions inside it split it evenly. Known
		// difference: the engine takes a ligature as one cluster, whose positions all share its left edge.
		[TestMethod]
		[RequiresScaling(1f)]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI)] // Calibri
		public async Task When_Ligature_Positions_Split_It_Evenly()
		{
			var rtb = new RichTextBlock { Width = 400, FontSize = 20, FontFamily = new FontFamily("Calibri") };
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "office" }));

			try
			{
				await Show(rtb);
				var x = Lefts(rtb);
				var (f1, f2, i) = (x[2] - x[1], x[3] - x[2], x[4] - x[3]);
				var nativeWinUI = IsNativeWinUI;
				if (nativeWinUI)
				{
					Assert.IsTrue(Math.Abs(f1 - f2) < 0.5 && Math.Abs(f2 - i) < 0.5, $"The ligature's three positions should be evenly spaced: {f1:F1}, {f2:F1}, {i:F1}");
				}
				else
				{
					Assert.IsTrue(f1 == 0 && f2 == 0 && i > 10, $"The ligature should be one cluster: {f1:F1}, {f2:F1}, {i:F1}");
				}
			}
			finally
			{
				await Clear();
			}
		}

		// The left of each character position in the content, in order, LineBreaks left out.
		private static List<double> Lefts(RichTextBlock rtb)
		{
			var lefts = new List<double>();
			for (var position = rtb.ContentStart; position is not null && position.Offset < rtb.ContentEnd!.Offset; position = position.GetPositionAtOffset(1, LogicalDirection.Forward))
			{
				if (position.Parent is Run run && position.Offset < run.ContentEnd!.Offset)
				{
					lefts.Add(position.GetCharacterRect(LogicalDirection.Forward).X);
				}
			}

			return lefts;
		}

		private static async Task AssertLineHeights(RichTextBlock rtb, double height, double baseline)
		{
			try
			{
				await Show(rtb);

				Assert.AreEqual(height, rtb.DesiredSize.Height, 1.0, "Desired height");
				Assert.AreEqual(baseline, rtb.BaselineOffset, 1.0, "Baseline offset");
			}
			finally
			{
				await Clear();
			}
		}

		private static Paragraph CreateParagraph(params Inline[] inlines)
		{
			var paragraph = new Paragraph();
			foreach (var inline in inlines)
			{
				paragraph.Inlines.Add(inline);
			}

			return paragraph;
		}

		// Puts the element at the top left of a white host, which is what the screenshots are taken of.
		private static async Task<Grid> Show(FrameworkElement element)
		{
			var host = new Grid { Width = 340, Height = 200, Background = new SolidColorBrush(Colors.White) };
			element.HorizontalAlignment = HorizontalAlignment.Left;
			element.VerticalAlignment = VerticalAlignment.Top;
			host.Children.Add(element);

			WindowHelper.WindowContent = host;
			await WindowHelper.WaitForLoaded(host);
			await WindowHelper.WaitForIdle();

			return host;
		}

		private static async Task Clear()
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}

		// The pixel bounds of the pixels close to the color in the given rows, or all -1 when there are none.
		// The tolerance takes in the antialiased edges of the glyphs.
		private static (int Left, int Top, int Right, int Bottom) GetInkBounds(RawBitmap bitmap, Color color, int top = 0, int bottom = int.MaxValue)
		{
			(int Left, int Top, int Right, int Bottom) ink = (-1, -1, -1, -1);
			for (var y = Math.Max(0, top); y <= Math.Min(bottom, bitmap.Height - 1); y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.A > 128 && Math.Abs(pixel.R - color.R) + Math.Abs(pixel.G - color.G) + Math.Abs(pixel.B - color.B) < 150)
					{
						ink = ink.Right < 0
							? (x, y, x, y)
							: (Math.Min(ink.Left, x), ink.Top, Math.Max(ink.Right, x), y);
					}
				}
			}

			return ink;
		}
	}
}
