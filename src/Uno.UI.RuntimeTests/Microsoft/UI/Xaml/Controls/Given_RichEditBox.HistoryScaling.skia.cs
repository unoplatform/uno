#nullable enable

using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Small_Edits_Retain_Local_History_Deltas()
		{
			const int paragraphCount = 4096;
			const int paragraphLength = 32;
			var rtf = new StringBuilder(@"{\rtf1\ansi ");
			for (var i = 0; i < paragraphCount; i++)
			{
				rtf.Append(i % 2 == 0 ? @"\b " : @"\b0 ");
				rtf.Append('a', paragraphLength - 1);
				rtf.Append(@"\par ");
			}
			rtf.Append('}');

			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf.ToString());
			document.ClearUndoRedoHistory();
			for (var i = 0; i < 100; i++)
			{
				var paragraph = i * 127 % paragraphCount;
				var position = paragraph * paragraphLength + 10;
				document.GetRange(position, position + 1).Text = "z";
			}

			Assert.AreEqual(100, document.UndoEntryCount);
			Assert.IsTrue(document.UndoRetainedTextLength <= 200, $"Retained text was {document.UndoRetainedTextLength} UTF-16 code units.");
			Assert.IsTrue(document.UndoRetainedRunCount <= 1_000, $"Retained runs were {document.UndoRetainedRunCount}.");
			Assert.IsTrue(document.UndoHistoryCost < 1_000_000, $"History cost was {document.UndoHistoryCost} bytes.");
			Assert.IsTrue(document.AreRunIndexesValid());
		}

		[TestMethod]
		public void When_History_Budget_Evicts_Oldest_Entries_Deterministically()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, new string('a', 16_384));
			document.ClearUndoRedoHistory();
			document.UndoLimit = 10;
			for (var i = 0; i < 20; i++)
			{
				document.GetRange(i, i + 1).Text = ((char)('A' + i)).ToString();
			}

			Assert.AreEqual(10, document.UndoEntryCount);
			for (var i = 0; i < 10; i++)
			{
				document.Undo();
			}
			Assert.IsFalse(document.CanUndo());
			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("ABCDEFGHIJ", text[..10]);
			Assert.AreEqual(new string('a', 10), text.Substring(10, 10));
		}

		[TestMethod]
		public void When_Image_Insert_Undo_Redo_Restores_Object_State()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "ab");
			document.ClearUndoRedoHistory();
			using var image = CreateImageStream(SKColors.Blue);

			document.GetRange(1, 1).InsertImage(
				8,
				9,
				6,
				VerticalCharacterAlignment.Baseline,
				"picture",
				image);
			GetTextWithoutFinalEop(document, out var inserted);
			Assert.AreEqual("a\ufffcb", inserted);

			document.Undo();
			GetTextWithoutFinalEop(document, out var undone);
			Assert.AreEqual("ab", undone);

			document.Redo();
			GetTextWithoutFinalEop(document, TextGetOptions.UseObjectText, out var redone);
			Assert.AreEqual("apictureb", redone);
		}
	}
}
