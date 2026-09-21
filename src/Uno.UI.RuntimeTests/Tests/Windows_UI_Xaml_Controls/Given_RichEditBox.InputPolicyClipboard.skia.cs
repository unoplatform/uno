#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_NativeClipboard_Returns_Text_And_Preserves_Cut_History(bool isCut)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			editor.Document.GetRange(1, 3).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.ClearUndoRedoHistory();
			var copied = 0;
			var cut = 0;
			editor.CopyingToClipboard += (_, _) => copied++;
			editor.CuttingToClipboard += (_, _) => cut++;

			var operation = editor.PrepareNativeClipboard(isCut);

			Assert.IsNotNull(operation);
			Assert.AreEqual("bc", operation.Text);
			GetTextWithoutFinalEop(editor.Document, out var beforeCommit);
			Assert.AreEqual("abcd", beforeCommit, "Preparing a cut must not delete before the clipboard write.");
			Assert.IsFalse(editor.Document.CanUndo(), "Preparing the payload must not record history.");
			Assert.IsTrue(editor.CommitNativeClipboard(operation));
			Assert.IsFalse(editor.CommitNativeClipboard(operation), "A gesture can commit only once.");
			Assert.AreEqual(isCut ? 0 : 1, copied);
			Assert.AreEqual(isCut ? 1 : 0, cut);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual(isCut ? "ad" : "abcd", text);
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.AreEqual(isCut ? 1 : 3, editor.Document.Selection.EndPosition);
			Assert.AreEqual(isCut, editor.Document.CanUndo());
			if (isCut)
			{
				editor.Document.Undo();
				GetTextWithoutFinalEop(editor.Document, out text);
				Assert.AreEqual("abcd", text);
				Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(1, 3).CharacterFormat.Bold);
				Assert.IsFalse(editor.Document.CanUndo());
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_NativeClipboard_Honors_Cancellation(bool isCut)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			editor.Document.Selection.SetRange(3, 1);
			var events = 0;
			editor.CopyingToClipboard += (_, args) => { events++; args.Handled = true; };
			editor.CuttingToClipboard += (_, args) => { events++; args.Handled = true; };

			Assert.IsNull(editor.PrepareNativeClipboard(isCut));

			Assert.AreEqual(1, events);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text);
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.AreEqual(3, editor.Document.Selection.EndPosition);
			Assert.IsTrue(editor.NativeSelectionIsBackward);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_NativeClipboard_Uses_Selection_After_Handler(bool isCut)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			void ChangeSelection()
			{
				editor.Document.GetRange(0, 1).Text = "A";
				editor.Document.Selection.SetRange(2, 4);
			}
			editor.CopyingToClipboard += (_, _) => ChangeSelection();
			editor.CuttingToClipboard += (_, _) => ChangeSelection();

			var operation = editor.PrepareNativeClipboard(isCut);
			Assert.IsNotNull(operation);
			Assert.AreEqual("cd", operation.Text);
			Assert.IsTrue(editor.CommitNativeClipboard(operation));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual(isCut ? "Ab" : "Abcd", text);
			Assert.AreEqual(2, editor.Document.Selection.StartPosition);
			Assert.AreEqual(isCut ? 2 : 4, editor.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_NativeClipboard_Stops_After_Focus_Transfer(bool isCut)
	{
		var editor = new RichEditBox { Width = 300 };
		var other = new Button { Content = "Other focus" };
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { editor, other } });
			editor.Document.SetText(TextSetOptions.None, "abcd");
			editor.Document.Selection.SetRange(1, 3);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			editor.CopyingToClipboard += (_, _) => Assert.IsTrue(other.Focus(FocusState.Programmatic));
			editor.CuttingToClipboard += (_, _) => Assert.IsTrue(other.Focus(FocusState.Programmatic));

			Assert.IsNull(editor.PrepareNativeClipboard(isCut));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false, "Disabled", null)]
	[DataRow(true, "Disabled", null)]
	[DataRow(false, "ReadOnly", "bc")]
	[DataRow(true, "ReadOnly", null)]
	[DataRow(false, "Protected", "bc")]
	[DataRow(true, "Protected", null)]
	[DataRow(false, "Empty", null)]
	[DataRow(true, "Empty", null)]
	[DataRow(false, "Unloaded", null)]
	[DataRow(true, "Unloaded", null)]
	public async Task When_InputPolicy_NativeClipboard_Rechecks_State_After_Handler(bool isCut, string change, string? expectedText)
	{
		var editor = new RichEditBox { Width = 300, AllowFocusWhenDisabled = true };
		try
		{
			await LoadNativeClipboardEditor(editor);
			void ChangeState()
			{
				switch (change)
				{
					case "Disabled":
						editor.IsEnabled = false;
						break;
					case "ReadOnly":
						editor.IsReadOnly = true;
						break;
					case "Protected":
						editor.Document.GetRange(1, 3).CharacterFormat.ProtectedText = FormatEffect.On;
						break;
					case "Empty":
						editor.Document.Selection.SetRange(2, 2);
						break;
					case "Unloaded":
						WindowHelper.WindowContent = null;
						break;
					default:
						throw new InvalidOperationException(change);
				}
			}
			editor.CopyingToClipboard += (_, _) => ChangeState();
			editor.CuttingToClipboard += (_, _) => ChangeState();

			var operation = editor.PrepareNativeClipboard(isCut);
			Assert.AreEqual(expectedText, operation?.Text);
			if (operation is not null)
			{
				Assert.IsTrue(editor.CommitNativeClipboard(operation));
			}

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_NativeClipboard_ReadOnly_Allows_Only_Copy(bool isCut)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			editor.IsReadOnly = true;
			await WindowHelper.WaitForIdle();
			var events = 0;
			editor.CopyingToClipboard += (_, _) => events++;
			editor.CuttingToClipboard += (_, _) => events++;

			var operation = editor.PrepareNativeClipboard(isCut);
			Assert.AreEqual(isCut ? null : "bc", operation?.Text);
			if (operation is not null)
			{
				Assert.IsTrue(editor.CommitNativeClipboard(operation));
			}
			Assert.AreEqual(isCut ? 0 : 1, events);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_NativeClipboard_Cut_Preserves_Reentrant_App_Selection()
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			editor.TextChanging += (_, args) =>
			{
				if (args.IsContentChanging)
				{
					editor.Document.Selection.SetRange(2, 0);
				}
			};

			var operation = editor.PrepareNativeClipboard(isCut: true);
			Assert.IsNotNull(operation);
			Assert.AreEqual("bc", operation.Text);
			Assert.IsTrue(editor.CommitNativeClipboard(operation));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("ad", text);
			Assert.AreEqual(0, editor.Document.Selection.StartPosition);
			Assert.AreEqual(2, editor.Document.Selection.EndPosition);
			Assert.IsTrue(editor.NativeSelectionIsBackward);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(RichEditClipboardFormat.AllFormats)]
	[DataRow(RichEditClipboardFormat.PlainText)]
	public async Task When_InputPolicy_NativeClipboard_Prepare_Preserves_Supported_Formats(RichEditClipboardFormat copyFormat)
	{
		var editor = new RichEditBox { Width = 300, ClipboardCopyFormat = copyFormat };
		try
		{
			await LoadNativeClipboardEditor(editor);
			editor.Document.GetRange(1, 3).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.ClearUndoRedoHistory();

			var operation = editor.PrepareNativeClipboard(isCut: true);

			Assert.IsNotNull(operation);
			Assert.AreEqual("bc", operation.Text);
			if (copyFormat == RichEditClipboardFormat.AllFormats)
			{
				Assert.IsNotNull(operation.Rtf);
				var pasted = new RichEditBox();
				pasted.Document.SetText(TextSetOptions.FormatRtf, operation.Rtf);
				pasted.Document.GetRange(0, 2).GetText(TextGetOptions.None, out var pastedText);
				Assert.AreEqual("bc", pastedText);
				Assert.AreEqual(FormatEffect.On, pasted.Document.GetRange(0, 2).CharacterFormat.Bold);
			}
			else
			{
				Assert.IsNull(operation.Rtf);
			}

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text, "Discarding an operation after absent/failed clipboard writes must leave the source intact.");
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.AreEqual(3, editor.Document.Selection.EndPosition);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("Text")]
	[DataRow("TextRestored")]
	[DataRow("Selection")]
	[DataRow("SelectionRestored")]
	[DataRow("SelectionDirection")]
	[DataRow("CharacterFormat")]
	[DataRow("ParagraphFormat")]
	[DataRow("CopyFormat")]
	[DataRow("Protected")]
	[DataRow("ReadOnly")]
	[DataRow("Disabled")]
	[DataRow("Focus")]
	[DataRow("Unloaded")]
	public async Task When_InputPolicy_NativeClipboard_Rejects_Changes_Before_Commit(string change)
	{
		var editor = new RichEditBox { Width = 300, AllowFocusWhenDisabled = true };
		var other = new Button { Content = "Other focus" };
		try
		{
			await LoadNativeClipboardEditor(editor, new StackPanel { Children = { editor, other } });
			var operation = editor.PrepareNativeClipboard(isCut: true);
			Assert.IsNotNull(operation);

			switch (change)
			{
				case "Text":
					editor.Document.GetRange(0, 1).Text = "A";
					break;
				case "TextRestored":
					editor.Document.GetRange(0, 1).Text = "A";
					editor.Document.GetRange(0, 1).Text = "a";
					break;
				case "Selection":
					editor.Document.Selection.SetRange(0, 1);
					break;
				case "SelectionRestored":
					editor.Document.Selection.SetRange(0, 1);
					editor.Document.Selection.SetRange(1, 3);
					break;
				case "SelectionDirection":
					editor.Document.Selection.Options |= SelectionOptions.StartActive;
					break;
				case "CharacterFormat":
					editor.Document.GetRange(1, 3).CharacterFormat.Bold = FormatEffect.On;
					break;
				case "ParagraphFormat":
					editor.Document.GetRange(1, 3).ParagraphFormat.SpaceBefore = 12;
					break;
				case "CopyFormat":
					editor.ClipboardCopyFormat = RichEditClipboardFormat.PlainText;
					break;
				case "Protected":
					editor.Document.GetRange(1, 3).CharacterFormat.ProtectedText = FormatEffect.On;
					break;
				case "ReadOnly":
					editor.IsReadOnly = true;
					break;
				case "Disabled":
					editor.IsEnabled = false;
					break;
				case "Focus":
					Assert.IsTrue(other.Focus(FocusState.Programmatic));
					break;
				case "Unloaded":
					WindowHelper.WindowContent = null;
					break;
				default:
					throw new InvalidOperationException(change);
			}
			GetTextWithoutFinalEop(editor.Document, out var beforeCommit);

			Assert.IsFalse(editor.CommitNativeClipboard(operation));
			Assert.IsFalse(editor.CommitNativeClipboard(operation));

			GetTextWithoutFinalEop(editor.Document, out var afterCommit);
			Assert.AreEqual(beforeCommit, afterCommit, "A stale clipboard gesture must not delete current document content.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_NativeClipboard_Rejects_Foreign_And_Superseded_Operations()
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			var first = editor.PrepareNativeClipboard(isCut: true);
			Assert.IsNotNull(first);
			Assert.IsFalse(new RichEditBox().CommitNativeClipboard(first));
			var latest = editor.PrepareNativeClipboard(isCut: true);
			Assert.IsNotNull(latest);

			Assert.IsFalse(editor.CommitNativeClipboard(first));
			Assert.IsTrue(editor.CommitNativeClipboard(latest));
			Assert.IsFalse(editor.CommitNativeClipboard(latest));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("ad", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_NativeClipboard_Nested_Prepare_Supersedes_Outer_Cut()
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await LoadNativeClipboardEditor(editor);
			RichEditBox.NativeClipboardOperation? nested = null;
			editor.CuttingToClipboard += (_, _) => nested = editor.PrepareNativeClipboard(isCut: false);

			var outer = editor.PrepareNativeClipboard(isCut: true);

			Assert.IsNull(outer);
			Assert.IsNotNull(nested);
			Assert.AreEqual("bc", nested.Text);
			Assert.IsTrue(editor.CommitNativeClipboard(nested));
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abcd", text);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task LoadNativeClipboardEditor(RichEditBox editor, FrameworkElement? root = null)
	{
		await UITestHelper.Load(root ?? editor);
		editor.Document.SetText(TextSetOptions.None, "abcd");
		editor.Document.Selection.SetRange(1, 3);
		Assert.IsTrue(editor.Focus(FocusState.Programmatic));
		await WindowHelper.WaitForIdle();
		editor.Document.ClearUndoRedoHistory();
	}
}
