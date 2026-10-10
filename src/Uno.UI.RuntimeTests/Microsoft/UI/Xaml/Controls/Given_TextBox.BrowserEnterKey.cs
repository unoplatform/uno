#if __SKIA__
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// In a browser, Enter reaches a TextBox that does not accept returns through its keydown, the beforeinput of
/// the line break the browser sets out to insert, and its keyup, and keyboards differ in which of those they
/// send. These tests replay the browser's event sequences on the hidden native input.
/// </summary>
public partial class Given_TextBox
{
	private const string HiddenInput = "document.getElementById('uno-input')";

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Pressed()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// A key that has a code is raised by the keyboard input source. The line break the browser sets out to
		// insert for it does not raise it again.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Moves_Focus_On_Release()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var first = new TextBox();
		var second = new TextBox();
		var third = new TextBox();
		first.KeyUp += (_, e) => MoveFocusOnEnter(e.Key, second);
		second.KeyUp += (_, e) => MoveFocusOnEnter(e.Key, third);
		await UITestHelper.Load(new StackPanel { Children = { first, second, third } });
		await FocusHiddenInput(first);

		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		// One release moves focus one field on. A second one would go to the field that just took focus.
		Assert.AreNotEqual(FocusState.Unfocused, second.FocusState);
		Assert.AreEqual(FocusState.Unfocused, third.FocusState);

		static void MoveFocusOnEnter(VirtualKey key, TextBox next)
		{
			if (key == VirtualKey.Enter)
			{
				next.Focus(FocusState.Keyboard);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Is_Named_On_Release_Only()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// The shape of an Enter that commits an IME composition: its keydown is reported as the IME's, and only
		// its keyup names the key. The key is raised for the keyup, with a single release though it has a code.
		DispatchKeyDown("Process", code: "Enter", keyCode: 229);
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Is_Named_On_Release_After_Line_Break()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// The line break raised the key, its release included. The keyup has the code the keyboard input source
		// tells the key by, and is kept from it.
		DispatchKeyDown("Process", code: "Enter", keyCode: 229);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	[DataRow("Process", "KeyA", 229, DisplayName = "A key the IME takes")]
	[DataRow("Shift", "ShiftLeft", 16, DisplayName = "A modifier")]
	public async Task When_Another_Key_Goes_Down_Before_Enter_Named_On_Release_After_Line_Break_Is_Released(string otherKey, string otherCode, int otherKeyCode)
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		DispatchKeyDown("Process", code: "Enter", keyCode: 229);
		DispatchBeforeInput("insertLineBreak");
		try
		{
			// The line break raised Enter, which is still down when the next key is pressed: the keyup of Enter is
			// still not to raise it again.
			DispatchKeyDown(otherKey, code: otherCode, keyCode: otherKeyCode);
			DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
		}
		finally
		{
			// Leaves the other key released for the tests that follow.
			DispatchKeyUp(otherKey, code: otherCode, keyCode: otherKeyCode);
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	[DataRow("Enter", 13, true, DisplayName = "Enter named on its press")]
	[DataRow("Process", 229, false, DisplayName = "Enter named on its release only")]
	public async Task When_Hardware_Keyboard_Enter_Follows_Line_Break_Without_Key_Events(string keyOnPress, int keyCodeOnPress, bool lineBreakFollows)
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// No keyup ends the line break a soft keyboard sent on its own, and the keys typed after it are other keys:
		// neither keeps the next press of Enter from being raised.
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyDown("a", code: "KeyA", keyCode: 65);
		DispatchKeyUp("a", code: "KeyA", keyCode: 65);

		DispatchKeyDown(keyOnPress, code: "Enter", keyCode: keyCodeOnPress);
		if (lineBreakFollows)
		{
			DispatchBeforeInput("insertLineBreak");
		}
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up", "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Enter_Has_No_Code()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// Chrome on Android sends the action key as a press of Enter without a code for the keyboard input
		// source to tell the key by. It reaches the TextBox through the beforeinput event.
		DispatchKeyDown("Enter", code: "", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Enter_Is_Named_On_Release_Only()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// Other Android keyboards report the press as an unidentified key and only name Enter on its release.
		DispatchKeyDown("Unidentified", code: "", keyCode: 229);
		DispatchKeyUp("Enter", code: "", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Enter_Is_Named_On_Release_After_Line_Break()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// The line break raised the key already when its release comes with the name.
		DispatchKeyDown("Unidentified", code: "", keyCode: 229);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Enter_Is_Never_Named()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// A keyboard that never names the key only has the line break to tell of it, press after press.
		for (var press = 0; press < 2; press++)
		{
			DispatchKeyDown("Unidentified", code: "", keyCode: 229);
			DispatchBeforeInput("insertLineBreak");
			DispatchKeyUp("Unidentified", code: "", keyCode: 229);
		}
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up", "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Line_Break_Comes_Without_Key_Events()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		DispatchBeforeInput("insertLineBreak");
		DispatchBeforeInput("insertLineBreak");

		// Nothing of it is left for the press that follows.
		DispatchKeyDown("Unidentified", code: "", keyCode: 229);
		DispatchKeyUp("Enter", code: "", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up", "down", "up", "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Line_Break_Follows_Hardware_Keyboard_Enter()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// No line break comes for a keydown that a handler marked as handled, which this press stands for. The
		// one that comes after its release is a soft keyboard's own.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up", "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_SoftKeyboard_Line_Break_Follows_Hardware_Keyboard_Enter_That_Moved_Focus()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var first = new TextBox();
		var button = new Button { Content = "next" };
		var second = new TextBox();
		first.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				button.Focus(FocusState.Keyboard);
			}
		};
		await UITestHelper.Load(new StackPanel { Children = { first, button, second } });
		await FocusHiddenInput(first);

		// The press moves focus off the TextBox: its input is removed and never gets the release.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(HiddenInputExists(), "The hidden native input should be gone once focus is on the button.");

		await FocusHiddenInput(second);
		try
		{
			var keys = TrackEnterKey(second);

			// A line break on the input of the next TextBox is a soft keyboard's own.
			DispatchBeforeInput("insertLineBreak");
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
		}
		finally
		{
			// The release of the first press went to no input. This one leaves the key released for the tests that follow.
			DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	[DataRow(true, DisplayName = "Its release goes to the document")]
	[DataRow(false, DisplayName = "Its release is lost")]
	public async Task When_Enter_Named_On_Release_After_Line_Break_Moves_Focus_Off_The_TextBox(bool isReleased)
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = new TextBox();
		var button = new Button { Content = "Next" };
		SUT.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				button.Focus(FocusState.Keyboard);
			}
		};
		var panel = new StackPanel { Children = { SUT, button } };
		await UITestHelper.Load(panel);
		await FocusHiddenInput(SUT);

		// The Enter key events of both controls, the ones a Button handles included.
		var keys = new List<string>();
		panel.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) => AddEnterKey(e, "down")), handledEventsToo: true);
		panel.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler((_, e) => AddEnterKey(e, "up")), handledEventsToo: true);

		// The line break raises the key, its release included, and the press moves focus off the TextBox: its input
		// is gone by the keyup, which the browser sends to the document.
		DispatchKeyDown("Process", code: "Enter", keyCode: 229);
		DispatchBeforeInput("insertLineBreak");
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(HiddenInputExists(), "The hidden native input should be gone once focus is on the button.");

		if (isReleased)
		{
			DispatchKeyUpOnDocument("Enter", code: "Enter", keyCode: 13);
			await WindowHelper.WaitForIdle();
		}

		// The release was raised with the press: the control that took focus does not get it again.
		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);

		// The next press of the key, on that control, is raised in full.
		DispatchKeyDownOnDocument("Enter", code: "Enter", keyCode: 13);
		DispatchKeyUpOnDocument("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up", "down", "up" }, keys);

		void AddEnterKey(KeyRoutedEventArgs e, string name)
		{
			if (e.Key == VirtualKey.Enter)
			{
				keys.Add(name);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Enter_And_Numpad_Enter_Presses_Overlap()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// Two keys, two presses: the release of the first does not leave the second to be raised again.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyDown("Enter", code: "NumpadEnter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		DispatchKeyUp("Enter", code: "NumpadEnter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "down", "up", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	[DataRow(true, DisplayName = "A soft keyboard's, named on its release only")]
	[DataRow(false, DisplayName = "One the input only gets the release of")]
	public async Task When_Enter_Named_On_Release_Only_Follows_Hardware_Keyboard_Enter_That_Moved_Focus(bool softKeyboard)
	{
		using var _ = UITestHelper.ResetWindowContent();

		var first = new TextBox();
		var button = new Button { Content = "Next" };
		var second = new TextBox();
		first.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				button.Focus(FocusState.Keyboard);
			}
		};
		await UITestHelper.Load(new StackPanel { Children = { first, button, second } });
		await FocusHiddenInput(first);

		// The press moves focus off the TextBox: its input is removed, and the release goes to the document.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(HiddenInputExists(), "The hidden native input should be gone once focus is on the button.");
		DispatchKeyUpOnDocument("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		await FocusHiddenInput(second);
		var keys = TrackEnterKey(second);

		// The press on the first TextBox is over: an Enter the next one only learns of by its keyup is raised.
		if (softKeyboard)
		{
			DispatchKeyDown("Unidentified", code: "", keyCode: 229);
			DispatchKeyUp("Enter", code: "", keyCode: 13);
		}
		else
		{
			// The fallback of the keyup handler raises a keyup named Enter that no keydown was seen for.
			DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		}
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Is_Held()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var SUT = await LoadFocusedTextBox("abc");
		var keys = TrackEnterKey(SUT);

		// Each repeat is a keydown of the same key with its own line break: none of them is another press.
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyDown("Enter", code: "Enter", keyCode: 13);
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "down", "down", "up" }, keys);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25173")]
	public async Task When_Hardware_Keyboard_Enter_Pressed_On_Another_Control_Is_Released_On_The_TextBox()
	{
		using var _ = UITestHelper.ResetWindowContent();

		var button = new Button { Content = "Edit" };
		var SUT = new TextBox();
		// A Button handles the KeyDown of Enter itself.
		button.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				SUT.Focus(FocusState.Keyboard);
			}
		}), handledEventsToo: true);
		await UITestHelper.Load(new StackPanel { Children = { button, SUT } });
		button.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();
		var keys = TrackEnterKey(SUT);

		// The press starts on the button, whose keydown goes to the document, and moves focus to the TextBox.
		DispatchKeyDownOnDocument("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached once focus is on the TextBox.");

		// The TextBox gets the release of that press, and no press of its own for it: neither for the line break
		// the browser sets out to insert for that keydown, nor for the keyup.
		DispatchBeforeInput("insertLineBreak");
		DispatchKeyUp("Enter", code: "Enter", keyCode: 13);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "up" }, keys);
	}

	private static async Task<TextBox> LoadFocusedTextBox(string text, int? caret = null)
	{
		var textBox = new TextBox { Text = text };
		await UITestHelper.Load(textBox);
		await FocusHiddenInput(textBox);
		var position = caret ?? text.Length;
		textBox.Select(position, 0);
		SetHiddenInputValue(text, caret: position);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(position, textBox.SelectionStart);
		return textBox;
	}

	private static async Task FocusHiddenInput(Control control, FocusState focusState = FocusState.Programmatic)
	{
		control.Focus(focusState);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused control.");
	}

	private static bool HiddenInputExists()
		=> InvokeBrowserJs($"(function(){{ return {HiddenInput} ? '1' : '0'; }})()") == "1";

	private static void SetHiddenInputValue(string value, int caret)
		=> InvokeBrowserJs($"(function(){{ const input = {HiddenInput}; input.value = {JsString(value)}; input.setSelectionRange({caret}, {caret}); return ''; }})()");

	private static bool DispatchKeyDown(string key, string code, int keyCode = 0)
		=> InvokeBrowserJs($"(function(){{ const ev = new KeyboardEvent('keydown', {{ key: {JsString(key)}, code: {JsString(code)}, keyCode: {keyCode}, bubbles: true, cancelable: true }}); {HiddenInput}.dispatchEvent(ev); return ev.defaultPrevented ? '1' : '0'; }})()") == "1";

	private static void DispatchKeyUp(string key, string code, int keyCode)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}?.dispatchEvent(new KeyboardEvent('keyup', {{ key: {JsString(key)}, code: {JsString(code)}, keyCode: {keyCode}, bubbles: true, cancelable: true }})); return ''; }})()");

	// A keydown the browser sends to the document, as when no text input has focus.
	private static void DispatchKeyDownOnDocument(string key, string code, int keyCode)
		=> InvokeBrowserJs($"(function(){{ document.body.dispatchEvent(new KeyboardEvent('keydown', {{ key: {JsString(key)}, code: {JsString(code)}, keyCode: {keyCode}, bubbles: true, cancelable: true }})); return ''; }})()");

	// A keyup the browser sends to the document, as for a key whose input is gone.
	private static void DispatchKeyUpOnDocument(string key, string code, int keyCode)
		=> InvokeBrowserJs($"(function(){{ document.body.dispatchEvent(new KeyboardEvent('keyup', {{ key: {JsString(key)}, code: {JsString(code)}, keyCode: {keyCode}, bubbles: true, cancelable: true }})); return ''; }})()");

	private static void DispatchBeforeInput(string inputType)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}.dispatchEvent(new InputEvent('beforeinput', {{ inputType: '{inputType}', bubbles: true, cancelable: true }})); return ''; }})()");

	// Records the Enter key events the control sees, in order.
	private static List<string> TrackEnterKey(Control control)
	{
		var keys = new List<string>();
		control.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				keys.Add("down");
			}
		};
		control.KeyUp += (_, e) =>
		{
			if (e.Key == VirtualKey.Enter)
			{
				keys.Add("up");
			}
		};
		return keys;
	}

	private static string JsString(string value)
		=> "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
}
#endif
