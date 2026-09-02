#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[DataRow("backspace", "a")]
		[DataRow("delete", "d")]
		public async Task When_Sequential_Keyboard_Deletes_Coalesce(string operation, string expectedAfterDelete)
		{
			var box = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = box;
				await WindowHelper.WaitForLoaded(box);
				box.Document.SetText(TextSetOptions.None, "abcd");
				box.Document.ClearUndoRedoHistory();
				box.Focus(FocusState.Programmatic);
				box.Document.Selection.SetRange(operation == "backspace" ? 4 : 0, operation == "backspace" ? 4 : 0);
				await WindowHelper.WaitForIdle();

				var key = operation == "backspace" ? VirtualKey.Back : VirtualKey.Delete;
				for (var i = 0; i < 3; i++)
				{
					RaiseKey(box, key);
					await WindowHelper.WaitForIdle();
				}

				GetTextWithoutFinalEop(box.Document, out var afterDelete);
				Assert.AreEqual(expectedAfterDelete, afterDelete);
				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var afterUndo);
				Assert.AreEqual("abcd", afterUndo);
				Assert.IsFalse(box.Document.CanUndo());

				box.Document.Redo();
				GetTextWithoutFinalEop(box.Document, out var afterRedo);
				Assert.AreEqual(expectedAfterDelete, afterRedo);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Interactive_Caret_Move_Breaks_Typing_Coalescing()
		{
			var box = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = box;
				await WindowHelper.WaitForLoaded(box);
				box.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				await TypeAsync(box, "ab");
				RaiseKey(box, VirtualKey.Left);
				await WindowHelper.WaitForIdle();
				await TypeAsync(box, "X");

				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var firstUndo);
				Assert.AreEqual("ab", firstUndo);

				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var secondUndo);
				Assert.AreEqual(string.Empty, secondUndo);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow("backspace", "a")]
		[DataRow("delete", "d")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Wasm_Native_Selection_Echo_Preserves_Delete_Coalescing(string operation, string expectedAfterDelete)
		{
			var box = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = box;
				await WindowHelper.WaitForLoaded(box);
				box.Document.SetText(TextSetOptions.None, "abcd");
				box.Document.ClearUndoRedoHistory();
				box.Focus(FocusState.Programmatic);
				box.Document.Selection.SetRange(operation == "backspace" ? 4 : 0, operation == "backspace" ? 4 : 0);
				await WindowHelper.WaitForIdle();

				var key = operation == "backspace" ? VirtualKey.Back : VirtualKey.Delete;
				for (var i = 0; i < 3; i++)
				{
					RaiseKey(box, key);
					await WindowHelper.WaitForIdle();
					box.SelectFromNative(box.NativeSelectionStart, box.NativeSelectionLength);
				}

				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var afterUndo);
				Assert.AreEqual("abcd", afterUndo);
				Assert.IsFalse(box.Document.CanUndo());

				box.Document.Redo();
				GetTextWithoutFinalEop(box.Document, out var afterRedo);
				Assert.AreEqual(expectedAfterDelete, afterRedo);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Wasm_Native_Caret_Move_Breaks_Typing_Coalescing()
		{
			var box = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = box;
				await WindowHelper.WaitForLoaded(box);
				box.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				await TypeAsync(box, "ab");
				box.SelectFromNative(1, 0);
				await TypeAsync(box, "X");

				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var firstUndo);
				Assert.AreEqual("ab", firstUndo);

				box.Document.Undo();
				GetTextWithoutFinalEop(box.Document, out var secondUndo);
				Assert.AreEqual(string.Empty, secondUndo);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
