using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlockView
	{
		private static (BlockLayoutEngine engine, BlockNode page, RichTextBlockView view) MeasureArrange(RichTextBlock rtb, double width)
		{
			var engine = new BlockLayoutEngine(rtb);
			var page = engine.CreatePageNode(rtb.Blocks, rtb);
			page.Measure(new Size(width, 1e6), 0, 0f, true, false, false, null, out _);
			page.Arrange(new Size(width, Math.Ceiling(page.GetDesiredSize().Height)));
			return (engine, page, new RichTextBlockView(page, rtb));
		}

		// Selection hit-testing routes tap/click through SkiaTextLine caret / bounds members. Before
		// the Stage-6 bridge these threw NotSupportedException, crashing on the very first click.
		[TestMethod]
		public async Task When_HitTest_And_Bounds_Do_Not_Throw()
		{
			var run = new Run { Text = "Hello world from RichTextBlock" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 400, IsTextSelectionEnabled = true };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var (_, page, view) = MeasureArrange(SUT, SUT.ActualWidth);

				// PixelPositionToTextPosition -> ParagraphNode -> SkiaTextLine.GetCharacterHitFromDistance.
				var container = view.PixelPositionToTextPosition(new Point(10, 5), false, out _);

				// TextPositionToPixelPosition -> SkiaTextLine.GetTextBounds.
				view.TextPositionToPixelPosition(
					container,
					TextGravity.LineForwardCharacterForward,
					out _, out _, out _, out _, out var lineHeight, out _, out _);
				Assert.IsTrue(lineHeight > 0, "Caret line height should be positive after hit-testing");

				// TextRangeToTextBounds -> SkiaTextLine.GetTextBounds over a range.
				uint start = view.GetContentStartPosition();
				uint end = start + view.GetContentLength();
				var bounds = view.TextRangeToTextBounds(start, end);
				Assert.IsTrue(bounds.Length > 0, "A whole-paragraph range should produce at least one bounds rect");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// An empty <Paragraph/> measures a single line with zero flat length. The PageNode break logic
		// must not read that as "no content fit" and drop every following paragraph.
		[TestMethod]
		public async Task When_Empty_Middle_Paragraph_Is_Not_Dropped()
		{
			var SUT = new RichTextBlock { Width = 400 };
			var p1 = new Paragraph();
			p1.Inlines.Add(new Run { Text = "First line" });
			var p2 = new Paragraph(); // intentionally empty
			var p3 = new Paragraph();
			p3.Inlines.Add(new Run { Text = "Third line" });
			SUT.Blocks.Add(p1);
			SUT.Blocks.Add(p2);
			SUT.Blocks.Add(p3);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var engine = new BlockLayoutEngine(SUT);
				var page = engine.CreatePageNode(SUT.Blocks, SUT);
				page.Measure(new Size(SUT.ActualWidth, 1e6), 0, 0f, true, false, false, null, out _);

				Assert.AreEqual(3u, page.GetMeasuredLinesCount(),
					"All three paragraphs (including the empty one) must be measured; none dropped by a spurious page break");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// A real <LineBreak/> contributes one flat character (WinUI's run model yields an EOL char), so
		// the node's flat length must count it - otherwise every node query after the break is off by one.
		[TestMethod]
		public async Task When_LineBreak_Counts_As_One_Flat_Character()
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "ab" });
			paragraph.Inlines.Add(new LineBreak());
			paragraph.Inlines.Add(new Run { Text = "cd" });
			var SUT = new RichTextBlock { Width = 400 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var engine = new BlockLayoutEngine(SUT);
				var page = engine.CreatePageNode(SUT.Blocks, SUT);
				page.Measure(new Size(SUT.ActualWidth, 1e6), 0, 0f, true, false, false, null, out _);

				// "ab"(2) + <LineBreak/>(1) + "cd"(2) == 5 flat characters.
				Assert.AreEqual(5u, page.GetContentLength(),
					"Node flat length must count the <LineBreak/> as one character");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// The public container<->flat conversion (separator-full space) must remain a clean round-trip
		// for valid caret positions after the node/separator refactor. The separator-full flat space is
		// "Hello\r\nWorld"; every index round-trips except the position between the "\r" and "\n" (6),
		// which is not a valid caret position (WinUI never produces it either).
		[TestMethod]
		public async Task When_Multiple_Paragraphs_Public_Position_RoundTrips()
		{
			var p1 = new Paragraph();
			p1.Inlines.Add(new Run { Text = "Hello" });
			var p2 = new Paragraph();
			p2.Inlines.Add(new Run { Text = "World" });
			var SUT = new RichTextBlock { Width = 400 };
			SUT.Blocks.Add(p1);
			SUT.Blocks.Add(p2);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var (_, _, view) = MeasureArrange(SUT, SUT.ActualWidth);

				foreach (int i in new[] { 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12 })
				{
					int pos = view.GetAdjustedPosition(i);
					int back = view.GetCharacterIndex(pos);
					Assert.AreEqual(i, back, $"Public round-trip failed at flat index {i} (container {pos})");
				}

				// The container end must map back to the full separator-full flat length (12).
				uint containerEnd = view.GetContentStartPosition() + view.GetContentLength();
				Assert.AreEqual(12, view.GetCharacterIndex((int)containerEnd),
					"Container end should map to the total separator-full flat length");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// A click inside the second paragraph must resolve to a caret in the second paragraph, i.e. the
		// container<->node-flat conversion must feed the node the separator-less index, not the
		// separator-full one (which shifts every position by 2 per preceding paragraph).
		[TestMethod]
		public async Task When_HitTest_In_Second_Paragraph_Resolves_To_Second_Line()
		{
			var p1 = new Paragraph();
			p1.Inlines.Add(new Run { Text = "First paragraph" });
			var p2 = new Paragraph();
			p2.Inlines.Add(new Run { Text = "Second paragraph" });
			var SUT = new RichTextBlock { Width = 400 };
			SUT.Blocks.Add(p1);
			SUT.Blocks.Add(p2);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var (_, page, view) = MeasureArrange(SUT, SUT.ActualWidth);

				double totalHeight = page.GetDesiredSize().Height;
				double firstLineBottom = totalHeight / 2;

				// Click clearly inside the second paragraph's vertical band.
				var container = view.PixelPositionToTextPosition(new Point(10, firstLineBottom + firstLineBottom / 2), false, out _);

				Assert.IsTrue(view.ContainsPosition(container, TextGravity.LineForwardCharacterForward),
					"The hit-tested position should be reported as contained by the view");

				view.TextPositionToPixelPosition(
					container,
					TextGravity.LineForwardCharacterForward,
					out _, out _, out _, out var lineTop, out _, out _, out _);

				Assert.IsTrue(lineTop > firstLineBottom / 2,
					$"A click in the second paragraph should map back to the second line (lineTop={lineTop}, firstLineBottom={firstLineBottom})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
