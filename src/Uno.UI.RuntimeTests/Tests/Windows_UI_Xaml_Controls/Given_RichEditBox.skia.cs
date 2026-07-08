using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.UI.Xaml.Controls.Extensions;
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

		[TestMethod]
		public async Task When_ParagraphFormat_Alignment_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			// A fresh paragraph resolves to Left.
			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			format.Alignment = ParagraphAlignment.Center;

			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Applies_To_Whole_Paragraph()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Setting alignment through a caret inside the second paragraph applies to the whole
			// paragraph, and leaves the first paragraph untouched.
			var second = SUT.Document.GetRange(5, 5).ParagraphFormat;
			second.Alignment = ParagraphAlignment.Right;

			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 0).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetRange(4, 7).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Mixed_Reports_Undefined()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			var first = SUT.Document.GetRange(0, 3).ParagraphFormat;
			first.Alignment = ParagraphAlignment.Center;
			var second = SUT.Document.GetRange(4, 7).ParagraphFormat;
			second.Alignment = ParagraphAlignment.Right;

			// A range spanning both paragraphs disagrees, so the shared value is Undefined.
			Assert.AreEqual(ParagraphAlignment.Undefined, SUT.Document.GetRange(0, 7).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_SetIndents_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.SetIndents(10f, 20f, 30f);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			Assert.AreEqual(10f, format.FirstLineIndent);
			Assert.AreEqual(20f, format.LeftIndent);
			Assert.AreEqual(30f, format.RightIndent);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_SetLineSpacing_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.SetLineSpacing(LineSpacingRule.Exactly, 24f);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			Assert.AreEqual(LineSpacingRule.Exactly, format.LineSpacingRule);
			Assert.AreEqual(24f, format.LineSpacing);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_ListType_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one\rtwo");

			var first = SUT.Document.GetRange(0, 0).ParagraphFormat;
			first.ListType = MarkerType.Bullet;

			Assert.AreEqual(MarkerType.Bullet, SUT.Document.GetRange(0, 3).ParagraphFormat.ListType);
			// The other paragraph keeps its (undefined) list type.
			Assert.AreEqual(MarkerType.Undefined, SUT.Document.GetRange(4, 7).ParagraphFormat.ListType);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_GetClone_IsEqual()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			format.Alignment = ParagraphAlignment.Center;

			var clone = format.GetClone();
			Assert.IsTrue(format.IsEqual(clone));

			clone.Alignment = ParagraphAlignment.Right;
			Assert.IsFalse(format.IsEqual(clone));
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Undo_Reverts_Alignment()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);

			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();

			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("hello", text);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Survives_Text_Edit()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			SUT.Document.GetRange(4, 7).ParagraphFormat.Alignment = ParagraphAlignment.Center;

			// Typing into the formatted paragraph splices the paragraph run in lock-step, so the
			// alignment is preserved.
			SUT.Document.Selection.SetRange(7, 7);
			SUT.Document.Selection.TypeText("X");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("aaa\rbbbX", text);
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_GetRect_Returns_Caret_Geometry()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(3, 3).GetRect(PointOptions.None, out var rect, out var hit);

			Assert.AreEqual(0, hit, "hit is reported as 0 (documented approximation).");
			Assert.IsTrue(rect.Height > 0, $"Caret rect should have a positive height, was {rect.Height}.");
			Assert.IsTrue(rect.Width <= 1, $"A degenerate (caret) range should have a ~zero width, was {rect.Width}.");
			Assert.IsTrue(rect.X > 0, $"The caret at index 3 should be offset from the left edge, was {rect.X}.");
		}

		[TestMethod]
		public async Task When_GetRect_Range_Has_Positive_Width()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(0, 5).GetRect(PointOptions.None, out var rangeRect, out _);
			SUT.Document.GetRange(0, 0).GetRect(PointOptions.None, out var caretRect, out _);

			Assert.IsTrue(rangeRect.Width > caretRect.Width, $"A non-degenerate range should be wider than a caret, was {rangeRect.Width} vs {caretRect.Width}.");
			Assert.IsTrue(rangeRect.Height > 0, $"Range rect should have a positive height, was {rangeRect.Height}.");
		}

		[TestMethod]
		public async Task When_GetRect_ClientCoordinates_Preserves_Size()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(2, 5);
			range.GetRect(PointOptions.None, out var rootRect, out _);
			range.GetRect(PointOptions.ClientCoordinates, out var clientRect, out _);

			// Both transforms are translation-only, so the size is invariant across coordinate spaces.
			Assert.AreEqual(rootRect.Width, clientRect.Width, 0.5, "Width should be coordinate-space invariant.");
			Assert.AreEqual(rootRect.Height, clientRect.Height, 0.5, "Height should be coordinate-space invariant.");
		}

		[TestMethod]
		public async Task When_GetPoint_Vertical_Alignment_Orders_Points()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var topPoint);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Bottom, PointOptions.None, out var bottomPoint);

			Assert.AreEqual(topPoint.X, bottomPoint.X, 0.5, "Left alignment should give the same X for both vertical anchors.");
			Assert.IsTrue(bottomPoint.Y > topPoint.Y, $"Bottom anchor should be below the top anchor, was {bottomPoint.Y} vs {topPoint.Y}.");
		}

		[TestMethod]
		public async Task When_GetPoint_GetRangeFromPoint_RoundTrips()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(6, 6).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var point);
			var recovered = SUT.Document.GetRangeFromPoint(point, PointOptions.None);

			Assert.IsTrue(Math.Abs(recovered.StartPosition - 6) <= 1, $"GetRangeFromPoint should recover the origin index, was {recovered.StartPosition}.");
		}

		[TestMethod]
		public async Task When_SetPoint_Moves_Caret()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(7, 7).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var point);

			var range = SUT.Document.GetRange(0, 0);
			range.SetPoint(point, PointOptions.None, extend: false);

			Assert.IsTrue(Math.Abs(range.StartPosition - 7) <= 1, $"SetPoint should place the caret at the hit index, was {range.StartPosition}.");
			Assert.AreEqual(range.StartPosition, range.EndPosition, "A non-extending SetPoint should collapse the range.");
		}

		[TestMethod]
		public async Task When_ScrollIntoView_Does_Not_Throw()
		{
			var SUT = new RichEditBox { Width = 200, Height = 60 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line one\rLine two\rLine three\rLine four\rLine five");
			await WindowHelper.WaitForIdle();

			// Should not throw regardless of whether the range is on-screen.
			SUT.Document.GetRange(40, 44).ScrollIntoView(PointOptions.None);
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var def = SUT.Document.GetDefaultCharacterFormat();
			def.Bold = FormatEffect.On;
			def.Size = 24;

			var reread = SUT.Document.GetDefaultCharacterFormat();
			Assert.AreEqual(FormatEffect.On, reread.Bold);
			Assert.AreEqual(24f, reread.Size);
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Applies_To_New_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultCharacterFormat().Bold = FormatEffect.On;
			SUT.Document.SetText(TextSetOptions.None, "abc");

			// SetText content is created against the (now bold) document default.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Applies_To_Typed_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultCharacterFormat().Italic = FormatEffect.On;
			SUT.Document.Selection.TypeText("hi");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("hi", text);
			// Text typed into the empty document inherits the document default formatting.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 2).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_SetDefaultCharacterFormat_From_Value()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var value = SUT.Document.GetDefaultCharacterFormat().GetClone();
			value.Bold = FormatEffect.On;
			SUT.Document.SetDefaultCharacterFormat(value);

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetDefaultCharacterFormat().Bold);
		}

		[TestMethod]
		public async Task When_DefaultParagraphFormat_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Right;

			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetDefaultParagraphFormat().Alignment);
		}

		[TestMethod]
		public async Task When_DefaultParagraphFormat_Applies_To_New_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Center;
			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Both paragraphs are created against the (centered) document default.
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(1, 1).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_TextChanged_Raised_On_SetText()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var count = 0;
			object sender = null;
			RoutedEventArgs args = null;
			SUT.TextChanged += (s, e) =>
			{
				count++;
				sender = s;
				args = e;
			};

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			Assert.AreSame(SUT, sender);
			Assert.IsNotNull(args);
		}

		[TestMethod]
		public async Task When_TextChanged_Not_Raised_On_Same_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanged += (s, e) => count++;

			// Re-setting the identical text must not raise (choke-point de-dupes against last value).
			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_TextChanged_Not_Raised_On_Format_Only_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanged += (s, e) => count++;

			// A character-format change mutates the run model and re-renders, but the plain text is
			// unchanged, so TextChanged must not fire.
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_SelectionChanged_Raised_On_Programmatic_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var count = 0;
			object sender = null;
			SUT.SelectionChanged += (s, e) =>
			{
				count++;
				sender = s;
			};

			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(count >= 1, $"SelectionChanged should raise on a programmatic selection change, count was {count}.");
			Assert.AreSame(SUT, sender);
		}

		[TestMethod]
		public async Task When_SelectionChanged_Not_Raised_On_Same_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.SelectionChanged += (s, e) => count++;

			// Re-applying the identical range must not raise.
			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_DefaultTabStop_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(36f, SUT.Document.DefaultTabStop);

			SUT.Document.DefaultTabStop = 48f;
			Assert.AreEqual(48f, SUT.Document.DefaultTabStop);
		}

		[TestMethod]
		public async Task When_CaretType_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(CaretType.Normal, SUT.Document.CaretType);

			SUT.Document.CaretType = CaretType.Null;
			Assert.AreEqual(CaretType.Null, SUT.Document.CaretType);
		}

		[TestMethod]
		public async Task When_Trailing_Whitespace_And_Spacing_Knobs_RoundTrip()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.IsFalse(SUT.Document.AlignmentIncludesTrailingWhitespace);
			Assert.IsFalse(SUT.Document.IgnoreTrailingCharacterSpacing);

			SUT.Document.AlignmentIncludesTrailingWhitespace = true;
			SUT.Document.IgnoreTrailingCharacterSpacing = true;

			Assert.IsTrue(SUT.Document.AlignmentIncludesTrailingWhitespace);
			Assert.IsTrue(SUT.Document.IgnoreTrailingCharacterSpacing);
		}

		[TestMethod]
		public async Task When_CanCopy_Reflects_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");

			SUT.Document.Selection.SetRange(0, 0);
			Assert.IsFalse(SUT.Document.CanCopy());

			SUT.Document.Selection.SetRange(0, 5);
			Assert.IsTrue(SUT.Document.CanCopy());
		}

		[TestMethod]
		public async Task When_CanPaste_True_With_Text_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var dp = new DataPackage();
			dp.SetText("clip");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.CanPaste());
		}

		[TestMethod]
		public async Task When_ClearUndoRedoHistory_Clears_Stacks()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one");
			SUT.Document.SetText(TextSetOptions.None, "two");
			Assert.IsTrue(SUT.Document.CanUndo());

			SUT.Document.Undo();
			Assert.IsTrue(SUT.Document.CanRedo());

			SUT.Document.ClearUndoRedoHistory();
			Assert.IsFalse(SUT.Document.CanUndo());
			Assert.IsFalse(SUT.Document.CanRedo());
		}

		[TestMethod]
		public async Task When_TextChanging_Raised_Before_TextChanged()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var order = new System.Collections.Generic.List<string>();
			var contentChanging = false;
			SUT.TextChanging += (s, e) =>
			{
				order.Add("changing");
				contentChanging = e.IsContentChanging;
			};
			SUT.TextChanged += (s, e) => order.Add("changed");

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "changing", "changed" }, order);
			Assert.IsTrue(contentChanging);
		}

		[TestMethod]
		public async Task When_TextChanging_Not_Raised_On_Format_Only_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanging += (s, e) => count++;

			// A format-only change re-renders but leaves the plain text unchanged, so TextChanging
			// (paired with TextChanged) must not fire.
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_SelectionChanging_Raised_On_Interactive_Move()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			var count = 0;
			var proposedStart = -1;
			var proposedLength = -1;
			SUT.SelectionChanging += (s, e) =>
			{
				count++;
				proposedStart = e.SelectionStart;
				proposedLength = e.SelectionLength;
			};

			// Caret is at 3; moving left proposes a collapsed selection at 2.
			RaiseKey(SUT, VirtualKey.Left);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(count >= 1, $"SelectionChanging should raise on an interactive move, count was {count}.");
			Assert.AreEqual(2, proposedStart);
			Assert.AreEqual(0, proposedLength);
		}

		[TestMethod]
		public async Task When_SelectionChanging_Cancel_Prevents_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);

			var raised = false;
			SUT.SelectionChanging += (s, e) =>
			{
				raised = true;
				e.Cancel = true;
			};

			// Home would move the caret to 0, but the cancelling handler must prevent it.
			RaiseKey(SUT, VirtualKey.Home);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(raised, "SelectionChanging should have been raised for the Home key.");
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition, "The cancelled selection change must not move the caret.");
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_CopyingToClipboard_Raised()
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

			var count = 0;
			SUT.CopyingToClipboard += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public async Task When_CopyingToClipboard_Handled_Suppresses_Copy()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Seed the clipboard with a sentinel; a suppressed copy must leave it untouched.
			var seed = new DataPackage();
			seed.SetText("SENTINEL");
			Clipboard.SetContent(seed);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			SUT.CopyingToClipboard += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var text = await content.GetTextAsync();
			Assert.AreEqual("SENTINEL", text);
		}

		[TestMethod]
		public async Task When_CuttingToClipboard_Raised_And_Cuts()
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

			var count = 0;
			SUT.CuttingToClipboard += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.X, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		[TestMethod]
		public async Task When_CuttingToClipboard_Handled_Suppresses_Cut()
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

			SUT.CuttingToClipboard += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.X, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			// A suppressed cut must leave the document content intact.
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_Paste_Raised()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.Paste += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public async Task When_Paste_Handled_Suppresses_Paste()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XYZ");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			SUT.Paste += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			// A suppressed paste must not replace the selected text.
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Center_Sets_Override()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			// A fresh (Left) document does not override the control-level TextAlignment.
			Assert.IsNull(SUT.ParagraphAlignmentOverride);

			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Center, SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Right_Sets_Override()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Right, SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Mixed_Leaves_Override_Null()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Two paragraphs with different alignments cannot be rendered on a single TextBlock, so no
			// uniform override is projected.
			SUT.Document.GetRange(0, 0).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Cleared_Restores_Null()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(TextAlignment.Center, SUT.ParagraphAlignmentOverride);

			// Returning to the (default) Left alignment relinquishes the override back to the control DP.
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Left;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Projects_To_DisplayBlock()
		{
			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hi");
			await WindowHelper.WaitForIdle();

			var block = FindDisplayBlock(SUT);
			Assert.IsNotNull(block, "The DisplayBlock hosting the text should be in the visual tree.");

			// Baseline: no paragraph-alignment override is active, so IsTextAlignmentSetToDefault is true.
			// While it is true, the shared TextBlock renders with its own default alignment regardless of
			// the block's TextAlignment property value (see GetAdjustedTextAlignment), which mirrors the
			// control-level TextAlignment DP. Capture that default rather than hard-coding it: the DP's
			// metadata default is default(TextAlignment) == Center, which is a framework-wide constant, not
			// something this feature controls.
			Assert.IsTrue(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);
			var controlDefaultAlignment = ((ITextBoxViewHost)SUT).TextAlignment;
			Assert.AreEqual(controlDefaultAlignment, block.TextAlignment);

			// Applying a uniform Right paragraph alignment projects Right onto the DisplayBlock (the exact
			// value the shared TextBlock renders with) and disables the deferral to the default so it takes
			// effect. Right is deliberately distinct from the control DP default so the projection is
			// unambiguous. This is the identical render mechanism TextBox uses for its own TextAlignment.
			SUT.Document.GetRange(0, 2).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Right, SUT.ParagraphAlignmentOverride);
			Assert.AreEqual(TextAlignment.Right, block.TextAlignment);
			Assert.IsFalse(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);

			// Relinquishing the paragraph alignment (the model default is Left) clears the override and
			// hands the block back to the control-level DP default.
			SUT.Document.GetRange(0, 2).ParagraphFormat.Alignment = ParagraphAlignment.Left;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
			Assert.IsTrue(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);
			Assert.AreEqual(controlDefaultAlignment, block.TextAlignment);
		}

		[TestMethod]
		public async Task When_IME_Composition_Inserts_And_Commits()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var composing);
			Assert.AreEqual("ni", composing);

			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var committed);
			Assert.AreEqual("nihao", committed);
		}

		[TestMethod]
		public async Task When_IME_ReadOnly_Ignores_Composition()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox { IsReadOnly = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "Original");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Original", text);
		}

		[TestMethod]
		public async Task When_IME_Composition_Is_Single_Undo_Entry()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("n");
			fake.SimulateCompositionUpdate("ni");
			fake.SimulateCompositionUpdate("nihao");
			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var afterCommit);
			Assert.AreEqual("nihao", afterCommit);

			// The whole composition collapses into ONE undo entry (matching WinUI).
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("", afterUndo);
		}

		[TestMethod]
		public async Task When_IME_Composition_Cancel_Keeps_Text()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(SUT.IsComposing);

			fake.SimulateCompositionCancel();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ni", text);
		}

		[TestMethod]
		public async Task When_IME_Composition_Events_Raised()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var started = 0;
			var changed = 0;
			var ended = 0;
			SUT.TextCompositionStarted += (s, e) => started++;
			SUT.TextCompositionChanged += (s, e) => changed++;
			SUT.TextCompositionEnded += (s, e) => ended++;

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, started);
			Assert.IsTrue(changed >= 1);
			Assert.AreEqual(1, ended);
		}

		[TestMethod]
		public async Task When_IME_Composing_Swallows_Char_Key_Guard()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.ShouldSwallowKeyDuringComposition);

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ShouldSwallowKeyDuringComposition);

			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.ShouldSwallowKeyDuringComposition);
		}

		[TestMethod]
		public async Task When_IME_Composition_Preserves_Existing_Formatting()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			SUT.Document.Selection.SetRange(2, 2);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("xy");
			fake.SimulateCompositionComplete("xy");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ABxy", text);

			// The pre-existing bold run on "A" survives the composition edit.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_IME_External_Change_Cancels_Composition()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(SUT.IsComposing);

			fake.EndImeSessionCalled = false;
			SUT.Document.SetText(TextSetOptions.None, "Replaced");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Replaced", text);
			Assert.IsTrue(fake.EndImeSessionCalled, "EndImeSession should be called when composition is cancelled by external text change");
		}

		private class FakeImeTextBoxExtension : IImeTextBoxExtension
		{
			public bool IsComposing { get; private set; }
			public bool EndImeSessionCalled { get; set; }

			public event EventHandler CompositionStarted;
			public event EventHandler<ImeCompositionEventArgs> CompositionUpdated;
			public event EventHandler<ImeCompositionEventArgs> CompositionCompleted;
			public event EventHandler CompositionEnded;

			public void StartImeSession(IImeSessionHost host) { }

			public void EndImeSession()
			{
				EndImeSessionCalled = true;
				if (IsComposing)
				{
					IsComposing = false;
					CompositionEnded?.Invoke(this, EventArgs.Empty);
				}
			}

			public void SimulateCompositionStart()
			{
				IsComposing = true;
				CompositionStarted?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCompositionUpdate(string text, int cursorPosition = -1)
			{
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, cursorPosition));
			}

			public void SimulateCompositionComplete(string text)
			{
				IsComposing = false;
				CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCompositionCancel()
			{
				IsComposing = false;
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}
		}

		[TestMethod]
		public async Task When_CtrlB_Toggles_Bold_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(1, 4);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);
			// The unselected characters stay unformatted.
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);

			// A second toggle over the same (fully bold) selection turns it back off.
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CtrlI_Toggles_Italic_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 3);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Italic);

			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 3).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_CtrlU_Toggles_Underline_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.U, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(UnderlineType.Single, SUT.Document.GetRange(0, 5).CharacterFormat.Underline);

			RaiseKey(SUT, VirtualKey.U, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(UnderlineType.None, SUT.Document.GetRange(0, 5).CharacterFormat.Underline);
		}

		[TestMethod]
		public async Task When_CtrlB_On_Mixed_Selection_Makes_All_Bold()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");

			// Make only the first two characters bold, leaving the rest unformatted (a mixed range).
			SUT.Document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.Undefined, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			// A mixed selection toggles to fully-on (matching "make it all bold").
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_DisabledFormattingAccelerators_Suppresses_Bold_Only()
		{
			var SUT = new RichEditBox { DisabledFormattingAccelerators = DisabledFormattingAccelerators.Bold };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			// Bold is disabled, so Ctrl+B does nothing.
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			// Italic is still enabled and applies normally.
			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_CtrlB_Is_Single_Undo_Entry()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			// A single undo reverts the whole formatting toggle while keeping the text.
			RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abcde", text);
		}

		[TestMethod]
		public async Task When_ReadOnly_Suppresses_Formatting_Accelerator()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			SUT.IsReadOnly = true;
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_Range_Copy_Puts_Text_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(0, 5).Copy();
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var clipboardText = await content.GetTextAsync();
			Assert.AreEqual("Hello", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Copy_Empty_Is_NoOp()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var seed = new DataPackage();
			seed.SetText("SEED");
			Clipboard.SetContent(seed);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// A degenerate (caret) range has nothing to copy, so the clipboard is untouched.
			SUT.Document.GetRange(3, 3).Copy();
			await WindowHelper.WaitForIdle();

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("SEED", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Cut_Removes_And_Copies()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(0, 6);
			range.Cut();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(0, range.EndPosition, "Cut should collapse the range to its start.");

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Paste_Replaces_Range()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XY");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(1, 1);
			range.Paste(0);

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "AXYB";
			});

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition, "Paste should collapse the range past the inserted text.");
		}

		[TestMethod]
		public async Task When_Range_CanPaste_Reflects_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("paste me");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.GetRange(0, 0).CanPaste(0));
		}

		[TestMethod]
		public async Task When_Range_GetCharacterUtf32_Returns_CodePoint()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(1, 1);
			range.GetCharacterUtf32(out var codePoint, 0);
			Assert.AreEqual((uint)'e', codePoint);

			// Reading past the end of the story yields 0.
			range.GetCharacterUtf32(out var oob, 100);
			Assert.AreEqual(0u, oob);
		}

		[TestMethod]
		public async Task When_Range_MatchSelection_Aligns_To_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(2, 4);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(8, 10);
			range.MatchSelection();

			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(4, range.EndPosition);
		}

		private static TextBlock FindDisplayBlock(DependencyObject root)
		{
			var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
				if (child is TextBlock { Text: "hi" } tb)
				{
					return tb;
				}

				var found = FindDisplayBlock(child);
				if (found is not null)
				{
					return found;
				}
			}

			return null;
		}
	}
}
