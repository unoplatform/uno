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
