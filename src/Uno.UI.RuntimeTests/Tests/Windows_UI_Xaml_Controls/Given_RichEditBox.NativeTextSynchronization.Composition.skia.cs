#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("a", 0, "a")]
	[DataRow("a", 1, "a")]
	[DataRow("aba", 1, "b")]
	[DataRow("aa", 1, "a")]
	public void When_NativeTextSynchronization_Rejected_Repeated_Input_Preserves_Original_And_Caret(string originalText, int caret, string input)
	{
		var editor = new RichEditBox { MaxLength = originalText.Length };
		editor.Document.SetText(TextSetOptions.None, originalText);
		var originalPosition = Math.Min(caret, originalText.Length - 1);
		editor.Document.GetRange(originalPosition, originalPosition + 1).CharacterFormat.Bold = FormatEffect.On;
		editor.Document.Selection.SetRange(caret, caret);
		editor.Document.ClearUndoRedoHistory();

		Assert.IsTrue(editor.TryUpdateTextFromNative(originalText.Insert(caret, input), caret + input.Length, 0));

		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual(originalText, text);
		Assert.AreEqual(caret, editor.Document.Selection.StartPosition);
		Assert.AreEqual(caret, editor.Document.Selection.EndPosition);
		Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(originalPosition, originalPosition + 1).CharacterFormat.Bold);
		Assert.IsFalse(editor.Document.CanUndo());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("a", 0, "a", "b")]
	[DataRow("a", 1, "a", "b")]
	[DataRow("aba", 1, "b", "x")]
	[DataRow("aa", 1, "a", "b")]
	public async Task When_NativeTextSynchronization_Rejected_Composition_Does_Not_Consume_Repeated_Context(
		string originalText, int caret, string preedit, string commit)
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, MaxLength = originalText.Length };
		try
		{
			await LoadNativeCompositionEditor(editor, originalText, caret);
			var originalPosition = Math.Min(caret, originalText.Length - 1);
			editor.Document.GetRange(originalPosition, originalPosition + 1).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.ClearUndoRedoHistory();
			var host = (IImeSessionHost)editor;
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate(preedit, cursorPosition: preedit.Length, textAlreadyApplied: true);

			host.UpdateTextFromNative(originalText.Insert(caret, preedit), caret + preedit.Length, 0);
			host.ReconcileCompositionFromNative(caret, 0);

			GetTextWithoutFinalEop(editor.Document, out var rejectedText);
			Assert.AreEqual(originalText, rejectedText);
			Assert.AreEqual(caret, editor.Document.Selection.StartPosition);
			Assert.AreEqual(0, editor.CompositionLength);
			Assert.IsTrue(editor.TryGetAccessibilityCompositionRange(false, out var start, out var end));
			Assert.AreEqual(caret, start);
			Assert.AreEqual(caret, end);

			fake.SimulateCompositionUpdate(commit, cursorPosition: commit.Length, textAlreadyApplied: true);
			host.UpdateTextFromNative(originalText.Insert(caret, commit), caret + commit.Length, 0);
			host.ReconcileCompositionFromNative(caret, 0);
			fake.SimulateCompositionComplete(string.Empty, textAlreadyApplied: true);

			GetTextWithoutFinalEop(editor.Document, out var committedText);
			Assert.AreEqual(originalText, committedText, "The rejected preedit must not claim the original character as its replacement span.");
			Assert.AreEqual(caret, editor.Document.Selection.StartPosition);
			Assert.AreEqual(caret, editor.Document.Selection.EndPosition);
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(originalPosition, originalPosition + 1).CharacterFormat.Bold);
			Assert.IsFalse(editor.Document.CanUndo());
			Assert.IsFalse(editor.IsComposing);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(0)]
	[DataRow(2)]
	public async Task When_NativeTextSynchronization_Composition_Metadata_Excludes_Suffix_And_Preserves_Undo(int resolvedLength)
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, MaxLength = 3 };
		try
		{
			await LoadNativeCompositionEditor(editor, "AB", 1);
			var host = (IImeSessionHost)editor;
			var changed = 0;
			var ended = 0;
			editor.TextCompositionChanged += (_, _) => changed++;
			editor.TextCompositionEnded += (_, _) => ended++;
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni", cursorPosition: 2, resolvedLength: resolvedLength, textAlreadyApplied: true);
			host.UpdateTextFromNative("AniB", 3, 0);
			var version = editor.Document.TextVersion;
			var selectionVersion = editor.Document.SelectionChangeVersion;

			host.ReconcileCompositionFromNative(1, 1);

			Assert.AreEqual(version, editor.Document.TextVersion);
			Assert.AreEqual(selectionVersion, editor.Document.SelectionChangeVersion);
			Assert.AreEqual(1, editor.CompositionLength);
			Assert.AreEqual(2, editor.Document.Selection.StartPosition);
			Assert.AreEqual(1, changed, "Reconciliation must not emit another pre-apply composition event.");
			Assert.AreEqual(0, ended);
			Assert.IsTrue(editor.TryGetAccessibilityCompositionRange(false, out var start, out var end));
			Assert.AreEqual(1, start);
			Assert.AreEqual(2, end);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			Assert.IsNotNull(peer);
			var provider = peer.GetPattern(PatternInterface.TextEdit) as ITextEditProvider;
			Assert.IsNotNull(provider);
			Assert.AreEqual("n", provider.GetActiveComposition().GetText(-1));
			if (resolvedLength == 0)
			{
				Assert.AreEqual("n", provider.GetConversionTarget().GetText(-1));
			}
			else
			{
				Assert.IsNull(provider.GetConversionTarget());
			}

			fake.SimulateCompositionUpdate("x", cursorPosition: 1, textAlreadyApplied: true);
			host.UpdateTextFromNative("AxB", 2, 0);
			host.ReconcileCompositionFromNative(1, 1);
			fake.SimulateCompositionComplete("x", textAlreadyApplied: true);
			Assert.AreEqual(2, changed);
			Assert.AreEqual(1, ended);
			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out var undone);
			Assert.AreEqual("AB", undone, "Metadata reconciliation must leave the composition undo group open.");
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_NativeTextSynchronization_Composition_Reconciliation_Does_Not_Rearm_Apply_Guard()
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, MaxLength = 3 };
		try
		{
			await LoadNativeCompositionEditor(editor, "AB", 1);
			var host = (IImeSessionHost)editor;
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni", cursorPosition: 2, textAlreadyApplied: true);
			host.UpdateTextFromNative("AniB", 3, 0);
			host.ReconcileCompositionFromNative(1, 1);

			editor.Document.GetRange(0, 1).Text = "X";

			Assert.IsFalse(editor.IsComposing, "An app edit after reconciliation must not be consumed as pending platform input.");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("XnB", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(-2, 99, 0, 3)]
	[DataRow(99, 99, 3, 0)]
	[DataRow(1, -1, 1, 0)]
	public async Task When_NativeTextSynchronization_Composition_Reconciliation_Clamps_Only_Metadata(
		int requestedStart, int requestedLength, int expectedStart, int expectedLength)
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, MaxLength = 3 };
		try
		{
			await LoadNativeCompositionEditor(editor, "AB", 1);
			var host = (IImeSessionHost)editor;
			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni", cursorPosition: 2, resolvedLength: 2, textAlreadyApplied: true);
			host.UpdateTextFromNative("AniB", 3, 0);
			var textVersion = editor.Document.TextVersion;
			var selectionVersion = editor.Document.SelectionChangeVersion;
			var canUndo = editor.Document.CanUndo();
			var events = 0;
			editor.TextChanging += (_, _) => events++;
			editor.SelectionChanged += (_, _) => events++;
			editor.TextCompositionChanged += (_, _) => events++;
			editor.TextCompositionEnded += (_, _) => events++;

			host.ReconcileCompositionFromNative(requestedStart, requestedLength);

			Assert.AreEqual(expectedStart, editor.CompositionStartIndex);
			Assert.AreEqual(expectedLength, editor.CompositionLength);
			Assert.AreEqual(textVersion, editor.Document.TextVersion);
			Assert.AreEqual(selectionVersion, editor.Document.SelectionChangeVersion);
			Assert.AreEqual(2, editor.Document.Selection.StartPosition);
			Assert.AreEqual(2, editor.Document.Selection.EndPosition);
			Assert.AreEqual(canUndo, editor.Document.CanUndo());
			Assert.IsTrue(editor.IsComposing);
			Assert.AreEqual(0, events);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("AnB", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task LoadNativeCompositionEditor(RichEditBox editor, string text, int caret)
	{
		await UITestHelper.Load(editor);
		editor.Document.SetText(TextSetOptions.None, text);
		editor.Document.Selection.SetRange(caret, caret);
		Assert.IsTrue(editor.Focus(FocusState.Programmatic));
		await WindowHelper.WaitForIdle();
		editor.Document.ClearUndoRedoHistory();
	}
}
