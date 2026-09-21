#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_BrowserInput_Repeated_Insertion_And_Deletion_Are_Applied_Once(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);

			DispatchBrowserInput(id, 1, 1, "x");
			AssertBrowserInputState(editor, id, "AxB", 2);
			DispatchBrowserInput(id, 2, 2, "yz");
			AssertBrowserInputState(editor, id, "AxyzB", 4);
			DispatchBrowserInput(id, 1, 2, "", "deleteContentForward");
			AssertBrowserInputState(editor, id, "AyzB", 1);
			DispatchBrowserInput(id, 2, 3, "", "deleteContentBackward");
			AssertBrowserInputState(editor, id, "AyB", 2);
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "AyB", 2);
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
	public async Task When_BrowserInput_Casing_And_MaxLength_Correct_The_Dom(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300, CharacterCasing = CharacterCasing.Upper, MaxLength = 3 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "aB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);

			DispatchBrowserInput(id, 1, 1, "xyz");
			AssertBrowserInputState(editor, id, "aXB", 2);
			DispatchBrowserInput(id, 2, 2, "q");
			AssertBrowserInputState(editor, id, "aXB", 2);
			DispatchBrowserInput(id, 1, 2, "z");
			AssertBrowserInputState(editor, id, "aZB", 2);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_Rejected_Edits_Restore_Text_And_Selection(bool semantic, bool readOnly)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.Selection.SetRange(1, 2);
			if (readOnly)
			{
				editor.IsReadOnly = true;
			}
			else
			{
				editor.Document.GetRange(1, 2).CharacterFormat.ProtectedText = FormatEffect.On;
			}
			var id = await FocusBrowserInput(editor, semantic);

			DispatchBrowserInput(id, 1, 2, "x");
			AssertBrowserInputState(editor, id, "abc", 1, 2);
			DispatchBrowserInput(id, 1, 2, "", "deleteContentForward");
			AssertBrowserInputState(editor, id, "abc", 1, 2);
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
	public async Task When_BrowserInput_Reentrant_Document_Change_Is_Reconciled(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);
			var replaced = false;
			editor.TextChanging += (_, _) =>
			{
				if (!replaced)
				{
					replaced = true;
					editor.Document.SetText(TextSetOptions.None, "replacement");
				}
			};

			DispatchBrowserInput(id, 1, 1, "x");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("replacement", text, "A synchronous app mutation must supersede the native insertion.");
			Assert.AreEqual(text, RunBrowserInput(id, "return input.value;"));
			DispatchBrowserInput(id, text.Length, text.Length, "!");
			AssertBrowserInputState(editor, id, "replacement!", 12);
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
	public async Task When_BrowserInput_Rejection_Preserves_Backward_Selection(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			editor.Document.Selection.SetRange(1, 3);
			editor.Document.Selection.Options |= SelectionOptions.StartActive;
			editor.IsReadOnly = true;
			var id = await FocusBrowserInput(editor, semantic);

			DispatchBrowserInput(id, 1, 3, "x");
			AssertBrowserInputState(editor, id, "ABCD", 1, 3);
			Assert.AreEqual("backward", RunBrowserInput(id, "return input.selectionDirection;"));
			Assert.IsTrue(editor.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));
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
	public async Task When_BrowserInput_Cancelled_Selection_Is_Restored(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(2, 2);
			var id = await FocusBrowserInput(editor, semantic);
			editor.SelectionChanging += (_, args) => args.Cancel = true;

			RunBrowserInput(id, $$"""
				input.setSelectionRange(0, 1);
				{{(semantic ? "input.dispatchEvent(new Event('select'));" : "document.dispatchEvent(new Event('selectionchange'));")}}
				return 'ok';
				""");
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "AB", 2);
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
	public async Task When_BrowserInput_Reentrant_Focus_Change_Does_Not_Modify_The_New_Owner(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		var next = new RichEditBox { Width = 300 };
		var panel = new StackPanel { Children = { editor, next } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			next.Document.SetText(TextSetOptions.None, "next");
			next.Document.Selection.SetRange(4, 4);
			var id = await FocusBrowserInput(editor, semantic);
			editor.TextChanging += (_, _) => next.Focus(FocusState.Programmatic);

			DispatchBrowserInput(id, 1, 1, "x");
			await WindowHelper.WaitForIdle();
			Assert.AreSame(next, GetBrowserFocusedElement(next));
			var nextId = semantic ? GetSemanticElementId(next) : "uno-input";
			AssertBrowserInputState(next, nextId, "next", 4);
			DispatchBrowserInput(nextId, 4, 4, "!");
			AssertBrowserInputState(next, nextId, "next!", 5);
			GetTextWithoutFinalEop(editor.Document, out var originalText);
			Assert.AreEqual("AxB", originalText);
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
	public async Task When_BrowserInput_Paragraphs_Are_Independent_Of_The_Enter_Policy(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300, TextWrapping = TextWrapping.Wrap, AcceptsReturn = false };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "A\rB");
			editor.Document.GetRange(2, 3).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.Selection.SetRange(3, 3);
			var id = await FocusBrowserInput(editor, semantic);
			AssertBrowserInputState(editor, id, "A\rB", 3);

			DispatchBrowserInput(id, 3, 3, "x");
			AssertBrowserInputState(editor, id, "A\rBx", 4);
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(2, 3).CharacterFormat.Bold);
			Assert.AreEqual("true", RunBrowserInput(id, """
				const ev = new InputEvent('beforeinput', { inputType: 'insertLineBreak', bubbles: true, cancelable: true });
				input.dispatchEvent(ev); return String(ev.defaultPrevented);
				"""));
			AssertBrowserInputState(editor, id, "A\rBx", 4);

			editor.AcceptsReturn = true;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("false", RunBrowserInput(id, """
				const ev = new InputEvent('beforeinput', { inputType: 'insertParagraph', bubbles: true, cancelable: true });
				input.dispatchEvent(ev); return String(ev.defaultPrevented);
				"""));
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
	public async Task When_BrowserInput_Clipboard_Cancellation_Is_Synchronous(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(0, 1);
			var id = await FocusBrowserInput(editor, semantic);
			var copies = 0;
			var cuts = 0;
			editor.CopyingToClipboard += (_, args) => { copies++; args.Handled = true; };
			editor.CuttingToClipboard += (_, args) => { cuts++; args.Handled = true; };

			var result = RunBrowserInput(id, """
				return ['copy', 'cut', 'copy', 'cut'].map(type => {
					const data = new DataTransfer();
					data.setData('text/plain', 'unchanged');
					const ev = new ClipboardEvent(type, { bubbles: true, cancelable: true, clipboardData: data });
					input.dispatchEvent(ev);
					return `${ev.defaultPrevented}:${data.getData('text/plain')}`;
				}).join('|');
				""");
			Assert.AreEqual("true:unchanged|true:unchanged|true:unchanged|true:unchanged", result);
			Assert.AreEqual(2, copies);
			Assert.AreEqual(2, cuts);
			AssertBrowserInputState(editor, id, "AB", 0, 1);
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
	public async Task When_BrowserInput_Clipboard_Cut_Is_Applied_Once(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			var id = await FocusBrowserInput(editor, semantic);
			var copies = 0;
			var cuts = 0;
			editor.CopyingToClipboard += (_, _) => copies++;
			editor.CuttingToClipboard += (_, _) => cuts++;

			Assert.AreEqual("true:BC", DispatchBrowserClipboard(id, "copy", 1, 3));
			AssertBrowserInputState(editor, id, "ABCD", 1, 3);
			Assert.AreEqual("true:BC", DispatchBrowserClipboard(id, "cut", 1, 3));
			AssertBrowserInputState(editor, id, "AD", 1);
			Assert.AreEqual("true:A", DispatchBrowserClipboard(id, "cut", 0, 1));
			AssertBrowserInputState(editor, id, "D", 0);
			Assert.AreEqual(1, copies, "Cut raises CuttingToClipboard, not CopyingToClipboard.");
			Assert.AreEqual(2, cuts);
			DispatchBrowserInput(id, 0, 0, "x");
			AssertBrowserInputState(editor, id, "xD", 1);
			DispatchBrowserInput(id, 1, 2, "", "deleteContentForward");
			AssertBrowserInputState(editor, id, "x", 1);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, "missing")]
	[DataRow(false, "throw")]
	[DataRow(false, "ignore")]
	[DataRow(false, "rtf-throw")]
	[DataRow(true, "missing")]
	[DataRow(true, "throw")]
	[DataRow(true, "ignore")]
	[DataRow(true, "rtf-throw")]
	public async Task When_BrowserInput_Clipboard_Write_Failure_Does_Not_Cut(bool semantic, string failure)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300, ClipboardCopyFormat = RichEditClipboardFormat.AllFormats };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.Selection.SetRange(0, 1);
			var id = await FocusBrowserInput(editor, semantic);
			var cuts = 0;
			editor.CuttingToClipboard += (_, _) => cuts++;

			var result = RunBrowserInput(id, $$"""
				const failure = {{JsonSerializer.Serialize(failure)}};
				const data = failure === 'missing' ? null : new DataTransfer();
				if (data) {
					const write = data.setData.bind(data);
					Object.defineProperty(data, 'setData', { value: (format, text) => {
						if (failure === 'throw' || (failure === 'rtf-throw' && format === 'text/rtf')) {
							throw new DOMException('Clipboard write intentionally failed', 'NotAllowedError');
						}
						if (failure !== 'ignore') { write(format, text); }
					} });
				}
				let errors = 0;
				const onError = event => {
					if (event.error?.message === 'Clipboard write intentionally failed') {
						errors++; event.preventDefault();
					}
				};
				window.addEventListener('error', onError);
				try {
					const ev = new ClipboardEvent('cut', { bubbles: true, cancelable: true, clipboardData: data });
					input.dispatchEvent(ev);
					return `${ev.defaultPrevented}|${errors}`;
				} finally {
					window.removeEventListener('error', onError);
				}
				""");
			Assert.AreEqual(failure is "throw" or "rtf-throw" ? "true|1" : "true|0", result);
			Assert.AreEqual(failure == "missing" ? 0 : 1, cuts);
			AssertBrowserInputState(editor, id, "AB", 0, 1);

			Assert.AreEqual("true:A", DispatchBrowserClipboard(id, "cut", 0, 1));
			AssertBrowserInputState(editor, id, "B", 0);
			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out var restored);
			Assert.AreEqual("AB", restored);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, "selection")]
	[DataRow(false, "document")]
	[DataRow(false, "focus")]
	[DataRow(true, "selection")]
	[DataRow(true, "document")]
	[DataRow(true, "focus")]
	public async Task When_BrowserInput_Clipboard_Write_Reentrancy_Invalidates_The_Cut(bool semantic, string change)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		var next = new RichEditBox { Width = 300 };
		var panel = new StackPanel { Children = { editor, next } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			editor.Document.Selection.SetRange(1, 3);
			next.Document.SetText(TextSetOptions.None, "next");
			next.Document.Selection.SetRange(4, 4);
			var id = await FocusBrowserInput(editor, semantic);
			var changed = false;
			editor.SelectionChanged += (_, _) =>
			{
				if (changed)
				{
					return;
				}
				changed = true;
				if (change == "document")
				{
					editor.Document.SetText(TextSetOptions.None, "replacement");
				}
				else if (change == "focus")
				{
					next.Focus(FocusState.Programmatic);
				}
			};

			Assert.AreEqual("true:BC", RunBrowserInput(id, $$"""
				const data = new DataTransfer();
				const write = data.setData.bind(data);
				let changed = false;
				Object.defineProperty(data, 'setData', { value: (format, text) => {
					write(format, text);
					if (!changed) {
						changed = true;
						input.setSelectionRange(0, 0);
						{{(semantic ? "input.dispatchEvent(new Event('select'));" : "document.dispatchEvent(new Event('selectionchange'));")}}
					}
				} });
				const ev = new ClipboardEvent('cut', { bubbles: true, cancelable: true, clipboardData: data });
				input.dispatchEvent(ev);
				return `${ev.defaultPrevented}:${data.getData('text/plain')}`;
				"""));
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(changed, "The managed callback must interleave between clipboard preparation and commit.");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual(change == "document" ? "replacement" : "ABCD", text);
			if (change == "focus")
			{
				Assert.AreSame(next, GetBrowserFocusedElement(next));
				AssertBrowserInputState(next, semantic ? GetSemanticElementId(next) : "uno-input", "next", 4);
			}
			else
			{
				Assert.AreSame(editor, GetBrowserFocusedElement(editor));
				Assert.AreEqual(text, RunBrowserInput(id, "return input.value;"));
			}
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_Clipboard_Preserves_The_Requested_Formats(bool semantic, bool plainText)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox
		{
			Width = 300,
			ClipboardCopyFormat = plainText ? RichEditClipboardFormat.PlainText : RichEditClipboardFormat.AllFormats,
		};
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.Selection.SetRange(0, 1);
			var id = await FocusBrowserInput(editor, semantic);

			using var result = JsonDocument.Parse(RunBrowserInput(id, """
				const data = new DataTransfer();
				const ev = new ClipboardEvent('copy', { bubbles: true, cancelable: true, clipboardData: data });
				input.dispatchEvent(ev);
				return JSON.stringify({ text: data.getData('text/plain'), rtf: data.getData('text/rtf'), rich: data.types.includes('text/rtf') });
				"""));
			Assert.AreEqual("A", result.RootElement.GetProperty("text").GetString());
			Assert.AreEqual(!plainText, result.RootElement.GetProperty("rich").GetBoolean());
			var rtf = result.RootElement.GetProperty("rtf").GetString()!;
			if (plainText)
			{
				Assert.AreEqual("", rtf);
			}
			else
			{
				var copied = new RichEditBox();
				copied.Document.SetText(TextSetOptions.FormatRtf, rtf);
				GetTextWithoutFinalEop(copied.Document, out var text);
				Assert.AreEqual("A", text);
				Assert.AreEqual(FormatEffect.On, copied.Document.GetRange(0, 1).CharacterFormat.Bold);
			}
			AssertBrowserInputState(editor, id, "AB", 0, 1);
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
	public async Task When_BrowserInput_Clipboard_Uses_The_Selection_After_The_Handler(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			var id = await FocusBrowserInput(editor, semantic);
			editor.CopyingToClipboard += (_, _) => editor.Document.Selection.SetRange(1, 3);
			editor.CuttingToClipboard += (_, _) => editor.Document.Selection.SetRange(2, 4);

			Assert.AreEqual("true:BC", DispatchBrowserClipboard(id, "copy", 0, 1));
			AssertBrowserInputState(editor, id, "ABCD", 1, 3);
			Assert.AreEqual("true:CD", DispatchBrowserClipboard(id, "cut", 0, 1));
			AssertBrowserInputState(editor, id, "AB", 2);
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
	public async Task When_BrowserInput_Clipboard_Handler_Replaces_The_Document(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			var id = await FocusBrowserInput(editor, semantic);
			editor.CuttingToClipboard += (_, _) =>
			{
				editor.Document.SetText(TextSetOptions.None, "WXYZ");
				editor.Document.Selection.SetRange(1, 3);
			};

			Assert.AreEqual("true:XY", DispatchBrowserClipboard(id, "cut", 0, 1));
			AssertBrowserInputState(editor, id, "WZ", 1);
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
	public async Task When_BrowserInput_Clipboard_Handler_Moves_Focus(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		var next = new RichEditBox { Width = 300 };
		var panel = new StackPanel { Children = { editor, next } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "ABCD");
			next.Document.SetText(TextSetOptions.None, "next");
			next.Document.Selection.SetRange(4, 4);
			var id = await FocusBrowserInput(editor, semantic);
			editor.CuttingToClipboard += (_, _) => next.Focus(FocusState.Programmatic);

			Assert.AreEqual("true:", DispatchBrowserClipboard(id, "cut", 0, 1));
			await WindowHelper.WaitForIdle();
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("ABCD", text);
			Assert.AreSame(next, GetBrowserFocusedElement(next));
			AssertBrowserInputState(next, semantic ? GetSemanticElementId(next) : "uno-input", "next", 4);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_Composition_Commits_And_Cancels_As_One_Session(bool semantic, bool trailingInput)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);
			var started = 0;
			var changed = 0;
			var endedTexts = new List<string>();
			editor.TextCompositionStarted += (_, _) => started++;
			editor.TextCompositionChanged += (_, _) => changed++;
			editor.TextCompositionEnded += (_, _) =>
			{
				GetTextWithoutFinalEop(editor.Document, out var text);
				endedTexts.Add(text);
			};

			StartBrowserComposition(id);
			AssertBrowserInputState(editor, id, "AniB", 3);
			RunBrowserInput(id, $$"""
				input.value = 'A你B'; input.setSelectionRange(2, 2);
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '你' }));
				{{(trailingInput ? "input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: '你' }));" : "")}}
				return input.value;
				""");
			AssertBrowserInputState(editor, id, "A你B", 2);
			CollectionAssert.AreEqual(new[] { "A你B" }, endedTexts);
			Assert.AreEqual(1, started);
			Assert.IsTrue(changed > 0);

			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "AB", 1);
			StartBrowserComposition(id);
			RunBrowserInput(id, """
				input.value = 'AB'; input.setSelectionRange(1, 1);
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '' }));
				return input.value;
				""");
			AssertBrowserInputState(editor, id, "AB", 1);
			DispatchBrowserInput(id, 1, 1, "x");
			AssertBrowserInputState(editor, id, "AxB", 2);
			Assert.AreEqual(2, started);
			CollectionAssert.AreEqual(new[] { "A你B", "AB" }, endedTexts);
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
	public async Task When_BrowserInput_External_Edit_Rejects_Stale_Composition_Without_Losing_Focus(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);
			StartBrowserComposition(id);

			editor.Document.SetText(TextSetOptions.None, "XY");
			await WindowHelper.WaitForIdle();
			Assert.AreSame(editor, GetBrowserFocusedElement(editor));
			RunBrowserInput(id, """
				input.value = 'AstaleB';
				input.dispatchEvent(new CompositionEvent('compositionupdate', { bubbles: true, data: 'stale' }));
				input.value = 'AstaleB';
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'stale' }));
				input.value = 'AstaleB';
				input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: 'stale' }));
				return input.value;
				""");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("XY", text);
			Assert.AreEqual("XY", RunBrowserInput(id, "return input.value;"));
			DispatchBrowserInput(id, 2, 2, "z");
			AssertBrowserInputState(editor, id, "XYz", 3);
			Assert.AreEqual(semantic ? "0" : "1", InvokeBrowserJs("String(document.querySelectorAll('#uno-input').length)"));
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
	public async Task When_BrowserInput_Composition_Callback_Replaces_The_Document(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);
			editor.TextCompositionChanged += (_, _) => editor.Document.SetText(TextSetOptions.None, "XY");

			StartBrowserComposition(id);
			await WindowHelper.WaitForIdle();
			Assert.AreSame(editor, GetBrowserFocusedElement(editor));
			RunBrowserInput(id, """
				input.value = 'A你B'; input.setSelectionRange(2, 2);
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '你' }));
				input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: '你' }));
				return input.value;
				""");
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("XY", text);
			Assert.AreEqual("XY", RunBrowserInput(id, "return input.value;"));
			DispatchBrowserInput(id, 2, 2, "!");
			AssertBrowserInputState(editor, id, "XY!", 3);
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_Real_Blur_Is_Not_Confused_With_A_Managed_Restart(bool semantic, bool duringRestart)
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			var id = await FocusBrowserInput(editor, semantic);
			StartBrowserComposition(id);
			RunBrowserInput(id, $$"""
				const other = document.createElement('button');
				other.id = 'uno-browser-input-test-focus';
				document.body.appendChild(other);
				{{(duringRestart ? "input.addEventListener('blur', () => other.focus(), { once: true });" : "other.focus();")}}
				return 'ok';
				""");
			if (duringRestart)
			{
				editor.Document.SetText(TextSetOptions.None, "XY");
			}
			await WindowHelper.WaitForIdle();

			Assert.AreNotSame(editor, GetBrowserFocusedElement(editor));
			Assert.AreEqual("uno-browser-input-test-focus", InvokeBrowserJs("document.activeElement.id"));
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_BrowserInput_Semantic_Focus_Replaces_The_Native_Surface()
	{
		ResetAccessibilityThroughDom();
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			await FocusBrowserInput(editor, semantic: false);
			var id = await FocusBrowserInput(editor, semantic: true);

			Assert.AreEqual("0", InvokeBrowserJs("String(document.querySelectorAll('#uno-input').length)"));
			DispatchBrowserInput(id, 1, 1, "x");
			AssertBrowserInputState(editor, id, "AxB", 2);
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
	public async Task When_BrowserInput_TextBox_Keeps_Native_Clipboard_Defaults(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new TextBox { Width = 300, Text = "ABCD" };
		try
		{
			await UITestHelper.Load(editor);
			var id = await FocusBrowserInput(editor, semantic);

			Assert.AreEqual("false:", DispatchBrowserClipboard(id, "copy", 1, 3));
			Assert.AreEqual("false:", DispatchBrowserClipboard(id, "cut", 1, 3));
			Assert.AreEqual("ABCD", editor.Text, "Synthetic events do not execute the browser's default cut.");
			DispatchBrowserInput(id, 1, 3, "", "deleteByCut");
			Assert.AreEqual("AD", editor.Text);
			Assert.AreEqual("AD", RunBrowserInput(id, "return input.value;"));
			DispatchBrowserInput(id, 1, 1, "x");
			Assert.AreEqual("AxD", editor.Text);
			Assert.AreEqual("AxD", RunBrowserInput(id, "return input.value;"));
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
	public async Task When_BrowserInput_TextBox_Composition_Is_Not_Applied_As_Ordinary_Input(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new TextBox { Width = 300, Text = "AB" };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(1, 0);
			var id = await FocusBrowserInput(editor, semantic);
			var started = 0;
			var changed = 0;
			var ended = 0;
			editor.TextCompositionStarted += (_, _) => started++;
			editor.TextCompositionChanged += (_, _) => changed++;
			editor.TextCompositionEnded += (_, _) => ended++;

			StartBrowserComposition(id);
			RunBrowserInput(id, """
				input.value = 'A你B'; input.setSelectionRange(2, 2);
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '你' }));
				input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: '你' }));
				return input.value;
				""");
			Assert.AreEqual("A你B", editor.Text);
			Assert.AreEqual("A你B", RunBrowserInput(id, "return input.value;"));
			Assert.AreEqual(1, started);
			Assert.IsTrue(changed > 0);
			Assert.AreEqual(1, ended);
			DispatchBrowserInput(id, 2, 2, "x");
			Assert.AreEqual("A你xB", editor.Text);
			Assert.AreEqual("A你xB", RunBrowserInput(id, "return input.value;"));
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	private static async Task<string> FocusBrowserInput(Control editor, bool semantic)
	{
		var id = "uno-input";
		if (semantic)
		{
			editor.GetOrCreateAutomationPeer();
			InvokeBrowserJs("(function(){globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.getAssemblyExports().Uno.UI.Runtime.Skia.WebAssemblyAccessibility.EnableAccessibility();return 'ok';})()");
			await UITestHelper.WaitFor(() => SemanticElementExists(editor), timeoutMS: 5000, message: "Missing semantic text input.");
			id = GetSemanticElementId(editor);
			InvokeBrowserJs($"(function(){{document.getElementById('{id}').focus({{preventScroll:true}});return 'ok';}})()");
		}
		else
		{
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
		}
		await WindowHelper.WaitForIdle();
		Assert.AreSame(editor, GetBrowserFocusedElement(editor));
		Assert.AreEqual("true", RunBrowserInput(id, "return String(document.activeElement === input);"));
		return id;
	}

	private static string RunBrowserInput(string id, string script)
		=> InvokeBrowserJs($$"""
			(function() {
				const input = document.getElementById('{{id}}');
				if (!input || document.activeElement !== input) { throw new Error('Expected focused input: {{id}}'); }
				{{script}}
			})()
			""");

	private static object? GetBrowserFocusedElement(Control editor)
		=> FocusManager.GetFocusedElement(editor.XamlRoot ?? throw new InvalidOperationException("Editor is not attached."));

	private static string DispatchBrowserInput(string id, int start, int end, string replacement, string inputType = "insertText")
		=> RunBrowserInput(id, $$"""
			input.setRangeText({{JsonSerializer.Serialize(replacement)}}, {{start}}, {{end}}, 'end');
			input.dispatchEvent(new InputEvent('input', {
				bubbles: true, inputType: {{JsonSerializer.Serialize(inputType)}}, data: {{JsonSerializer.Serialize(replacement)}}
			}));
			return input.value;
			""");

	private static string DispatchBrowserClipboard(string id, string type, int start, int end)
		=> RunBrowserInput(id, $$"""
			input.setSelectionRange({{start}}, {{end}});
			const data = new DataTransfer();
			const ev = new ClipboardEvent({{JsonSerializer.Serialize(type)}}, { bubbles: true, cancelable: true, clipboardData: data });
			input.dispatchEvent(ev);
			return `${ev.defaultPrevented}:${data.getData('text/plain')}`;
			""");

	private static void StartBrowserComposition(string id)
		=> RunBrowserInput(id, """
			input.setSelectionRange(1, 1);
			input.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true, data: '' }));
			input.value = 'AniB'; input.setSelectionRange(3, 3);
			input.dispatchEvent(new CompositionEvent('compositionupdate', { bubbles: true, data: 'ni' }));
			input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertCompositionText', data: 'ni', isComposing: true }));
			return input.value;
			""");

	private static void AssertBrowserInputState(RichEditBox editor, string id, string expected, int selectionStart, int? selectionEnd = null)
	{
		GetTextWithoutFinalEop(editor.Document, out var text);
		Assert.AreEqual(expected, text, "Managed document");
		Assert.AreEqual(expected.Replace('\r', '\n'), RunBrowserInput(id, "return input.value;"), "DOM mirror");
		Assert.AreEqual(selectionStart, editor.Document.Selection.StartPosition, "Managed selection start");
		Assert.AreEqual(selectionEnd ?? selectionStart, editor.Document.Selection.EndPosition, "Managed selection end");
		Assert.AreEqual($"{selectionStart}|{selectionEnd ?? selectionStart}", RunBrowserInput(id, "return `${input.selectionStart}|${input.selectionEnd}`;"), "DOM selection");
	}

	private static void CleanupBrowserInput()
	{
		WindowHelper.WindowContent = null;
		InvokeBrowserJs("(function(){document.getElementById('uno-browser-input-test-focus')?.remove();return 'ok';})()");
		ResetAccessibilityThroughDom();
	}
}
