#if __SKIA__
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// Soft keyboards on Android browsers type through IME composition on the hidden native input and report
/// key events without a code. These tests replay the browser's event sequences on that input.
/// </summary>
public partial class Given_TextBox
{
	private const string HiddenInput = "document.getElementById('uno-input')";

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Committed_By_Space()
	{
		var SUT = await LoadFocusedTextBox("");

		// Chrome order: the input event carrying the committed preedit precedes compositionend, and the
		// space that committed the word is a separate plain insertion.
		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "a", caret: 1);
		ComposePreedit("ab", "ab", caret: 2);
		DispatchComposition("compositionend", "ab");
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("insertText", " ", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Committed_Then_Input_Event_Follows()
	{
		var SUT = await LoadFocusedTextBox("");
		var ended = new List<(string text, int start, int length)>();
		SUT.TextCompositionEnded += (_, e) => ended.Add((SUT.Text, e.StartIndex, e.Length));

		// Safari order: the value is updated before compositionend, but the input event carrying the
		// committed preedit only follows it. Handlers of the completion must still see the committed text.
		DispatchComposition("compositionstart", "");
		DispatchComposition("compositionupdate", "ab");
		SetHiddenInputValue("ab", caret: 2);
		DispatchComposition("compositionend", "ab");
		DispatchInput("insertCompositionText", "ab", isComposing: false);
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("insertText", " ", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(("ab", 0, 2), ended[^1]);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Extends_Existing_Text()
	{
		var SUT = await LoadFocusedTextBox("abc@");
		var compositionRanges = TrackCompositionRanges(SUT);

		// Gboard re-opens the composition on the whole token when a letter follows it.
		DispatchComposition("compositionstart", "");
		ComposePreedit("abc@d", "abc@d", caret: 5);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("abc@d", SUT.Text);
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual((0, 5, "abc@d"), compositionRanges[^1]);

		DispatchComposition("compositionend", "abc@d");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("abc@d", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Repeats_Existing_Character()
	{
		var SUT = await LoadFocusedTextBox("a");
		var compositionRanges = TrackCompositionRanges(SUT);
		SUT.Select(0, 0);
		SetHiddenInputValue("a", caret: 0);
		await WindowHelper.WaitForIdle();

		// The preedit matches the text that was already there, which must not pass for the text being in place.
		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "aa", caret: 1);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("aa", SUT.Text);
		Assert.AreEqual((0, 1, "aa"), compositionRanges[^1]);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Caret_Moved_Inside_Preedit_Repeating_Text()
	{
		var SUT = await LoadFocusedTextBox("ab");
		var compositionRanges = TrackCompositionRanges(SUT);
		SUT.Select(1, 0);
		SetHiddenInputValue("ab", caret: 1);
		await WindowHelper.WaitForIdle();

		DispatchComposition("compositionstart", "");
		ComposePreedit("b", "abb", caret: 2);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual((1, 1, "abb"), compositionRanges[^1]);

		// The IME moves its caret to the start of the preedit; the text alone can't tell which "b" is
		// the preedit, so the start found when it was inserted must be kept.
		DispatchComposition("compositionupdate", "b");
		SetHiddenInputValue("abb", caret: 1);
		DispatchInput("insertCompositionText", "b", isComposing: true);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual((1, 1, "abb"), compositionRanges[^1]);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Coerced_Shorter_Than_Its_Start()
	{
		var SUT = await LoadFocusedTextBox("ab");
		var compositionRanges = TrackCompositionRanges(SUT);
		SUT.TextChanging += (sender, _) =>
		{
			if (sender.Text.Length > 2)
			{
				sender.Text = "";
			}
		};

		DispatchComposition("compositionstart", "");
		ComposePreedit("c", "abc", caret: 3);
		await WindowHelper.WaitForIdle();

		// The composition started at 2, past the end of the coerced text: writing the coerced text back ends the
		// composition, and any range reported along the way (checked by the tracker) must fit the text.
		Assert.AreEqual("", SUT.Text);
		Assert.AreEqual("", GetHiddenInputValue());
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Typing_Raises_SelectionChanged_Once()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var selectionChanges = new List<int>();
		SUT.SelectionChanged += (_, _) => selectionChanges.Add(SUT.SelectionStart);

		DispatchComposition("compositionstart", "");
		ComposePreedit("x", "ab x", caret: 4);
		DispatchComposition("compositionend", "x");
		SetHiddenInputValue("ab x ", caret: 5);
		DispatchInput("insertText", " ", isComposing: false);
		await WindowHelper.WaitForIdle();

		// The caret moves straight to where the keyboard put it, not through the start of the text first.
		CollectionAssert.AreEqual(new[] { 4, 5 }, selectionChanges);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Same_TextBox_Refocused_During_Composition()
	{
		var SUT = new TextBox();
		await UITestHelper.Load(SUT);
		// Tab (or a focus-next helper) gave the TextBox keyboard focus.
		SUT.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");
		var ended = 0;
		SUT.TextCompositionEnded += (_, _) => ended++;

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		// A tap in the TextBox re-enters it with pointer focus, which replaces the hidden input while the
		// composition is open; the browser's compositionend for the removed input is ignored, so the TextBox
		// must be told the composition ended here.
		SUT.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(FocusState.Pointer, SUT.FocusState);
		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(1, ended);
		Assert.AreEqual("hello", SUT.Text);
		Assert.AreEqual("hello", GetHiddenInputValue());

		DispatchComposition("compositionstart", "");
		ComposePreedit("!", "hello!", caret: 6);
		DispatchComposition("compositionend", "!");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("hello!", SUT.Text);
		Assert.AreEqual(6, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(2, ended);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Input_Detached_During_Composition()
	{
		var SUT = await LoadFocusedTextBox("");
		var ended = 0;
		SUT.TextCompositionEnded += (_, _) => ended++;

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		// Accessibility hands a focused TextBox over to its semantic element by detaching the hidden input while
		// the TextBox keeps focus; the browser's compositionend for the removed input is ignored, so the TextBox
		// must be told the composition ended here.
		DetachHiddenInput();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(HiddenInputExists());
		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(1, ended);
		Assert.AreEqual("hello", SUT.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Text_Set_By_TextCompositionEnded_Handler()
	{
		var SUT = await LoadFocusedTextBox("");
		SUT.TextCompositionEnded += (_, _) =>
		{
			if (SUT.Text.Length == 0)
			{
				SUT.Text = "done";
			}
		};

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();

		// Clearing the text ends the composition; the handler's own text must be what the hidden input ends up with.
		SUT.Text = "";
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("done", SUT.Text);
		Assert.AreEqual("done", GetHiddenInputValue());
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Focus_Moved_By_TextCompositionChanged_Handler()
	{
		var first = new TextBox();
		var second = new TextBox { AcceptsReturn = true };
		await UITestHelper.Load(new StackPanel { Children = { first, second } });
		first.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("hello", first.Text);

		// A composition update that only moves the caret raises TextCompositionChanged right away; the handler
		// moves focus to a multiline TextBox, whose textarea replaces the input the update came from.
		first.TextCompositionChanged += (_, _) => second.Focus(FocusState.Programmatic);
		DispatchComposition("compositionupdate", "hello");
		SetHiddenInputValue("hello", caret: 2);
		DispatchInput("insertCompositionText", "hello", isComposing: true);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("", second.Text);
		Assert.AreEqual("hello", first.Text);
		Assert.IsFalse(second.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Ends_On_Replaced_Input()
	{
		var first = new TextBox();
		var second = new TextBox { AcceptsReturn = true };
		await UITestHelper.Load(new StackPanel { Children = { first, second } });
		first.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("hello", first.Text);

		// Focus moves to a multiline TextBox, whose textarea replaces the input; the browser then ends
		// the composition of the input it removed, which must not land in the newly focused TextBox.
		StashHiddenInput();
		second.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		DispatchCompositionOnStashedInput("compositionend", "hello");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("", second.Text);
		Assert.AreEqual("hello", first.Text);
		Assert.IsFalse(second.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Preedit_Emptied()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var compositionRanges = TrackCompositionRanges(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("x", "ab x", caret: 4);
		// The IME moves its caret away while clearing the preedit.
		ComposePreedit("", "ab ", caret: 0);
		await WindowHelper.WaitForIdle();

		// Deleting the whole preedit leaves an empty composition where it was, not at the caret or the end of the text.
		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual((3, 0, "ab "), compositionRanges[^1]);

		DispatchComposition("compositionend", "");
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(SUT.IsComposing);
		// The caret itself follows where the IME left it.
		Assert.AreEqual(0, SUT.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Multiline_Selection_Follows_Native()
	{
		var SUT = new TextBox { AcceptsReturn = true, Text = "ab\ncd" };
		await UITestHelper.Load(SUT);
		SUT.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");

		// The multiline TextBox is backed by a textarea; a caret move the keyboard makes in it must reach the TextBox.
		SetHiddenInputValue("ab\ncd", caret: 4);
		DispatchSelectionChange();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(4, SUT.SelectionStart);
		Assert.AreEqual(0, SUT.SelectionLength);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Multiline_Line_Break_During_Composition()
	{
		var SUT = new TextBox { AcceptsReturn = true };
		await UITestHelper.Load(SUT);
		SUT.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");

		DispatchComposition("compositionstart", "");
		ComposePreedit("word", "word", caret: 4);

		// The TextBox stores the line break as CR while the textarea stores LF; syncing it must not read as a
		// change to write back into the textarea, which would end the composition (and, on Android, make the
		// keyboard move the caret back to the end of the word).
		SetHiddenInputValue("word\n", caret: 5);
		DispatchInput("insertLineBreak", "", isComposing: true);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("word\r", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual("word\n", GetHiddenInputValue());
		Assert.AreEqual(5, GetHiddenInputCaret());

		DispatchComposition("compositionend", "word");
		DispatchComposition("compositionstart", "");
		ComposePreedit("x", "word\nx", caret: 6);
		DispatchComposition("compositionend", "x");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("word\rx", SUT.Text);
		Assert.AreEqual(6, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Caret_Moved_During_Composition()
	{
		var SUT = await LoadFocusedTextBox("");

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();

		// Tapping elsewhere moves the managed caret; the hidden input must follow so the next
		// characters are inserted there rather than at the end of the composition.
		SUT.Select(2, 0);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, GetHiddenInputCaret());

		// The browser then finishes the composition, and typing continues at the new caret.
		DispatchComposition("compositionend", "hello");
		SetHiddenInputValue("hexllo", caret: 3);
		DispatchInput("insertText", "x", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("hexllo", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Text_Cleared_During_Composition()
	{
		var SUT = await LoadFocusedTextBox("");

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();

		// Clearing the text (delete button, Text set from code) must reach the hidden input even
		// though the keyboard still has its composition open, or the next keystroke brings the
		// cleared text back.
		SUT.Text = "";
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("", GetHiddenInputValue());
		Assert.AreEqual(0, GetHiddenInputCaret());
		Assert.IsFalse(SUT.IsComposing);

		// The browser still reports the end of the composition the replacement cut short; nothing comes back from it.
		DispatchComposition("compositionend", "hello");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("", SUT.Text);
		Assert.AreEqual("", GetHiddenInputValue());

		// The browser fires the selectionchange for the replaced value asynchronously, possibly once the
		// keyboard is already composing the next word; it must not move the caret under that composition.
		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "a", caret: 1);
		DispatchSelectionChange();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, GetHiddenInputCaret());

		ComposePreedit("ab", "ab", caret: 2);
		DispatchComposition("compositionend", "ab");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab", SUT.Text);
		Assert.AreEqual(2, SUT.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Rejected_By_BeforeTextChanging()
	{
		var SUT = await LoadFocusedTextBox("");
		SUT.BeforeTextChanging += (_, e) => e.Cancel = e.NewText.Contains('x');

		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "a", caret: 1);
		ComposePreedit("ax", "ax", caret: 2);
		await WindowHelper.WaitForIdle();

		// The rejected character must not linger in the hidden input, or the next keystroke would bring it back.
		Assert.AreEqual("a", SUT.Text);
		Assert.AreEqual(1, SUT.SelectionStart);
		Assert.AreEqual("a", GetHiddenInputValue());
		Assert.AreEqual(1, GetHiddenInputCaret());
		Assert.IsFalse(SUT.IsComposing);

		DispatchComposition("compositionstart", "");
		ComposePreedit("b", "ab", caret: 2);
		DispatchComposition("compositionend", "b");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab", SUT.Text);
		Assert.AreEqual(2, SUT.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Input_Rejected_Mid_Text()
	{
		var SUT = await LoadFocusedTextBox("abc");
		SUT.BeforeTextChanging += (_, e) => e.Cancel = e.NewText.Contains('x');
		SUT.Select(1, 0);
		SetHiddenInputValue("abc", caret: 1);
		await WindowHelper.WaitForIdle();

		// A rejected character in the middle of the text: the caret goes back to where it was rather than staying
		// where the rejected input left it, and does so before the input can report anything else.
		SetHiddenInputValue("axbc", caret: 2);
		DispatchInput("insertText", "x", isComposing: false);

		Assert.AreEqual("abc", SUT.Text);
		Assert.AreEqual(1, SUT.SelectionStart);
		Assert.AreEqual("abc", GetHiddenInputValue());
		Assert.AreEqual(1, GetHiddenInputCaret());

		await WindowHelper.WaitForIdle();
		Assert.AreEqual(1, SUT.SelectionStart);
		Assert.AreEqual(1, GetHiddenInputCaret());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_TextBox_Is_ReadOnly()
	{
		var SUT = new TextBox { Text = "abc", IsReadOnly = true };
		await UITestHelper.Load(SUT);
		SUT.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");

		// The browser enforces read-only on the hidden input, so the soft keyboard cannot type into it.
		Assert.IsTrue(IsHiddenInputReadOnly());

		SUT.IsReadOnly = false;
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(IsHiddenInputReadOnly());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_TextBox_Made_ReadOnly_During_Composition()
	{
		var SUT = await LoadFocusedTextBox("");
		var ended = 0;
		SUT.TextCompositionEnded += (_, _) => ended++;

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		// Making the TextBox read-only mid-word reaches the hidden input; the browser then ends the composition it
		// had open, which must close the TextBox's composition too even though read-only input is not applied.
		SUT.IsReadOnly = true;
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(IsHiddenInputReadOnly());
		DispatchComposition("compositionend", "hello");
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(1, ended);
		Assert.AreEqual("hello", SUT.Text);

		SUT.IsReadOnly = false;
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(IsHiddenInputReadOnly());

		DispatchComposition("compositionstart", "");
		ComposePreedit("!", "hello!", caret: 6);
		DispatchComposition("compositionend", "!");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("hello!", SUT.Text);
		Assert.IsFalse(SUT.IsComposing);
		Assert.AreEqual(2, ended);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Backspace_Has_No_Code()
	{
		var SUT = await LoadFocusedTextBox("abc");

		// The key is left to the browser, which deletes in the hidden input and reports it through the input event.
		Assert.IsFalse(DispatchKeyDown("Backspace", code: ""), "A soft keyboard key must not be prevented.");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("abc", SUT.Text);

		SetHiddenInputValue("ab", caret: 2);
		DispatchInput("deleteContentBackward", "", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab", SUT.Text);
		Assert.AreEqual(2, SUT.SelectionStart);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Backspace_Has_No_Code_In_PasswordBox()
	{
		var SUT = new PasswordBox { Password = "abc" };
		await UITestHelper.Load(SUT);
		SUT.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused PasswordBox.");
		SetHiddenInputValue("abc", caret: 3);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(DispatchKeyDown("Backspace", code: ""), "A soft keyboard key must not be prevented.");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("abc", SUT.Password);

		SetHiddenInputValue("ab", caret: 2);
		DispatchInput("deleteContentBackward", "", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab", SUT.Password);
	}

	private static async Task<TextBox> LoadFocusedTextBox(string text)
	{
		var textBox = new TextBox { Text = text };
		await UITestHelper.Load(textBox);
		textBox.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(HiddenInputExists(), "The hidden native input should be attached to the focused TextBox.");
		textBox.Select(text.Length, 0);
		SetHiddenInputValue(text, caret: text.Length);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(text.Length, textBox.SelectionStart);
		return textBox;
	}

	// Records composition ranges along with the text handlers see at that point, which the range must fit in.
	private static List<(int start, int length, string text)> TrackCompositionRanges(TextBox textBox)
	{
		var ranges = new List<(int start, int length, string text)>();
		textBox.TextCompositionChanged += (_, e) =>
		{
			Assert.IsTrue(e.StartIndex + e.Length <= textBox.Text.Length, $"Composition range {e.StartIndex}+{e.Length} exceeds the text '{textBox.Text}'.");
			ranges.Add((e.StartIndex, e.Length, textBox.Text));
		};
		return ranges;
	}

	private static void StashHiddenInput()
		=> InvokeBrowserJs($"(function(){{ window.__unoStashedInput = {HiddenInput}; return ''; }})()");

	private static void DispatchCompositionOnStashedInput(string type, string data)
		=> InvokeBrowserJs($"(function(){{ const input = window.__unoStashedInput; window.__unoStashedInput = null; input.dispatchEvent(new CompositionEvent('{type}', {{ data: {JsString(data)}, bubbles: true }})); return ''; }})()");

	// One IME keystroke as the browser delivers it: the preedit is announced, then the value is updated and reported.
	private static void ComposePreedit(string preedit, string value, int caret)
	{
		DispatchComposition("compositionupdate", preedit);
		SetHiddenInputValue(value, caret);
		DispatchInput("insertCompositionText", preedit, isComposing: true);
	}

	private static bool HiddenInputExists()
		=> InvokeBrowserJs($"(function(){{ return {HiddenInput} ? '1' : '0'; }})()") == "1";

	private static string GetHiddenInputValue()
		=> InvokeBrowserJs($"(function(){{ return {HiddenInput}.value; }})()");

	// What DetachNativeInputPreservingFocus does: the input is removed while the TextBox keeps focus.
	private static void DetachHiddenInput()
		=> InvokeBrowserJs("(function(){ globalThis.Uno.UI.Runtime.Skia.BrowserInvisibleTextBoxViewExtension.detach(); return ''; })()");

	private static bool IsHiddenInputReadOnly()
		=> InvokeBrowserJs($"(function(){{ return {HiddenInput}.readOnly ? '1' : '0'; }})()") == "1";

	private static int GetHiddenInputCaret()
		=> int.Parse(InvokeBrowserJs($"(function(){{ return String({HiddenInput}.selectionStart); }})()"));

	private static void SetHiddenInputValue(string value, int caret)
		=> InvokeBrowserJs($"(function(){{ const input = {HiddenInput}; input.value = {JsString(value)}; input.setSelectionRange({caret}, {caret}); return ''; }})()");

	private static void DispatchSelectionChange()
		=> InvokeBrowserJs("(function(){ document.dispatchEvent(new Event('selectionchange')); return ''; })()");

	private static void DispatchComposition(string type, string data)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}.dispatchEvent(new CompositionEvent('{type}', {{ data: {JsString(data)}, bubbles: true }})); return ''; }})()");

	private static void DispatchInput(string inputType, string data, bool isComposing)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}.dispatchEvent(new InputEvent('input', {{ inputType: '{inputType}', data: {JsString(data)}, isComposing: {(isComposing ? "true" : "false")}, bubbles: true }})); return ''; }})()");

	// Returns whether the key was prevented, i.e. taken over by managed code instead of the browser.
	private static bool DispatchKeyDown(string key, string code)
		=> InvokeBrowserJs($"(function(){{ const ev = new KeyboardEvent('keydown', {{ key: {JsString(key)}, code: {JsString(code)}, bubbles: true, cancelable: true }}); {HiddenInput}.dispatchEvent(ev); return ev.defaultPrevented ? '1' : '0'; }})()") == "1";

	private static string JsString(string value)
		=> "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
}
#endif
