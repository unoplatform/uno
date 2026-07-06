using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public async Task When_Pointer_Click_Places_Caret()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Coordinate-based hit-testing depends on the default font, which differs on Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.Selection.StartPosition > 0, $"Caret should move into the text, was {SUT.Document.Selection.StartPosition}.");
			Assert.AreEqual(SUT.Document.Selection.StartPosition, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Pointer_Drag_Selects_Text()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.MoveBy(40, 0);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				SUT.Document.Selection.EndPosition > SUT.Document.Selection.StartPosition,
				$"Drag should create a non-empty selection, was [{SUT.Document.Selection.StartPosition}, {SUT.Document.Selection.EndPosition}].");
		}

		[TestMethod]
		public async Task When_Shift_Click_Extends_Selection()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var caret = SUT.Document.Selection.StartPosition;

			mouse.MoveBy(40, 0);
			mouse.Press(VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(caret, SUT.Document.Selection.StartPosition);
			Assert.IsTrue(
				SUT.Document.Selection.EndPosition > caret,
				$"Shift+click should extend selection past the caret {caret}, was {SUT.Document.Selection.EndPosition}.");
		}

		[TestMethod]
		public async Task When_Copy_Puts_Selection_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var clipboardText = await content.GetTextAsync();
			Assert.AreEqual("Hello", clipboardText);
		}

		[TestMethod]
		public async Task When_Cut_Removes_Selection_And_Copies()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 6);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CutSelectionToClipboard();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(0, SUT.Document.Selection.EndPosition);

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		public async Task When_Paste_Inserts_At_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XY");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "AXYB";
			});

			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Ctrl_C_Ctrl_V_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.End);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "HelloHello";
			});
		}

		[TestMethod]
		public async Task When_Cut_Is_NoOp_When_ReadOnly()
		{
			var SUT = new RichEditBox { IsReadOnly = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CutSelectionToClipboard();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello world", text);
		}

		[TestMethod]
		public async Task When_Programmatic_SetRange_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 7);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Positions_Update_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.StartPosition = 3;
			SUT.Document.Selection.EndPosition = 8;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Collapse_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 7);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.Collapse(true);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Selection_While_Unfocused_Applies_On_Focus()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			// Setting the selection while unfocused must not throw; it is picked up on focus.
			SUT.Document.Selection.SetRange(4, 9);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Expand_Word_Selects_Word_At_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(2, 2);
			var added = range.Expand(TextRangeUnit.Word);

			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
			Assert.AreEqual(6, added);
		}

		[TestMethod]
		public async Task When_Expand_Word_Spans_Multiple_Words()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(2, 8);
			range.Expand(TextRangeUnit.Word);

			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
		}

		[TestMethod]
		public async Task When_StartOf_Word_Moves_To_Word_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(8, 8);
			var moved = range.StartOf(TextRangeUnit.Word, false);

			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
			Assert.AreEqual(-2, moved);
		}

		[TestMethod]
		public async Task When_EndOf_Word_Moves_To_Word_End()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(8, 8);
			var moved = range.EndOf(TextRangeUnit.Word, false);

			Assert.AreEqual(12, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
			Assert.AreEqual(4, moved);
		}

		[TestMethod]
		public async Task When_Move_Word_Forward_Steps_Word_Starts()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Word, 2);

			Assert.AreEqual(12, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
			Assert.AreEqual(2, moved);
		}

		[TestMethod]
		public async Task When_Move_Word_Backward_Steps_Word_Starts()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(12, 12);
			var moved = range.Move(TextRangeUnit.Word, -1);

			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(-1, moved);
		}

		[TestMethod]
		public async Task When_GetIndex_Word_Returns_One_Based_Index()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			Assert.AreEqual(1, SUT.Document.GetRange(2, 2).GetIndex(TextRangeUnit.Word));
			Assert.AreEqual(2, SUT.Document.GetRange(8, 8).GetIndex(TextRangeUnit.Word));
			Assert.AreEqual(3, SUT.Document.GetRange(13, 13).GetIndex(TextRangeUnit.Word));
		}

		[TestMethod]
		public async Task When_Expand_Paragraph_Selects_Paragraph()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");

			var range = SUT.Document.GetRange(4, 4);
			range.Expand(TextRangeUnit.Paragraph);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Move_Paragraph_Forward_Steps_Paragraphs()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Paragraph, 1);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(1, moved);
		}

		[TestMethod]
		public async Task When_Programmatic_Expand_Word_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 2);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.Expand(TextRangeUnit.Word);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStartForTesting);
			Assert.AreEqual(6, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_StartOf_Line_Moves_To_Line_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.StartOf(TextRangeUnit.Line, false);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition);
		}

		[TestMethod]
		public async Task When_EndOf_Line_Moves_To_Line_End_Before_Paragraph_Mark()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.EndOf(TextRangeUnit.Line, false);

			// The visual line end stops before the trailing carriage return.
			Assert.AreEqual(5, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Expand_Line_Selects_Whole_Line()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.Expand(TextRangeUnit.Line);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
		}

		[TestMethod]
		public async Task When_GetIndex_Line_Returns_One_Based_Line_Number()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.Document.GetRange(0, 0).GetIndex(TextRangeUnit.Line));
			Assert.AreEqual(2, SUT.Document.GetRange(4, 4).GetIndex(TextRangeUnit.Line));
			Assert.AreEqual(3, SUT.Document.GetRange(7, 7).GetIndex(TextRangeUnit.Line));
		}

		[TestMethod]
		public async Task When_Selection_HomeKey_Line_Moves_Caret_To_Line_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(4, 4);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.HomeKey(TextRangeUnit.Line, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Selection_EndKey_Line_Moves_Caret_To_Line_End()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(3, 3);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.EndKey(TextRangeUnit.Line, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(5, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Selection_MoveDown_Moves_Caret_To_Next_Line()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// Caret at the start of the first line (column 0).
			SUT.Document.Selection.SetRange(0, 0);
			await WindowHelper.WaitForIdle();

			var moved = SUT.Document.Selection.MoveDown(TextRangeUnit.Line, 1, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, moved);
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
		}

		[TestMethod]
		public async Task When_Selection_MoveUp_Moves_Caret_To_Previous_Line()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// Caret at the start of the third line (column 0).
			SUT.Document.Selection.SetRange(6, 6);
			await WindowHelper.WaitForIdle();

			var moved = SUT.Document.Selection.MoveUp(TextRangeUnit.Line, 1, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, moved);
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
		}

		[TestMethod]
		public async Task When_UndoGroup_Coalesces_Multiple_Edits_Into_One_Undo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "start");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(5, 5);
			SUT.Document.Selection.TypeText("A");
			SUT.Document.Selection.TypeText("B");
			SUT.Document.EndUndoGroup();

			SUT.Document.GetText(TextGetOptions.None, out var afterGroup);
			Assert.AreEqual("startAB", afterGroup);

			// A single undo reverts the whole group (both A and B), not just the last edit.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			// The pre-group SetText remains a separate undo entry.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterSecondUndo);
			Assert.AreEqual("", afterSecondUndo);
		}

		[TestMethod]
		public async Task When_UndoGroup_Redo_Reapplies_Whole_Group()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "start");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(5, 5);
			SUT.Document.Selection.TypeText("A");
			SUT.Document.Selection.TypeText("B");
			SUT.Document.EndUndoGroup();

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			Assert.IsTrue(SUT.Document.CanRedo());
			SUT.Document.Redo();
			SUT.Document.GetText(TextGetOptions.None, out var afterRedo);
			Assert.AreEqual("startAB", afterRedo);
		}

		[TestMethod]
		public async Task When_Nested_UndoGroup_Coalesces_Into_One_Undo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "x");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Document.Selection.TypeText("1");
			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.TypeText("2");
			SUT.Document.EndUndoGroup();
			SUT.Document.Selection.TypeText("3");
			SUT.Document.EndUndoGroup();

			SUT.Document.GetText(TextGetOptions.None, out var afterGroup);
			Assert.AreEqual("x123", afterGroup);

			// The outermost group is the only undo boundary; one undo reverts 1, 2 and 3.
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("x", afterUndo);
		}

		[TestMethod]
		public async Task When_EndUndoGroup_Without_Begin_Is_NoOp()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "a");

			// Unbalanced EndUndoGroup must not throw or corrupt the undo state.
			SUT.Document.EndUndoGroup();

			SUT.Document.Selection.SetRange(1, 1);
			SUT.Document.Selection.TypeText("b");

			SUT.Document.GetText(TextGetOptions.None, out var typed);
			Assert.AreEqual("ab", typed);

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("a", afterUndo);
		}

		[TestMethod]
		public async Task When_BatchDisplayUpdates_Returns_Nesting_Count()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(1, SUT.Document.BatchDisplayUpdates());
			Assert.AreEqual(2, SUT.Document.BatchDisplayUpdates());
			Assert.AreEqual(1, SUT.Document.ApplyDisplayUpdates());
			Assert.AreEqual(0, SUT.Document.ApplyDisplayUpdates());

			// An extra unbalanced ApplyDisplayUpdates stays clamped at zero.
			Assert.AreEqual(0, SUT.Document.ApplyDisplayUpdates());
		}

		[TestMethod]
		public async Task When_Edits_Batched_Are_Applied_After_ApplyDisplayUpdates()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "base");

			SUT.Document.BatchDisplayUpdates();
			SUT.Document.Selection.SetRange(4, 4);
			SUT.Document.Selection.TypeText("!");
			SUT.Document.ApplyDisplayUpdates();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("base!", text);
		}
	}
}
