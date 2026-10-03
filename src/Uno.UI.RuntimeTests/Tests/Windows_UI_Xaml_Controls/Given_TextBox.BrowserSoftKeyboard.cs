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
		var selectionChanges = new List<int>();
		SUT.SelectionChanged += (_, _) => selectionChanges.Add(SUT.SelectionStart);

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
		// One SelectionChanged per keystroke: the caret moves straight to where the keyboard put it.
		CollectionAssert.AreEqual(new[] { 1, 2, 3 }, selectionChanges);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Committed_Then_Input_Event_Follows()
	{
		var SUT = await LoadFocusedTextBox("");
		var events = TrackCompositionEvents(SUT);

		// WebKit order: compositionend is delivered with the preedit still in the input, selected, and the
		// input event that puts the committed text in its place only follows. The commit equals the preedit
		// the input already holds, so nothing is left to wait for (Blink finishes a composition whose word was
		// selected under the keyboard the same way, with no input event at all).
		DispatchComposition("compositionstart", "");
		ComposePreedit("ab", "ab", caret: 2);
		SetHiddenInputSelection(0, 2);
		DispatchComposition("compositionend", "ab");
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(SUT.IsComposing);

		DispatchInput("insertFromComposition", "ab", isComposing: false);
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("insertText", " ", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 0+2 ab", "ended 0+2 ab" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Candidate_Committed_Before_Input_Event()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// WebKit order for a candidate that differs from the preedit: compositionend comes with the preedit
		// still in the input (selected, about to be replaced), and the candidate lands with the input event
		// that follows. TextCompositionChanged and the text change precede TextCompositionEnded, which
		// sees the candidate.
		SetHiddenInputSelection(3, 8);
		DispatchComposition("compositionend", "日本");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual("ab nihon", SUT.Text);

		SetHiddenInputValue("ab 日本", caret: 5);
		DispatchInput("insertFromComposition", "日本", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab 日本", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+2 ab 日本", "ended 3+2 ab 日本" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Composition_Cancelled_Before_Input_Event()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// WebKit order for a cancelled composition: compositionend comes with the preedit still in the
		// input (selected, about to be removed), and the input event that follows restores the text.
		SetHiddenInputSelection(3, 8);
		DispatchComposition("compositionend", "");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("deleteCompositionText", "", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+0 ab ", "ended 3+0 ab " }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Preedit_Removed_Before_Candidate_Committed()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// WebKit can also remove the preedit in an input event of its own, deliver compositionend, and only
		// then put the candidate in place with another input event; the removal is reported with that insertion.
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		DispatchComposition("compositionend", "日本");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual("ab nihon", SUT.Text);

		SetHiddenInputValue("ab 日本", caret: 5);
		DispatchInput("insertFromComposition", "日本", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab 日本", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+2 ab 日本", "ended 3+2 ab 日本" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Preedit_Removed_After_Candidate_Committed()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// Or compositionend first, then the preedit removed in an input event of its own, then the candidate put
		// in place with another: the removal is part of the commit, reported with the insertion, not a keystroke
		// that ends the wait.
		SetHiddenInputSelection(3, 8);
		DispatchComposition("compositionend", "日本");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual("ab nihon", SUT.Text);

		SetHiddenInputValue("ab 日本", caret: 5);
		DispatchInput("insertFromComposition", "日本", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab 日本", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+2 ab 日本", "ended 3+2 ab 日本" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Candidate_Committed_Before_Compositionend()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// iOS order (observed on device): the preedit is removed and the candidate put in place, each with an
		// input event of its own and no compositionupdate, and compositionend only follows. The insertion's
		// data is the only word of what the composition holds, the removal is reported with it, and the
		// completion sees the candidate.
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		SetHiddenInputValue("ab 日本", caret: 5);
		DispatchInput("insertFromComposition", "日本", isComposing: true);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);
		Assert.AreEqual("ab 日本", SUT.Text);

		DispatchComposition("compositionend", "日本");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab 日本", SUT.Text);
		Assert.AreEqual(5, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+2 ab 日本", "ended 3+2 ab 日本" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Candidate_Applied_Then_Committed()
	{
		var SUT = await LoadFocusedTextBox("");
		var events = TrackCompositionEvents(SUT);

		// iOS (observed on device): a tapped candidate first replaces the preedit, then is committed by removing
		// it and putting it back with input events of WebKit's own before compositionend. Handlers see the
		// preedit, the candidate, and the completion; not the composition emptied and refilled in between.
		DispatchComposition("compositionstart", "");
		ComposePreedit("あ", "あ", caret: 1);
		ComposePreedit("亜", "亜", caret: 1);
		SetHiddenInputValue("", caret: 0);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		SetHiddenInputValue("亜", caret: 1);
		DispatchInput("insertFromComposition", "亜", isComposing: true);
		DispatchComposition("compositionend", "亜");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("亜", SUT.Text);
		Assert.AreEqual(1, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 0+1 あ", "changed 0+1 亜", "ended 0+1 亜" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Repeated_Preedit_Removed_Before_Commit()
	{
		var SUT = await LoadFocusedTextBox("aa");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "aaa", caret: 3);
		await WindowHelper.WaitForIdle();

		// The preedit repeats the text before it; removing it leaves text the preedit could pass for, so the
		// removal itself has to say the preedit is gone, where it was. Put back there by the commit, the
		// text reads as unchanged.
		SetHiddenInputValue("aa", caret: 2);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		DispatchComposition("compositionend", "a");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		SetHiddenInputValue("aaa", caret: 3);
		DispatchInput("insertFromComposition", "a", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("aaa", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 2+1 aaa", "ended 2+1 aaa" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Preedit_Removed_Before_Cancel()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		await WindowHelper.WaitForIdle();

		// The preedit removed in its own input event before a compositionend that commits nothing: the
		// text is already restored, so nothing is left to wait for.
		SetHiddenInputValue("ab ", caret: 3);
		DispatchInput("deleteCompositionText", "", isComposing: true);
		DispatchComposition("compositionend", "");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab ", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "changed 3+0 ab ", "ended 3+0 ab " }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Commit_Never_Applied_By_Browser()
	{
		var SUT = await LoadFocusedTextBox("ab ");
		var events = TrackCompositionEvents(SUT);

		// compositionend announces a commit no input event ever applies: the composition ends with the text
		// as it is at the next keystroke, whether that opens a new composition...
		DispatchComposition("compositionstart", "");
		ComposePreedit("nihon", "ab nihon", caret: 8);
		DispatchComposition("compositionend", "xyz");
		DispatchComposition("compositionstart", "");
		ComposePreedit("a", "ab nihona", caret: 9);
		DispatchComposition("compositionend", "a");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab nihona", SUT.Text);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 3+5 ab nihon", "ended 3+5 ab nihon", "changed 8+1 ab nihona", "ended 8+1 ab nihona" }, events);

		// ...or is a plain insertion.
		events.Clear();
		DispatchComposition("compositionstart", "");
		ComposePreedit("b", "ab nihonab", caret: 10);
		DispatchComposition("compositionend", "xyz");
		SetHiddenInputValue("ab nihonab!", caret: 11);
		DispatchInput("insertText", "!", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ab nihonab!", SUT.Text);
		Assert.AreEqual(11, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 9+1 ab nihonab", "ended 9+1 ab nihonab" }, events);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	public async Task When_SoftKeyboard_Commit_Prefixing_Preedit_Never_Applied()
	{
		var SUT = await LoadFocusedTextBox("");
		var events = TrackCompositionEvents(SUT);

		// The announced commit is the start of the preedit the browser then never replaces: the preedit left
		// in the input must not pass for the commit having landed when the next keystroke arrives.
		DispatchComposition("compositionstart", "");
		ComposePreedit("ab", "ab", caret: 2);
		DispatchComposition("compositionend", "a");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.IsComposing);

		SetHiddenInputValue("abx", caret: 3);
		DispatchInput("insertText", "x", isComposing: false);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("abx", SUT.Text);
		Assert.AreEqual(3, SUT.SelectionStart);
		Assert.IsFalse(SUT.IsComposing);
		CollectionAssert.AreEqual(new[] { "changed 0+2 ab", "ended 0+2 ab" }, events);
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
		var SUT = await LoadFocusedTextBox("a", caret: 0);
		var compositionRanges = TrackCompositionRanges(SUT);

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
		var SUT = await LoadFocusedTextBox("ab", caret: 1);
		var compositionRanges = TrackCompositionRanges(SUT);

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
		var events = TrackCompositionEvents(SUT);
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
		// The completion's range fits the coerced text as well.
		Assert.AreEqual("ended 0+0 ", events[^1]);
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
		await FocusHiddenInput(SUT, FocusState.Keyboard);
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
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_SoftKeyboard_Focus_Moved_By_TextCompositionChanged_Handler(bool secondAcceptsReturn)
	{
		var first = new TextBox();
		var second = new TextBox { AcceptsReturn = secondAcceptsReturn };
		await UITestHelper.Load(new StackPanel { Children = { first, second } });
		await FocusHiddenInput(first);

		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("hello", first.Text);

		// The handler of the next update moves focus to another TextBox, which gets a fresh input while a
		// composition is open; the rest of that update must not land in the new TextBox.
		first.TextCompositionChanged += (_, _) => second.Focus(FocusState.Programmatic);
		ComposePreedit("hello!", "hello!", caret: 6);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("", second.Text);
		Assert.AreEqual("hello!", first.Text);
		Assert.IsFalse(first.IsComposing);
		Assert.IsFalse(second.IsComposing);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22230")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_SoftKeyboard_Focus_Moved_By_TextCompositionEnded_Handler(bool secondAcceptsReturn)
	{
		var first = new TextBox();
		var second = new TextBox { AcceptsReturn = secondAcceptsReturn };
		await UITestHelper.Load(new StackPanel { Children = { first, second } });
		await FocusHiddenInput(first);

		// A commit the browser never applies leaves the composition open until the next compositionstart ends it.
		DispatchComposition("compositionstart", "");
		ComposePreedit("hello", "hello", caret: 5);
		DispatchComposition("compositionend", "xyz");
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(first.IsComposing);

		// The handler of that end moves focus to another TextBox: a multiline one replaces the input with a
		// textarea, one of the same kind takes the input over. Either way that composition must not be started
		// for the new TextBox.
		var started = 0;
		second.TextCompositionStarted += (_, _) => started++;
		first.TextCompositionEnded += (_, _) => second.Focus(FocusState.Programmatic);
		DispatchComposition("compositionstart", "");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("hello", first.Text);
		Assert.AreEqual("", second.Text);
		Assert.IsFalse(first.IsComposing);
		Assert.IsFalse(second.IsComposing);
		Assert.AreEqual(0, started);
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
		await FocusHiddenInput(first);

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
		await FocusHiddenInput(SUT);

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
		await FocusHiddenInput(SUT);

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
		var SUT = await LoadFocusedTextBox("abc", caret: 1);
		SUT.BeforeTextChanging += (_, e) => e.Cancel = e.NewText.Contains('x');

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
		await FocusHiddenInput(SUT);

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
		await FocusHiddenInput(SUT);
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

	// Records composition ranges along with the text as handlers see it at that point, which the range must fit in.
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

	// Records the composition events in order, with the text as handlers see it at that point.
	private static List<string> TrackCompositionEvents(TextBox textBox)
	{
		var events = new List<string>();
		textBox.TextCompositionChanged += (_, e) => events.Add($"changed {e.StartIndex}+{e.Length} {textBox.Text}");
		textBox.TextCompositionEnded += (_, e) => events.Add($"ended {e.StartIndex}+{e.Length} {textBox.Text}");
		return events;
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

	private static void SetHiddenInputSelection(int start, int end)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}.setSelectionRange({start}, {end}); return ''; }})()");

	private static void DispatchSelectionChange()
		=> InvokeBrowserJs("(function(){ document.dispatchEvent(new Event('selectionchange')); return ''; })()");

	private static void DispatchComposition(string type, string data)
		=> InvokeBrowserJs($"(function(){{ {HiddenInput}.dispatchEvent(new CompositionEvent('{type}', {{ data: {JsString(data)}, bubbles: true }})); return ''; }})()");

	// Chromium only constructs the input types it implements; WebKit's own (deleteCompositionText,
	// insertFromComposition) are set on the instance so its sequences can be replayed here.
	private static void DispatchInput(string inputType, string data, bool isComposing)
		=> InvokeBrowserJs($"(function(){{ const ev = new InputEvent('input', {{ inputType: '{inputType}', data: {JsString(data)}, isComposing: {(isComposing ? "true" : "false")}, bubbles: true }}); if (ev.inputType !== '{inputType}') {{ Object.defineProperty(ev, 'inputType', {{ value: '{inputType}' }}); }} {HiddenInput}.dispatchEvent(ev); return ''; }})()");

	// Returns whether the key was prevented, i.e. taken over by managed code instead of the browser.
	private static bool DispatchKeyDown(string key, string code)
		=> InvokeBrowserJs($"(function(){{ const ev = new KeyboardEvent('keydown', {{ key: {JsString(key)}, code: {JsString(code)}, bubbles: true, cancelable: true }}); {HiddenInput}.dispatchEvent(ev); return ev.defaultPrevented ? '1' : '0'; }})()") == "1";

	private static string JsString(string value)
		=> "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
}
#endif
