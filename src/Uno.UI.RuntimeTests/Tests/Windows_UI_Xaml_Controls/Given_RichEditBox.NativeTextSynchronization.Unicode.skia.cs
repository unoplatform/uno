#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n", false)]
	[DataRow("\n", true)]
	[DataRow("\r\n", false)]
	[DataRow("\r\n", true)]
	public void When_NativeTextSynchronization_Deletion_Preserves_Unchanged_Scalar_And_Formatting(
		string lineEnding, bool deleteSelection)
	{
		var editor = new RichEditBox { CharacterCasing = CharacterCasing.Upper };
		const string originalText = "a\r\U00010428\U00010429z";
		editor.Document.SetText(TextSetOptions.None, originalText);
		editor.Document.GetRange(2, 4).CharacterFormat.Underline = UnderlineType.Single;
		editor.Document.GetRange(4, 6).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.GetRange(6, 7).CharacterFormat.Italic = FormatEffect.On;
		editor.Document.Selection.SetRange(deleteSelection ? 2 : 4, 4);
		editor.Document.ClearUndoRedoHistory();

		Assert.IsTrue(editor.TryUpdateTextFromNative($"a{lineEnding}\U00010429z", 1 + lineEnding.Length, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\r\U00010429z", text, "An unchanged surviving scalar must not be imported as cased replacement text.");
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 4).CharacterFormat.Bold);
		Assert.AreEqual(UnderlineType.None, editor.Document.GetRange(2, 4).CharacterFormat.Underline);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(4, 5).CharacterFormat.Italic);
		Assert.AreEqual(2, editor.Document.Selection.StartPosition);
		Assert.AreEqual(2, editor.Document.Selection.EndPosition);
		editor.Document.Undo();
		GetTextWithoutFinalEop(editor.Document, out var undone);
		Assert.AreEqual(originalText, undone);
		Assert.AreEqual(UnderlineType.Single, editor.Document.GetRange(2, 4).CharacterFormat.Underline);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(4, 6).CharacterFormat.Bold);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n", "\U00010428", false)]
	[DataRow("\n", "\U00010428", true)]
	[DataRow("\r\n", "\U00010428", false)]
	[DataRow("\r\n", "\U00010428", true)]
	[DataRow("\n", "\U00010029", false)]
	[DataRow("\n", "\U00010029", true)]
	[DataRow("\r\n", "\U00010029", false)]
	[DataRow("\r\n", "\U00010029", true)]
	public void When_NativeTextSynchronization_Casing_Uses_Whole_Utf16_Scalars(
		string lineEnding, string originalScalar, bool replaceSelection)
	{
		var editor = new RichEditBox { CharacterCasing = CharacterCasing.Upper, MaxLength = 5 };
		var originalText = $"a\r{originalScalar}z";
		editor.Document.SetText(TextSetOptions.None, originalText);
		editor.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.GetRange(1, 2).CharacterFormat.Underline = UnderlineType.Single;
		editor.Document.GetRange(4, 5).CharacterFormat.Italic = FormatEffect.On;
		editor.Document.Selection.SetRange(replaceSelection ? 2 : 4, 4);
		editor.Document.ClearUndoRedoHistory();
		var nativeText = $"a{lineEnding}\U00010429z";

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, nativeText.Length - 1, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\r\U00010401z", text, "Casing needs the whole Deseret scalar, not only its differing surrogate.");
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 1).CharacterFormat.Bold);
		Assert.AreEqual(UnderlineType.Single, editor.Document.GetRange(1, 2).CharacterFormat.Underline);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(4, 5).CharacterFormat.Italic);
		Assert.AreEqual(4, editor.Document.Selection.StartPosition);
		Assert.AreEqual(4, editor.Document.Selection.EndPosition);
		editor.Document.Undo();
		GetTextWithoutFinalEop(editor.Document, out var undone);
		Assert.AreEqual(originalText, undone);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n")]
	[DataRow("\r\n")]
	public void When_NativeTextSynchronization_Truncation_Preserves_Whole_Cased_Scalar(string lineEnding)
	{
		var editor = new RichEditBox { CharacterCasing = CharacterCasing.Upper, MaxLength = 5 };
		editor.Document.SetText(TextSetOptions.None, "a\r\U00010428z");
		editor.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.GetRange(4, 5).CharacterFormat.Italic = FormatEffect.On;
		editor.Document.Selection.SetRange(2, 4);
		var nativeText = $"a{lineEnding}\U00010429xz";

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, nativeText.Length - 1, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\r\U00010401z", text);
		Assert.AreEqual(4, editor.Document.Selection.StartPosition);
		Assert.AreEqual(4, editor.Document.Selection.EndPosition);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 1).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(4, 5).CharacterFormat.Italic);
	}
}
