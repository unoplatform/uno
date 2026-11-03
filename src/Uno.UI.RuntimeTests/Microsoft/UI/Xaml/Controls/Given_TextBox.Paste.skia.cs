#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Pastes through the internal paste-with-text entry point, so these tests leave the clipboard alone.
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
