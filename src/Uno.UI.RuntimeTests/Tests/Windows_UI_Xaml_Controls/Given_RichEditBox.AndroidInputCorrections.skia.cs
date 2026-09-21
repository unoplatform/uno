#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_TextBox_Ime_Eligibility_Tracks_Enabled_And_ReadOnly()
	{
		var editor = new TextBox { Text = "fixed", AllowFocusWhenDisabled = true };
		var host = (IImeSessionHost)((ITextBoxHost)editor).Core;
		Assert.IsTrue(host.CanAcceptTextInput);

		editor.IsEnabled = false;
		Assert.IsFalse(host.CanAcceptTextInput);
		editor.Text = "managed";
		editor.Select(0, editor.Text.Length);
		Assert.AreEqual("managed", editor.SelectedText);
		editor.IsEnabled = true;
		editor.IsReadOnly = true;
		Assert.IsFalse(host.CanAcceptTextInput);
		editor.IsReadOnly = false;
		Assert.IsTrue(host.CanAcceptTextInput);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow("", "")]
	[DataRow("X", "")]
	[DataRow("A", "B")]
	[DataRow("A\r", "B")]
	public async Task When_AndroidInput_MaxLength_Correction_Preserves_Composing_Span(string prefix, string suffix)
	{
		var editor = new RichEditBox { Width = 240, MaxLength = prefix.Length + suffix.Length + 1 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, prefix + suffix);
			if (suffix.Length > 0)
			{
				editor.Document.GetRange(prefix.Length, prefix.Length + suffix.Length).CharacterFormat.Bold = FormatEffect.On;
			}
			editor.Document.Selection.SetRange(prefix.Length, prefix.Length);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			var endedCount = 0;
			string? textAtEnd = null;
			editor.TextCompositionEnded += (_, _) =>
			{
				endedCount++;
				textAtEnd = ((IImeSessionHost)editor).Text;
			};

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ni"));

			AssertAndroidInputText(editor, connection, prefix + "n" + suffix);
			AssertAndroidComposingSpan(connection, prefix.Length, prefix.Length + 1);
			Assert.AreEqual(prefix.Length, editor.CompositionStartIndex);
			Assert.AreEqual(1, editor.CompositionLength, "Managed composition must not include the untouched suffix.");
			Assert.IsTrue(editor.TryGetAccessibilityCompositionRange(false, out var compositionStart, out var compositionEnd));
			Assert.AreEqual(prefix.Length, compositionStart);
			Assert.AreEqual(prefix.Length + 1, compositionEnd);
			Assert.IsTrue(editor.IsComposing);
			Assert.AreEqual(0, endedCount, "Coercing preedit is not a native composition commit.");
			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "\u4f60"));
			AssertAndroidInputText(editor, connection, prefix + "\u4f60" + suffix);
			Assert.AreEqual(prefix + "\u4f60" + suffix, textAtEnd);
			Assert.AreEqual(1, endedCount);
			Assert.IsFalse(editor.IsComposing);
			if (suffix.Length > 0)
			{
				Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(
					prefix.Length + 1,
					prefix.Length + 1 + suffix.Length).CharacterFormat.Bold);
			}

			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			AssertAndroidInputText(editor, connection, prefix + suffix);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow("a", 0, "a")]
	[DataRow("aba", 1, "b")]
	[DataRow("aa", 1, "a")]
	public async Task When_AndroidInput_Rejected_Repeated_Preedit_Preserves_Original_Text(string original, int caret, string preedit)
	{
		var editor = new RichEditBox { Width = 240, MaxLength = original.Length };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, original);
			editor.Document.GetRange(0, original.Length).CharacterFormat.Bold = FormatEffect.On;
			editor.Document.Selection.SetRange(caret, caret);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			var endedCount = 0;
			editor.TextCompositionEnded += (_, _) => endedCount++;

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", preedit));

			AssertAndroidInputText(editor, connection, original);
			Assert.IsNull(InvokeAndroidMethod(connection, "GetComposingRange"), "Rejected preedit must not acquire original document text.");
			Assert.AreEqual(caret, editor.Document.Selection.StartPosition);
			Assert.AreEqual(caret, editor.Document.Selection.EndPosition);
			Assert.AreEqual(caret, editor.CompositionStartIndex);
			Assert.AreEqual(0, editor.CompositionLength);
			Assert.IsTrue(editor.IsComposing);
			Assert.AreEqual(0, endedCount);

			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "b"));

			AssertAndroidInputText(editor, connection, original);
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, original.Length).CharacterFormat.Bold);
			Assert.AreEqual(1, endedCount);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow("a", 0, "a")]
	[DataRow("aba", 1, "b")]
	[DataRow("aa", 1, "a")]
	public async Task When_AndroidInput_TextBox_Rejected_Repeated_Preedit_Preserves_Caret(string original, int caret, string preedit)
	{
		var editor = new TextBox { Width = 240, Text = original, MaxLength = original.Length };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(caret, 0);
			editor.Focus(FocusState.Programmatic);
			var core = ((ITextBoxHost)editor).Core;
			var connection = await WaitForAndroidInputConnection(core);
			var endedCount = 0;
			editor.TextCompositionEnded += (_, _) => endedCount++;

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", preedit));

			Assert.AreEqual(original, editor.Text);
			Assert.AreEqual(original, ReadAndroidProperty(connection, "Editable").ToString());
			Assert.IsNull(InvokeAndroidMethod(connection, "GetComposingRange"));
			Assert.AreEqual(caret, editor.SelectionStart);
			Assert.AreEqual(0, editor.SelectionLength);
			Assert.AreEqual(caret, core.CompositionStartIndex);
			Assert.AreEqual(0, core.CompositionLength);
			Assert.IsTrue(core.IsComposing);
			Assert.AreEqual(0, endedCount);

			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "b"));

			Assert.AreEqual(original, editor.Text);
			Assert.AreEqual(original, ReadAndroidProperty(connection, "Editable").ToString());
			Assert.AreEqual(caret, editor.SelectionStart);
			Assert.AreEqual(1, endedCount);
			Assert.IsFalse(core.IsComposing);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_TextBox_Correction_Does_Not_Hide_External_Edit()
	{
		var editor = new TextBox { Width = 240, Text = "a", MaxLength = 1 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Select(0, 0);
			editor.Focus(FocusState.Programmatic);
			var core = ((ITextBoxHost)editor).Core;
			var connection = await WaitForAndroidInputConnection(core);
			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "a"));
			Assert.AreEqual(0, core.CompositionLength);
			Assert.IsTrue(core.IsComposing);

			editor.Text = "z";

			Assert.IsFalse(core.IsComposing, "A rejected native echo must consume its platform-apply guard.");
			var replacement = await WaitForAndroidInputConnection(core, connection);
			Assert.AreEqual("z", editor.Text);
			Assert.AreEqual("z", ReadAndroidProperty(replacement, "Editable").ToString());
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "stale"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Correction_Metadata_Does_Not_Mask_External_Edit()
	{
		var editor = new RichEditBox { Width = 240, MaxLength = 3 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "AB");
			editor.Document.Selection.SetRange(1, 1);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ni"));
			AssertAndroidInputText(editor, connection, "AnB");
			Assert.AreEqual(1, editor.CompositionLength);

			editor.Document.GetRange(0, 1).Text = "Z";

			Assert.IsFalse(editor.IsComposing, "A metadata-only correction must not arm the native-apply guard.");
			var replacement = await WaitForAndroidInputConnection(editor, connection);
			AssertAndroidInputText(editor, replacement, "ZnB");
			Assert.IsFalse(ApplyAndroidInputText(connection, "CommitText", "stale"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Casing_Correction_Replaces_The_Previous_Preedit()
	{
		var editor = new RichEditBox { Width = 240, CharacterCasing = CharacterCasing.Upper };
		try
		{
			await UITestHelper.Load(editor);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			var startedCount = 0;
			var endedCount = 0;
			string? textAtEnd = null;
			editor.TextCompositionStarted += (_, _) => startedCount++;
			editor.TextCompositionEnded += (_, _) =>
			{
				endedCount++;
				textAtEnd = ((IImeSessionHost)editor).Text;
			};

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "a"));
			AssertAndroidInputText(editor, connection, "A");
			AssertAndroidComposingSpan(connection, 0, 1);
			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ab"));
			AssertAndroidInputText(editor, connection, "AB");
			AssertAndroidComposingSpan(connection, 0, 2);
			Assert.AreEqual(1, startedCount);
			Assert.AreEqual(0, endedCount);

			Assert.IsTrue(ApplyAndroidInputText(connection, "CommitText", "abc"));
			AssertAndroidInputText(editor, connection, "ABC");
			Assert.AreEqual("ABC", textAtEnd);
			Assert.AreEqual(1, endedCount);
			editor.Document.Undo();
			await WindowHelper.WaitForIdle();
			AssertAndroidInputText(editor, connection, string.Empty);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_AndroidInput_Rejected_Preedit_Does_Not_Complete_Until_Native_Finish()
	{
		var editor = new RichEditBox { Width = 240, MaxLength = 1 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "X");
			editor.Document.Selection.SetRange(1, 1);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			editor.Document.ClearUndoRedoHistory();
			var endedCount = 0;
			editor.TextCompositionEnded += (_, _) => endedCount++;

			Assert.IsTrue(ApplyAndroidInputText(connection, "SetComposingText", "ni"));

			AssertAndroidInputText(editor, connection, "X");
			Assert.IsTrue(editor.IsComposing);
			Assert.AreEqual(0, editor.CompositionLength);
			Assert.AreEqual(0, endedCount);
			Assert.AreEqual(true, InvokeAndroidMethod(connection, "FinishComposingText"));
			AssertAndroidInputText(editor, connection, "X");
			Assert.IsFalse(editor.IsComposing);
			Assert.AreEqual(1, endedCount);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DataRow("Del", false)]
	[DataRow("DpadLeft", false)]
	[DataRow("Enter", false)]
	[DataRow("Del", true)]
	public async Task When_AndroidInput_Retired_Key_Event_Does_Not_Edit_The_New_Host(string key, bool closeRetiredConnection)
	{
		var editor = new RichEditBox { Width = 240 };
		var next = new RichEditBox { Width = 240 };
		try
		{
			await UITestHelper.Load(new StackPanel { Children = { editor, next } });
			editor.Document.SetText(TextSetOptions.None, "first");
			next.Document.SetText(TextSetOptions.None, "second");
			next.Document.Selection.SetRange(6, 6);
			editor.Focus(FocusState.Programmatic);
			var connection = await WaitForAndroidInputConnection(editor);
			next.Focus(FocusState.Programmatic);
			var replacement = await WaitForAndroidInputConnection(next, connection);
			if (closeRetiredConnection)
			{
				InvokeAndroidMethod(connection, "CloseConnection");
			}
			var plugin = ReadAndroidProperty(GetAndroidRenderView(), "TextInputPlugin");
			var imm = plugin.GetType().GetField("_imm", AndroidInstanceFlags)!.GetValue(plugin);
			Assert.IsNotNull(imm);
			await WindowHelper.WaitFor(() => (bool)ReadAndroidProperty(imm, "IsAcceptingText"));
			var routedKeyCount = 0;
			next.KeyDown += (_, _) => routedKeyCount++;

			Assert.IsFalse(SendAndroidKey(connection, key));

			Assert.AreEqual("first", ((IImeSessionHost)editor).Text);
			AssertAndroidInputText(next, replacement, "second");
			Assert.AreEqual(6, next.Document.Selection.StartPosition);
			Assert.AreEqual(6, next.Document.Selection.EndPosition);
			Assert.AreEqual(0, routedKeyCount);
			Assert.IsTrue(SendAndroidKey(replacement, "Del"), "The current connection must still dispatch native key events.");
			AssertAndroidInputText(next, replacement, "secon");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static void AssertAndroidComposingSpan(object connection, int start, int end)
		=> Assert.AreEqual((start, end), (ValueTuple<int, int>)InvokeAndroidMethod(connection, "GetComposingRange")!);

	private static bool SendAndroidKey(object connection, string key)
	{
		var method = connection.GetType().GetMethod("SendKeyEvent")!;
		var keyEventType = method.GetParameters()[0].ParameterType;
		var actionType = keyEventType.Assembly.GetType("Android.Views.KeyEventActions", throwOnError: true)!;
		var keyCodeType = keyEventType.Assembly.GetType("Android.Views.Keycode", throwOnError: true)!;
		using var keyEvent = (IDisposable)(Activator.CreateInstance(
			keyEventType,
			[Enum.Parse(actionType, "Down"), Enum.Parse(keyCodeType, key)])
			?? throw new InvalidOperationException("Android key event construction failed."));
		return (bool)method.Invoke(connection, [keyEvent])!;
	}
}
