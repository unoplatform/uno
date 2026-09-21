#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using Uno.UI.DevTools.Input;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaTvOS)]
	[DataRow(VirtualKey.X)]
	[DataRow(VirtualKey.C)]
	[DataRow(VirtualKey.V)]
	[DataRow(VirtualKey.A)]
	[DataRow(VirtualKey.Z)]
	[DataRow(VirtualKey.Y)]
	[DataRow(VirtualKey.B)]
	[DataRow(VirtualKey.I)]
	[DataRow(VirtualKey.U)]
	public async Task When_InputPolicy_AltGr_Types_Without_Invoking_Accelerators(VirtualKey key)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.Selection.SetRange(1, 2);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			editor.Document.ClearUndoRedoHistory();
			var commands = 0;
			editor.CuttingToClipboard += (_, args) => { commands++; args.Handled = true; };
			editor.CopyingToClipboard += (_, args) => { commands++; args.Handled = true; };
			editor.Paste += (_, args) => { commands++; args.Handled = true; };

			editor.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(
				editor, key, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, unicodeKey: '\u017a'));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("a\u017ac", text);
			Assert.AreEqual(0, commands);
			Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(1, 2).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(1, 2).CharacterFormat.Italic);
			Assert.AreEqual(UnderlineType.None, editor.Document.GetRange(1, 2).CharacterFormat.Underline);
			editor.Document.Undo();
			GetTextWithoutFinalEop(editor.Document, out text);
			Assert.AreEqual("abc", text);
			Assert.IsFalse(editor.Document.CanUndo());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(VirtualKey.C, VirtualKeyModifiers.Control, 1)]
	[DataRow(VirtualKey.C, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, 0)]
	[DataRow(VirtualKey.X, VirtualKeyModifiers.Control, 1)]
	[DataRow(VirtualKey.X, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, 0)]
	[DataRow(VirtualKey.V, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, 1)]
	[DataRow(VirtualKey.Insert, VirtualKeyModifiers.Control, 1)]
	[DataRow(VirtualKey.Insert, VirtualKeyModifiers.Shift, 1)]
	[DataRow(VirtualKey.Insert, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, 0)]
	[DataRow(VirtualKey.Insert, VirtualKeyModifiers.Shift | VirtualKeyModifiers.Menu, 0)]
	public async Task When_InputPolicy_Clipboard_Modifiers_Match_EditKey_Predicates(VirtualKey key, VirtualKeyModifiers modifiers, int expectedCommands)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.Selection.SetRange(0, 3);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var commands = 0;
			editor.CuttingToClipboard += (_, args) => { commands++; args.Handled = true; };
			editor.CopyingToClipboard += (_, args) => { commands++; args.Handled = true; };
			editor.Paste += (_, args) => { commands++; args.Handled = true; };

			RaiseKey(editor, key, modifiers);

			Assert.AreEqual(expectedCommands, commands);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abc", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false, false, 1)]
	[DataRow(true, false, 0)]
	[DataRow(false, true, 0)]
	public async Task When_InputPolicy_ShiftDelete_Uses_Cancellable_Cut(bool readOnly, bool collapsed, int expectedCuts)
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.IsReadOnly = readOnly;
			editor.Document.Selection.SetRange(0, collapsed ? 0 : 3);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var cuts = 0;
			editor.CuttingToClipboard += (_, args) => { cuts++; args.Handled = true; };

			var args = RaiseKeyForResult(editor, VirtualKey.Delete, VirtualKeyModifiers.Shift);

			Assert.IsTrue(args.Handled);
			Assert.AreEqual(expectedCuts, cuts);
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("abc", text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_InputPolicy_PageKeys_Move_Caret_And_Extend_Selection(bool up, bool extend)
	{
		var editor = new RichEditBox { Width = 260, Height = 90, TextWrapping = TextWrapping.Wrap };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, string.Join('\r', Enumerable.Repeat("one two three", 30)));
			const int anchor = 15 * 14 + 1;
			editor.Document.Selection.SetRange(anchor, anchor);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			var args = RaiseKeyForResult(editor, up ? VirtualKey.PageUp : VirtualKey.PageDown,
				extend ? VirtualKeyModifiers.Shift : VirtualKeyModifiers.None);

			var selection = editor.Document.Selection;
			var caret = editor.IsSelectionBackwardForTesting ? selection.StartPosition : selection.EndPosition;
			Assert.IsTrue(args.Handled);
			Assert.IsTrue(up ? caret < anchor : caret > anchor);
			if (extend)
			{
				Assert.AreEqual(anchor, up ? selection.EndPosition : selection.StartPosition);
				Assert.AreEqual(up, editor.IsSelectionBackwardForTesting);
			}
			else
			{
				Assert.AreEqual(selection.StartPosition, selection.EndPosition);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("Border")]
	[DataRow("Grid")]
	public async Task When_InputPolicy_PageKeys_Use_Custom_ContentHost_Viewport(string hostType)
	{
		var editor = new RichEditBox { Width = 260, Height = 90, Template = CreateTemplateParityTemplate(hostType) };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, string.Join('\r', Enumerable.Repeat("one two three", 30)));
			editor.Document.Selection.SetRange(1, 1);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			var args = RaiseKeyForResult(editor, VirtualKey.PageDown, VirtualKeyModifiers.Shift);

			Assert.IsTrue(args.Handled);
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			Assert.IsTrue(editor.Document.Selection.EndPosition > 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_PageKeys_At_Boundary_Are_Unhandled(bool up)
	{
		var editor = new RichEditBox { Width = 260, Height = 90 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			var caret = up ? 0 : 3;
			editor.Document.Selection.SetRange(caret, caret);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			var args = RaiseKeyForResult(editor, up ? VirtualKey.PageUp : VirtualKey.PageDown);

			Assert.IsFalse(args.Handled, "Paging is handled only when the caret actually moves.");
			Assert.AreEqual(caret, editor.Document.Selection.StartPosition);
			Assert.AreEqual(caret, editor.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_First_Touch_Places_Caret_Then_Selects_Word(bool hadSelection)
	{
		var other = new Button { Content = "Other focus" };
		var editor = new RichEditBox { Width = 400, Height = 100, Margin = new Thickness(50) };
		var panel = new StackPanel { Children = { other, editor } };
		try
		{
			await UITestHelper.Load(panel);
			editor.Document.SetText(TextSetOptions.None, "Hello world");
			editor.Document.Selection.SetRange(0, hadSelection ? 5 : 0);
			Assert.IsTrue(other.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var flyout = editor.SelectionFlyout;
			Assert.IsNotNull(flyout);
			var opened = 0;
			flyout.Opened += (_, _) => opened++;
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Input injection unavailable.");
			using var finger = injector.GetFinger();

			finger.Press(GetTextPoint(editor, 8));
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(editor.Document.Selection.StartPosition, editor.Document.Selection.EndPosition);
			Assert.IsTrue(editor.Document.Selection.StartPosition >= 7);
			Assert.AreEqual(0, opened);

			finger.Press(GetTextPoint(editor, 8));
			finger.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(6, editor.Document.Selection.StartPosition);
			Assert.AreEqual(11, editor.Document.Selection.EndPosition);
			Assert.IsTrue(opened > 0);
		}
		finally
		{
			editor.SelectionFlyout?.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(VirtualKey.A, VirtualKeyModifiers.None)]
	[DataRow(VirtualKey.Back, VirtualKeyModifiers.None)]
	[DataRow(VirtualKey.Delete, VirtualKeyModifiers.None)]
	[DataRow(VirtualKey.X, VirtualKeyModifiers.Control)]
	[DataRow(VirtualKey.B, VirtualKeyModifiers.Control)]
	public async Task When_InputPolicy_Disabled_Focused_Editor_Rejects_Mutations(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		var editor = new RichEditBox { Width = 300, IsEnabled = false, AllowFocusWhenDisabled = true };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "fixed");
			editor.Document.Selection.SetRange(0, 5);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			RaiseKey(editor, key, modifiers, key == VirtualKey.A ? 'a' : '\0');
			Assert.IsFalse(editor.TryUpdateTextFromNative("native", 6, 0));
			editor.SafeRaiseEvent(UIElement.CharacterReceivedEvent,
				new CharacterReceivedRoutedEventArgs(editor, 'x', new CorePhysicalKeyStatus { IsKeyReleased = true }));

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("fixed", text);
			Assert.AreEqual(FormatEffect.Off, editor.Document.GetRange(0, 5).CharacterFormat.Bold);
			Assert.IsFalse(editor.IsCaretRenderedForTesting);
			editor.Document.GetRange(0, 1).Text = "F";
			GetTextWithoutFinalEop(editor.Document, out text);
			Assert.AreEqual("Fixed", text, "Disabling user editing must not disable the programmatic TOM.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InputPolicy_Enabling_Focused_Editor_Initializes_Editing(bool readOnly)
	{
		var fake = new FakeImeTextBoxExtension();
		using var scope = RichEditBox.SetImeExtensionForTesting(fake);
		var editor = new RichEditBox { Width = 300, IsEnabled = false, AllowFocusWhenDisabled = true };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.IsReadOnly = readOnly;
			editor.Document.Selection.SetRange(1, 1);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			Assert.AreNotSame(editor, ImeSessionCoordinator.ActiveHost);
			Assert.IsFalse(editor.IsCaretRenderedForTesting);

			editor.IsEnabled = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(!readOnly, ReferenceEquals(editor, ImeSessionCoordinator.ActiveHost));
			Assert.AreEqual(readOnly ? RichEditBox.RichEditCaretDisplayMode.ThumblessCaretHidden : RichEditBox.RichEditCaretDisplayMode.ThumblessCaretShowing, editor.CaretMode);
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
			editor.IsEnabled = false;
			await WindowHelper.WaitForIdle();
			Assert.AreNotSame(editor, ImeSessionCoordinator.ActiveHost);
			Assert.IsFalse(editor.IsCaretRenderedForTesting);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_Standalone_CharacterReceived_Uses_Typing_Policy()
	{
		var editor = new RichEditBox { Width = 300, CharacterCasing = CharacterCasing.Upper, MaxLength = 3 };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "ab");
			editor.Document.Selection.SetRange(1, 1);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var received = 0;
			editor.CharacterReceived += (_, _) => received++;
			var args = new CharacterReceivedRoutedEventArgs(editor, '\u0161',
				new CorePhysicalKeyStatus { IsKeyReleased = true, RepeatCount = 1 });

			editor.SafeRaiseEvent(UIElement.CharacterReceivedEvent, args);

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("a\u0160b", text);
			Assert.AreEqual(2, editor.Document.Selection.StartPosition);
			Assert.AreEqual(1, received, "WinUI still delivers CharacterReceived to ordinary application handlers.");
			Assert.IsFalse(args.Handled);
			editor.SafeRaiseEvent(UIElement.CharacterReceivedEvent,
				new CharacterReceivedRoutedEventArgs(editor, 'x', new CorePhysicalKeyStatus { IsKeyReleased = true }));
			GetTextWithoutFinalEop(editor.Document, out text);
			Assert.AreEqual("a\u0160b", text, "The standalone path must observe MaxLength.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_InputPolicy_KeyDown_CharacterReceived_And_Native_Echo_Insert_Once()
	{
		var editor = new RichEditBox { Width = 300 };
		try
		{
			await UITestHelper.Load(editor);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var received = 0;
			editor.CharacterReceived += (_, _) => received++;
			var keyboard = WindowHelper.XamlRoot.VisualTree.ContentRoot.InputManager.Keyboard;

			keyboard.OnKeyTestingOnly(new KeyEventArgs("test", VirtualKey.A, VirtualKeyModifiers.None,
				new CorePhysicalKeyStatus(), unicodeKey: 'a'), true);
			editor.UpdateTextFromNative("a", 1, 0);

			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("a", text);
			Assert.AreEqual(1, received);
			Assert.AreEqual(1, editor.Document.Selection.StartPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(VirtualKey.Right, true)]
	[DataRow(VirtualKey.Escape, true)]
	[DataRow(VirtualKey.Enter, false)]
	public async Task When_InputPolicy_Eligible_Keyboard_Input_Dismisses_SelectionFlyout(VirtualKey key, bool shouldClose)
	{
		var editor = new RichEditBox { Width = 300, AcceptsReturn = false, TextWrapping = TextWrapping.Wrap };
		try
		{
			await UITestHelper.Load(editor);
			editor.Document.SetText(TextSetOptions.None, "abc");
			editor.Document.Selection.SetRange(0, 3);
			Assert.IsTrue(editor.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			var flyout = editor.SelectionFlyout;
			Assert.IsNotNull(flyout);
			TextControlFlyoutHelper.ShowAt(flyout, editor, new Point(20, 20), default,
				FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(flyout.IsOpen);
			Assert.AreNotEqual(FocusState.Unfocused, editor.FocusState);

			RaiseKey(editor, key);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(!shouldClose, flyout.IsOpen);
		}
		finally
		{
			editor.SelectionFlyout?.Hide();
			WindowHelper.WindowContent = null;
		}
	}
}
