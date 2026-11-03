#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Pastes through the internal paste-with-text entry point, and cuts through the internal cut that doesn't copy, so these tests leave the clipboard alone.
// The expected values were recorded on WinUI (Windows App SDK 1.8) with an app that drives a TextBox through its own APIs, the
// WinUI recording app, unless a test says otherwise.
[TestClass]
[RunsOnUIThread]
public class Given_TextBox_Paste
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("012", 3, 0, "a\U0001F600b", "012a", DisplayName = "The pair doesn't fit")]
	[DataRow("01", 2, 0, "a\U0001F600b", "01a\U0001F600", DisplayName = "The pair fits exactly")]
	[DataRow("01", 2, 0, "\U0001F600\U0001F600\U0001F600", "01\U0001F600", DisplayName = "Several pairs, room for three code units")]
	[DataRow("01\U0001F600", 2, 2, "abc", "01abc", DisplayName = "Over a selected pair")]
	// U+1F601 is a different emoji, so the text changes; WinUI was measured pasting U+1F600 over itself.
	[DataRow("01\U0001F600", 2, 2, "\U0001F601\U0001F601", "01\U0001F601", DisplayName = "Pairs over a selected pair")]
	public async Task When_Pasted_Text_Is_Shortened_Within_A_Surrogate_Pair(
		string text, int selectionStart, int selectionLength, string pasted, string expectedText)
	{
		try
		{
			var SUT = await LoadFocused(text, maxLength: 5);
			SUT.Select(selectionStart, selectionLength);
			var events = new TextEvents(SUT);

			await Paste(SUT, pasted);

			AssertState(SUT, expectedText, expectedText.Length, 0);
			events.AssertCounts(beforeTextChanging: 1, textChanging: 1, textChanged: 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("0123", 5, 4, 0, "\U0001F600b", "0123", 4, DisplayName = "Room for half a pair")]
	[DataRow("", 1, 0, 0, "\U0001F600", "", 0, DisplayName = "MaxLength 1")]
	[DataRow("01234", 5, 4, 1, "\U0001F600", "0123", 4, DisplayName = "Over a selection, room for half a pair")]
	public async Task When_Paste_Has_Room_For_Half_A_Surrogate_Pair_Only(
		string text, int maxLength, int selectionStart, int selectionLength, string pasted, string expectedText, int expectedCaret)
	{
		try
		{
			var SUT = await LoadFocused(text, maxLength);
			SUT.Select(selectionStart, selectionLength);
			var events = new TextEvents(SUT);

			await Paste(SUT, pasted);

			// The selection is still deleted, but nothing is inserted; with nothing selected, no text event fires.
			AssertState(SUT, expectedText, expectedCaret, 0);
			var textEvents = selectionLength > 0 ? 1 : 0;
			events.AssertCounts(beforeTextChanging: textEvents, textChanging: textEvents, textChanged: textEvents);
			if (selectionLength > 0)
			{
				Assert.IsTrue(SUT.CanUndo, "deleting the selection is an undo step");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("0123", "e\u0301x", "0123e", DisplayName = "Room for a letter, not its combining mark")]
	[DataRow("012", "e\u0301x", "012e\u0301", DisplayName = "Room for a letter and its combining mark")]
	[DataRow("0", "\U0001F468\u200D\U0001F469x", "0\U0001F468\u200D", DisplayName = "Room for half a joined emoji")]
	[DataRow("01", "\U0001F468\u200D\U0001F469x", "01\U0001F468\u200D", DisplayName = "Room for less than half a joined emoji")]
	public async Task When_Pasted_Text_Is_Shortened_Within_A_Grapheme(string text, string pasted, string expectedText)
	{
		try
		{
			// WinUI shortens pasted text by code point: a letter can lose its combining mark, and a joined emoji its rest.
			var SUT = await LoadFocused(text, maxLength: 5);
			SUT.Select(text.Length, 0);

			await Paste(SUT, pasted);

			AssertState(SUT, expectedText, expectedText.Length, 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("012", "abcdef", "012ab", 1, DisplayName = "Shortened to fit")]
	[DataRow("0123", "\U0001F600b", "0123", 0, DisplayName = "Room for half a pair")]
	public async Task When_Pasting_Into_A_PasswordBox_With_MaxLength(string password, string pasted, string expectedPassword, int expectedPasswordChanged)
	{
		try
		{
			var SUT = new PasswordBox { Width = 200, Password = password, MaxLength = 5 };
			await UITestHelper.Load(SUT);
			SUT.Focus(FocusState.Programmatic);
			SUT.Core.Select(password.Length, 0);
			await WindowHelper.WaitForIdle();
			var passwordChanged = 0;
			SUT.PasswordChanged += (_, _) => passwordChanged++;

			SUT.Core.PasteFromClipboard(pasted);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedPassword, SUT.Password);
			Assert.AreEqual(expectedPasswordChanged, passwordChanged);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("a\nb", DisplayName = "LF")]
	[DataRow("a\r\nb", DisplayName = "CR LF")]
	[DataRow("a\rb", DisplayName = "CR")]
	public async Task When_Pasting_Several_Lines_Into_A_Single_Line_TextBox(string pasted)
	{
		try
		{
			var SUT = await LoadFocused("XY", maxLength: 0);
			SUT.Select(1, 0);
			var events = new TextEvents(SUT);

			await Paste(SUT, pasted);

			AssertState(SUT, "XaY", 2, 0, "the pasted text's first line goes in, and the text after the caret stays");
			events.AssertCounts(beforeTextChanging: 1, textChanging: 1, textChanged: 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Pasting_Text_That_Starts_With_A_Line_Break_Into_A_Single_Line_TextBox()
	{
		try
		{
			var SUT = await LoadFocused("012", maxLength: 0);
			SUT.Select(3, 0);
			var events = new TextEvents(SUT);

			await Paste(SUT, "\r\nabc");

			AssertState(SUT, "012", 3, 0);
			events.AssertCounts(beforeTextChanging: 0, textChanging: 0, textChanged: 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("01", "a\r\nb", "01a\rb", DisplayName = "CR LF counts as one character")]
	[DataRow("01", "a\r\nbc", "01a\rb", DisplayName = "Shortened after the line break")]
	[DataRow("01", "a\nb", "01a\rb", DisplayName = "LF")]
	[DataRow("012", "\r\n\r\n\r\n", "012\r\r", DisplayName = "Line breaks only")]
	public async Task When_Pasting_Line_Breaks_Into_A_Multiline_TextBox_With_MaxLength(string text, string pasted, string expectedText)
	{
		try
		{
			var SUT = await LoadFocused(text, maxLength: 5, acceptsReturn: true);
			SUT.Select(text.Length, 0);
			var events = new TextEvents(SUT);

			await Paste(SUT, pasted);

			AssertState(SUT, expectedText, expectedText.Length, 0);
			events.AssertCounts(beforeTextChanging: 1, textChanging: 1, textChanged: 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow("text/html", "<img>", "0123", 1, 2, DisplayName = "No text, an image")]
	[DataRow("text/plain", "ab", "0ab3", 3, 0, DisplayName = "Text")]
	[DataRow("text/plain", "", "03", 1, 0, DisplayName = "Empty text")]
	public async Task When_The_Browser_Pastes(string type, string data, string expectedText, int expectedSelectionStart, int expectedSelectionLength)
	{
		try
		{
			// WinUI was measured with an empty clipboard for the case without text.
			var SUT = await LoadFocused("0123", maxLength: 0);
			SUT.Select(1, 2);
			await UITestHelper.WaitFor(
				() => HiddenNativeTextBoxExists(), timeoutMS: 3000, message: "the hidden input should exist while the TextBox is focused");

			Assert.AreEqual("ok", InvokeBrowserJs(DispatchPaste("document.getElementById('uno-input')", type, data)));
			await WindowHelper.WaitForIdle();

			AssertState(SUT, expectedText, expectedSelectionStart, expectedSelectionLength);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_The_Semantic_Element_Pastes_No_Text()
	{
		try
		{
			// With accessibility on, the semantic element takes the paste instead of the hidden input.
			var SUT = await LoadFocused("0123", maxLength: 0);
			SUT.Select(1, 2);
			SUT.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(SUT), timeoutMS: 5000, message: "the semantic element should be created");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("ok", InvokeBrowserJs(DispatchPaste($"document.getElementById('{GetSemanticElementId(SUT)}')", "text/html", "<img>")));
			await WindowHelper.WaitForIdle();

			AssertState(SUT, "0123", 1, 2, "nothing to paste, so the selection stays");
		}
		finally
		{
			DisableAccessibility();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_The_Browser_Pastes_No_Text_Into_A_RichEditBox()
	{
		try
		{
			// Not recorded on WinUI. A RichEditBox still raises Paste when the clipboard has no text.
			var SUT = new RichEditBox { Width = 200 };
			await UITestHelper.Load(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			var paste = 0;
			SUT.Paste += (_, _) => paste++;
			await UITestHelper.WaitFor(
				() => HiddenNativeTextBoxExists(), timeoutMS: 3000, message: "the hidden input should exist while the RichEditBox is focused");

			Assert.AreEqual("ok", InvokeBrowserJs(DispatchPaste("document.getElementById('uno-input')", "text/html", "<img>")));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, paste);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// Dispatches a paste of data with the given MIME type to the element, the way the browser does.
	private static string DispatchPaste(string element, string type, string data)
		=> $$"""
			(function () {
				const element = {{element}};
				const clipboardData = new DataTransfer();
				clipboardData.setData('{{type}}', '{{data}}');
				element.dispatchEvent(new ClipboardEvent('paste', { clipboardData, bubbles: true, cancelable: true }));
				return 'ok';
			})()
			""";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_A_Cut()
	{
		try
		{
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Select(1, 2);

			await Cut(SUT);
			AssertState(SUT, "034", 1, 0);
			Assert.IsTrue(SUT.CanUndo);

			await Undo(SUT);
			AssertState(SUT, "01234", 1, 2, "Undo puts back the cut text and its selection");
			Assert.IsTrue(SUT.CanRedo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_A_Cut_Of_A_Backward_Selection()
	{
		try
		{
			// Not recorded on WinUI: the WinUI recording app can't select backward.
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Core.SelectInternal(3, -2);
			Assert.IsTrue(SUT.Core.IsBackwardSelection);

			await Cut(SUT);
			await Undo(SUT);

			AssertState(SUT, "01234", 1, 2);
			Assert.IsTrue(SUT.Core.IsBackwardSelection, "Undo puts the selection back with its direction");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Pasting_Right_After_Typing()
	{
		try
		{
			var SUT = await LoadFocused("", maxLength: 0);
			await Type(SUT, "he");

			await Paste(SUT, "X");
			AssertState(SUT, "heX", 3, 0);

			await Undo(SUT);
			AssertState(SUT, "he", 2, 0, "the paste is its own undo step");
			await Undo(SUT);
			AssertState(SUT, "", 0, 0, "then the typing");
			Assert.IsFalse(SUT.CanUndo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Cutting_Right_After_Typing()
	{
		try
		{
			var SUT = await LoadFocused("", maxLength: 0);
			await Type(SUT, "hey");
			SUT.Select(0, 1);

			await Cut(SUT);
			AssertState(SUT, "ey", 0, 0);

			await Undo(SUT);
			AssertState(SUT, "hey", 0, 1, "the cut is its own undo step");
			await Undo(SUT);
			AssertState(SUT, "", 0, 0, "then the typing");
			Assert.IsFalse(SUT.CanUndo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Cutting_Nothing_While_Typing()
	{
		try
		{
			var SUT = await LoadFocused("", maxLength: 0);
			await Type(SUT, "he");

			await Cut(SUT);
			await Type(SUT, "llo");
			Assert.AreEqual("hello", SUT.Text);

			await Undo(SUT);
			AssertState(SUT, "he", 2, 0, "cutting nothing ends the typing run but is no step of its own");
			await Undo(SUT);
			AssertState(SUT, "", 0, 0);
			Assert.IsFalse(SUT.CanUndo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Cutting_Nothing_Then_Redo_Is_Kept()
	{
		try
		{
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Select(5, 0);
			await Type(SUT, "y");
			await Undo(SUT);
			Assert.IsTrue(SUT.CanRedo);

			SUT.Select(2, 0);
			await Cut(SUT);
			AssertState(SUT, "01234", 2, 0);
			Assert.IsFalse(SUT.CanUndo);
			Assert.IsTrue(SUT.CanRedo, "cutting nothing keeps the redo history");

			await Redo(SUT);
			Assert.AreEqual("01234y", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	[DataRow("01234", 5, "ab", DisplayName = "At MaxLength")]
	[DataRow("0123", 5, "\U0001F600b", DisplayName = "Room for half a pair")]
	[DataRow("012", 0, "", DisplayName = "Empty paste, no selection")]
	[DataRow("012", 0, "\r\nabc", DisplayName = "Single-line, the first pasted line is empty")]
	public async Task When_Paste_Changes_Nothing_Then_No_Undo_Step(string text, int maxLength, string pasted)
	{
		try
		{
			var SUT = await LoadFocused(text, maxLength);
			SUT.Select(text.Length, 0);
			var events = new TextEvents(SUT);

			await Paste(SUT, pasted);

			AssertState(SUT, text, text.Length, 0);
			Assert.IsFalse(SUT.CanUndo);
			events.AssertCounts(beforeTextChanging: 0, textChanging: 0, textChanged: 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_After_A_Paste_That_Changed_Nothing()
	{
		try
		{
			var SUT = await LoadFocused("0", maxLength: 3);
			SUT.Select(1, 0);
			await Paste(SUT, "ab");
			Assert.AreEqual("0ab", SUT.Text);

			await Paste(SUT, "c");
			Assert.AreEqual("0ab", SUT.Text);

			await Undo(SUT);
			AssertState(SUT, "0", 1, 0, "one Undo reverts the last paste that changed the text");
			Assert.IsFalse(SUT.CanUndo);
			Assert.IsTrue(SUT.CanRedo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Redo_Survives_A_Paste_That_Changed_Nothing()
	{
		try
		{
			var SUT = await LoadFocused("0", maxLength: 3);
			SUT.Select(1, 0);
			await Paste(SUT, "ab");
			await Undo(SUT);
			Assert.IsTrue(SUT.CanRedo);

			SUT.Select(1, 0);
			await Paste(SUT, "");
			Assert.IsFalse(SUT.CanUndo);
			Assert.IsTrue(SUT.CanRedo, "a paste that changes nothing keeps the redo history");

			await Redo(SUT);
			Assert.AreEqual("0ab", SUT.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Typing_Then_A_Paste_That_Changes_Nothing()
	{
		try
		{
			var SUT = await LoadFocused("", maxLength: 3);
			await Type(SUT, "hey");

			await Paste(SUT, "z");
			Assert.AreEqual("hey", SUT.Text);

			await Undo(SUT);
			AssertState(SUT, "", 0, 0, "one Undo reverts the typing");
			Assert.IsFalse(SUT.CanUndo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Pasting_Empty_Text_Over_A_Selection()
	{
		try
		{
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Select(1, 2);
			var events = new TextEvents(SUT);

			await Paste(SUT, "");
			AssertState(SUT, "034", 1, 0, "the selection is deleted");
			events.AssertCounts(beforeTextChanging: 1, textChanging: 1, textChanged: 1);

			await Undo(SUT);
			AssertState(SUT, "01234", 1, 2);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Paste_Replaces_A_Selection_With_The_Same_Text()
	{
		try
		{
			// WinUI records the replacement even though the text stays the same. It also raises the text events here, so a
			// BeforeTextChanging handler could refuse it; Uno raises none, because the text doesn't change.
			var SUT = await LoadFocused("abc", maxLength: 0);
			SUT.Select(0, 1);

			await Paste(SUT, "a");
			AssertState(SUT, "abc", 1, 0);
			Assert.IsTrue(SUT.CanUndo);

			await Undo(SUT);
			AssertState(SUT, "abc", 0, 1, "Undo puts the selection back");
			Assert.IsTrue(SUT.CanRedo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_BeforeTextChanging_Cancels_A_Paste()
	{
		try
		{
			// WinUI also clears the rest of the undo history when a handler cancels an edit; Uno keeps it.
			var SUT = await LoadFocused("012", maxLength: 0);
			SUT.Select(3, 0);
			SUT.BeforeTextChanging += (_, e) => e.Cancel = true;
			var events = new TextEvents(SUT);

			await Paste(SUT, "ab");

			AssertState(SUT, "012", 3, 0);
			Assert.IsFalse(SUT.CanUndo, "a cancelled paste has nothing to undo");
			events.AssertCounts(beforeTextChanging: 1, textChanging: 0, textChanged: 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_BeforeTextChanging_Cancels_A_Cut()
	{
		try
		{
			// WinUI also clears the rest of the undo history when a handler cancels an edit; Uno keeps it.
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Select(1, 2);
			SUT.BeforeTextChanging += (_, e) => e.Cancel = true;
			var events = new TextEvents(SUT);

			await Cut(SUT);

			AssertState(SUT, "01234", 1, 2);
			Assert.IsFalse(SUT.CanUndo, "a cancelled cut has nothing to undo");
			events.AssertCounts(beforeTextChanging: 1, textChanging: 0, textChanged: 0, paste: 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_A_Paste_Shortened_To_Fit()
	{
		try
		{
			var SUT = await LoadFocused("01234", maxLength: 5);
			SUT.Select(1, 2);

			await Paste(SUT, "abcdef");
			AssertState(SUT, "0ab34", 3, 0);
			Assert.IsTrue(SUT.CanUndo);

			await Undo(SUT);
			AssertState(SUT, "01234", 1, 2, "Undo puts back the replaced text and its selection");
			Assert.IsTrue(SUT.CanRedo);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_A_Paste_Into_Text_Longer_Than_MaxLength()
	{
		try
		{
			var SUT = await LoadFocused("0123456789", maxLength: 5);
			SUT.Select(2, 3);

			await Paste(SUT, "ab");
			AssertState(SUT, "0156789", 2, 0, "the selection is deleted and nothing fits");

			await Undo(SUT);
			AssertState(SUT, "0123456789", 2, 3);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undoing_A_Paste_Over_A_Backward_Selection()
	{
		try
		{
			// Not recorded on WinUI: the WinUI recording app can't select backward.
			var SUT = await LoadFocused("01234", maxLength: 0);
			SUT.Core.SelectInternal(3, -2);

			await Paste(SUT, "x");
			AssertState(SUT, "0x34", 2, 0);

			await Undo(SUT);
			AssertState(SUT, "01234", 1, 2);
			Assert.IsTrue(SUT.Core.IsBackwardSelection, "Undo puts the selection back with its direction");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25998")]
	public async Task When_Undo_Runs_In_TextChanging_During_A_Cut()
	{
		try
		{
			// Not recorded on WinUI. An Undo that a handler runs inside a cut doesn't make the cut clear the history.
			var SUT = await LoadFocused("", maxLength: 0);
			await Type(SUT, "ab");
			SUT.Select(0, 0);
			SUT.Select(2, 0);
			await Type(SUT, "c");
			SUT.Select(0, 1);

			var undoOnce = true;
			SUT.TextChanging += (_, _) =>
			{
				if (undoOnce)
				{
					undoOnce = false;
					SUT.Undo();
				}
			};

			await Cut(SUT);

			while (SUT.CanUndo)
			{
				await Undo(SUT);
			}

			Assert.AreEqual("", SUT.Text, "the typing before the cut can still be undone");
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

	private static async Task Paste(TextBox textBox, string text)
	{
		textBox.Core.PasteFromClipboard(text);
		await WindowHelper.WaitForIdle();
	}

	private static async Task Cut(TextBox textBox)
	{
		textBox.Core.CutSelection();
		await WindowHelper.WaitForIdle();
	}

	private static async Task Type(UIElement element, string text)
	{
		foreach (var c in text)
		{
			element.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(element, VirtualKey.None, VirtualKeyModifiers.None, unicodeKey: c));
			await WindowHelper.WaitForIdle();
		}
	}

	private static async Task Undo(TextBox textBox)
	{
		textBox.Undo();
		await WindowHelper.WaitForIdle();
	}

	private static async Task Redo(TextBox textBox)
	{
		textBox.Redo();
		await WindowHelper.WaitForIdle();
	}

	private static void AssertState(TextBox textBox, string text, int selectionStart, int selectionLength, string because = "")
	{
		Assert.AreEqual(text, textBox.Text, because);
		Assert.AreEqual(selectionStart, textBox.SelectionStart, because);
		Assert.AreEqual(selectionLength, textBox.SelectionLength, because);
	}

	private sealed class TextEvents
	{
		public int BeforeTextChanging;
		public int TextChanging;
		public int TextChanged;
		public int Paste;

		public TextEvents(TextBox textBox)
		{
			textBox.BeforeTextChanging += (_, _) => BeforeTextChanging++;
			textBox.TextChanging += (_, _) => TextChanging++;
			textBox.TextChanged += (_, _) => TextChanged++;
			textBox.Paste += (_, _) => Paste++;
		}

		public void AssertCounts(int beforeTextChanging, int textChanging, int textChanged, int paste = 1)
		{
			Assert.AreEqual(beforeTextChanging, BeforeTextChanging, nameof(BeforeTextChanging));
			Assert.AreEqual(textChanging, TextChanging, nameof(TextChanging));
			Assert.AreEqual(textChanged, TextChanged, nameof(TextChanged));
			Assert.AreEqual(paste, Paste, nameof(Paste));
		}
	}
}
