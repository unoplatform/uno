#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	// A valid 1x1 PNG, so these tests don't depend on SkiaSharp to produce image data.
	private static readonly byte[] _onePixelPng = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==");

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Format_Range_Splits_Surrogate_Pair_Editor_Still_Loads()
	{
		var editor = new RichEditBox { Width = 200 };
		try
		{
			editor.Document.SetText(TextSetOptions.None, "a\U0001F600b");
			// Ends between the high and low surrogate of the emoji.
			editor.Document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;

			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			editor.Document.GetText(TextGetOptions.None, out var text);
			Assert.StartsWith("a\U0001F600b", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextChanging_Edits_During_Undo_History_Stays_Consistent()
	{
		var editor = new RichEditBox { Width = 200 };
		var reentered = false;
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.ClearUndoRedoHistory();
			editor.Document.Selection.SetRange(3, 3);
			editor.Document.Selection.TypeText("d");
			await WindowHelper.WaitForIdle();

			editor.TextChanging += (sender, args) =>
			{
				if (!reentered)
				{
					reentered = true;
					sender.Document.GetRange(0, 0).Text = "x";
				}
			};

			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			editor.Document.Redo();
			editor.Document.Undo();
			editor.Document.Undo();
			await WindowHelper.WaitForIdle();

			editor.Document.GetText(TextGetOptions.None, out var text);
			Assert.IsTrue(reentered);
			Assert.IsTrue(text.Length >= 3, $"History replay corrupted the document: '{text}'");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Undo_Group_Exceeds_Budget_Older_History_Is_Not_Replayed()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");
		document.ClearUndoRedoHistory();
		document.Selection.SetRange(3, 3);
		document.Selection.TypeText("d");

		// Larger than the 4 MiB undo cost budget, so the group's own operations are dropped.
		document.BeginUndoGroup();
		document.GetRange(0, 0).Text = new string('z', 3 * 1024 * 1024);
		document.EndUndoGroup();
		document.GetText(TextGetOptions.None, out var before);

		document.Undo();

		document.GetText(TextGetOptions.None, out var after);
		Assert.AreEqual(before, after, "Undoing an entry recorded before the dropped group would apply stale offsets.");
	}

	[TestMethod]
	public void When_ChangeCase_Preserves_Mixed_Formatting_And_Images()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "abc");
		document.GetRange(1, 2).CharacterFormat.Bold = FormatEffect.On;
		document.GetRange(3, 3).InsertImage(4, 4, 4, VerticalCharacterAlignment.Baseline, "img", new MemoryStream(_onePixelPng).AsRandomAccessStream());

		document.GetRange(0, 4).ChangeCase(LetterCase.Upper);

		Assert.AreEqual("ABC", document.GetRange(0, 3).Text);
		Assert.AreEqual(FormatEffect.Off, document.GetRange(0, 1).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.Off, document.GetRange(2, 3).CharacterFormat.Bold);
		document.GetText(TextGetOptions.UseObjectText, out var objectText);
		Assert.Contains("img", objectText, "The embedded image must survive a case change.");
	}
}
