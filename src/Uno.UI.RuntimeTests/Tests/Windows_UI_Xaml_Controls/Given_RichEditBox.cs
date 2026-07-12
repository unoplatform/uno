using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if __SKIA__
using Windows.System;
using Microsoft.UI.Xaml.Input;
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
		public async Task When_Range_FindText_Zero_Scan_Uses_Range_Or_Remainder()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one two one");

			var limited = SUT.Document.GetRange(4, 7);
			Assert.AreEqual(0, limited.FindText("one", 0, FindOptions.None));
			Assert.AreEqual(4, limited.StartPosition);
			Assert.AreEqual(7, limited.EndPosition);

			var remainder = SUT.Document.GetRange(4, 4);
			Assert.AreEqual(3, remainder.FindText("one", 0, FindOptions.None));
			Assert.AreEqual(8, remainder.StartPosition);
			Assert.AreEqual(11, remainder.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_FindText_Repeated_Search_Advances()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one one one");
			var forward = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(3, forward.FindText("one", 11, FindOptions.None));
			Assert.AreEqual(0, forward.StartPosition);
			Assert.AreEqual(3, forward.FindText("one", 11, FindOptions.None));
			Assert.AreEqual(4, forward.StartPosition);

			var backward = SUT.Document.GetRange(11, 11);
			Assert.AreEqual(3, backward.FindText("one", -11, FindOptions.None));
			Assert.AreEqual(8, backward.StartPosition);
			Assert.AreEqual(3, backward.FindText("one", -11, FindOptions.None));
			Assert.AreEqual(4, backward.StartPosition);
		}

		[TestMethod]
		public async Task When_Range_FindText_Word_Requires_Whole_Word()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "concatenate cat cat2");
			var range = SUT.Document.GetRange(0, 0);

			Assert.AreEqual(3, range.FindText("cat", 20, FindOptions.Word));
			Assert.AreEqual(12, range.StartPosition);
			Assert.AreEqual(15, range.EndPosition);
			Assert.AreEqual(0, range.FindText("cat", 20, FindOptions.Word));
		}

		[TestMethod]
		public async Task When_Range_FindText_Word_Does_Not_Split_Combining_Sequence()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "cafe\u0301 cafe");
			var range = SUT.Document.GetRange(0, 0);

			Assert.AreEqual(4, range.FindText("cafe", 10, FindOptions.Word));
			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(10, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_FindText_Match_Must_Fit_Scan_Window()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcde");
			var range = SUT.Document.GetRange(0, 0);

			Assert.AreEqual(0, range.FindText("cde", 4, FindOptions.None));
			Assert.AreEqual(3, range.FindText("cde", 5, FindOptions.None));
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
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
			Assert.AreEqual(1, removed);
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
		public async Task When_Range_Delete_NonDegenerate_Count_Deletes_Additional_Units()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var range = SUT.Document.GetRange(1, 4);

			Assert.AreEqual(2, range.Delete(TextRangeUnit.Character, 2));
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("af", text);
			Assert.AreEqual(1, range.StartPosition);
			Assert.AreEqual(1, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_Delete_Extreme_Counts_Clamp_Without_Overflow()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var forward = SUT.Document.GetRange(2, 4);
			Assert.AreEqual(3, forward.Delete(TextRangeUnit.Character, int.MaxValue));
			SUT.Document.GetText(TextGetOptions.None, out var afterForward);
			Assert.AreEqual("ab", afterForward);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var backward = SUT.Document.GetRange(2, 4);
			Assert.AreEqual(3, backward.Delete(TextRangeUnit.Character, int.MinValue));
			SUT.Document.GetText(TextGetOptions.None, out var afterBackward);
			Assert.AreEqual("ef", afterBackward);
		}

		[TestMethod]
		public async Task When_Live_Range_Rebases_After_Another_Range_Edits()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var trackedCaret = SUT.Document.GetRange(4, 4);
			SUT.Document.GetRange(0, 0).Text = "X";

			Assert.AreEqual(5, trackedCaret.StartPosition);
			Assert.AreEqual(5, trackedCaret.EndPosition);
		}

		[TestMethod]
		public async Task When_NoOp_Replacement_Does_Not_Rebase_Live_Range()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var trackedRange = SUT.Document.GetRange(2, 4);
			SUT.Document.GetRange(1, 5).Text = "bcde";

			Assert.AreEqual(2, trackedRange.StartPosition);
			Assert.AreEqual(4, trackedRange.EndPosition);
			Assert.AreEqual("cd", trackedRange.Text);
		}

		[TestMethod]
		public async Task When_InRange_Uses_Story_Identity()
		{
			var first = new RichEditBox();
			var second = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(first);
			panel.Children.Add(second);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			first.Document.SetText(TextSetOptions.None, "abc");
			second.Document.SetText(TextSetOptions.None, "abc");

			Assert.IsFalse(first.Document.GetRange(1, 2).InRange(second.Document.GetRange(0, 3)));
		}

		[TestMethod]
		public async Task When_GetCharacterUtf32_Offset_Is_Relative_To_Range_End()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var range = SUT.Document.GetRange(1, 3);
			range.GetCharacterUtf32(out var atEnd, 0);
			range.GetCharacterUtf32(out var beforeEnd, -1);

			Assert.AreEqual((uint)'d', atEnd);
			Assert.AreEqual((uint)'c', beforeEnd);
		}

		[TestMethod]
		public async Task When_GetCharacterUtf32_Offset_Inside_Surrogate_Returns_CodePoint()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "A\U0001F600B");
			var backward = SUT.Document.GetRange(3, 3);
			backward.GetCharacterUtf32(out var backwardCodePoint, -1);
			var forward = SUT.Document.GetRange(1, 1);
			forward.GetCharacterUtf32(out var forwardCodePoint, 1);

			Assert.AreEqual(0x1F600u, backwardCodePoint);
			Assert.AreEqual(0x1F600u, forwardCodePoint);
		}

		[TestMethod]
		public async Task When_MaxLength_Does_Not_Split_Surrogate_Pair()
		{
			var SUT = new RichEditBox { MaxLength = 1 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "\U0001F600");
			SUT.Document.GetText(TextGetOptions.None, out var setText);
			Assert.AreEqual(string.Empty, setText);

			SUT.MaxLength = 2;
			SUT.Document.Selection.TypeText("\U0001F600X");
			SUT.Document.GetText(TextGetOptions.None, out var typed);
			Assert.AreEqual("\U0001F600", typed);
		}

		[TestMethod]
		public async Task When_Word_Navigation_Does_Not_Split_Combining_Sequence()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "a\u0301b c");
			var range = SUT.Document.GetRange(0, 0);

			Assert.AreEqual(1, range.Move(TextRangeUnit.Word, 1));
			Assert.AreEqual(4, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Word_Navigation_Handles_Special_Text_Elements()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, " \uFE0F!\tA");
			var range = SUT.Document.GetRange(0, 0);

			Assert.AreEqual(1, range.Move(TextRangeUnit.Word, 1));
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(1, range.Move(TextRangeUnit.Word, 1));
			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(1, range.Move(TextRangeUnit.Word, 1));
			Assert.AreEqual(4, range.StartPosition);
		}

		[TestMethod]
		public async Task When_Selection_TypeText_Adheres_To_MaxLength_And_Overtype()
		{
			var SUT = new RichEditBox { MaxLength = 3 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.Selection.TypeText("abcd");
			SUT.Document.GetText(TextGetOptions.None, out var limited);
			Assert.AreEqual("abc", limited);

			SUT.MaxLength = 0;
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Document.Selection.Options = SelectionOptions.Overtype;
			SUT.Document.Selection.TypeText("X");
			SUT.Document.GetText(TextGetOptions.None, out var overtyped);
			Assert.AreEqual("aXc", overtyped);
		}

		[TestMethod]
		public async Task When_Document_Text_Normalizes_And_Converts_Line_Endings()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "a\r\nb\nc");
			SUT.Document.GetText(TextGetOptions.None, out var normalized);
			SUT.Document.GetText(TextGetOptions.UseLf, out var lf);
			SUT.Document.GetText(TextGetOptions.UseCrlf, out var crlf);

			Assert.AreEqual("a\rb\rc", normalized);
			Assert.AreEqual("a\nb\nc", lf);
			Assert.AreEqual("a\r\nb\r\nc", crlf);
			Assert.ThrowsExactly<ArgumentException>(() =>
				SUT.Document.GetText(TextGetOptions.UseLf | TextGetOptions.UseCrlf, out _));
		}

		[TestMethod]
		public async Task When_Range_GetText_Converts_Line_Endings()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "a\rb\rc");
			var range = SUT.Document.GetRange(0, 3);
			range.GetText(TextGetOptions.None, out var raw);
			range.GetText(TextGetOptions.AdjustCrlf, out var adjusted);
			range.GetText(TextGetOptions.UseLf, out var lf);
			range.GetText(TextGetOptions.UseCrlf, out var crlf);
			range.GetText(TextGetOptions.AdjustCrlf | TextGetOptions.UseLf, out var adjustedLf);

			Assert.AreEqual("a\rb", raw);
			Assert.AreEqual("a\rb", adjusted);
			Assert.AreEqual("a\nb", lf);
			Assert.AreEqual("a\r\nb", crlf);
			Assert.AreEqual("a\nb", adjustedLf);
			Assert.ThrowsExactly<ArgumentException>(() =>
				range.GetText(TextGetOptions.UseLf | TextGetOptions.UseCrlf, out _));
		}

		[TestMethod]
		public async Task When_Range_GetText_AdjustCrlf_Moves_To_Text_Element_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "A\U0001F600B");
			var range = SUT.Document.GetRange(2, 3);
			range.GetText(TextGetOptions.None, out var raw);
			range.GetText(TextGetOptions.AdjustCrlf, out var adjusted);

			Assert.AreEqual(1, raw.Length);
			Assert.IsTrue(char.IsLowSurrogate(raw[0]));
			Assert.AreEqual("\U0001F600", adjusted);
		}

		[TestMethod]
		public async Task When_Bound_Format_SetClone_Applies_To_Document()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			var sourceCharacterFormat = SUT.Document.GetRange(0, 3).CharacterFormat.GetClone();
			sourceCharacterFormat.Bold = FormatEffect.On;
			SUT.Document.GetRange(0, 3).CharacterFormat.SetClone(sourceCharacterFormat);

			var sourceParagraphFormat = SUT.Document.GetRange(0, 3).ParagraphFormat.GetClone();
			sourceParagraphFormat.Alignment = ParagraphAlignment.Right;
			SUT.Document.GetRange(0, 3).ParagraphFormat.SetClone(sourceParagraphFormat);

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Bold);
			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetRange(0, 3).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_Range_Delete_Word_Forward_Removes_Word()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// TOM Delete(Word, 1) from a caret deletes one word forward (incl. trailing space) and
			// returns the count of units deleted.
			var caret = SUT.Document.GetRange(0, 0);
			var removed = caret.Delete(TextRangeUnit.Word, 1);

			Assert.AreEqual(1, removed);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("World", text);
		}

		[TestMethod]
		public async Task When_Range_Delete_Word_Backward_Removes_Previous_Word()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// A negative count deletes toward the start of the story (like CTRL+BACKSPACE).
			var caret = SUT.Document.GetRange(11, 11);
			var removed = caret.Delete(TextRangeUnit.Word, -1);

			Assert.AreEqual(1, removed);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello ", text);
		}

		[TestMethod]
		public async Task When_Range_Delete_Paragraph_Removes_Paragraph()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");

			// A paragraph includes its trailing break, so deleting one paragraph forward removes "aa\r".
			var caret = SUT.Document.GetRange(0, 0);
			var removed = caret.Delete(TextRangeUnit.Paragraph, 1);

			Assert.AreEqual(1, removed);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("bb\rcc", text);
		}

		[TestMethod]
		public async Task When_Range_MoveEnd_MoveStart_Word_Steps_By_Word()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// MoveEnd(Word, 1) extends the end edge to the next word boundary (after "Hello ").
			var range = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(1, range.MoveEnd(TextRangeUnit.Word, 1));
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);

			// MoveStart(Word, 1) advances the start edge to the same boundary, collapsing the range.
			Assert.AreEqual(1, range.MoveStart(TextRangeUnit.Word, 1));
			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
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
		public async Task When_Range_Move_Count0_Leaves_Range_Unchanged()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			// TOM ITextRange::Move: count 0 must leave even a non-degenerate range untouched.
			var range = SUT.Document.GetRange(1, 4);
			Assert.AreEqual(0, range.Move(TextRangeUnit.Character, 0));
			Assert.AreEqual(1, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_Move_NonDegenerate_Collapse_Counts_As_Unit()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			// [1,4].Move(+1): collapsing to the right edge (4) is the single unit moved.
			var right = SUT.Document.GetRange(1, 4);
			Assert.AreEqual(1, right.Move(TextRangeUnit.Character, 1));
			Assert.AreEqual(4, right.StartPosition);
			Assert.AreEqual(4, right.EndPosition);

			// [1,4].Move(-1): collapsing to the left edge (1) is the single unit moved.
			var left = SUT.Document.GetRange(1, 4);
			Assert.AreEqual(-1, left.Move(TextRangeUnit.Character, -1));
			Assert.AreEqual(1, left.StartPosition);
			Assert.AreEqual(1, left.EndPosition);

			// [1,4].Move(+2): collapse to 4 (unit 1) then one further character to 5.
			var right2 = SUT.Document.GetRange(1, 4);
			Assert.AreEqual(2, right2.Move(TextRangeUnit.Character, 2));
			Assert.AreEqual(5, right2.StartPosition);
			Assert.AreEqual(5, right2.EndPosition);
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
		public async Task When_Selection_MoveRight_NonExtend_Collapses_To_Edge()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			var selection = SUT.Document.Selection;

			// Non-degenerate [2,5]: collapsing to the right edge already counts as the single unit moved.
			selection.SetRange(2, 5);
			Assert.AreEqual(1, selection.MoveRight(TextRangeUnit.Character, 1, false));
			Assert.AreEqual(5, selection.StartPosition);
			Assert.AreEqual(5, selection.EndPosition);

			// Non-degenerate [1,3] moving left collapses to the left edge (1) and counts as one unit.
			selection.SetRange(1, 3);
			Assert.AreEqual(1, selection.MoveLeft(TextRangeUnit.Character, 1, false));
			Assert.AreEqual(1, selection.StartPosition);
			Assert.AreEqual(1, selection.EndPosition);

			// A degenerate caret still moves the full count.
			selection.SetRange(1, 1);
			Assert.AreEqual(2, selection.MoveRight(TextRangeUnit.Character, 2, false));
			Assert.AreEqual(3, selection.StartPosition);
			Assert.AreEqual(3, selection.EndPosition);
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

		[TestMethod]
		public async Task When_CharacterFormat_Bold_RoundTrips_And_Mixed_Is_Undefined()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello World");

			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;

			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.Off, doc.GetRange(6, 11).CharacterFormat.Bold);
			// A range that spans both bold and non-bold text reports Undefined.
			Assert.AreEqual(FormatEffect.Undefined, doc.GetRange(0, 11).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Bold_Toggle_Flips_State()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			// Toggle on unformatted text turns Bold on...
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.Toggle;
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);

			// ...and Toggle again flips it back off.
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.Toggle;
			Assert.AreEqual(FormatEffect.Off, doc.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Toggle_Flips_Each_Character_Independently()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			// Seed the first two characters bold, leave the rest not bold.
			doc.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;

			// Toggle across the whole range flips each character from its own current state.
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.Toggle;

			Assert.AreEqual(FormatEffect.Off, doc.GetRange(0, 2).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, doc.GetRange(2, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_KeepTogether_Toggle_Flips_State()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			doc.GetRange(0, 5).ParagraphFormat.KeepTogether = FormatEffect.Toggle;
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).ParagraphFormat.KeepTogether);

			doc.GetRange(0, 5).ParagraphFormat.KeepTogether = FormatEffect.Toggle;
			Assert.AreEqual(FormatEffect.Off, doc.GetRange(0, 5).ParagraphFormat.KeepTogether);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Italic_Underline_Strikethrough_RoundTrip()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			var cf = doc.GetRange(0, 5).CharacterFormat;
			cf.Italic = FormatEffect.On;
			cf.Underline = UnderlineType.Single;
			cf.Strikethrough = FormatEffect.On;

			var read = doc.GetRange(0, 5).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, read.Italic);
			Assert.AreEqual(UnderlineType.Single, read.Underline);
			Assert.AreEqual(FormatEffect.On, read.Strikethrough);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Foreground_Size_Name_RoundTrip()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			var red = global::Windows.UI.Color.FromArgb(255, 255, 0, 0);
			var cf = doc.GetRange(0, 5).CharacterFormat;
			cf.ForegroundColor = red;
			cf.Size = 24f;
			cf.Name = "Comic Sans MS";

			var read = doc.GetRange(0, 5).CharacterFormat;
			Assert.AreEqual(red, read.ForegroundColor);
			Assert.AreEqual(24f, read.Size);
			Assert.AreEqual("Comic Sans MS", read.Name);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Inserted_Text_Inherits_Left_Format()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;

			var selection = doc.Selection;
			selection.SetRange(5, 5);
			selection.TypeText(" World");

			doc.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello World", text);

			// Original characters keep their bold, and text typed after a bold character inherits it.
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, doc.GetRange(5, 11).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Undo_Redo_Reverts_Formatting()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);
			Assert.IsTrue(doc.CanUndo());

			doc.Undo();
			Assert.AreEqual(FormatEffect.Off, doc.GetRange(0, 5).CharacterFormat.Bold);

			doc.Redo();
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_GetClone_Is_Unbound_And_IsEqual_Works()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;

			var read = doc.GetRange(0, 5).CharacterFormat;
			var clone = read.GetClone();
			Assert.IsTrue(read.IsEqual(clone));

			// The clone is a detached value object: mutating it must not touch the document.
			clone.Bold = FormatEffect.Off;
			Assert.IsFalse(read.IsEqual(clone));
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Whole_Assignment_Applies_Defined_Properties()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			var fmt = doc.GetRange(0, 0).CharacterFormat.GetClone();
			fmt.Italic = FormatEffect.On;
			doc.GetRange(0, 5).CharacterFormat = fmt;

			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 5).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Degenerate_Range_Reports_Basis()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;

			// A caret (degenerate range) reports the formatting newly typed text would take.
			Assert.AreEqual(FormatEffect.On, doc.GetRange(3, 3).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, doc.GetRange(0, 0).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Renders_As_Inline_Runs()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello World");
			doc.GetRange(0, 5).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Hello World", displayBlock.Text);

			// The bold prefix and the plain remainder become two distinct inline runs.
			Assert.AreEqual(2, displayBlock.Inlines.Count);
			var first = displayBlock.Inlines[0] as Run;
			var second = displayBlock.Inlines[1] as Run;
			Assert.IsNotNull(first);
			Assert.IsNotNull(second);
			Assert.AreEqual("Hello", first.Text);
			Assert.AreEqual(700, first.FontWeight.Weight);
			Assert.AreEqual(" World", second.Text);
			Assert.AreEqual(400, second.FontWeight.Weight);
		}

		[TestMethod]
		public async Task When_CharacterFormat_Removed_Falls_Back_To_Plain_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");
			var range = doc.GetRange(0, 5);
			range.CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			range.CharacterFormat.Bold = FormatEffect.Off;
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Hello", displayBlock.Text);

			// No inline should carry bold once the formatting is removed.
			foreach (var inline in displayBlock.Inlines)
			{
				if (inline is Run run)
				{
					Assert.AreEqual(400, run.FontWeight.Weight);
				}
			}
		}

		[TestMethod]
		public async Task When_CharacterFormat_Underline_Foreground_Size_Render()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var doc = SUT.Document;
			doc.SetText(TextSetOptions.None, "Hello");

			var red = global::Windows.UI.Color.FromArgb(255, 255, 0, 0);
			var cf = doc.GetRange(0, 5).CharacterFormat;
			cf.Underline = UnderlineType.Single;
			cf.ForegroundColor = red;
			cf.Size = 30f;
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);

			var run = displayBlock.Inlines[0] as Run;
			Assert.IsNotNull(run);
			Assert.AreEqual("Hello", run.Text);
			Assert.IsTrue(run.TextDecorations.HasFlag(global::Windows.UI.Text.TextDecorations.Underline));
			Assert.AreEqual(30d, run.FontSize);
			var brush = run.Foreground as SolidColorBrush;
			Assert.IsNotNull(brush);
			Assert.AreEqual(red, brush.Color);
		}
		#region Interactive keyboard editing (IE-4)

		private static void RaiseKey(RichEditBox sut, VirtualKey key, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None, char unicodeKey = '\0')
		{
			if (modifiers.HasFlag(VirtualKeyModifiers.Control)
				&& (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsTvOS()))
			{
				modifiers = (modifiers & ~VirtualKeyModifiers.Control) | VirtualKeyModifiers.Windows;
			}

			var args = unicodeKey != '\0'
				? new KeyRoutedEventArgs(sut, key, modifiers, unicodeKey: unicodeKey)
				: new KeyRoutedEventArgs(sut, key, modifiers);
			sut.SafeRaiseEvent(UIElement.KeyDownEvent, args);
		}

		private static async Task TypeAsync(RichEditBox sut, string text)
		{
			foreach (var c in text)
			{
				RaiseKey(sut, VirtualKey.None, VirtualKeyModifiers.None, c);
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		public async Task When_Typing_Inserts_At_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Backspace_Deletes_Before_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.Back);
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ab", text);
			Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
		}

		[TestMethod]
		public async Task When_Delete_Removes_After_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.Home);
			await WindowHelper.WaitForIdle();
			RaiseKey(SUT, VirtualKey.Delete);
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("bc", text);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
		}

		[TestMethod]
		public async Task When_Left_Right_Arrows_Move_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			RaiseKey(SUT, VirtualKey.Left);
			await WindowHelper.WaitForIdle();
			RaiseKey(SUT, VirtualKey.Left);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(1, SUT.Document.Selection.EndPosition);

			RaiseKey(SUT, VirtualKey.Right);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
		}

		[TestMethod]
		public async Task When_Shift_Left_Extends_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Home_And_End_Move_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			RaiseKey(SUT, VirtualKey.Home);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);

			RaiseKey(SUT, VirtualKey.End);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
		}

		[TestMethod]
		public async Task When_CtrlA_Selects_All()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Typing_Replaces_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			await TypeAsync(SUT, "X");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("X", text);
			Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
		}

		[TestMethod]
		public async Task When_Ctrl_Z_And_Y_Undo_Redo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "ab");

			RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("a", afterUndo);

			RaiseKey(SUT, VirtualKey.Y, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			SUT.Document.GetText(TextGetOptions.None, out var afterRedo);
			Assert.AreEqual("ab", afterRedo);
		}

		[TestMethod]
		public async Task When_Enter_Inserts_Paragraph_Break()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "a");
			RaiseKey(SUT, VirtualKey.Enter, VirtualKeyModifiers.None, '\r');
			await WindowHelper.WaitForIdle();
			await TypeAsync(SUT, "b");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("a\rb", text);
		}

		[TestMethod]
		public async Task When_ReadOnly_Ignores_Typing()
		{
			var SUT = new RichEditBox { IsReadOnly = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		#endregion
#endif
	}
}
