#nullable enable

using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_PasswordBoxSemanticEditing
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Backward_Selection_Precedes_Semantic_Creation_Then_Direction_Is_Preserved(bool password)
	{
#if HAS_UNO
		Control control = password
			? new PasswordBox { Password = "A\U0001F600B" }
			: new TextBox { Text = "A\U0001F600B" };
		var core = ((ITextBoxHost)control).Core;
		try
		{
			core.SelectInternal(3, -2);
			await Load(control);
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("1|3|backward", Selection(control));
			Assert.IsTrue(core.IsBackwardSelection);
			if (control is PasswordBox passwordBox)
			{
				AssertPassword(passwordBox, "A\U0001F600B");
			}
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[DataRow("delete", 3, "AB", 1)]
	[DataRow("forwardDelete", 1, "AB", 1)]
	[DataRow("delete", 2, "AB", 1)]
	[DataRow("forwardDelete", 2, "AB", 1)]
	[DataRow("delete", 4, "A\U0001F600", 3)]
	[DataRow("forwardDelete", 0, "\U0001F600B", 0)]
	public async Task When_Native_Browser_Deletion_Crosses_A_Supplementary_Character(string command, int caret, string expected, int expectedCaret)
	{
#if HAS_UNO
		var passwordBox = new PasswordBox { Password = "A\U0001F600B" };
		try
		{
			await Load(passwordBox);
			EditWithBrowser(passwordBox, command, caret, caret, "forward", string.Empty);
			await UITestHelper.WaitForIdle();

			AssertPassword(passwordBox, expected);
			Assert.AreEqual($"{expectedCaret}|{expectedCaret}|forward", Selection(passwordBox));
			Assert.AreEqual(expectedCaret, passwordBox.Core.SelectionStart);
			Assert.AreEqual(0, passwordBox.Core.SelectionLength);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[DataRow(1, 2, "forward", "AXB", 2)]
	[DataRow(2, 3, "forward", "AXB", 2)]
	[DataRow(1, 3, "backward", "AXB", 2)]
	[DataRow(2, 2, "forward", "A\U0001F600XB", 4)]
	public async Task When_Native_Browser_Replacement_Uses_Whole_Underlying_Characters(int start, int end, string direction, string expected, int expectedCaret)
	{
#if HAS_UNO
		var passwordBox = new PasswordBox { Password = "A\U0001F600B" };
		try
		{
			await Load(passwordBox);
			EditWithBrowser(passwordBox, "insertText", start, end, direction, "X");
			await UITestHelper.WaitForIdle();

			AssertPassword(passwordBox, expected);
			Assert.AreEqual($"{expectedCaret}|{expectedCaret}|forward", Selection(passwordBox));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[DataRow("\u754C", 2)]
	[DataRow("\U0001F642", 3)]
	[DataRow("\U0001F600", 3)]
	public async Task When_Composition_Replaces_A_Supplementary_Character_Then_It_Commits_Once(string text, int caret)
	{
#if HAS_UNO
		var passwordBox = new PasswordBox { Password = "A\U0001F600B" };
		try
		{
			await Load(passwordBox);
			var inputText = JsonSerializer.Serialize(text);
			InvokeBrowserJs($$"""
				(function() {
					const input = document.getElementById('{{GetSemanticElementId(passwordBox)}}');
					input.focus();
					input.setSelectionRange(1, 3, 'backward');
					input.dispatchEvent(new CompositionEvent('compositionstart'));
					input.dispatchEvent(new InputEvent('beforeinput', { inputType: 'insertCompositionText', isComposing: true }));
					const inserted = {{inputText}};
					input.value = input.value.slice(0, 1) + inserted + input.value.slice(3);
					input.setSelectionRange(1 + inserted.length, 1 + inserted.length);
					input.dispatchEvent(new InputEvent('input', { inputType: 'insertCompositionText', isComposing: true }));
					input.dispatchEvent(new CompositionEvent('compositionend', { data: inserted }));
					input.dispatchEvent(new InputEvent('input', { inputType: 'insertFromComposition' }));
					return 'ok';
				})()
				""");
			await UITestHelper.WaitForIdle();

			AssertPassword(passwordBox, $"A{text}B");
			Assert.AreEqual($"{caret}|{caret}|forward", Selection(passwordBox));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	public async Task When_Masked_Edit_Remap_Has_A_Backward_Selection_Then_Direction_Is_Preserved()
	{
#if HAS_UNO
		var passwordBox = new PasswordBox { Password = "A\U0001F600B" };
		try
		{
			await Load(passwordBox);
			InvokeBrowserJs($$"""
				(function() {
					const input = document.getElementById('{{GetSemanticElementId(passwordBox)}}');
					input.focus();
					input.setSelectionRange(2, 3, 'backward');
					input.dispatchEvent(new InputEvent('beforeinput', { inputType: 'insertReplacementText' }));
					input.value = input.value.slice(0, 2) + 'XY' + input.value.slice(3);
					input.setSelectionRange(2, 4, 'backward');
					input.dispatchEvent(new InputEvent('input', { inputType: 'insertReplacementText' }));
					return 'ok';
				})()
				""");
			await UITestHelper.WaitForIdle();

			AssertPassword(passwordBox, "AXYB");
			Assert.AreEqual("1|3|backward", Selection(passwordBox));
			Assert.AreEqual(1, passwordBox.Core.SelectionStart);
			Assert.AreEqual(2, passwordBox.Core.SelectionLength);
			Assert.IsTrue(passwordBox.Core.IsBackwardSelection);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Ordinary_Text_Input_Has_A_Backward_Selection_Then_Direction_Is_Preserved(bool unchanged)
	{
#if HAS_UNO
		var textBox = new TextBox { Text = unchanged ? "AXYB" : "AB" };
		try
		{
			await Load(textBox);
			InvokeBrowserJs($$"""
				(function() {
					const input = document.getElementById('{{GetSemanticElementId(textBox)}}');
					input.value = 'AXYB';
					input.setSelectionRange(1, 3, 'backward');
					input.dispatchEvent(new InputEvent('input', { inputType: 'insertReplacementText' }));
					return 'ok';
				})()
				""");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("AXYB", textBox.Text);
			Assert.AreEqual("1|3|backward", Selection(textBox));
			Assert.IsTrue(textBox.Core.IsBackwardSelection);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

#if HAS_UNO
	private static async Task Load(FrameworkElement element)
	{
		await UITestHelper.Load(element);
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(element), timeoutMS: 5000);
	}

	private static void EditWithBrowser(PasswordBox passwordBox, string command, int start, int end, string direction, string text)
	{
		var inputType = command switch
		{
			"delete" => "deleteContentBackward",
			"forwardDelete" => "deleteContentForward",
			"insertText" => "insertText",
			_ => throw new ArgumentOutOfRangeException(nameof(command)),
		};
		var inserted = JsonSerializer.Serialize(text);
		var result = InvokeBrowserJs($$"""
			(function() {
				const input = document.getElementById('{{GetSemanticElementId(passwordBox)}}');
				input.focus();
				input.setSelectionRange({{start}}, {{end}}, '{{direction}}');
				// execCommand performs a native browser edit and emits a trusted input event,
				// but Chromium does not emit beforeinput for this programmatic command.
				input.dispatchEvent(new InputEvent('beforeinput', { bubbles: true, inputType: '{{inputType}}' }));
				let observed = '';
				input.addEventListener('input', event => {
					observed = event.inputType + '|' + event.isTrusted;
				}, { once: true, capture: true });
				const edited = document.execCommand('{{command}}', false, {{inserted}});
				return edited ? observed : 'command-failed';
			})()
			""");
		Assert.AreEqual($"{inputType}|true", result, "The test must exercise the browser's native input operation.");
	}

	private static string Selection(UIElement element)
		=> InvokeBrowserJs($"(function(){{const input=document.getElementById('{GetSemanticElementId(element)}');return input.selectionStart+'|'+input.selectionEnd+'|'+input.selectionDirection;}})()");

	private static void AssertPassword(PasswordBox passwordBox, string expected)
	{
		_ = new UTF8Encoding(false, true).GetByteCount(passwordBox.Password);
		Assert.AreEqual(expected, passwordBox.Password);
		Assert.AreEqual(new string('•', expected.Length),
			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(passwordBox)}').value"),
			"Only masked code units may be present in the semantic DOM.");
	}
#endif
}
