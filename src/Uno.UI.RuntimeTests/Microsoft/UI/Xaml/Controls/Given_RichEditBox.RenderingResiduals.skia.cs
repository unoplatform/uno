#nullable enable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Bounded_Rich_Layout_Inherits_Visual_State_Foreground()
	{
		var normal = Microsoft.UI.Colors.Blue;
		var pointerOver = Microsoft.UI.Colors.Lime;
		var focused = Microsoft.UI.Colors.Orange;
		var disabled = Microsoft.UI.Colors.Gray;
		var explicitColor = Microsoft.UI.Colors.Red;
		var explicitLinkColor = Microsoft.UI.Colors.Purple;
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Foreground = new SolidColorBrush(normal),
			TextWrapping = TextWrapping.Wrap,
		};
		editor.Resources["TextControlForegroundPointerOver"] = new SolidColorBrush(pointerOver);
		editor.Resources["TextControlForegroundFocused"] = new SolidColorBrush(focused);
		editor.Resources["TextControlForegroundDisabled"] = new SolidColorBrush(disabled);
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(8200, 1));
			editor.Document.GetRange(0, 1).CharacterFormat.ForegroundColor = explicitColor;
			var link = editor.Document.GetRange(1, 2);
			link.Link = "\"https://contoso.example\"";
			link.CharacterFormat.ForegroundColor = explicitLinkColor;

			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			await AssertRenderedColors(editor, normal, explicitColor, explicitLinkColor);

			Assert.IsTrue(VisualStateManager.GoToState(editor, "PointerOver", false));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(pointerOver, ((SolidColorBrush)block.Foreground).Color);
			await AssertRenderedColors(editor, pointerOver, explicitColor, explicitLinkColor);

			Assert.IsTrue(VisualStateManager.GoToState(editor, "Focused", false));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(focused, ((SolidColorBrush)block.Foreground).Color);
			await AssertRenderedColors(editor, focused, explicitColor, explicitLinkColor);

			Assert.IsTrue(VisualStateManager.GoToState(editor, "Disabled", false));
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(disabled, ((SolidColorBrush)block.Foreground).Color);
			await AssertRenderedColors(editor, disabled, explicitColor, explicitLinkColor);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Local_Edit_Rebuilds_Only_Affected_Bounded_Paragraph()
	{
		const int paragraphCount = 200;
		const int runsPerParagraph = 100;
		const int runCount = paragraphCount * runsPerParagraph;
		var editor = new RichEditBox
		{
			Width = 360,
			Height = 120,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			editor.Document.SetText(
				TextSetOptions.FormatRtf,
				BuildAlternatingParagraphRunRtf(paragraphCount, runsPerParagraph));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.AreEqual(paragraphCount, editor.BoundedRichLayoutCachedParagraphCount);
			Assert.HasCount(0, block.Inlines);
			Assert.AreEqual(1, editor.BoundedRichLayoutRetainedInlineCount);
			Assert.IsLessThanOrEqualTo(128, editor.BoundedRichLayoutRetainedResourceCount);

			var paragraphs = editor.Document.GetUnitBoundaries(TextRangeUnit.Paragraph);
			Assert.IsNotNull(paragraphs);
			var secondParagraphStart = paragraphs[1].Start;
			var leftWord = block.ParsedText.GetWordAt(secondParagraphStart, right: false);
			var rightWord = block.ParsedText.GetWordAt(secondParagraphStart, right: true);
			Assert.IsTrue(leftWord.start < secondParagraphStart);
			Assert.IsTrue(rightWord.start >= secondParagraphStart);
			var editPosition = paragraphs[paragraphCount / 2].Start + runsPerParagraph / 2;
			var visits = editor.BoundedRichLayoutRunVisitCount;
			var paragraphRebuilds = editor.BoundedRichLayoutParagraphRebuildCount;
			var shapes = editor.BoundedRichLayoutShapingOperationCount;

			editor.Document.GetRange(editPosition, editPosition + 1).Text = "Z";
			await WindowHelper.WaitForIdle();

			Assert.IsLessThanOrEqualTo(
				runsPerParagraph * 2L + 8,
				editor.BoundedRichLayoutRunVisitCount - visits);
			Assert.IsTrue(
				editor.BoundedRichLayoutParagraphRebuildCount - paragraphRebuilds is >= 1 and <= 2);
			Assert.IsLessThanOrEqualTo(1, editor.BoundedRichLayoutShapingOperationCount - shapes);
			Assert.AreEqual("Z", editor.Document.GetTextInRange(editPosition, editPosition + 1));

			visits = editor.BoundedRichLayoutRunVisitCount;
			paragraphRebuilds = editor.BoundedRichLayoutParagraphRebuildCount;
			shapes = editor.BoundedRichLayoutShapingOperationCount;
			editor.Width = 280;
			await WindowHelper.WaitForIdle();

			Assert.IsGreaterThanOrEqualTo(
				paragraphCount,
				editor.BoundedRichLayoutParagraphRebuildCount - paragraphRebuilds);
			Assert.IsLessThanOrEqualTo(
				paragraphCount * 2,
				editor.BoundedRichLayoutParagraphRebuildCount - paragraphRebuilds);
			Assert.IsLessThanOrEqualTo(1, editor.BoundedRichLayoutShapingOperationCount - shapes);
			Assert.IsLessThanOrEqualTo(
				runCount * 2L,
				editor.BoundedRichLayoutRunVisitCount - visits);

			var lastIndex = editor.Document.TextLength - 2;
			var rect = block.ParsedText.GetRectForIndex(lastIndex);
			var hit = block.ParsedText.GetIndexAt(
				new Windows.Foundation.Point(
					rect.X + Math.Max(0.5, rect.Width / 2),
					rect.Y + rect.Height / 2),
				ignoreEndingNewLine: false,
				extendedSelection: true);
			Assert.IsTrue(hit is >= 0 && hit <= editor.Document.TextLength);

			editor.Document.GetRange(lastIndex, lastIndex + 1).ScrollIntoView(PointOptions.Start);
			await WindowHelper.WaitForIdle();
			var scrollViewer = editor.FindFirstChild<ScrollViewer>(static view => view.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);
			Assert.IsGreaterThan(scrollViewer.ViewportHeight, scrollViewer.ExtentHeight);
			Assert.IsGreaterThan(0, scrollViewer.VerticalOffset);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task AssertRenderedColors(
		RichEditBox editor,
		global::Windows.UI.Color inherited,
		global::Windows.UI.Color explicitColor,
		global::Windows.UI.Color explicitLinkColor)
	{
		var screenshot = await UITestHelper.ScreenShot(editor);
		Assert.IsNotNull(ImageAssert.GetColorBounds(screenshot, inherited, tolerance: 16));
		Assert.IsNotNull(ImageAssert.GetColorBounds(screenshot, explicitColor, tolerance: 16));
		Assert.IsNotNull(ImageAssert.GetColorBounds(screenshot, explicitLinkColor, tolerance: 16));
	}

	private static string BuildAlternatingParagraphRunRtf(int paragraphCount, int runsPerParagraph)
	{
		var rtf = new StringBuilder(@"{\rtf1\ansi ");
		for (var paragraph = 0; paragraph < paragraphCount; paragraph++)
		{
			for (var run = 0; run < runsPerParagraph; run++)
			{
				rtf.Append(run % 2 == 0 ? @"\b " : @"\b0 ");
				rtf.Append((char)('a' + run % 26));
			}
			rtf.Append(@"\par ");
		}
		rtf.Append('}');
		return rtf.ToString();
	}
}
