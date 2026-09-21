#nullable enable

using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_Authoritative_Text_Change_Expires_Composition_Echo(bool semantic, bool undo)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			var id = await FocusBrowserInput(editor, semantic);
			CommitBrowserCharacterBeforeCompositionEnd(id);
			AssertBrowserInputState(editor, id, "a", 1);

			if (undo)
			{
				Assert.IsTrue(editor.Document.CanUndo());
				editor.Document.Undo();
			}
			else
			{
				editor.Document.SetText(TextSetOptions.None, "");
			}
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "", 0);

			DispatchBrowserInput(id, 0, 0, "a");
			AssertBrowserInputState(editor, id, "a", 1);
			DispatchBrowserInput(id, 1, 1, "b");
			AssertBrowserInputState(editor, id, "ab", 2);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_BrowserInput_Authoritative_Selection_Expires_Composition_Echo(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			var id = await FocusBrowserInput(editor, semantic);
			CommitBrowserCharacterBeforeCompositionEnd(id);
			AssertBrowserInputState(editor, id, "a", 1);
			editor.Document.Selection.SetRange(0, 1);
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "a", 0, 1);

			DispatchBrowserInput(id, 0, 1, "a");
			AssertBrowserInputState(editor, id, "a", 1);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, "insertCompositionText")]
	[DataRow(false, "insertFromComposition")]
	[DataRow(true, "insertCompositionText")]
	[DataRow(true, "insertFromComposition")]
	public async Task When_BrowserInput_Authoritative_Edit_Does_Not_Reopen_Stale_Final_Composition_Input(bool semantic, string inputType)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			var id = await FocusBrowserInput(editor, semantic);
			CommitBrowserCharacterBeforeCompositionEnd(id);
			editor.Document.SetText(TextSetOptions.None, "XY");
			await WindowHelper.WaitForIdle();

			RunBrowserInput(id, $$"""
				input.value = 'a'; input.setSelectionRange(1, 1);
				const inputType = {{JsonSerializer.Serialize(inputType)}};
				const ev = new InputEvent('input', { bubbles: true, inputType, data: 'a' });
				// Chromium drops legacy inputType names; preserve the cross-browser event being tested.
				if (ev.inputType !== inputType) {
					Object.defineProperty(ev, 'inputType', { value: inputType });
				}
				input.dispatchEvent(ev);
				return input.value;
				""");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("XY", text);
			Assert.AreEqual("XY", RunBrowserInput(id, "return input.value;"));
			DispatchBrowserInput(id, 2, 2, "a");
			AssertBrowserInputState(editor, id, "XYa", 3);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_BrowserInput_Nested_Clipboard_Prepare_Supersedes_Outer_Cut(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		var other = new RichEditBox { Width = 300 };
		var panel = new StackPanel { Children = { editor, other } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			editor.Document.Selection.SetRange(1, 3);
			other.Document.SetText(TextSetOptions.None, "WXYZ");
			other.Document.Selection.SetRange(1, 3);
			other.GetOrCreateAutomationPeer();
			var id = await FocusBrowserInput(editor, semantic);
			var otherId = semantic ? GetSemanticElementId(other) : "uno-input";
			var cuts = 0;
			var copies = 0;
			var nestedPayload = "";
			other.CopyingToClipboard += (_, _) => copies++;
			editor.CuttingToClipboard += (_, _) =>
			{
				cuts++;
				Assert.IsTrue(other.Focus(FocusState.Programmatic));
				nestedPayload = DispatchBrowserClipboard(otherId, "copy", 1, 3);
				Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			};

			var outerPayload = DispatchBrowserClipboard(id, "cut", 1, 3);
			Assert.AreEqual("true:", outerPayload, $"Nested payload: {nestedPayload}; cuts: {cuts}; copies: {copies}.");
			Assert.AreEqual("true:XY", nestedPayload);
			Assert.AreEqual(1, cuts);
			Assert.AreEqual(1, copies);
			Assert.AreSame(editor, GetBrowserFocusedElement(editor));
			AssertBrowserInputState(editor, id, "ABCD", 1, 3);
			GetTextWithoutFinalEop(other.Document, out var otherText);
			Assert.AreEqual("WXYZ", otherText);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	private static void CommitBrowserCharacterBeforeCompositionEnd(string id)
		=> RunBrowserInput(id, """
			input.setSelectionRange(0, 0);
			input.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true, data: '' }));
			input.value = 'a'; input.setSelectionRange(1, 1);
			input.dispatchEvent(new CompositionEvent('compositionupdate', { bubbles: true, data: 'a' }));
			input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: 'a', isComposing: false }));
			input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'a' }));
			return input.value;
			""");
}
