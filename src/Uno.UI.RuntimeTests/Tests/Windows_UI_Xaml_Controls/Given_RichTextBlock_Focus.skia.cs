using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#nullable enable

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Covers CRichTextBlock::GetFocusableChildren: the focusable-inline (Hyperlink) enumeration that lets
	// keyboard focus reach hyperlinks inside a RichTextBlock. Exercises document-order + span nesting, the
	// empty case, the cache reset on content change, and end-to-end Tab navigation into a hyperlink.
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_Focus
	{
		[TestMethod]
		public async Task When_GetFocusableChildren_Returns_Hyperlinks_In_DocumentOrder()
		{
			var link1 = new Hyperlink();
			link1.Inlines.Add(new Run { Text = "first" });
			var link2 = new Hyperlink();
			link2.Inlines.Add(new Run { Text = "second" });

			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "before " });
			paragraph.Inlines.Add(link1);
			paragraph.Inlines.Add(new Run { Text = " middle " });

			// Nest the second hyperlink inside a Span to exercise the recursive helper.
			var span = new Span();
			span.Inlines.Add(link2);
			paragraph.Inlines.Add(span);

			var SUT = new RichTextBlock();
			SUT.Blocks.Add(paragraph);

			await UITestHelper.Load(SUT);

			var focusable = SUT.GetFocusableChildren();

			CollectionAssert.AreEqual(
				new DependencyObject[] { link1, link2 },
				focusable.ToArray(),
				"GetFocusableChildren should return the hyperlinks in document order, descending into spans.");
		}

		[TestMethod]
		public async Task When_No_Hyperlinks_Then_GetFocusableChildren_Is_Empty()
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "plain text, no links" });

			var SUT = new RichTextBlock();
			SUT.Blocks.Add(paragraph);

			await UITestHelper.Load(SUT);

			Assert.AreEqual(0, SUT.GetFocusableChildren().Count);
		}

		[TestMethod]
		public async Task When_Content_Changes_Then_GetFocusableChildren_Cache_Resets()
		{
			var link1 = new Hyperlink();
			link1.Inlines.Add(new Run { Text = "first" });
			var paragraph1 = new Paragraph();
			paragraph1.Inlines.Add(link1);

			var SUT = new RichTextBlock();
			SUT.Blocks.Add(paragraph1);

			await UITestHelper.Load(SUT);

			Assert.AreEqual(1, SUT.GetFocusableChildren().Count, "Initially there is one hyperlink.");

			// Adding a block fires OnBlocksChanged -> InvalidateBlockContent, which must invalidate the
			// cached focusable-children collection so the next query re-walks the tree.
			var link2 = new Hyperlink();
			link2.Inlines.Add(new Run { Text = "second" });
			var paragraph2 = new Paragraph();
			paragraph2.Inlines.Add(link2);
			SUT.Blocks.Add(paragraph2);

			await WindowHelper.WaitForIdle();

			var focusable = SUT.GetFocusableChildren();
			Assert.AreEqual(2, focusable.Count, "The cache must repopulate after a content change.");
			Assert.AreSame(link2, focusable[1]);
		}

		[TestMethod]
		public async Task When_Tab_Then_Navigates_Into_Hyperlink()
		{
			var before = new Button { Content = "before" };
			var after = new Button { Content = "after" };

			var link = new Hyperlink();
			link.Inlines.Add(new Run { Text = "link" });
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "text " });
			paragraph.Inlines.Add(link);
			var rtb = new RichTextBlock();
			rtb.Blocks.Add(paragraph);

			var panel = new StackPanel
			{
				Children = { before, rtb, after }
			};

			await UITestHelper.Load(panel);

			before.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var moved = FocusManager.TryMoveFocus(
				FocusNavigationDirection.Next,
				new FindNextElementOptions { SearchRoot = WindowHelper.XamlRoot!.Content });
			await WindowHelper.WaitForIdle();

			var focused = FocusManager.GetFocusedElement(WindowHelper.XamlRoot!);

			Assert.IsTrue(moved, "Focus should move to the next focusable element.");
			Assert.AreSame(link, focused, "Tab from the preceding button should focus the hyperlink inside the RichTextBlock.");
		}
	}
}
