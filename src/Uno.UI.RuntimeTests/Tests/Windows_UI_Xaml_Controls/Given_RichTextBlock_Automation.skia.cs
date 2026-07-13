using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Automation
	{
		private const string Text = "Hello brave new world from RichTextBlock";

		private static RichTextBlock BuildSut(bool selectionEnabled = false)
		{
			var sut = new RichTextBlock { Width = 400, IsTextSelectionEnabled = selectionEnabled };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = Text });
			sut.Blocks.Add(paragraph);
			return sut;
		}

		private static ITextProvider GetTextProvider(FrameworkElement element)
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
			Assert.IsNotNull(peer, "Automation peer should be created for the element");
			var provider = peer!.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(provider, "The element's automation peer should expose the Text pattern");
			return provider!;
		}

		[TestMethod]
		public async Task When_DocumentRange_Returns_Content_Text()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var provider = GetTextProvider(SUT);
				var documentRange = provider.DocumentRange;
				Assert.IsNotNull(documentRange, "DocumentRange should be non-null once content is laid out");

				var text = documentRange.GetText(-1);
				// UIA text includes the closing paragraph mark ("\r\n" per closed Paragraph), matching
				// WinUI's CTextBoxHelpers::GetText(insertNewlines=TRUE) that backs the Text pattern.
				Assert.AreEqual(Text + "\r\n", text, "DocumentRange text should match the RichTextBlock content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ExpandToEnclosingUnit_Character()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var range = GetTextProvider(SUT).DocumentRange;
				range.ExpandToEnclosingUnit(TextUnit.Character);

				var character = range.GetText(-1);
				Assert.IsFalse(string.IsNullOrEmpty(character), "Character unit should enclose a single non-empty character");
				Assert.IsTrue(character.Length <= 2, $"Character unit should span a single character (was '{character}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ExpandToEnclosingUnit_Word_Moves()
		{
			// Regression guard for TextUnit.Word: MoveByWord used to be a no-op, so expanding to the word
			// unit collapsed to an empty range. It should now enclose the first word.
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var range = GetTextProvider(SUT).DocumentRange;
				range.ExpandToEnclosingUnit(TextUnit.Word);

				var word = range.GetText(-1);
				Assert.IsFalse(string.IsNullOrEmpty(word), "Word navigation should enclose a non-empty word (MoveByWord regression)");
				Assert.IsTrue(word.Length < Text.Length, $"Word unit should be a single word, not the whole document (was '{word}')");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Move_By_Word_Reports_Movement()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var range = GetTextProvider(SUT).DocumentRange;
				range.ExpandToEnclosingUnit(TextUnit.Word);

				var moved = range.Move(TextUnit.Word, 1);
				Assert.IsTrue(moved >= 1, $"Move by one word should report a non-zero move count (was {moved})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_GetBoundingRectangles_NonEmpty_After_Layout()
		{
			var SUT = BuildSut();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var range = GetTextProvider(SUT).DocumentRange;
				range.GetBoundingRectangles(out var rectangles);

				Assert.IsNotNull(rectangles);
				Assert.IsTrue(rectangles.Length >= 4, "Laid-out content should produce at least one bounding rectangle (4 doubles)");
				Assert.AreEqual(0, rectangles.Length % 4, "Bounding rectangles are flattened as [X,Y,W,H] quadruples");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Surfaces_As_Range()
		{
			var SUT = BuildSut(selectionEnabled: true);
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				SUT.Select(SUT.ContentStart!, SUT.ContentEnd!);
				await WindowHelper.WaitForIdle();

				var provider = GetTextProvider(SUT);
				Assert.AreEqual(SupportedTextSelection.Single, provider.SupportedTextSelection);

				var selection = provider.GetSelection();
				Assert.AreEqual(1, selection.Length, "A single selection range should be surfaced");

				var selectedText = selection[0].GetText(-1);
				// SelectAll spans through the closing paragraph mark, so the UIA text includes "\r\n".
				Assert.AreEqual(Text + "\r\n", selectedText, "The surfaced selection range should cover the selected content");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Overflow_DocumentRange_Returns_Slice()
		{
			// The overflow's Text pattern should expose its own page slice, not a null range.
			var master = new RichTextBlock { Width = 180, MaxLines = 2 };
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run
			{
				Text =
					"Line one of the content. Line two of the content. Line three of the content. " +
					"Line four of the content. Line five of the content. Line six of the content."
			});
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

				Assert.IsTrue(master.HasOverflowContent, "Master should overflow into the target");

				var provider = GetTextProvider(overflow);
				var documentRange = provider.DocumentRange;
				Assert.IsNotNull(documentRange, "Overflow DocumentRange should expose the overflowed slice, not null");

				var sliceText = documentRange.GetText(-1);
				Assert.IsFalse(string.IsNullOrEmpty(sliceText), "The overflow slice should surface its text to the Text pattern");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
