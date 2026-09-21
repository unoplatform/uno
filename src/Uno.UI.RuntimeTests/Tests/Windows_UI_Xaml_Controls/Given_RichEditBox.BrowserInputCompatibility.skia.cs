#nullable enable

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
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
	public async Task When_BrowserInput_TextBox_Selection_Changes_Are_Managed(bool semantic)
	{
		ResetAccessibilityThroughDom();
		var editor = new TextBox { Width = 300, Text = "ABCD" };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(4, 0);
			var id = await FocusBrowserInput(editor, semantic);
			RunBrowserInput(id, $$"""
				input.setSelectionRange(1, 3, 'backward');
				{{(semantic ? "input.dispatchEvent(new Event('select'));" : "document.dispatchEvent(new Event('selectionchange'));")}}
				return 'ok';
				""");

			Assert.AreEqual(1, editor.SelectionStart);
			Assert.AreEqual(2, editor.SelectionLength);
			Assert.AreEqual("BC", editor.SelectedText);
			editor.SelectedText = "x";
			await WindowHelper.WaitForIdle();
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
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_BrowserInput_TextBox_Backward_Selection_And_Paste_Are_Synchronized(bool semantic, bool selectionEventFirst)
	{
		ResetAccessibilityThroughDom();
		var editor = new TextBox { Width = 300, Text = "ABCD" };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(4, 0);
			var id = await FocusBrowserInput(editor, semantic);

			Assert.AreEqual("backward|true|AxD", RunBrowserInput(id, $$"""
				input.setSelectionRange(1, 3, 'backward');
				{{(selectionEventFirst ? (semantic ? "input.dispatchEvent(new Event('select'));" : "document.dispatchEvent(new Event('selectionchange'));") : "")}}
				const direction = input.selectionDirection;
				const data = new DataTransfer();
				data.setData('text/plain', 'x');
				const ev = new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: data });
				input.dispatchEvent(ev);
				return `${direction}|${ev.defaultPrevented}|${input.value}`;
				"""));
			Assert.AreEqual("AxD", editor.Text);
			Assert.AreEqual(2, editor.SelectionStart);
			Assert.AreEqual(0, editor.SelectionLength);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("AxD", RunBrowserInput(id, "return input.value;"));
		}
		finally
		{
			CleanupBrowserInput();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(false, "insertFromComposition")]
	[DataRow(false, "insertText")]
	[DataRow(true, "insertFromComposition")]
	[DataRow(true, "insertText")]
	public async Task When_BrowserInput_Stale_Composition_Input_Arrives_In_A_Later_Task(bool semantic, string inputType)
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

			RunBrowserInput(id, """
				input.value = 'AniB'; input.setSelectionRange(3, 3);
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'ni' }));
				return input.value;
				""");
			await WindowHelper.WaitForIdle();
			RunBrowserInput(id, $$"""
				input.value = 'AniB'; input.setSelectionRange(3, 3);
				input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: {{JsonSerializer.Serialize(inputType)}}, data: 'ni' }));
				return input.value;
				""");

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("XY", text);
			Assert.AreEqual("XY", RunBrowserInput(id, "return input.value;"));
			Assert.AreSame(editor, GetBrowserFocusedElement(editor));
			DispatchBrowserInput(id, 2, 2, "ni");
			AssertBrowserInputState(editor, id, "XYni", 4);
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
	public async Task When_BrowserInput_No_Composition_Echo_Does_Not_Drop_The_Same_Characters(bool semantic)
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
			RunBrowserInput(id, """
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'ni' }));
				return input.value;
				""");
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "AniB", 3);

			DispatchBrowserInput(id, 3, 3, "ni");
			AssertBrowserInputState(editor, id, "AniniB", 5);
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
	public async Task When_BrowserInput_Composition_Final_Input_Order_Preserves_Commit_Cancel_And_New_Typing(bool semantic, bool inputBeforeEnd)
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
			var ended = 0;
			editor.TextCompositionStarted += (_, _) => started++;
			editor.TextCompositionEnded += (_, _) => ended++;
			StartBrowserComposition(id);
			RunBrowserInput(id, $$"""
				input.value = 'A你B'; input.setSelectionRange(2, 2);
				{{(inputBeforeEnd ? "input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '你' }));" : "")}}
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '你' }));
				return input.value;
				""");
			await WindowHelper.WaitForIdle();
			if (!inputBeforeEnd)
			{
				RunBrowserInput(id, """
					input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '你' }));
					return input.value;
					""");
			}
			AssertBrowserInputState(editor, id, "A你B", 2);
			Assert.AreEqual(1, started);
			Assert.AreEqual(1, ended);
			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			AssertBrowserInputState(editor, id, "AB", 1);

			StartBrowserComposition(id);
			RunBrowserInput(id, $$"""
				input.value = 'AB'; input.setSelectionRange(1, 1);
				{{(inputBeforeEnd ? "input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '' }));" : "")}}
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '' }));
				return input.value;
				""");
			await WindowHelper.WaitForIdle();
			if (!inputBeforeEnd)
			{
				RunBrowserInput(id, """
					input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '' }));
					return input.value;
					""");
			}
			AssertBrowserInputState(editor, id, "AB", 1);
			Assert.AreEqual(2, started);
			Assert.AreEqual(2, ended);
			DispatchBrowserInput(id, 1, 1, "z");
			AssertBrowserInputState(editor, id, "AzB", 2);
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
	public async Task When_BrowserInput_PasswordBox_Keeps_Native_Composition_Input(bool semantic, bool finalInputAfterEnd)
	{
		ResetAccessibilityThroughDom();
		var editor = new PasswordBox { Width = 300, Password = "AB" };
		try
		{
			await UITestHelper.Load(editor);
			var id = await FocusBrowserInput(editor, semantic);
			Assert.AreEqual("password", RunBrowserInput(id, "return input.type;"));
			Assert.IsNull(ImeSessionCoordinator.ActiveHost, "PasswordBox must not enter the managed IME coordinator.");
			StartBrowserComposition(id);
			Assert.AreEqual("AniB", editor.Password);
			Assert.IsNull(ImeSessionCoordinator.ActiveHost);

			RunBrowserInput(id, $$"""
				input.value = 'A你B'; input.setSelectionRange(2, 2);
				{{(!finalInputAfterEnd ? "input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '你' }));" : "")}}
				input.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: '你' }));
				{{(finalInputAfterEnd ? "input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertFromComposition', data: '你' }));" : "")}}
				return input.value;
				""");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("A你B", editor.Password);
			Assert.AreEqual("A你B", RunBrowserInput(id, "return input.value;"));
			Assert.IsNull(ImeSessionCoordinator.ActiveHost);
			DispatchBrowserInput(id, 1, 2, "x");
			Assert.AreEqual("AxB", editor.Password);
			Assert.AreEqual("AxB", RunBrowserInput(id, "return input.value;"));
		}
		finally
		{
			CleanupBrowserInput();
		}
	}
}
