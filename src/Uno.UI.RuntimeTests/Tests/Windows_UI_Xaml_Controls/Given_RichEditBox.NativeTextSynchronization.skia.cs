#nullable enable

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
	[DataRow("\n", false)]
	[DataRow("\n", true)]
	[DataRow("\r\n", false)]
	[DataRow("\r\n", true)]
	[DataRow("\r", false)]
	[DataRow("\r", true)]
	public void When_NativeTextSynchronization_Edit_After_Paragraph_Preserves_Formatting(string lineEnding, bool acceptsReturn)
	{
		var editor = new RichEditBox { AcceptsReturn = acceptsReturn };
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.GetRange(2, 3).ParagraphFormat.SpaceBefore = 12;
		editor.Document.Selection.SetRange(3, 3);
		editor.Document.ClearUndoRedoHistory();
		var nativeText = $"a{lineEnding}bx";

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, nativeText.Length, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\rbx", text);
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(0, 1).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.AreEqual(12f, editor.Document.GetRange(2, 3).ParagraphFormat.SpaceBefore);
		Assert.AreEqual(4, editor.Document.Selection.StartPosition);
		Assert.AreEqual(4, editor.Document.Selection.EndPosition);
		editor.Document.Undo();
		GetTextWithoutFinalEop(editor.Document, out text);
		Assert.AreEqual("a\rb", text);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("Xa\nbcd", "Xa\rbcd", 3, 4, 5)]
	[DataRow("a\nXbcd", "a\rXbcd", 3, 4, 5)]
	[DataRow("a\nbXcd", "a\rbXcd", 2, 4, 5)]
	[DataRow("a\nbcXd", "a\rbcXd", 2, 3, 5)]
	[DataRow("Xa\r\nbcd", "Xa\rbcd", 3, 4, 5)]
	[DataRow("a\r\nXbcd", "a\rXbcd", 3, 4, 5)]
	[DataRow("a\r\nbXcd", "a\rbXcd", 2, 4, 5)]
	[DataRow("a\r\nbcXd", "a\rbcXd", 2, 3, 5)]
	public void When_NativeTextSynchronization_Edit_At_Format_Boundaries_Preserves_Unchanged_Runs(
		string nativeText, string expectedText, int boldPosition, int italicPosition, int underlinePosition)
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rbcd");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.GetRange(3, 4).CharacterFormat.Italic = FormatEffect.On;
		editor.Document.GetRange(4, 5).CharacterFormat.Underline = UnderlineType.Single;

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, nativeText.Length, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual(expectedText, text);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(boldPosition, boldPosition + 1).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(italicPosition, italicPosition + 1).CharacterFormat.Bold);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(italicPosition, italicPosition + 1).CharacterFormat.Italic);
		Assert.AreEqual(UnderlineType.Single, editor.Document.GetRange(underlinePosition, underlinePosition + 1).CharacterFormat.Underline);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n", false, false)]
	[DataRow("\n", false, true)]
	[DataRow("\n", true, true)]
	[DataRow("\r\n", false, false)]
	[DataRow("\r\n", false, true)]
	[DataRow("\r\n", true, true)]
	public void When_NativeTextSynchronization_Unchanged_Echo_Preserves_Document_And_Direction(
		string lineEnding, bool acceptsReturn, bool backward)
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.Selection.SetRange(backward ? 3 : 0, backward ? 0 : 3);
		editor.CharacterCasing = CharacterCasing.Upper;
		editor.MaxLength = 1;
		editor.AcceptsReturn = acceptsReturn;
		editor.Document.ClearUndoRedoHistory();
		var version = editor.Document.TextVersion;
		var changing = 0;
		editor.TextChanging += (_, _) => changing++;
		var nativeText = $"a{lineEnding}b";

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, 0, nativeText.Length));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\rb", text);
		Assert.AreEqual(version, editor.Document.TextVersion);
		Assert.AreEqual(0, changing);
		Assert.IsFalse(editor.Document.CanUndo());
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.AreEqual(0, editor.Document.Selection.StartPosition);
		Assert.AreEqual(3, editor.Document.Selection.EndPosition);
		Assert.AreEqual(backward, editor.NativeSelectionIsBackward);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(2, 0, 2, 2, false)]
	[DataRow(3, 0, 2, 2, false)]
	[DataRow(5, 0, 4, 4, false)]
	[DataRow(6, 0, 4, 4, false)]
	[DataRow(7, 0, 5, 5, false)]
	[DataRow(0, 7, 0, 5, false)]
	[DataRow(7, -6, 1, 5, true)]
	[DataRow(6, -3, 2, 4, true)]
	public void When_NativeTextSynchronization_Crlf_Maps_Both_Selection_Endpoints(
		int nativeStart, int nativeLength, int expectedStart, int expectedEnd, bool backward)
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb\rc");
		editor.Document.ClearUndoRedoHistory();

		Assert.IsTrue(editor.TryUpdateTextFromNative("a\r\nb\r\nc", nativeStart, nativeLength));

		Assert.AreEqual(expectedStart, editor.Document.Selection.StartPosition);
		Assert.AreEqual(expectedEnd, editor.Document.Selection.EndPosition);
		Assert.AreEqual(backward, editor.NativeSelectionIsBackward);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n", false)]
	[DataRow("\n", true)]
	[DataRow("\r\n", false)]
	[DataRow("\r\n", true)]
	public void When_NativeTextSynchronization_Coercion_Rebases_Normalized_Selection(string lineEnding, bool backward)
	{
		var editor = new RichEditBox { CharacterCasing = CharacterCasing.Upper, MaxLength = 5 };
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.Selection.SetRange(3, 3);
		var nativeText = $"a{lineEnding}bxyz";

		Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText,
			backward ? nativeText.Length : nativeText.Length - 3, backward ? -3 : 3));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\rbXY", text);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.AreEqual(3, editor.Document.Selection.StartPosition);
		Assert.AreEqual(5, editor.Document.Selection.EndPosition);
		Assert.AreEqual(backward, editor.NativeSelectionIsBackward);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_NativeTextSynchronization_Protected_Edit_Is_Rejected_After_Normalization()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.ProtectedText = FormatEffect.On;
		editor.Document.Selection.SetRange(3, 3);
		editor.Document.ClearUndoRedoHistory();

		Assert.IsFalse(editor.TryUpdateTextFromNative("a\r\nx", 4, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\rb", text);
		Assert.AreEqual(3, editor.Document.Selection.StartPosition);
		Assert.AreEqual(3, editor.Document.Selection.EndPosition);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_NativeTextSynchronization_Selection_Cancellation_Preserves_Text_Edit()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.Selection.SetRange(3, 3);
		editor.SelectionChanging += (_, args) => args.Cancel = true;

		Assert.IsTrue(editor.TryUpdateTextFromNative("a\r\nbx", 5, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("a\rbx", text);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.AreEqual(3, editor.Document.Selection.StartPosition);
		Assert.AreEqual(3, editor.Document.Selection.EndPosition);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_NativeTextSynchronization_Reentrant_TextChanging_Preserves_App_Selection()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.Selection.SetRange(3, 3);
		editor.TextChanging += (_, args) =>
		{
			if (args.IsContentChanging)
			{
				editor.Document.GetRange(0, 1).Text = "A";
				editor.Document.Selection.SetRange(3, 2);
			}
		};

		Assert.IsTrue(editor.TryUpdateTextFromNative("a\r\nbx", 5, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("A\rbx", text);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
		Assert.AreEqual(2, editor.Document.Selection.StartPosition);
		Assert.AreEqual(3, editor.Document.Selection.EndPosition);
		Assert.IsTrue(editor.NativeSelectionIsBackward);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public void When_NativeTextSynchronization_SelectionChanging_Can_Edit_Document_And_Cancel()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "a\rb");
		editor.Document.Selection.SetRange(3, 3);
		editor.SelectionChanging += (_, args) =>
		{
			editor.Document.GetRange(0, 1).Text = "A";
			args.Cancel = true;
		};

		Assert.IsTrue(editor.TryUpdateTextFromNative("a\r\nbx", 5, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual("A\rbx", text);
		Assert.AreEqual(3, editor.Document.Selection.StartPosition);
		Assert.AreEqual(3, editor.Document.Selection.EndPosition);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("\n")]
	[DataRow("\r\n")]
	public async Task When_NativeTextSynchronization_Coerced_NoOp_Consumes_Platform_Apply_Guard(string lineEnding)
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, MaxLength = 3 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "a\rb");
			editor.Document.Selection.SetRange(3, 3);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("x", cursorPosition: 1, textAlreadyApplied: true);
			var nativeText = $"a{lineEnding}bx";

			Assert.IsTrue(editor.TryUpdateTextFromNative(nativeText, nativeText.Length, 0));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("a\rb", text);
			Assert.IsTrue(editor.IsComposing);
			editor.Document.GetRange(0, 1).Text = "A";
			Assert.IsFalse(editor.IsComposing, "A corrected native no-op must not mask the next external edit.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
