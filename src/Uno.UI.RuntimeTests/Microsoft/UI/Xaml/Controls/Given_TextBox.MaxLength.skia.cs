#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// How MaxLength limits user input: typing, deleting, line breaks, paste, native views, autofill and accessibility input. Undo,
// Redo, UI Automation and text set from code aren't limited the same way.
// The expected values were recorded on WinUI (Windows App SDK 1.8) with an app that drives a TextBox through its own APIs, the
// WinUI recording app, unless a test says otherwise.
public partial class Given_TextBox_MaxLength
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow(10, 0, 'x', "0123456789", 10, 0, 0, DisplayName = "At the end")]
	[DataRow(0, 3, 'y', "0123456789", 0, 3, 0, DisplayName = "Over a selection, still too long")]
	[DataRow(0, 5, 'u', "0123456789", 0, 5, 0, DisplayName = "Over a selection, one past the limit")]
	[DataRow(0, 6, 'v', "v6789", 1, 0, 1, DisplayName = "Over a selection, reaching the limit")]
	[DataRow(0, 9, 'w', "w9", 1, 0, 1, DisplayName = "Over a selection, under the limit")]
	public async Task When_Typing_Into_Text_Longer_Than_MaxLength(
		int selectionStart,
		int selectionLength,
		char typed,
		string expectedText,
		int expectedSelectionStart,
		int expectedSelectionLength,
		int expectedBeforeTextChanging)
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			var beforeTextChanging = 0;
			SUT.BeforeTextChanging += (_, _) => beforeTextChanging++;

			SUT.Select(selectionStart, selectionLength);
			await Type(SUT, typed.ToString());

			Assert.AreEqual(expectedText, SUT.Text);
			Assert.AreEqual(expectedSelectionStart, SUT.SelectionStart);
			Assert.AreEqual(expectedSelectionLength, SUT.SelectionLength);
			Assert.AreEqual(expectedBeforeTextChanging, beforeTextChanging, "BeforeTextChanging is raised only when the key press goes in");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Deleting_From_Text_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);

			SUT.Select(10, 0);
			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("012345678", SUT.Text);
			Assert.AreEqual(9, SUT.SelectionStart);

			SUT.Select(0, 0);
			await Press(SUT, VirtualKey.Delete);
			Assert.AreEqual("12345678", SUT.Text);
			Assert.AreEqual(0, SUT.SelectionStart);

			SUT.Select(1, 2);
			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("145678", SUT.Text);
			Assert.AreEqual(1, SUT.SelectionStart);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Moving_The_Caret_In_Text_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			SUT.Select(10, 0);

			await Press(SUT, VirtualKey.Left);
			Assert.AreEqual(9, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);

			// Not recorded on WinUI: the WinUI recording app can't hold Shift down.
			await Press(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);
			Assert.AreEqual(8, SUT.SelectionStart);
			Assert.AreEqual(1, SUT.SelectionLength);
			Assert.AreEqual("0123456789", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Editing_Multiline_Text_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("ab\r\ncdefghij", maxLength: 5, acceptsReturn: true);
			Assert.AreEqual("ab\rcdefghij", SUT.Text);

			SUT.Select(3, 0);
			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("abcdefghij", SUT.Text, "Backspace after the line break deletes it");

			SUT.Text = "ab\rcdefghij";
			SUT.Select(11, 0);
			await Press(SUT, VirtualKey.Enter, unicodeKey: '\r');
			Assert.AreEqual("ab\rcdefghij", SUT.Text, "Enter adds a line break, which doesn't fit");

			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("ab\rcdefghi", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Native_View_Deletes_From_Multiline_Text_Longer_Than_MaxLength()
	{
		try
		{
			// Not recorded on WinUI, which has no native view. Browsers and the Android keyboard report line breaks
			// as \n, where the TextBox stores \r.
			var SUT = await LoadFocused("ab\rcdefghij", maxLength: 5, acceptsReturn: true);

			SUT.Core.TextBoxView.UpdateTextFromNative("ab\ncdefghi");

			Assert.AreEqual("ab\rcdefghi", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow(9, "012345678x", 10, 9, DisplayName = "At the end")]
	[DataRow(3, "012x345678", 4, 3, DisplayName = "In the middle")]
	public async Task When_Native_View_Inserts_Past_MaxLength(int caret, string nativeText, int nativeCaret, int expectedCaret)
	{
		try
		{
			// Not recorded on WinUI, which has no native view. The Android and iOS hosts report the native text this way.
			var SUT = await LoadFocused("012345678", maxLength: 5);
			SUT.Select(caret, 0);
			var host = (IImeSessionHost)SUT.Core;

			host.UpdateTextFromNative(nativeText, nativeCaret, 0);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("012345678", SUT.Text, "the inserted character doesn't fit");
			Assert.AreEqual(expectedCaret, SUT.SelectionStart, "the caret of the refused native text isn't applied");

			host.UpdateTextFromNative("01234567", 8, 0);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("01234567", SUT.Text, "a deletion goes in");
			Assert.AreEqual(8, SUT.SelectionStart);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow(true, "0123456789", DisplayName = "In TextChanging")]
	[DataRow(false, "abcdefghij", DisplayName = "In SelectionChanged")]
	public async Task When_Code_Sets_Text_During_A_User_Edit(bool inTextChanging, string setText)
	{
		try
		{
			var SUT = await LoadFocused("0123", maxLength: 5);
			SUT.Select(4, 0);
			var armed = true;
			void SetOnce()
			{
				if (armed && SUT.Text == "0123z")
				{
					armed = false;
					SUT.Text = setText;
				}
			}

			if (inTextChanging)
			{
				SUT.TextChanging += (_, _) => SetOnce();
			}
			else
			{
				SUT.SelectionChanged += (_, _) => SetOnce();
			}

			await Type(SUT, "z");

			Assert.AreEqual(setText, SUT.Text, "text set from code inside a handler isn't limited");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow(10, 0, "ab", "0123456789", 10, DisplayName = "No selection, nothing fits")]
	[DataRow(0, 8, "abcdef", "abc89", 3, DisplayName = "Over a selection, cut to fit")]
	public async Task When_Pasting_Into_Text_Longer_Than_MaxLength(
		int selectionStart, int selectionLength, string pasted, string expectedText, int expectedSelectionStart)
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			SUT.Select(selectionStart, selectionLength);

			SUT.Core.PasteFromClipboard(pasted);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedText, SUT.Text);
			Assert.AreEqual(expectedSelectionStart, SUT.SelectionStart);
			Assert.AreEqual(0, SUT.SelectionLength);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Undoing_Deletion_From_Text_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			Assert.IsFalse(SUT.CanUndo, "text set from code isn't an undo step");

			SUT.Select(10, 0);
			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("012345678", SUT.Text);
			// WinUI reports CanUndo here already; Uno counts a typing run only once it ends, a separate and older difference.

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0123456789", SUT.Text);
			Assert.IsFalse(SUT.CanUndo);
			Assert.IsTrue(SUT.CanRedo);

			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("012345678", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Redoing_Typing_Past_A_Lowered_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 0);

			SUT.Select(10, 0);
			await Type(SUT, "k");
			Assert.AreEqual("0123456789k", SUT.Text);

			SUT.MaxLength = 5;

			SUT.Undo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0123456789", SUT.Text);

			SUT.Redo();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0123456789k", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Editing_After_Bound_Text_Longer_Than_MaxLength()
	{
		try
		{
			var shortItem = new BoundItem { Value = "abc" };
			var longItem = new BoundItem { Value = "0123456789" };
			var SUT = await LoadFocused("", maxLength: 5, configure: tb =>
			{
				tb.DataContext = shortItem;
				tb.SetBinding(TextBox.TextProperty, new Binding
				{
					Path = new(nameof(BoundItem.Value)),
					Mode = BindingMode.TwoWay,
					UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				});
			});

			SUT.DataContext = longItem;
			await WindowHelper.WaitForIdle();

			SUT.Select(SUT.Text.Length, 0);
			await Press(SUT, VirtualKey.Back);

			Assert.AreEqual("012345678", longItem.Value, "the edit applies to the current item's text");
			Assert.AreEqual("abc", shortItem.Value);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Typing_Into_Password_Longer_Than_MaxLength()
	{
		try
		{
			// Password before MaxLength, the order XAML applies them in when Password comes first.
			var SUT = new PasswordBox { Width = 200, Password = "0123456789", MaxLength = 5 };
			await UITestHelper.Load(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await Press(SUT, VirtualKey.End);
			await Type(SUT, "x");
			Assert.AreEqual("0123456789", SUT.Password, "typing doesn't fit");

			await Press(SUT, VirtualKey.Back);
			Assert.AreEqual("012345678", SUT.Password, "Backspace deletes");

			SUT.Password = "0123";
			await Press(SUT, VirtualKey.End);
			await Type(SUT, "yz");
			Assert.AreEqual("0123y", SUT.Password, "typing stops at MaxLength");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Deleting_Words_From_Text_Longer_Than_MaxLength()
	{
		try
		{
			// Not recorded on WinUI: the WinUI recording app can't hold Ctrl down.
			var SUT = await LoadFocused("abcdef ghij", maxLength: 5);
			SUT.Select(11, 0);
			await Press(SUT, VirtualKey.Back, VirtualKeyModifiers.Control);
			Assert.AreEqual("abcdef ", SUT.Text, "Ctrl+Backspace deletes the last word");

			SUT.Text = "abcd efghij";
			SUT.Select(0, 0);
			await Press(SUT, VirtualKey.Delete, VirtualKeyModifiers.Control);
			Assert.AreEqual("efghij", SUT.Text, "Ctrl+Delete deletes the first word and the space after it");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Clear_Button_Clears_Text_Longer_Than_MaxLength()
	{
		try
		{
			// Not recorded on WinUI: the WinUI recording app doesn't reach the template's delete button.
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			var button = (Button)SUT.FindVisualChildByName("DeleteButton");

			button.Command.Execute(null);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow("0123456789", "01234", DisplayName = "Cut to fit")]
	[DataRow("0123\U0001F600", "0123", DisplayName = "A surrogate pair left out whole")]
	[DataRow("012", "012", DisplayName = "Fits")]
	public async Task When_Autofilled(string value, string expectedText)
	{
		try
		{
			// Not recorded on WinUI, which has no autofill service. Android's own text fields cut an autofilled value to their
			// maximum length.
			var SUT = new TextBox { Width = 200, MaxLength = 5 };
			var passwordBox = new PasswordBox { Width = 200, MaxLength = 5 };
			await UITestHelper.Load(new StackPanel { Children = { SUT, passwordBox } }, x => x.IsLoaded);

			SUT.Core.ApplyAutofill(value);
			passwordBox.Core.ApplyAutofill(value);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedText, SUT.Text);
			Assert.AreEqual(expectedText, passwordBox.Password);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[DataRow(true, DisplayName = "Read-only")]
	[DataRow(false, DisplayName = "Disabled")]
	public async Task When_Autofilled_While_ReadOnly_Or_Disabled(bool readOnly)
	{
		try
		{
			// Not recorded on WinUI, which has no autofill service. The user can't edit the text, so neither can autofill.
			var SUT = new TextBox { Width = 200, Text = "abc" };
			if (readOnly)
			{
				SUT.IsReadOnly = true;
			}
			else
			{
				SUT.IsEnabled = false;
			}

			await UITestHelper.Load(SUT, x => x.IsLoaded);

			SUT.Core.ApplyAutofill("xyz");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("abc", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Automation_Sets_A_Value_Longer_Than_MaxLength()
	{
		try
		{
			// On WinUI, the UI Automation Value pattern refuses such a value (E_FAIL) and keeps the text, and applies a value
			// that fits like text set from code. Uno's automation peer stands in for that pattern on the other hosts.
			var SUT = new TextBox { Width = 200, MaxLength = 5, Text = "ab" };
			var passwordBox = new PasswordBox { Width = 200, MaxLength = 5, Password = "ab" };
			await UITestHelper.Load(new StackPanel { Children = { SUT, passwordBox } }, x => x.IsLoaded);
			var textBoxValue = (IValueProvider)SUT.GetOrCreateAutomationPeer()!.GetPattern(PatternInterface.Value)!;
			var passwordBoxValue = (IValueProvider)passwordBox.GetOrCreateAutomationPeer()!.GetPattern(PatternInterface.Value)!;

			Assert.ThrowsExactly<InvalidOperationException>(() => textBoxValue.SetValue("0123456789"));
			Assert.ThrowsExactly<InvalidOperationException>(() => passwordBoxValue.SetValue("0123456789"));
			Assert.AreEqual("ab", SUT.Text);
			Assert.AreEqual("ab", passwordBox.Password);

			textBoxValue.SetValue("xyz");
			passwordBoxValue.SetValue("xyz");
			Assert.AreEqual("xyz", SUT.Text);
			Assert.AreEqual("xyz", passwordBox.Password);
			Assert.IsFalse(SUT.CanUndo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Accessibility_Input_Edits_Text_Longer_Than_MaxLength()
	{
		try
		{
			// Not recorded on WinUI. The browser's semantic text box and a macOS selected-text replacement edit the text this way.
			var SUT = await LoadFocused("0123456789", maxLength: 5);

			Assert.IsFalse(SUT.Core.ApplyAccessibilityTextInput("01234x56789", 6, 6), "an insertion doesn't fit");
			Assert.AreEqual("0123456789", SUT.Text);

			Assert.IsTrue(SUT.Core.ApplyAccessibilityTextInput("012346789", 5, 5), "a deletion goes in");
			Assert.AreEqual("012346789", SUT.Text);
			Assert.AreEqual(5, SUT.SelectionStart);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	public async Task When_Code_Set_During_A_User_Edit_Is_Cancelled()
	{
		try
		{
			// Not recorded on WinUI. Cancelling the text a handler sets from code doesn't cancel the user's own edit.
			var SUT = await LoadFocused("0123", maxLength: 5);
			var armed = true;
			SUT.TextChanging += (_, _) =>
			{
				if (armed && SUT.Text == "0123z")
				{
					armed = false;
					SUT.Text = "abc";
				}
			};
			SUT.BeforeTextChanging += (_, e) => e.Cancel = e.NewText == "abc";

			Assert.IsTrue(SUT.Core.ApplyAccessibilityTextInput("0123z", 5, 5), "the user's edit went in");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("0123z", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25997")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Browser_Input_Is_Refused_By_MaxLength()
	{
		try
		{
			// Not recorded on WinUI, which has no browser input. The browser reports its hidden input's text and caret.
			var SUT = await LoadFocused("012345678", maxLength: 5);
			SUT.Select(3, 0);
			await UITestHelper.WaitFor(
				() => HiddenNativeTextBoxExists(), timeoutMS: 3000, message: "the hidden input should exist while the TextBox is focused");

			var input = """
				(function () {
					const input = document.getElementById('uno-input');
					input.value = '012x345678';
					input.setSelectionRange(4, 4);
					input.dispatchEvent(new Event('input', { bubbles: true }));
					return 'ok';
				})()
				""";
			Assert.AreEqual("ok", InvokeBrowserJs(input));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("012345678", SUT.Text, "the inserted character doesn't fit");
			Assert.AreEqual(3, SUT.SelectionStart, "the caret of the refused input isn't applied");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task<TextBox> LoadFocused(string text, int maxLength, bool acceptsReturn = false, Action<TextBox>? configure = null)
	{
		// Text before MaxLength, the order XAML applies them in when Text comes first.
		var SUT = new TextBox { Width = 200, AcceptsReturn = acceptsReturn, Text = text, MaxLength = maxLength };
		configure?.Invoke(SUT);
		await UITestHelper.Load(SUT);

		SUT.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		return SUT;
	}

	private static async Task Type(UIElement element, string text)
	{
		foreach (var c in text)
		{
			element.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(element, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			await WindowHelper.WaitForIdle();
		}
	}

	private static async Task Press(UIElement element, VirtualKey key, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None, char? unicodeKey = null)
	{
		element.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(element, key, modifiers, unicodeKey: unicodeKey));
		await WindowHelper.WaitForIdle();
	}
}
