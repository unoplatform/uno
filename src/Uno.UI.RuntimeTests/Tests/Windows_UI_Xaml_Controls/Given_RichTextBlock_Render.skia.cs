using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using Rectangle = System.Drawing.Rectangle;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Render
	{
		// A run of short words that wraps to many lines at a narrow width, so MaxLines / overflow slices
		// leave plenty of lines outside the page (a whole-paragraph repaint would be obvious overdraw).
		private const string WrappingText =
			"aa bb cc dd ee ff gg hh ii jj kk ll mm nn oo pp qq rr ss tt uu vv ww xx yy zz";

		[TestMethod]
		public async Task When_MaxLines_Master_Does_Not_Paint_Beyond_Slice()
		{
			// A MaxLines-limited master measures/arranges only its N lines, but the render path used to draw the
			// whole paragraph's ParsedText from y=0 - overdrawing every remaining line below the control's bounds.
			// The page-slice fix must draw only the measured lines.
			var rtb = new RichTextBlock
			{
				Width = 130,
				MaxLines = 2,
				FontSize = 24,
				TextWrapping = TextWrapping.Wrap,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = WrappingText });
			rtb.Blocks.Add(paragraph);

			var host = new Border
			{
				Width = 220,
				Height = 320,
				Background = new SolidColorBrush(Microsoft.UI.Colors.White),
				Child = rtb,
			};

			try
			{
				WindowHelper.WindowContent = host;
				await WindowHelper.WaitForLoaded(host);
				await WindowHelper.WaitForIdle();

				var slice = (int)Math.Ceiling(rtb.ActualHeight);
				Assert.IsTrue(slice > 0 && slice < 200, $"MaxLines=2 should keep the master short (ActualHeight {rtb.ActualHeight})");

				var shot = await UITestHelper.ScreenShot(host);

				// The two measured lines render red text.
				ImageAssert.HasColorInRectangle(shot, new Rectangle(0, 0, (int)Math.Ceiling(rtb.ActualWidth), slice), Microsoft.UI.Colors.Red, tolerance: 16);

				// Lines 3+ must NOT overdraw below the arranged slice.
				ImageAssert.DoesNotHaveColorInRectangle(shot, new Rectangle(0, slice + 3, 220, 320 - slice - 3), Microsoft.UI.Colors.Red, tolerance: 16);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Linked_Overflow_Paints_Only_Its_Slice()
		{
			// A master page-broken into an overflow paints only the lines it measured; the overflow paints the
			// continuation. Before the page-slice fix both drew the whole paragraph from line 0, so the master
			// overdrew past its 2-line box and the overflow repainted the master's lines at its own top.
			var master = new RichTextBlock
			{
				Width = 160,
				MaxLines = 2,
				FontSize = 24,
				TextWrapping = TextWrapping.Wrap,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = WrappingText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow
			{
				Width = 160,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
			};
			master.OverflowContentTarget = overflow;

			var masterHost = new Border { Width = 200, Height = 300, Background = new SolidColorBrush(Microsoft.UI.Colors.White), Child = master };
			var overflowHost = new Border { Width = 200, Height = 300, Background = new SolidColorBrush(Microsoft.UI.Colors.White), Child = overflow };

			var root = new StackPanel { Orientation = Orientation.Horizontal };
			root.Children.Add(masterHost);
			root.Children.Add(overflowHost);

			try
			{
				WindowHelper.WindowContent = root;
				await WindowHelper.WaitForLoaded(root);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Master should overflow (MaxLines=2 with long text)");
				Assert.IsTrue(overflow.ActualHeight > 0, $"Overflow should render its content slice (height {overflow.ActualHeight})");

				var masterSlice = (int)Math.Ceiling(master.ActualHeight);
				var overflowSlice = (int)Math.Ceiling(overflow.ActualHeight);

				var masterShot = await UITestHelper.ScreenShot(masterHost);
				var overflowShot = await UITestHelper.ScreenShot(overflowHost);

				// Master paints its two measured lines and nothing below them.
				ImageAssert.HasColorInRectangle(masterShot, new Rectangle(0, 0, (int)Math.Ceiling(master.ActualWidth), masterSlice), Microsoft.UI.Colors.Red, tolerance: 16);
				ImageAssert.DoesNotHaveColorInRectangle(masterShot, new Rectangle(0, masterSlice + 3, 200, 300 - masterSlice - 3), Microsoft.UI.Colors.Red, tolerance: 16);

				// Overflow paints its continuation slice: text is present and nothing overdraws below its measured
				// box. A whole-paragraph repaint from line 0 would paint more lines than the overflow measured and
				// spill below its box.
				ImageAssert.HasColorInRectangle(overflowShot, new Rectangle(0, 0, (int)Math.Ceiling(overflow.ActualWidth), overflowSlice), Microsoft.UI.Colors.Red, tolerance: 16);
				ImageAssert.DoesNotHaveColorInRectangle(overflowShot, new Rectangle(0, overflowSlice + 3, 200, 300 - overflowSlice - 3), Microsoft.UI.Colors.Red, tolerance: 16);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Standalone_LineBreak_Highlight_Targets_Correct_Chars()
		{
			// Content "XXXX" + <LineBreak/> + "Y" => flat "XXXX\nY" (X=0..3, \n=4, Y=5). A standalone <LineBreak/>
			// counts one flat character (matching _text and the node/selection space), but the render index path
			// used to count it as zero, shifting the highlight one character. Highlighting exactly the single 'Y'
			// on line 2 (range [5,1)) must paint the highlight under 'Y'; with the skew the range collapsed to
			// zero width and nothing painted on line 2.
			var highlighter = new TextHighlighter
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
				Ranges =
				{
					new TextRange { StartIndex = 5, Length = 1 },
				},
			};

			var rtb = new RichTextBlock
			{
				Width = 240,
				FontSize = 40,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
			};
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "XXXX" });
			paragraph.Inlines.Add(new LineBreak());
			paragraph.Inlines.Add(new Run { Text = "Y" });
			rtb.Blocks.Add(paragraph);
			rtb.TextHighlighters.Add(highlighter);

			var host = new Border
			{
				Width = 260,
				Height = 160,
				Background = new SolidColorBrush(Microsoft.UI.Colors.White),
				Child = rtb,
			};

			try
			{
				WindowHelper.WindowContent = host;
				await WindowHelper.WaitForLoaded(host);
				await WindowHelper.WaitForIdle();

				var full = (int)Math.Ceiling(rtb.ActualHeight);
				var lineBoundary = full / 2;
				Assert.IsTrue(full > 0 && lineBoundary > 0, $"Two-line paragraph should have measurable height (ActualHeight {rtb.ActualHeight})");

				var shot = await UITestHelper.ScreenShot(host);

				// Line 2 (the single 'Y') is highlighted yellow.
				ImageAssert.HasColorInRectangle(shot, new Rectangle(0, lineBoundary + 3, 60, full - lineBoundary - 3), Microsoft.UI.Colors.Yellow, tolerance: 24);

				// Line 1 ("XXXX") is never highlighted.
				ImageAssert.DoesNotHaveColorInRectangle(shot, new Rectangle(0, 0, (int)Math.Ceiling(rtb.ActualWidth), Math.Max(1, lineBoundary - 3)), Microsoft.UI.Colors.Yellow, tolerance: 24);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
