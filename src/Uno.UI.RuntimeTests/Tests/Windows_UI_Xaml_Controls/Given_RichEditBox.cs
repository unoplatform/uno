using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if __SKIA__
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_RichEditBox
	{
#if __SKIA__
		[TestMethod]
		public async Task When_Document_SetText_GetText_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello RichEditBox");
			SUT.Document.GetText(TextGetOptions.None, out var text);

			Assert.AreEqual("Hello RichEditBox", text);
		}

		[TestMethod]
		public async Task When_Document_And_TextDocument_Are_Same_Instance()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.Document);
			Assert.AreSame(SUT.Document, SUT.TextDocument);
		}

		[TestMethod]
		public async Task When_SetText_Renders_In_DisplayBlock()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Rendered content");
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			Assert.IsNotNull(contentElement);

			var displayBlock = contentElement.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Rendered content", displayBlock.Text);
		}

		[TestMethod]
		public async Task When_Placeholder_Reflects_Emptiness()
		{
			var SUT = new RichEditBox { PlaceholderText = "Type here" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var placeholder = SUT.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
			Assert.IsNotNull(placeholder);
			Assert.AreEqual(Visibility.Visible, placeholder.Visibility);

			SUT.Document.SetText(TextSetOptions.None, "Not empty anymore");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility);
		}

		[TestMethod]
		public async Task When_Header_Presenter_Visibility_Follows_Header()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var header = SUT.FindFirstChild<ContentPresenter>(cp => cp.Name == "HeaderContentPresenter");
			Assert.IsNotNull(header);
			Assert.AreEqual(Visibility.Collapsed, header.Visibility);

			SUT.Header = "A header";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, header.Visibility);
		}

		[TestMethod]
		public async Task When_IsReadOnly_Is_Reflected()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsReadOnly);
			SUT.IsReadOnly = true;
			Assert.IsTrue(SUT.IsReadOnly);
		}

		[TestMethod]
		public async Task When_GetRange_Returns_Positions_And_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
			Assert.AreEqual(5, range.Length);
			Assert.AreEqual("Hello", range.Text);
			Assert.AreEqual("Hello World".Length + 1, range.StoryLength);
		}

		[TestMethod]
		public async Task When_Range_Text_Set_Edits_Document_And_Renders()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			range.Text = "Howdy";

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Howdy World", text);
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);

			await WindowHelper.WaitForIdle();
			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Howdy World", displayBlock.Text);
		}

		[TestMethod]
		public async Task When_Range_SetRange_Normalizes_And_Collapses()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			var range = SUT.Document.GetRange(0, 0);
			range.SetRange(4, 1);
			Assert.AreEqual(1, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);

			range.Collapse(true);
			Assert.AreEqual(1, range.StartPosition);
			Assert.AreEqual(1, range.EndPosition);

			range.SetRange(1, 4);
			range.Collapse(false);
			Assert.AreEqual(4, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_FindText_Honors_Case()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "the quick brown fox");

			var range = SUT.Document.GetRange(0, 0);
			var found = range.FindText("quick", 100, FindOptions.None);
			Assert.AreEqual(5, found);
			Assert.AreEqual(4, range.StartPosition);
			Assert.AreEqual(9, range.EndPosition);

			// Case-insensitive by default.
			var ci = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(3, ci.FindText("THE", 100, FindOptions.None));

			// Case-sensitive miss leaves the range untouched.
			var cs = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(0, cs.FindText("THE", 100, FindOptions.Case));
			Assert.AreEqual(0, cs.StartPosition);
			Assert.AreEqual(0, cs.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_ChangeCase_Rewrites_Content()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello world");

			var range = SUT.Document.GetRange(0, 5);
			range.ChangeCase(LetterCase.Upper);

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("HELLO world", text);
		}

		[TestMethod]
		public async Task When_Range_Delete_Removes_Selection_And_Characters()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// A non-degenerate range deletes its own content.
			var range = SUT.Document.GetRange(5, 11);
			var removed = range.Delete(TextRangeUnit.Character, 1);
			Assert.AreEqual(6, removed);
			SUT.Document.GetText(TextGetOptions.None, out var afterSelection);
			Assert.AreEqual("Hello", afterSelection);
			Assert.AreEqual(5, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);

			// A degenerate range deletes forward by count.
			var caret = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(2, caret.Delete(TextRangeUnit.Character, 2));
			SUT.Document.GetText(TextGetOptions.None, out var afterForward);
			Assert.AreEqual("llo", afterForward);
		}

		[TestMethod]
		public async Task When_Range_Move_Collapses_And_Clamps()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			var range = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(3, range.Move(TextRangeUnit.Character, 3));
			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition);

			// Movement clamps at the end of the story.
			Assert.AreEqual(2, range.Move(TextRangeUnit.Character, 10));
			Assert.AreEqual(5, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);

			// MoveStart / MoveEnd adjust each edge independently.
			var edges = SUT.Document.GetRange(2, 2);
			Assert.AreEqual(2, edges.MoveEnd(TextRangeUnit.Character, 2));
			Assert.AreEqual(2, edges.StartPosition);
			Assert.AreEqual(4, edges.EndPosition);
			Assert.AreEqual(-1, edges.MoveStart(TextRangeUnit.Character, -1));
			Assert.AreEqual(1, edges.StartPosition);
		}

		[TestMethod]
		public async Task When_Range_GetClone_And_Comparisons()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			var clone = range.GetClone();
			Assert.AreEqual(0, clone.StartPosition);
			Assert.AreEqual(5, clone.EndPosition);
			Assert.IsTrue(range.IsEqual(clone));

			var inner = SUT.Document.GetRange(1, 3);
			Assert.IsTrue(inner.InRange(range));
			Assert.IsFalse(range.InRange(inner));
			Assert.IsTrue(range.InStory(inner));
		}

		[TestMethod]
		public async Task When_Selection_TypeText_Drives_Document_And_Renders()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var selection = SUT.Document.Selection;
			Assert.AreSame(selection, SUT.Document.Selection);
			Assert.AreEqual(SelectionType.InsertionPoint, selection.Type);

			selection.TypeText("Hi");
			Assert.AreEqual(2, selection.StartPosition);
			Assert.AreEqual(2, selection.EndPosition);

			selection.TypeText(" there");
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hi there", text);

			await WindowHelper.WaitForIdle();
			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Hi there", displayBlock.Text);
		}

		[TestMethod]
		public async Task When_Selection_MoveRight_Extends_And_TypeText_Replaces()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			var selection = SUT.Document.Selection;
			selection.SetRange(0, 0);
			Assert.AreEqual(SelectionType.InsertionPoint, selection.Type);

			Assert.AreEqual(3, selection.MoveRight(TextRangeUnit.Character, 3, true));
			Assert.AreEqual(0, selection.StartPosition);
			Assert.AreEqual(3, selection.EndPosition);
			Assert.AreEqual(SelectionType.Normal, selection.Type);

			selection.TypeText("X");
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Xlo", text);
			Assert.AreEqual(1, selection.StartPosition);
			Assert.AreEqual(1, selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Selection_HomeKey_EndKey_On_Story()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			var selection = SUT.Document.Selection;
			selection.SetRange(2, 2);

			selection.EndKey(TextRangeUnit.Story, false);
			Assert.AreEqual(5, selection.StartPosition);
			Assert.AreEqual(5, selection.EndPosition);

			selection.HomeKey(TextRangeUnit.Story, false);
			Assert.AreEqual(0, selection.StartPosition);
			Assert.AreEqual(0, selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Undo_Redo_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.Document.CanUndo());
			Assert.IsFalse(SUT.Document.CanRedo());

			SUT.Document.SetText(TextSetOptions.None, "A");
			SUT.Document.SetText(TextSetOptions.None, "AB");
			Assert.IsTrue(SUT.Document.CanUndo());

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterFirstUndo);
			Assert.AreEqual("A", afterFirstUndo);
			Assert.IsTrue(SUT.Document.CanRedo());

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterSecondUndo);
			Assert.AreEqual("", afterSecondUndo);
			Assert.IsFalse(SUT.Document.CanUndo());

			SUT.Document.Redo();
			SUT.Document.Redo();
			SUT.Document.GetText(TextGetOptions.None, out var afterRedo);
			Assert.AreEqual("AB", afterRedo);
			Assert.IsFalse(SUT.Document.CanRedo());
		}

		[TestMethod]
		public async Task When_Edit_After_Undo_Clears_Redo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "A");
			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.Undo();
			Assert.IsTrue(SUT.Document.CanRedo());

			SUT.Document.SetText(TextSetOptions.None, "AC");
			Assert.IsFalse(SUT.Document.CanRedo());
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("AC", text);
		}

		[TestMethod]
		public async Task When_UndoLimit_Zero_Disables_History()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.UndoLimit = 0;
			SUT.Document.SetText(TextSetOptions.None, "A");
			SUT.Document.SetText(TextSetOptions.None, "B");

			Assert.IsFalse(SUT.Document.CanUndo());
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("B", text);
		}

		[TestMethod]
		public async Task When_UndoLimit_Caps_History()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.UndoLimit = 1;
			SUT.Document.SetText(TextSetOptions.None, "A");
			SUT.Document.SetText(TextSetOptions.None, "B");
			SUT.Document.SetText(TextSetOptions.None, "C");

			// Only the most recent action is retained.
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("B", afterUndo);
			Assert.IsFalse(SUT.Document.CanUndo());
		}

		[TestMethod]
		public async Task When_Undo_Rerenders_DisplayBlock()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			SUT.Document.SetText(TextSetOptions.None, "World");
			SUT.Document.Undo();
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Hello", displayBlock.Text);
		}
#endif
	}
}
