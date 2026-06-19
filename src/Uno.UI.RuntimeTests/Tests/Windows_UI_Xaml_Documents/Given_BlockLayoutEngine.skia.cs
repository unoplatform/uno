using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_BlockLayoutEngine
	{
		// MarginCollapsingState is a float alias in the layout tree.
		[TestMethod]
		public async Task When_Measure_Single_Paragraph_Matches_ParsedText()
		{
			var run = new Run { Text = "The quick brown fox jumps over the lazy dog and keeps on running past the edge of the block" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 160 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var width = SUT.ActualWidth;

				// Oracle: a direct single-pass parse of the same inputs the engine feeds the formatter.
				var inlines = paragraph.Inlines.TraversedTree.leafTree;
				var (defaultFont, _) = FontDetailsCache.GetFont(SUT.FontFamily?.Source, (float)SUT.FontSize, SUT.FontWeight, SUT.FontStretch, SUT.FontStyle);
				var expected = ParsedText.ParseText(
					new Size(width, double.PositiveInfinity),
					inlines,
					defaultFont.SKFontSize,
					maxLines: 0,
					lineHeight: 0,
					LineStackingStrategy.MaxHeight,
					SUT.TextAlignment,
					SUT.TextWrapping,
					SUT.FlowDirection,
					out var expectedSize);

				// Drive the ported BlockLayout engine directly.
				var engine = new BlockLayoutEngine(SUT);
				var page = engine.CreatePageNode(SUT.Blocks, SUT);

				var result = page.Measure(
					new Size(width, 1e6),
					maxLines: 0,
					mcsIn: 0f,
					allowEmptyContent: true,
					measureBottomless: false,
					suppressTopMargin: false,
					pPreviousBreak: null,
					out _);

				Assert.AreEqual(Result.Success, result, "PageNode.Measure should succeed for basic text");

				var desired = page.GetDesiredSize();

				Assert.IsTrue(expected.RenderLines.Count > 1, "Text should wrap onto multiple lines to exercise the loop");
				Assert.IsTrue(desired.Width > 0 && desired.Height > 0, $"Engine produced a degenerate size {desired}");

				// Desired width is the widest line measured WITHOUT trailing whitespace, matching
				// WinUI TextLine.Width semantics (SkiaTextLine.Width) rather than ParsedText's
				// out-size, which includes trailing whitespace.
				double expectedWidth = 0;
				foreach (var line in expected.RenderLines)
				{
					expectedWidth = Math.Max(expectedWidth, line.WidthWithoutTrailingSpaces);
				}

				Assert.AreEqual(expectedWidth, desired.Width, 1.0, "Engine width should match the widest line");
				Assert.AreEqual(expectedSize.Height, desired.Height, 1.0, "Engine height should match ParsedText");

				// And it must agree with the live control's own measured size.
				Assert.AreEqual(SUT.DesiredSize.Height, desired.Height, 1.0, "Engine height should match the managed RichTextBlock");

				// Arrange must run cleanly (no stubbed-path throw) and preserve the content height.
				page.Arrange(new Size(width, Math.Ceiling(desired.Height)));
				var render = page.GetRenderSize();
				Assert.AreEqual(desired.Height, render.Height, 1.5, "Render height should match desired height");
				Assert.IsTrue(render.Width >= expectedWidth - 1, $"Render width {render.Width} should cover the content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Measure_Multiple_Paragraphs_Stacks_Height()
		{
			var SUT = new RichTextBlock { Width = 400 };
			var p1 = new Paragraph();
			p1.Inlines.Add(new Run { Text = "First paragraph of text." });
			var p2 = new Paragraph();
			p2.Inlines.Add(new Run { Text = "Second paragraph, a little longer than the first one here." });
			SUT.Blocks.Add(p1);
			SUT.Blocks.Add(p2);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var width = SUT.ActualWidth;

				var engine = new BlockLayoutEngine(SUT);
				var page = engine.CreatePageNode(SUT.Blocks, SUT);
				var result = page.Measure(
					new Size(width, 1e6),
					maxLines: 0,
					mcsIn: 0f,
					allowEmptyContent: true,
					measureBottomless: false,
					suppressTopMargin: false,
					pPreviousBreak: null,
					out _);

				Assert.AreEqual(Result.Success, result);

				var desired = page.GetDesiredSize();
				Assert.IsTrue(desired.Height > 0, "Two stacked paragraphs should have a positive height");

				// The page height must agree with the live managed control measuring the same content.
				Assert.AreEqual(SUT.DesiredSize.Height, desired.Height, 1.5, "Engine should match the managed RichTextBlock for multiple paragraphs");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		// Validates the container<->flat offset conversion that the Stage-9b selection swap relies on:
		// RichTextBlockView.GetAdjustedPosition (flat char index -> container position) must be inverted
		// by GetCharacterIndex (container position -> flat char index).
		// R3 reconciliation: the node tree measures in flat (ParsedText) char space; the view bridges to
		// container-position space (RichTextBlockView.GetContentLength computes the container length from
		// the run model). GetAdjustedPosition (flat->container) must round-trip via GetCharacterIndex.
		[TestMethod]
		public async Task When_View_Position_CharacterIndex_RoundTrips()
		{
			var run = new Run { Text = "Hello world from RichTextBlock" };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 400 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var width = SUT.ActualWidth;
				var engine = new BlockLayoutEngine(SUT);
				var page = engine.CreatePageNode(SUT.Blocks, SUT);
				page.Measure(new Size(width, 1e6), 0, 0f, true, false, false, null, out _);
				page.Arrange(new Size(width, Math.Ceiling(page.GetDesiredSize().Height)));

				var view = new RichTextBlockView(page, SUT);

				int textLen = run.Text.Length;
				uint contentLen = view.GetContentLength();
				paragraph.Inlines.GetPositionCount(out var inlinePos);
				for (int i = 0; i <= textLen; i++)
				{
					int pos = view.GetAdjustedPosition(i);   // flat -> container
					int back = view.GetCharacterIndex(pos);   // container -> flat
					Assert.AreEqual(i, back, $"Round-trip failed at flat index {i} (container pos {pos}); textLen={textLen} GetContentLength={contentLen} inlinePositions={inlinePos}");
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
