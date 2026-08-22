using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Selection
	{
		private const string LongText =
			"Line one of the content. Line two of the content. Line three of the content. " +
			"Line four of the content. Line five of the content. Line six of the content. " +
			"Line seven of the content. Line eight of the content. Line nine of the content.";

		private static RichTextBlock CreateRichTextBlock(string text, double width = 300)
		{
			var rtb = new RichTextBlock { Width = width };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = text });
			rtb.Blocks.Add(paragraph);
			return rtb;
		}

		[TestMethod]
		public void When_IsTextSelectionEnabled_Default_Is_True()
		{
			// WinUI CRichTextBlock sets m_isTextSelectionEnabled = true in its ctor, so content is
			// selectable by default (unlike TextBlock).
			var SUT = new RichTextBlock();
			Assert.IsTrue(SUT.IsTextSelectionEnabled, "IsTextSelectionEnabled should default to true on RichTextBlock");
		}

		[TestMethod]
		public void When_Select_Null_Throws()
		{
			var SUT = CreateRichTextBlock("Hello world");
			// WinUI's Select fails with E_POINTER for null positions (ArgumentNullException in the projection).
			Assert.ThrowsExactly<ArgumentNullException>(() => SUT.Select(null, null));
		}

		[TestMethod]
		public async Task When_Linked_Overflow_SelectAll_Does_Not_Throw()
		{
			// A master with an OverflowContentTarget set before the first measure created the linked view
			// while the selection manager was still null. If the manager is not wired to the linked view
			// on creation, m_pTextSelection stays null and SelectAll/first click NREs.
			var master = new RichTextBlock { Width = 180, MaxLines = 2 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				// Must not throw a NullReferenceException.
				master.SelectAll();
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(string.IsNullOrEmpty(master.SelectedText), "SelectAll on a linked master should select content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Survives_Remeasure()
		{
			var SUT = CreateRichTextBlock(LongText);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();

				var start = SUT.SelectionStart;
				var end = SUT.SelectionEnd;
				if (start is null || end is null)
				{
					Assert.Fail($"SelectAll should produce non-null selection endpoints (start={start}, end={end})");
					return;
				}

				Assert.IsTrue(end.Offset > start.Offset, "SelectAll should produce a non-empty selection");

				// Force a re-measure; the page node/view must stay stable so the selection is preserved.
				SUT.FontSize = 22;
				await WindowHelper.WaitForIdle();

				var remeasuredStart = SUT.SelectionStart;
				var remeasuredEnd = SUT.SelectionEnd;
				if (remeasuredStart is null || remeasuredEnd is null)
				{
					Assert.Fail($"Selection endpoints should stay non-null across a re-measure (start={remeasuredStart}, end={remeasuredEnd})");
					return;
				}

				Assert.IsTrue(
					remeasuredEnd.Offset > remeasuredStart.Offset,
					"Selection should survive a re-measure (stable view identity)");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Local_Inline_FontSize_Changes_Selection_Is_Cleared()
		{
			// The mirror of When_Selection_Survives_Remeasure. An inherited cascade from the owner is
			// CRichTextBlock::MarkInheritedPropertyDirty and keeps the selection, but setting the property
			// locally on the Run goes through CTextElement::SetValue -> MarkDirty -> OnContentChanged,
			// which does clear it.
			var run = new Run { Text = LongText };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(run);
			var SUT = new RichTextBlock { Width = 300 };
			SUT.Blocks.Add(paragraph);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();

				var preChangeStart = SUT.SelectionStart;
				var preChangeEnd = SUT.SelectionEnd;
				if (preChangeStart is null || preChangeEnd is null)
				{
					Assert.Fail($"Precondition: SelectAll should produce non-null selection endpoints (start={preChangeStart}, end={preChangeEnd})");
					return;
				}

				Assert.IsTrue(preChangeEnd.Offset > preChangeStart.Offset, "Precondition: non-empty selection");

				run.FontSize = 22;
				await WindowHelper.WaitForIdle();

				var postChangeStart = SUT.SelectionStart;
				var postChangeEnd = SUT.SelectionEnd;
				if (postChangeStart is null || postChangeEnd is null)
				{
					Assert.Fail($"Selection endpoints should be non-null after a content change (start={postChangeStart}, end={postChangeEnd})");
					return;
				}

				Assert.AreEqual(
					postChangeStart.Offset,
					postChangeEnd.Offset,
					"A locally set inline property is a content change and must clear the selection");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Content_Changes_Selection_Is_Cleared()
		{
			var SUT = CreateRichTextBlock(LongText);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();

				var preChangeStart = SUT.SelectionStart;
				var preChangeEnd = SUT.SelectionEnd;
				if (preChangeStart is null || preChangeEnd is null)
				{
					Assert.Fail($"Precondition: SelectAll should produce non-null selection endpoints (start={preChangeStart}, end={preChangeEnd})");
					return;
				}

				Assert.IsTrue(preChangeEnd.Offset > preChangeStart.Offset, "Precondition: non-empty selection");

				// Mutating the content collapses the selection (CRichTextBlock::OnContentChanged Select(0,0)).
				var extra = new Paragraph();
				extra.Inlines.Add(new Run { Text = "Appended paragraph." });
				SUT.Blocks.Add(extra);
				await WindowHelper.WaitForIdle();

				var postChangeStart = SUT.SelectionStart;
				var postChangeEnd = SUT.SelectionEnd;
				if (postChangeStart is null || postChangeEnd is null)
				{
					Assert.Fail($"Selection endpoints should be non-null after a content change (start={postChangeStart}, end={postChangeEnd})");
					return;
				}

				Assert.AreEqual(
					postChangeStart.Offset,
					postChangeEnd.Offset,
					"Selection should be cleared after content changes");
				Assert.AreEqual(string.Empty, SUT.SelectedText, "SelectedText should be empty after content changes");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_IsTextSelectionEnabled_Toggled_Off_Clears_Selection()
		{
			var SUT = CreateRichTextBlock(LongText);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "Precondition: selection present");

				// Disabling selection destroys the manager and clears the flat selection.
				SUT.IsTextSelectionEnabled = false;
				await WindowHelper.WaitForIdle();

				Assert.IsNull(SUT.SelectionStart, "SelectionStart should be null once selection is disabled");
				Assert.AreEqual(string.Empty, SUT.SelectedText, "SelectedText should be cleared when selection is disabled");

				// Re-enabling recreates the manager so selection works again.
				SUT.IsTextSelectionEnabled = true;
				await WindowHelper.WaitForIdle();

				SUT.SelectAll();
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(string.IsNullOrEmpty(SUT.SelectedText), "Selection should work again after re-enabling");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_GetPositionFromPoint_After_ContentStart_Uses_Live_Layout()
		{
			// Reading ContentStart/ContentEnd must not rebuild a measure-dirty tree, otherwise every
			// subsequent hit-test returns offset 0. Query the pointers first, then hit-test two distinct
			// x positions on the same line — they must resolve to different offsets.
			var SUT = CreateRichTextBlock("The quick brown fox jumps over the lazy dog.", width: 400);

			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				// Access the TextPointer read paths that previously triggered a layout rebuild.
				_ = SUT.ContentStart;
				_ = SUT.ContentEnd;

				var near = SUT.GetPositionFromPoint(new Point(5, 8));
				var far = SUT.GetPositionFromPoint(new Point(200, 8));

				if (near is null || far is null)
				{
					Assert.Fail($"GetPositionFromPoint should return non-null positions (near={near}, far={far})");
					return;
				}

				Assert.IsTrue(
					far.Offset > near.Offset,
					$"Hit-test should use live layout after ContentStart access (near {near.Offset}, far {far.Offset})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Overflow_Content_Reports_IsTextTrimmed()
		{
			// WinUI UpdateIsTextTrimmed reports true whenever content flows to an overflow target,
			// independent of TextTrimming/MaxLines. Use a height-constrained master (no MaxLines, no
			// TextTrimming) so the only trigger is HasOverflowContent.
			var master = new RichTextBlock { Width = 180, Height = 28 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = LongText });
			master.Blocks.Add(paragraph);

			var overflow = new RichTextBlockOverflow { Width = 180 };
			master.OverflowContentTarget = overflow;

			var panel = new StackPanel();
			panel.Children.Add(master);
			panel.Children.Add(overflow);

			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(master.HasOverflowContent, "Precondition: master should overflow");
				Assert.IsTrue(master.IsTextTrimmed, "IsTextTrimmed should be true when content flows to an overflow target");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
