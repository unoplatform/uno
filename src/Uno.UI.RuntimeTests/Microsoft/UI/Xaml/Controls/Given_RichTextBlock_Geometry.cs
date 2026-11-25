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

		[TestMethod]
		[RequiresScaling(1f)]
		[DataRow("LTR-Left")]
		[DataRow("LTR-Center")]
		[DataRow("LTR-Right")]
		[DataRow("LTR-Left-Padding")]
		[DataRow("LTR-Left-TwoParagraphs")]
		[DataRow("LTR-Left-Wrapped")]
		public async Task When_CharacterRect_On_Ink(string row)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20, Foreground = new SolidColorBrush(Colors.Red) };
			var text = "Hello world.";
			switch (row)
			{
				case "LTR-Center":
					rtb.TextAlignment = TextAlignment.Center;
					break;
				case "LTR-Right":
					rtb.TextAlignment = TextAlignment.Right;
					break;
				case "LTR-Left-Padding":
					// Wraps, so its lines run up to the right padding.
					rtb.Padding = new Thickness(10, 0, 30, 0);
					text = WrappingText;
					break;
				case "LTR-Left-Wrapped":
					rtb.Width = 150;
					text = WrappingText;
					break;
			}

			rtb.Blocks.Add(CreateParagraph(new Run { Text = text }));
			if (row == "LTR-Left-TwoParagraphs")
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
					if (position.Parent is Run)
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

				if (row is "LTR-Left-Wrapped" or "LTR-Left-Padding")
				{
					Assert.IsTrue(lines.Count > 1, $"The text should wrap ({lines.Count} line)");
				}
				else
				{
					Assert.AreEqual(row == "LTR-Left-TwoParagraphs" ? 2 : 1, lines.Count, "Lines with character rects");
				}

				foreach (var line in lines.Values)
				{
					// Stay inside the line so a neighbouring line's ink doesn't count.
					var ink = GetInkBounds(bitmap, Colors.Red, (int)line.Top + 2, (int)(line.Top + line.Height) - 2);
					Assert.IsTrue(ink.Right >= 0, $"No ink in the line at y={line.Top}");

					// Where the line is: rects and ink could otherwise move together. Ink edges are pixel columns,
					// so the ink spans [Left, Right + 1). 4 px covers the side bearings of 'H', 'S', 'w' and '.' at 20 px.
					switch (row)
					{
						case "LTR-Center":
							Assert.AreEqual(bounds.Left + bounds.Width / 2, (ink.Left + ink.Right + 1) / 2.0, 4, "The ink should be centered in the control");
							break;
						case "LTR-Right":
							Assert.AreEqual(bounds.Right, ink.Right + 1, 4, "The ink should end at the control's right edge");
							break;
						case "LTR-Left-Padding":
							Assert.AreEqual(bounds.Left + 10, ink.Left, 4, $"The line at y={line.Top} should start at the left padding");
							Assert.IsTrue(ink.Right + 1 <= bounds.Right - 30, $"The line at y={line.Top} should end before the right padding (ink ends at {ink.Right + 1}, padding starts at {bounds.Right - 30})");
							break;
						default:
							Assert.AreEqual(bounds.Left, ink.Left, 4, $"The line at y={line.Top} should start at the control's left edge");
							break;
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
		public async Task When_GetPositionFromPoint_Beside_Line(string row)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20 };
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "Hello world." }));

			try
			{
				await Show(rtb);

				// Offsets count the paragraph's and the run's start, so the text spans 2 to 14.
				Assert.AreEqual(2, rtb.GetPositionFromPoint(new Point(-5, 12))?.Offset, "Left of the line maps to its reading start");
				Assert.AreEqual(14, rtb.GetPositionFromPoint(new Point(290, 12))?.Offset, "Right of the line maps to its reading end");
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
		[DataRow("H7 LineHeight10 Block 2 lines", 20.0, 8.1)]
		[DataRow("H9 TextLineBounds Tight", 14.4, 14.4)]
		public async Task When_Line_Heights_Match_WinUI(string row, double height, double baseline)
		{
			var rtb = new RichTextBlock { Width = 300, FontSize = 20 };
			switch (row)
			{
				case "H4 LineHeight40 MaxHeight":
					rtb.LineHeight = 40;
					break;
				case "H7 LineHeight10 Block 2 lines":
					rtb.LineHeight = 10;
					rtb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
					break;
				case "H9 TextLineBounds Tight":
					rtb.TextLineBounds = TextLineBounds.Tight;
					break;
			}

			rtb.Blocks.Add(row == "H7 LineHeight10 Block 2 lines"
				? CreateParagraph(new Run { Text = "Hello" }, new LineBreak(), new Run { Text = "World" })
				: CreateParagraph(new Run { Text = "Hello" }));

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
		public async Task When_Object_Only_Paragraph_Keeps_Following_Paragraphs()
		{
			var rtb = new RichTextBlock { Width = 200, FontSize = 20 };
			// Narrower than "Text", so text drawn over the object still shows beside it.
			rtb.Blocks.Add(CreateParagraph(new InlineUIContainer { Child = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Red) } }));
			rtb.Blocks.Add(CreateParagraph(new Run { Text = "Text", Foreground = new SolidColorBrush(Colors.Blue) }));

			try
			{
				var bitmap = await UITestHelper.ScreenShot(await Show(rtb));

				var red = GetInkBounds(bitmap, Colors.Red);
				var blue = GetInkBounds(bitmap, Colors.Blue);
				Assert.IsTrue(red.Right >= 0, "The object is drawn");
				Assert.IsTrue(blue.Right >= 0, "The paragraph after the object-only paragraph is drawn");
				Assert.IsTrue(blue.Top > red.Bottom, $"The following paragraph is drawn below the object (text top {blue.Top}, object bottom {red.Bottom})");
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
