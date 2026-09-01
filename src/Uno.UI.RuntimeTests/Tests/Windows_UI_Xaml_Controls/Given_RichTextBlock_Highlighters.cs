using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Pixel-level coverage for TextHighlighter rendering. The collection-state tests in
	// Given_RichTextBlock only assert the list they built, so a regression that draws no
	// highlight still passes. These assert the highlight Background actually paints at the
	// highlighted glyphs (and not elsewhere), including the global->paragraph-local range
	// translation across the +2 inter-paragraph separator.
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Highlighters
	{
		[TestMethod]
		public async Task When_Highlight_Paints_First_Word_Only()
		{
			var SUT = new RichTextBlock
			{
				Width = 400,
				FontSize = 24,
				TextWrapping = TextWrapping.NoWrap,
				Foreground = new SolidColorBrush(Colors.Black),
			};
			var paragraph = new Paragraph();
			// Two equal-length words; only the first is highlighted.
			paragraph.Inlines.Add(new Run { Text = "AAAAAAAAAA BBBBBBBBBB" });
			SUT.Blocks.Add(paragraph);

			var highlighter = new TextHighlighter { Background = new SolidColorBrush(Colors.Red) };
			highlighter.Ranges.Add(new TextRange { StartIndex = 0, Length = 10 });
			SUT.TextHighlighters.Add(highlighter);

			try
			{
				await UITestHelper.Load(SUT);
				var screenshot = await UITestHelper.ScreenShot(SUT);

				var leftQuarter = new System.Drawing.Rectangle(0, 0, screenshot.Width / 4, screenshot.Height);
				var rightQuarter = new System.Drawing.Rectangle(screenshot.Width * 3 / 4, 0, screenshot.Width / 4, screenshot.Height);

				ImageAssert.HasColorInRectangle(screenshot, leftQuarter, Colors.Red, tolerance: 5);
				ImageAssert.DoesNotHaveColorInRectangle(screenshot, rightQuarter, Colors.Red, tolerance: 5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// Horizontal extent of a colour across the whole bitmap, as (minX, maxX); (-1, -1) when absent.
		private static (int Min, int Max) HorizontalExtent(RawBitmap bitmap, Windows.UI.Color color)
		{
			var min = int.MaxValue;
			var max = -1;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var p = bitmap.GetPixel(x, y);
					if (System.Math.Abs(p.R - color.R) <= 5 && System.Math.Abs(p.G - color.G) <= 5 && System.Math.Abs(p.B - color.B) <= 5 && p.A > 200)
					{
						min = System.Math.Min(min, x);
						max = System.Math.Max(max, x);
					}
				}
			}

			return max < 0 ? (-1, -1) : (min, max);
		}

		[TestMethod]
		[RequiresScaling(1f)]
		public async Task When_Multiple_Highlighters_All_Paint()
		{
			// The renderer used to take highlighters.FirstOrDefault().Ranges.FirstOrDefault(), so only one
			// range of one highlighter ever painted - and an app highlighter hid the selection entirely.
			var SUT = new RichTextBlock
			{
				Width = 400,
				FontSize = 24,
				TextWrapping = TextWrapping.NoWrap,
				Foreground = new SolidColorBrush(Colors.Black),
			};
			var paragraph = new Paragraph();
			// Four equal-length words, highlighted by two separate highlighters with two ranges each.
			paragraph.Inlines.Add(new Run { Text = "AAAA BBBB CCCC DDDD" });
			SUT.Blocks.Add(paragraph);

			var first = new TextHighlighter { Background = new SolidColorBrush(Colors.Red) };
			first.Ranges.Add(new TextRange { StartIndex = 0, Length = 4 });
			first.Ranges.Add(new TextRange { StartIndex = 10, Length = 4 });
			SUT.TextHighlighters.Add(first);

			var second = new TextHighlighter { Background = new SolidColorBrush(Colors.Blue) };
			second.Ranges.Add(new TextRange { StartIndex = 5, Length = 4 });
			second.Ranges.Add(new TextRange { StartIndex = 15, Length = 4 });
			SUT.TextHighlighters.Add(second);

			try
			{
				await UITestHelper.Load(SUT);
				var screenshot = await UITestHelper.ScreenShot(SUT);

				var full = new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height);

				// The second highlighter must paint at all - it was dropped entirely before.
				ImageAssert.HasColorInRectangle(screenshot, full, Colors.Blue, tolerance: 5);
				// ...and so must the first highlighter.
				ImageAssert.HasColorInRectangle(screenshot, full, Colors.Red, tolerance: 5);

				// The words alternate red, blue, red, blue, so the two colours must interleave along the
				// line. Red reaching past where blue starts proves its second range painted, and blue
				// reaching past where red ends proves the same for its own.
				await screenshot.Populate();
				var red = HorizontalExtent(screenshot, Colors.Red);
				var blue = HorizontalExtent(screenshot, Colors.Blue);

				Assert.IsTrue(red.Min < blue.Min, $"The first word should be red (red {red}, blue {blue})");
				Assert.IsTrue(red.Max > blue.Min, $"The first highlighter's second range should paint (red {red}, blue {blue})");
				Assert.IsTrue(blue.Max > red.Max, $"The second highlighter's second range should paint (red {red}, blue {blue})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Highlight_Spans_Second_Paragraph_Across_Separator()
		{
			// Global text is "First\r\nSecond"; the second paragraph starts at global offset 7
			// (5 chars + the 2-char paragraph separator). Highlighting global 7..13 must translate
			// to the second paragraph's local 0..6 and paint only that paragraph's row.
			var SUT = new RichTextBlock
			{
				Width = 300,
				FontSize = 24,
				TextWrapping = TextWrapping.NoWrap,
				Foreground = new SolidColorBrush(Colors.Black),
			};
			var para1 = new Paragraph();
			para1.Inlines.Add(new Run { Text = "First" });
			SUT.Blocks.Add(para1);
			var para2 = new Paragraph();
			para2.Inlines.Add(new Run { Text = "Second" });
			SUT.Blocks.Add(para2);

			var highlighter = new TextHighlighter { Background = new SolidColorBrush(Colors.Red) };
			highlighter.Ranges.Add(new TextRange { StartIndex = 7, Length = 6 });
			SUT.TextHighlighters.Add(highlighter);

			try
			{
				await UITestHelper.Load(SUT);
				var screenshot = await UITestHelper.ScreenShot(SUT);

				var topHalf = new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height / 2);
				var bottomHalf = new System.Drawing.Rectangle(0, screenshot.Height / 2, screenshot.Width, screenshot.Height / 2);

				ImageAssert.HasColorInRectangle(screenshot, bottomHalf, Colors.Red, tolerance: 5);
				ImageAssert.DoesNotHaveColorInRectangle(screenshot, topHalf, Colors.Red, tolerance: 5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
