#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[DataRow("typing", 1)]
		[DataRow("backspace", 3)]
		[DataRow("delete", 3)]
		[DataRow("format", 2)]
		public void When_Tom_Edits_Have_Native_Undo_Boundaries(string operation, int expectedUndoCount)
		{
			var document = new RichEditBox().Document;
			switch (operation)
			{
				case "typing":
					document.ClearUndoRedoHistory();
					document.Selection.TypeText("a");
					document.Selection.TypeText("b");
					document.Selection.TypeText("c");
					break;
				case "backspace":
					document.SetText(TextSetOptions.None, "abcd");
					document.ClearUndoRedoHistory();
					var backspace = document.GetRange(4, 4);
					backspace.Delete(TextRangeUnit.Character, -1);
					backspace.Delete(TextRangeUnit.Character, -1);
					backspace.Delete(TextRangeUnit.Character, -1);
					break;
				case "delete":
					document.SetText(TextSetOptions.None, "abcd");
					document.ClearUndoRedoHistory();
					var delete = document.GetRange(0, 0);
					delete.Delete(TextRangeUnit.Character, 1);
					delete.Delete(TextRangeUnit.Character, 1);
					delete.Delete(TextRangeUnit.Character, 1);
					break;
				case "format":
					document.SetText(TextSetOptions.None, "abcd");
					document.ClearUndoRedoHistory();
					document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
					document.GetRange(2, 4).CharacterFormat.Italic = FormatEffect.On;
					break;
				default:
					Assert.Fail($"Unknown operation {operation}.");
					break;
			}

			var undoCount = 0;
			while (document.CanUndo())
			{
				document.Undo();
				undoCount++;
			}

			Assert.AreEqual(expectedUndoCount, undoCount);
		}

		[TestMethod]
		public void When_TypeText_Undo_Restores_Replaced_Selection()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "abcdef");
			document.ClearUndoRedoHistory();
			document.Selection.SetRange(2, 5);

			document.Selection.TypeText("X");
			document.Undo();

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("abcdef", text);
			Assert.AreEqual(2, document.Selection.StartPosition);
			Assert.AreEqual(5, document.Selection.EndPosition);
		}

		[TestMethod]
		public void When_Nested_Undo_Group_Closes_On_First_End()
		{
			var document = new RichEditBox().Document;
			document.BeginUndoGroup();
			document.Selection.TypeText("a");
			document.BeginUndoGroup();
			document.Selection.TypeText("b");
			document.EndUndoGroup();
			document.Selection.TypeText("c");
			document.EndUndoGroup();

			document.Undo();
			GetTextWithoutFinalEop(document, out var firstUndo);
			Assert.AreEqual("ab", firstUndo);

			document.Undo();
			GetTextWithoutFinalEop(document, out var secondUndo);
			Assert.AreEqual(string.Empty, secondUndo);
		}

		[TestMethod]
		public void When_Clear_History_Terminates_Open_Group()
		{
			var document = new RichEditBox().Document;
			document.BeginUndoGroup();
			document.Selection.TypeText("a");
			document.ClearUndoRedoHistory();
			document.Selection.TypeText("b");
			document.EndUndoGroup();

			document.Undo();

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("a", text);
			Assert.IsFalse(document.CanUndo());
		}

		[TestMethod]
		[DataRow("selection")]
		[DataRow("selection-options")]
		[DataRow("default-character-format")]
		[DataRow("default-paragraph-format")]
		public void When_NonDocument_State_Does_Not_Create_Undo(string operation)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "abcd");
			document.ClearUndoRedoHistory();

			switch (operation)
			{
				case "selection":
					document.Selection.SetRange(1, 3);
					break;
				case "selection-options":
					document.Selection.Options = SelectionOptions.Overtype | SelectionOptions.StartActive;
					break;
				case "default-character-format":
					document.GetDefaultCharacterFormat().Bold = FormatEffect.On;
					break;
				case "default-paragraph-format":
					document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Right;
					break;
			}

			Assert.IsFalse(document.CanUndo());
		}

		[TestMethod]
		public void When_NoOp_SetText_Still_Creates_An_Undo_Boundary()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "same");
			document.ClearUndoRedoHistory();
			document.Selection.SetRange(1, 3);

			document.SetText(TextSetOptions.None, "same");

			Assert.IsTrue(document.CanUndo());
			Assert.AreEqual(0, document.Selection.StartPosition);
			Assert.AreEqual(0, document.Selection.EndPosition);
			document.Undo();
			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("same", text);
			Assert.AreEqual(1, document.Selection.StartPosition);
			Assert.AreEqual(3, document.Selection.EndPosition);
			Assert.IsFalse(document.CanUndo());
			Assert.IsTrue(document.CanRedo());
		}

		[TestMethod]
		public void When_UndoLimit_Default_Reports_Zero_But_History_Is_Enabled()
		{
			var document = new RichEditBox().Document;
			Assert.AreEqual(0u, document.UndoLimit);

			document.SetText(TextSetOptions.None, "a");
			Assert.IsTrue(document.CanUndo());

			document.UndoLimit = 0;
			Assert.IsFalse(document.CanUndo());
			Assert.IsFalse(document.CanRedo());
			document.SetText(TextSetOptions.None, "b");
			Assert.IsFalse(document.CanUndo());

			document.UndoLimit = 2;
			document.SetText(TextSetOptions.None, "c");
			Assert.IsTrue(document.CanUndo());
		}

		[TestMethod]
		public void When_Grouped_Edit_Invalidates_Redo_Immediately()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "a");
			document.SetText(TextSetOptions.None, "b");
			document.Undo();
			Assert.IsTrue(document.CanRedo());

			document.BeginUndoGroup();
			document.Selection.SetRange(1, 1);
			document.Selection.TypeText("x");

			Assert.IsFalse(document.CanRedo());
			document.EndUndoGroup();
		}

		[TestMethod]
		public async Task When_Undo_Raises_One_Text_And_Selection_Notification()
		{
			var box = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = box;
				await WindowHelper.WaitForLoaded(box);
				box.Document.SetText(TextSetOptions.None, "abcdef");
				box.Document.ClearUndoRedoHistory();
				box.Document.Selection.SetRange(2, 5);
				await WindowHelper.WaitForIdle();

				var textChanging = 0;
				var textChanged = 0;
				var selectionChanging = 0;
				var selectionChanged = 0;
				box.TextChanging += (_, _) => textChanging++;
				box.TextChanged += (_, _) => textChanged++;
				box.SelectionChanging += (_, _) => selectionChanging++;
				box.SelectionChanged += (_, _) => selectionChanged++;

				box.Document.Selection.TypeText("X");
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, textChanging);
				Assert.AreEqual(1, textChanged);
				Assert.AreEqual(1, selectionChanging);
				Assert.AreEqual(1, selectionChanged);

				textChanging = textChanged = selectionChanging = selectionChanged = 0;
				box.Document.Undo();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, textChanging);
				Assert.AreEqual(1, textChanged);
				Assert.AreEqual(1, selectionChanging);
				Assert.AreEqual(1, selectionChanged);
				Assert.AreEqual(2, box.Document.Selection.StartPosition);
				Assert.AreEqual(5, box.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
