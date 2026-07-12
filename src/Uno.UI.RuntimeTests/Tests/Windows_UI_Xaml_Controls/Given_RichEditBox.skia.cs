using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Default_Properties_Match_WinUI()
		{
			var SUT = new RichEditBox();

			Assert.IsTrue(SUT.AcceptsReturn);
			Assert.IsTrue(SUT.IsSpellCheckEnabled);
			Assert.IsTrue(SUT.IsTextPredictionEnabled);
			Assert.AreEqual(TextAlignment.Left, SUT.TextAlignment);
			Assert.AreEqual(TextAlignment.Left, SUT.HorizontalTextAlignment);
			Assert.AreEqual(TextReadingOrder.DetectFromContent, SUT.TextReadingOrder);
			Assert.AreEqual(TextWrapping.Wrap, SUT.TextWrapping);
			Assert.ThrowsExactly<ArgumentException>(() => SUT.MaxLength = -1);
			Assert.ThrowsExactly<ArgumentException>(() => SUT.TextWrapping = TextWrapping.WrapWholeWords);
		}

		[TestMethod]
		public async Task When_Text_View_Properties_Change_After_Load()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
				var displayBlock = contentElement?.Content as TextBlock;
				Assert.IsNotNull(displayBlock);
				Assert.AreEqual(TextWrapping.Wrap, displayBlock.TextWrapping);
				Assert.AreEqual(TextAlignment.Left, displayBlock.TextAlignment);

				SUT.TextWrapping = TextWrapping.NoWrap;
				SUT.HorizontalTextAlignment = TextAlignment.Right;
				var selectionBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
				SUT.SelectionHighlightColor = selectionBrush;

				Assert.AreEqual(TextWrapping.NoWrap, displayBlock.TextWrapping);
				Assert.AreEqual(TextAlignment.Right, SUT.TextAlignment);
				Assert.AreEqual(TextAlignment.Right, displayBlock.TextAlignment);
				Assert.AreSame(selectionBrush, displayBlock.SelectionHighlightColor);

				SUT.TextAlignment = TextAlignment.Center;
				Assert.AreEqual(TextAlignment.Center, SUT.HorizontalTextAlignment);
				Assert.AreEqual(TextAlignment.Center, displayBlock.TextAlignment);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Description_Changes_After_Load()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Description = "Description";
				await WindowHelper.WaitForIdle();

				var presenter = SUT.FindFirstChild<ContentPresenter>(x => x.Name == "DescriptionPresenter");
				Assert.IsNotNull(presenter);
				Assert.AreEqual(Visibility.Visible, presenter.Visibility);

				SUT.Description = null;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(Visibility.Collapsed, presenter.Visibility);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_AcceptsReturn_False_Ignores_Enter()
		{
			var SUT = new RichEditBox { AcceptsReturn = false };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				await TypeAsync(SUT, "ab");
				RaiseKey(SUT, VirtualKey.Enter, unicodeKey: '\r');
				await WindowHelper.WaitForIdle();

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("ab", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_TextChanging_Precedes_Render_And_TextChanged_Is_Async()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
				var displayBlock = contentElement?.Content as TextBlock;
				Assert.IsNotNull(displayBlock);

				var textChangingRaised = false;
				var textChangedRaised = false;
				var setTextReturned = false;
				SUT.TextChanging += (_, _) =>
				{
					textChangingRaised = true;
					Assert.AreEqual(string.Empty, displayBlock.Text);
				};
				SUT.TextChanged += (_, _) =>
				{
					textChangedRaised = true;
					Assert.IsTrue(setTextReturned);
					Assert.AreEqual("updated", displayBlock.Text);
				};

				SUT.Document.SetText(TextSetOptions.None, "updated");
				setTextReturned = true;

				Assert.IsTrue(textChangingRaised);
				Assert.IsFalse(textChangedRaised);
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(textChangedRaised);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Pointer_Click_Places_Caret()
		{
			if (OperatingSystem.IsBrowser())
			{
				// Coordinate-based hit-testing depends on the default font, which differs on Wasm Skia.
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.Selection.StartPosition > 0, $"Caret should move into the text, was {SUT.Document.Selection.StartPosition}.");
			Assert.AreEqual(SUT.Document.Selection.StartPosition, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_Pointer_Drag_Selects_Text()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.MoveBy(40, 0);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(
				SUT.Document.Selection.EndPosition > SUT.Document.Selection.StartPosition,
				$"Drag should create a non-empty selection, was [{SUT.Document.Selection.StartPosition}, {SUT.Document.Selection.EndPosition}].");
		}

		[TestMethod]
		public async Task When_Shift_Click_Extends_Selection()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 220 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world hello");
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var caret = SUT.Document.Selection.StartPosition;

			mouse.MoveBy(40, 0);
			mouse.Press(VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(caret, SUT.Document.Selection.StartPosition);
			Assert.IsTrue(
				SUT.Document.Selection.EndPosition > caret,
				$"Shift+click should extend selection past the caret {caret}, was {SUT.Document.Selection.EndPosition}.");
		}

		[TestMethod]
		public async Task When_Copy_Puts_Selection_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CopySelectionToClipboard();
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var clipboardText = await content.GetTextAsync();
			Assert.AreEqual("Hello", clipboardText);
		}

		[TestMethod]
		public async Task When_Cut_Removes_Selection_And_Copies()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 6);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CutSelectionToClipboard();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(0, SUT.Document.Selection.EndPosition);

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		public async Task When_Paste_Inserts_At_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XY");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "AXYB";
			});

			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_MaxLength_Blocks_Typing_Beyond_Limit()
		{
			var SUT = new RichEditBox();
			SUT.MaxLength = 3;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcdef");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_MaxLength_Interactive_Typing_Does_Not_Split_Surrogate_Pair()
		{
			var SUT = new RichEditBox { MaxLength = 1 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "\U0001F600");
			SUT.Document.GetText(TextGetOptions.None, out var rejected);
			Assert.AreEqual(string.Empty, rejected);

			SUT.MaxLength = 2;
			await TypeAsync(SUT, "\U0001F600");
			SUT.Document.GetText(TextGetOptions.None, out var accepted);
			Assert.AreEqual("\U0001F600", accepted);
		}

		[TestMethod]
		public async Task When_Keyboard_Editing_Does_Not_Split_Text_Elements()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "\U0001F600");
			RaiseKey(SUT, VirtualKey.Left);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			RaiseKey(SUT, VirtualKey.Right);
			Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
			RaiseKey(SUT, VirtualKey.Back);
			SUT.Document.GetText(TextGetOptions.None, out var afterBackspace);
			Assert.AreEqual(string.Empty, afterBackspace);

			await TypeAsync(SUT, "\U0001F600X");
			RaiseKey(SUT, VirtualKey.Home);
			RaiseKey(SUT, VirtualKey.Delete);
			SUT.Document.GetText(TextGetOptions.None, out var afterDelete);
			Assert.AreEqual("X", afterDelete);
		}

		[TestMethod]
		public async Task When_Keyboard_Delete_From_Inside_Surrogate_Removes_Whole_Pair()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "\U0001F600X");
			SUT.Document.Selection.SetRange(1, 1);
			RaiseKey(SUT, VirtualKey.Back);
			SUT.Document.GetText(TextGetOptions.None, out var afterBackspace);
			Assert.AreEqual("X", afterBackspace);

			SUT.Document.SetText(TextSetOptions.None, "\U0001F600X");
			SUT.Document.Selection.SetRange(1, 1);
			RaiseKey(SUT, VirtualKey.Delete);
			SUT.Document.GetText(TextGetOptions.None, out var afterDelete);
			Assert.AreEqual("X", afterDelete);
		}

		[TestMethod]
		public async Task When_MaxLength_Typing_Over_Selection_Replaces()
		{
			var SUT = new RichEditBox();
			SUT.MaxLength = 3;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			// Select "ab" via the keyboard so the interactive selection is what typing replaces.
			RaiseKey(SUT, VirtualKey.Home);
			RaiseKey(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift);
			RaiseKey(SUT, VirtualKey.Right, VirtualKeyModifiers.Shift);
			await WindowHelper.WaitForIdle();

			// Replacing a non-empty selection frees room, so the character is accepted even at the limit.
			await TypeAsync(SUT, "X");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Xc", text);
		}

		[TestMethod]
		public async Task When_MaxLength_Clamps_Paste()
		{
			var SUT = new RichEditBox();
			SUT.MaxLength = 5;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			SUT.Document.Selection.SetRange(3, 3);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XYZ12");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();

			// Only two characters fit before MaxLength (5) is reached.
			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "abcXY";
			});
		}

		[TestMethod]
		public async Task When_CharacterCasing_Upper_Uppercases_Typing()
		{
			var SUT = new RichEditBox();
			SUT.CharacterCasing = CharacterCasing.Upper;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ABC", text);
		}

		[TestMethod]
		public async Task When_CharacterCasing_Lower_Lowercases_Typing()
		{
			var SUT = new RichEditBox();
			SUT.CharacterCasing = CharacterCasing.Lower;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "ABC");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_CharacterCasing_Follows_Current_Value_Per_Character()
		{
			// Mirrors WinUI RichEditBoxTests.cs:236 RichEditBoxCharacterCasingTest: casing applies to the
			// newly typed character using the value in effect at type time; existing text is not re-cased.
			var SUT = new RichEditBox();
			SUT.CharacterCasing = CharacterCasing.Upper;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "a");
			SUT.Document.GetText(TextGetOptions.None, out var afterUpper);
			Assert.AreEqual("A", afterUpper);

			SUT.CharacterCasing = CharacterCasing.Lower;
			await TypeAsync(SUT, "B");
			SUT.Document.GetText(TextGetOptions.None, out var afterLower);
			Assert.AreEqual("Ab", afterLower);

			SUT.CharacterCasing = CharacterCasing.Normal;
			await TypeAsync(SUT, "aB");
			SUT.Document.GetText(TextGetOptions.None, out var afterNormal);
			Assert.AreEqual("AbaB", afterNormal);
		}

		[TestMethod]
		public async Task When_CharacterCasing_Upper_Uppercases_Paste()
		{
			var SUT = new RichEditBox();
			SUT.CharacterCasing = CharacterCasing.Upper;
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.Selection.SetRange(0, 0);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("Test String");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.PasteFromClipboard();

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "TEST STRING";
			});
		}

		[TestMethod]
		public async Task When_Caret_PendingBold_Applies_To_Next_Typed_Text()
		{
			// Mirrors WinUI RichEditBoxTOMTests.cpp SelectionFormat (829): setting Bold=On at a collapsed
			// caret in an empty doc, then typing "12", makes both typed characters bold.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(0, 0);
			SUT.Document.Selection.CharacterFormat.Bold = FormatEffect.On;

			await TypeAsync(SUT, "12");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("12", text);
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold, "First char should be bold.");
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(1, 2).CharacterFormat.Bold, "Second char should inherit bold.");
		}

		[TestMethod]
		public async Task When_Caret_PendingFormat_Accumulates_Onto_Inherited()
		{
			// Mirrors WinUI RichEditBoxTOMTests.cpp SelectionFormat (829-870): Bold=On, type "12" (bold,
			// black); then set foreground Red at the caret and type "34" — "34" is bold+red while "12"
			// stays bold+black, proving the new caret format accumulates onto the inherited bold.
			var red = Windows.UI.Color.FromArgb(255, 255, 0, 0);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(0, 0);
			SUT.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
			await TypeAsync(SUT, "12");

			SUT.Document.Selection.CharacterFormat.ForegroundColor = red;
			await TypeAsync(SUT, "34");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("1234", text);

			var first = SUT.Document.GetRange(0, 2).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, first.Bold, "\"12\" should be bold.");
			Assert.AreNotEqual(red, first.ForegroundColor, "\"12\" should stay the default foreground.");

			var second = SUT.Document.GetRange(2, 4).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, second.Bold, "\"34\" should remain bold via accumulation.");
			Assert.AreEqual(red, second.ForegroundColor, "\"34\" should be red.");
		}

		[TestMethod]
		public async Task When_CtrlB_At_Caret_Bolds_Next_Typed_Text()
		{
			// Ctrl+B at a collapsed caret establishes a pending bold applied to subsequently typed text.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "x");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("x", text);
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold, "Ctrl+B should bold the next typed char.");
		}

		[TestMethod]
		public async Task When_Caret_Move_Clears_Pending_Format()
		{
			// Moving the caret away from a pending insertion-point format discards it, so text typed after
			// the move does not pick up the format.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "ab");

			// Caret is at 2; establish a pending bold there, then move the caret left before typing.
			SUT.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
			RaiseKey(SUT, VirtualKey.Left);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "x");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("axb", text);
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 2).CharacterFormat.Bold, "Pending bold should clear once the caret moves.");
		}

		[TestMethod]
		public async Task When_Ctrl_C_Ctrl_V_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.End);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "HelloHello";
			});
		}

		[TestMethod]
		public async Task When_Cut_Is_NoOp_When_ReadOnly()
		{
			var SUT = new RichEditBox { IsReadOnly = true };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CutSelectionToClipboard();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello world", text);
		}

		[TestMethod]
		public async Task When_Programmatic_SetRange_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 7);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Positions_Update_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.StartPosition = 3;
			SUT.Document.Selection.EndPosition = 8;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Collapse_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 7);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.Collapse(true);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Programmatic_Selection_While_Unfocused_Applies_On_Focus()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			// Setting the selection while unfocused must not throw; it is picked up on focus.
			SUT.Document.Selection.SetRange(4, 9);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Expand_Word_Selects_Word_At_Caret()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(2, 2);
			var added = range.Expand(TextRangeUnit.Word);

			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
			Assert.AreEqual(6, added);
		}

		[TestMethod]
		public async Task When_Expand_Word_Spans_Multiple_Words()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(2, 8);
			range.Expand(TextRangeUnit.Word);

			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
		}

		[TestMethod]
		public async Task When_StartOf_Word_Moves_To_Word_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(8, 8);
			var moved = range.StartOf(TextRangeUnit.Word, false);

			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
			Assert.AreEqual(-2, moved);
		}

		[TestMethod]
		public async Task When_EndOf_Word_Moves_To_Word_End()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(8, 8);
			var moved = range.EndOf(TextRangeUnit.Word, false);

			Assert.AreEqual(12, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
			Assert.AreEqual(4, moved);
		}

		[TestMethod]
		public async Task When_Move_Word_Forward_Steps_Word_Starts()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Word, 2);

			Assert.AreEqual(12, range.StartPosition);
			Assert.AreEqual(12, range.EndPosition);
			Assert.AreEqual(2, moved);
		}

		[TestMethod]
		public async Task When_Move_Word_Backward_Steps_Word_Starts()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			var range = SUT.Document.GetRange(12, 12);
			var moved = range.Move(TextRangeUnit.Word, -1);

			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(-1, moved);
		}

		[TestMethod]
		public async Task When_GetIndex_Word_Returns_One_Based_Index()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

			Assert.AreEqual(1, SUT.Document.GetRange(2, 2).GetIndex(TextRangeUnit.Word));
			Assert.AreEqual(2, SUT.Document.GetRange(8, 8).GetIndex(TextRangeUnit.Word));
			Assert.AreEqual(3, SUT.Document.GetRange(13, 13).GetIndex(TextRangeUnit.Word));
		}

		[TestMethod]
		public async Task When_SetIndex_Without_Extend_Collapses_At_Unit_Start()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

				var range = SUT.Document.GetRange(2, 9);
				range.SetIndex(TextRangeUnit.Word, 2, false);

				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetIndex_With_Extend_Moves_Only_End()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

				var range = SUT.Document.GetRange(2, 3);
				range.SetIndex(TextRangeUnit.Word, 2, true);

				Assert.AreEqual(2, range.StartPosition);
				Assert.AreEqual(12, range.EndPosition);

				// SetIndex moves EndPosition only. TOM's endpoint invariant collapses the range
				// when that new end precedes the current start.
				range.SetRange(13, 14);
				range.SetIndex(TextRangeUnit.Word, 1, true);
				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetIndex_Supports_Line_And_End_Character_Index()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(0, 7);
				range.SetIndex(TextRangeUnit.Line, 1, true);
				Assert.AreEqual(0, range.StartPosition);
				Assert.AreEqual(3, range.EndPosition, "The indexed line includes its hard paragraph break.");

				range.SetIndex(TextRangeUnit.Line, 2, false);
				Assert.AreEqual(3, range.StartPosition);
				Assert.AreEqual(3, range.EndPosition);

				range.SetIndex(TextRangeUnit.Line, -1, false);
				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);

				range.SetIndex(TextRangeUnit.Character, -1, false);
				Assert.AreEqual(8, range.StartPosition);
				Assert.AreEqual(9, range.GetIndex(TextRangeUnit.Character));

				range.SetRange(8, 8);
				Assert.AreEqual(9, range.GetIndex(TextRangeUnit.Character));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetIndex_Models_Final_Story_Units()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var empty = SUT.Document.GetRange(0, 0);
				Assert.AreEqual(1, empty.GetIndex(TextRangeUnit.Character));
				Assert.AreEqual(1, empty.GetIndex(TextRangeUnit.Word));
				Assert.AreEqual(1, empty.GetIndex(TextRangeUnit.Paragraph));

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\r");
				var range = SUT.Document.GetRange(0, 0);
				range.SetIndex(TextRangeUnit.Word, -1, false);
				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(5, range.GetIndex(TextRangeUnit.Word));

				range.SetIndex(TextRangeUnit.Paragraph, -1, false);
				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(3, range.GetIndex(TextRangeUnit.Paragraph));

				range.SetRange(2, 2);
				range.SetIndex(TextRangeUnit.Story, -1, true);
				Assert.AreEqual(2, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetIndex_Rejects_Invalid_Indexes_Without_Moving()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "one two");

				var range = SUT.Document.GetRange(1, 4);
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Word, 0, false));
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Word, 4, false));
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Word, -4, false));
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Character, int.MaxValue, false));
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Character, int.MinValue, false));
				Assert.ThrowsExactly<ArgumentException>(() => range.SetIndex(TextRangeUnit.Story, 2, false));

				Assert.AreEqual(1, range.StartPosition);
				Assert.AreEqual(4, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Expand_Paragraph_Selects_Paragraph()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");

			var range = SUT.Document.GetRange(4, 4);
			range.Expand(TextRangeUnit.Paragraph);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Move_Paragraph_Forward_Steps_Paragraphs()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Paragraph, 1);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(1, moved);
		}

		[TestMethod]
		public async Task When_Programmatic_Expand_Word_Updates_Caret_While_Focused()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello World");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 2);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.Expand(TextRangeUnit.Word);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, SUT.SelectionStartForTesting);
			Assert.AreEqual(6, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_StartOf_Line_Moves_To_Line_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.StartOf(TextRangeUnit.Line, false);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition);
		}

		[TestMethod]
		public async Task When_EndOf_Line_Moves_To_Line_End_Before_Paragraph_Mark()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.EndOf(TextRangeUnit.Line, false);

			// The visual line end stops before the trailing carriage return.
			Assert.AreEqual(5, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Expand_Line_Selects_Whole_Line()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.Expand(TextRangeUnit.Line);

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
		}

		[TestMethod]
		public async Task When_GetIndex_Line_Returns_One_Based_Line_Number()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.Document.GetRange(0, 0).GetIndex(TextRangeUnit.Line));
			Assert.AreEqual(2, SUT.Document.GetRange(4, 4).GetIndex(TextRangeUnit.Line));
			Assert.AreEqual(3, SUT.Document.GetRange(7, 7).GetIndex(TextRangeUnit.Line));
		}

		[TestMethod]
		public async Task When_Range_Delete_Line_Removes_Line()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			// Delete(Line, 1) from the start of the second line removes "bb\r" and returns one unit.
			var caret = SUT.Document.GetRange(3, 3);
			var removed = caret.Delete(TextRangeUnit.Line, 1);

			Assert.AreEqual(1, removed);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("aa\rcc", text);
		}

		[TestMethod]
		public async Task When_Range_MoveEnd_Line_Extends_By_Line()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			await WindowHelper.WaitForIdle();

			// MoveEnd(Line, 1) extends the end edge to the start of the next visual line.
			var range = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(1, range.MoveEnd(TextRangeUnit.Line, 1));
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Selection_HomeKey_Line_Moves_Caret_To_Line_Start()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(4, 4);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.HomeKey(TextRangeUnit.Line, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Selection_EndKey_Line_Moves_Caret_To_Line_End()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(3, 3);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.EndKey(TextRangeUnit.Line, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(5, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
		}

		[TestMethod]
		public async Task When_Selection_MoveDown_Moves_Caret_To_Next_Line()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// Caret at the start of the first line (column 0).
			SUT.Document.Selection.SetRange(0, 0);
			await WindowHelper.WaitForIdle();

			var moved = SUT.Document.Selection.MoveDown(TextRangeUnit.Line, 1, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, moved);
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
		}

		[TestMethod]
		public async Task When_Selection_MoveUp_Moves_Caret_To_Previous_Line()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// Caret at the start of the third line (column 0).
			SUT.Document.Selection.SetRange(6, 6);
			await WindowHelper.WaitForIdle();

			var moved = SUT.Document.Selection.MoveUp(TextRangeUnit.Line, 1, false);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, moved);
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
		}

		[TestMethod]
		public async Task When_UndoGroup_Coalesces_Multiple_Edits_Into_One_Undo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "start");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(5, 5);
			SUT.Document.Selection.TypeText("A");
			SUT.Document.Selection.TypeText("B");
			SUT.Document.EndUndoGroup();

			SUT.Document.GetText(TextGetOptions.None, out var afterGroup);
			Assert.AreEqual("startAB", afterGroup);

			// A single undo reverts the whole group (both A and B), not just the last edit.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			// The pre-group SetText remains a separate undo entry.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterSecondUndo);
			Assert.AreEqual("", afterSecondUndo);
		}

		[TestMethod]
		public async Task When_UndoGroup_Redo_Reapplies_Whole_Group()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "start");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(5, 5);
			SUT.Document.Selection.TypeText("A");
			SUT.Document.Selection.TypeText("B");
			SUT.Document.EndUndoGroup();

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			Assert.IsTrue(SUT.Document.CanRedo());
			SUT.Document.Redo();
			SUT.Document.GetText(TextGetOptions.None, out var afterRedo);
			Assert.AreEqual("startAB", afterRedo);
		}

		[TestMethod]
		public async Task When_Nested_UndoGroup_Coalesces_Into_One_Undo()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "x");

			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Document.Selection.TypeText("1");
			SUT.Document.BeginUndoGroup();
			SUT.Document.Selection.TypeText("2");
			SUT.Document.EndUndoGroup();
			SUT.Document.Selection.TypeText("3");
			SUT.Document.EndUndoGroup();

			SUT.Document.GetText(TextGetOptions.None, out var afterGroup);
			Assert.AreEqual("x123", afterGroup);

			// The outermost group is the only undo boundary; one undo reverts 1, 2 and 3.
			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("x", afterUndo);
		}

		[TestMethod]
		public async Task When_EndUndoGroup_Without_Begin_Is_NoOp()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "a");

			// Unbalanced EndUndoGroup must not throw or corrupt the undo state.
			SUT.Document.EndUndoGroup();

			SUT.Document.Selection.SetRange(1, 1);
			SUT.Document.Selection.TypeText("b");

			SUT.Document.GetText(TextGetOptions.None, out var typed);
			Assert.AreEqual("ab", typed);

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("a", afterUndo);
		}

		[TestMethod]
		public async Task When_BatchDisplayUpdates_Returns_Nesting_Count()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(1, SUT.Document.BatchDisplayUpdates());
			Assert.AreEqual(2, SUT.Document.BatchDisplayUpdates());
			Assert.AreEqual(1, SUT.Document.ApplyDisplayUpdates());
			Assert.AreEqual(0, SUT.Document.ApplyDisplayUpdates());

			// An extra unbalanced ApplyDisplayUpdates stays clamped at zero.
			Assert.AreEqual(0, SUT.Document.ApplyDisplayUpdates());
		}

		[TestMethod]
		public async Task When_Edits_Batched_Are_Applied_After_ApplyDisplayUpdates()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "base");

			SUT.Document.BatchDisplayUpdates();
			SUT.Document.Selection.SetRange(4, 4);
			SUT.Document.Selection.TypeText("!");
			SUT.Document.ApplyDisplayUpdates();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("base!", text);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Alignment_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			// A fresh paragraph resolves to Left.
			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			format.Alignment = ParagraphAlignment.Center;

			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Applies_To_Whole_Paragraph()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Setting alignment through a caret inside the second paragraph applies to the whole
			// paragraph, and leaves the first paragraph untouched.
			var second = SUT.Document.GetRange(5, 5).ParagraphFormat;
			second.Alignment = ParagraphAlignment.Right;

			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 0).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetRange(4, 7).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Mixed_Reports_Undefined()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			var first = SUT.Document.GetRange(0, 3).ParagraphFormat;
			first.Alignment = ParagraphAlignment.Center;
			var second = SUT.Document.GetRange(4, 7).ParagraphFormat;
			second.Alignment = ParagraphAlignment.Right;

			// A range spanning both paragraphs disagrees, so the shared value is Undefined.
			Assert.AreEqual(ParagraphAlignment.Undefined, SUT.Document.GetRange(0, 7).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_SetIndents_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.SetIndents(10f, 20f, 30f);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			Assert.AreEqual(10f, format.FirstLineIndent);
			Assert.AreEqual(20f, format.LeftIndent);
			Assert.AreEqual(30f, format.RightIndent);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_SetLineSpacing_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.SetLineSpacing(LineSpacingRule.Exactly, 24f);

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			Assert.AreEqual(LineSpacingRule.Exactly, format.LineSpacingRule);
			Assert.AreEqual(24f, format.LineSpacing);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_ListType_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one\rtwo");

			var first = SUT.Document.GetRange(0, 0).ParagraphFormat;
			first.ListType = MarkerType.Bullet;

			Assert.AreEqual(MarkerType.Bullet, SUT.Document.GetRange(0, 3).ParagraphFormat.ListType);
			// The other paragraph keeps its (undefined) list type.
			Assert.AreEqual(MarkerType.Undefined, SUT.Document.GetRange(4, 7).ParagraphFormat.ListType);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_GetClone_IsEqual()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			var format = SUT.Document.GetRange(0, 5).ParagraphFormat;
			format.Alignment = ParagraphAlignment.Center;

			var clone = format.GetClone();
			Assert.IsTrue(format.IsEqual(clone));

			clone.Alignment = ParagraphAlignment.Right;
			Assert.IsFalse(format.IsEqual(clone));
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Undo_Reverts_Alignment()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "hello");

			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);

			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();

			Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("hello", text);
		}

		[TestMethod]
		public async Task When_ParagraphFormat_Survives_Text_Edit()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			SUT.Document.GetRange(4, 7).ParagraphFormat.Alignment = ParagraphAlignment.Center;

			// Typing into the formatted paragraph splices the paragraph run in lock-step, so the
			// alignment is preserved.
			SUT.Document.Selection.SetRange(7, 7);
			SUT.Document.Selection.TypeText("X");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("aaa\rbbbX", text);
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_GetRect_Returns_Caret_Geometry()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(3, 3).GetRect(PointOptions.None, out var rect, out var hit);

			Assert.AreEqual(0, hit, "hit is reported as 0 (documented approximation).");
			Assert.IsTrue(rect.Height > 0, $"Caret rect should have a positive height, was {rect.Height}.");
			Assert.IsTrue(rect.Width <= 1, $"A degenerate (caret) range should have a ~zero width, was {rect.Width}.");
			Assert.IsTrue(rect.X > 0, $"The caret at index 3 should be offset from the left edge, was {rect.X}.");
		}

		[TestMethod]
		public async Task When_GetRect_Range_Has_Positive_Width()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(0, 5).GetRect(PointOptions.None, out var rangeRect, out _);
			SUT.Document.GetRange(0, 0).GetRect(PointOptions.None, out var caretRect, out _);

			Assert.IsTrue(rangeRect.Width > caretRect.Width, $"A non-degenerate range should be wider than a caret, was {rangeRect.Width} vs {caretRect.Width}.");
			Assert.IsTrue(rangeRect.Height > 0, $"Range rect should have a positive height, was {rangeRect.Height}.");
		}

		[TestMethod]
		public async Task When_GetRect_ClientCoordinates_Preserves_Size()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(2, 5);
			range.GetRect(PointOptions.None, out var rootRect, out _);
			range.GetRect(PointOptions.ClientCoordinates, out var clientRect, out _);

			// Both transforms are translation-only, so the size is invariant across coordinate spaces.
			Assert.AreEqual(rootRect.Width, clientRect.Width, 0.5, "Width should be coordinate-space invariant.");
			Assert.AreEqual(rootRect.Height, clientRect.Height, 0.5, "Height should be coordinate-space invariant.");
		}

		[TestMethod]
		public async Task When_GetPoint_Vertical_Alignment_Orders_Points()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var topPoint);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Bottom, PointOptions.None, out var bottomPoint);

			Assert.AreEqual(topPoint.X, bottomPoint.X, 0.5, "Left alignment should give the same X for both vertical anchors.");
			Assert.IsTrue(bottomPoint.Y > topPoint.Y, $"Bottom anchor should be below the top anchor, was {bottomPoint.Y} vs {topPoint.Y}.");
		}

		[TestMethod]
		public async Task When_GetPoint_GetRangeFromPoint_RoundTrips()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(6, 6).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var point);
			var recovered = SUT.Document.GetRangeFromPoint(point, PointOptions.None);

			Assert.IsTrue(Math.Abs(recovered.StartPosition - 6) <= 1, $"GetRangeFromPoint should recover the origin index, was {recovered.StartPosition}.");
		}

		[TestMethod]
		public async Task When_SetPoint_Moves_Caret()
		{
			if (OperatingSystem.IsBrowser())
			{
				Assert.Inconclusive("Skipped on Wasm Skia due to font differences.");
			}

			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(7, 7).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var point);

			var range = SUT.Document.GetRange(0, 0);
			range.SetPoint(point, PointOptions.None, extend: false);

			Assert.IsTrue(Math.Abs(range.StartPosition - 7) <= 1, $"SetPoint should place the caret at the hit index, was {range.StartPosition}.");
			Assert.AreEqual(range.StartPosition, range.EndPosition, "A non-extending SetPoint should collapse the range.");
		}

		[TestMethod]
		public async Task When_ScrollIntoView_Does_Not_Throw()
		{
			var SUT = new RichEditBox { Width = 200, Height = 60 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line one\rLine two\rLine three\rLine four\rLine five");
			await WindowHelper.WaitForIdle();

			// Should not throw regardless of whether the range is on-screen.
			SUT.Document.GetRange(40, 44).ScrollIntoView(PointOptions.None);
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var def = SUT.Document.GetDefaultCharacterFormat();
			def.Bold = FormatEffect.On;
			def.Size = 24;

			var reread = SUT.Document.GetDefaultCharacterFormat();
			Assert.AreEqual(FormatEffect.On, reread.Bold);
			Assert.AreEqual(24f, reread.Size);
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Applies_To_New_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultCharacterFormat().Bold = FormatEffect.On;
			SUT.Document.SetText(TextSetOptions.None, "abc");

			// SetText content is created against the (now bold) document default.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Applies_To_Typed_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultCharacterFormat().Italic = FormatEffect.On;
			SUT.Document.Selection.TypeText("hi");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("hi", text);
			// Text typed into the empty document inherits the document default formatting.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 2).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_SetDefaultCharacterFormat_From_Value()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var value = SUT.Document.GetDefaultCharacterFormat().GetClone();
			value.Bold = FormatEffect.On;
			SUT.Document.SetDefaultCharacterFormat(value);

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetDefaultCharacterFormat().Bold);
		}

		[TestMethod]
		public async Task When_DefaultParagraphFormat_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Right;

			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetDefaultParagraphFormat().Alignment);
		}

		[TestMethod]
		public async Task When_DefaultParagraphFormat_Applies_To_New_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Center;
			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Both paragraphs are created against the (centered) document default.
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(1, 1).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_TextChanged_Raised_On_SetText()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var count = 0;
			object sender = null;
			RoutedEventArgs args = null;
			SUT.TextChanged += (s, e) =>
			{
				count++;
				sender = s;
				args = e;
			};

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			Assert.AreSame(SUT, sender);
			Assert.IsNotNull(args);
		}

		[TestMethod]
		public async Task When_TextChanged_Not_Raised_On_Same_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanged += (s, e) => count++;

			// Re-setting the identical text must not raise (choke-point de-dupes against last value).
			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_TextChanged_Not_Raised_On_Format_Only_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanged += (s, e) => count++;

			// A character-format change mutates the run model and re-renders, but the plain text is
			// unchanged, so TextChanged must not fire.
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_SelectionChanged_Raised_On_Programmatic_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var count = 0;
			object sender = null;
			SUT.SelectionChanged += (s, e) =>
			{
				count++;
				sender = s;
			};

			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(count >= 1, $"SelectionChanged should raise on a programmatic selection change, count was {count}.");
			Assert.AreSame(SUT, sender);
		}

		[TestMethod]
		public async Task When_SelectionChanged_Not_Raised_On_Same_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.SelectionChanged += (s, e) => count++;

			// Re-applying the identical range must not raise.
			SUT.Document.Selection.SetRange(2, 5);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_Programmatic_SelectionChanging_Cancel_Restores_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				SUT.Document.Selection.SetRange(1, 1);

				var changingCount = 0;
				var changedCount = 0;
				var proposedStart = -1;
				var proposedLength = -1;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					proposedStart = e.SelectionStart;
					proposedLength = e.SelectionLength;
					e.Cancel = true;
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 5);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(2, proposedStart);
				Assert.AreEqual(3, proposedLength);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_SelectionChanging_Precedes_SelectionChanged()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");

				var events = new System.Collections.Generic.List<string>();
				SUT.SelectionChanging += (s, e) => events.Add("changing");
				SUT.SelectionChanged += (s, e) => events.Add("changed");

				SUT.Document.Selection.SetRange(2, 5);

				CollectionAssert.AreEqual(new[] { "changing", "changed" }, events);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_SelectionChanging_Handler_Selection_Wins_Over_Cancel()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.Selection.SetRange(4, 6);
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 5);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(6, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(4, SUT.SelectionStartForTesting);
				Assert.AreEqual(2, SUT.SelectionLengthForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Interactive_SelectionChanging_Handler_Selection_Wins_Over_Cancel()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.Selection.SetRange(0, 1);
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				RaiseKey(SUT, VirtualKey.Left);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(0, SUT.SelectionStartForTesting);
				Assert.AreEqual(1, SUT.SelectionLengthForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_Same_Selection_Raises_Only_SelectionChanging()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 2);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) => changingCount++;
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(1, 2);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Interactive_Text_Edit_SelectionChanging_Cancel_Restores_Caret_Once()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				RaiseKey(SUT, VirtualKey.A, unicodeKey: 'a');

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("a", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(0, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(0, SUT.SelectionStartForTesting);
				Assert.AreEqual(0, SUT.SelectionLengthForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Text_Rebase_SelectionChanging_Cancel_Restores_Accepted_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					Assert.AreEqual(4, e.SelectionStart);
					Assert.AreEqual(0, e.SelectionLength);
					e.Cancel = true;
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.GetRange(0, 0).Text = "x";

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("xabc", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Grows_Text_Uses_Final_Document_Length()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 1);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.SetText(TextSetOptions.None, "abcdef");
					SUT.Document.Selection.SetRange(5, 5);
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 2);

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(5, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Text_Mutation_Does_Not_Override_Cancel()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 1);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.SetText(TextSetOptions.None, "abcdef");
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 2);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Explicit_Same_Proposal_Wins_Over_Cancel()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 1);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.Selection.SetRange(e.SelectionStart, e.SelectionStart + e.SelectionLength);
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 2);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Same_Text_SetText_Selection_Reset_Is_Cancellable()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(2, 2);

				var changingCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
				};

				SUT.Document.SetText(TextSetOptions.None, "abc");

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Throws_Restores_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 1);

				var changedCount = 0;
				SUT.SelectionChanging += (s, e) => throw new InvalidOperationException("test");
				SUT.SelectionChanged += (s, e) => changedCount++;

				Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Document.Selection.SetRange(2, 2));

				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Cancelled_SelectionChanging_Text_Shrink_Restores_Silently()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(5, 5);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.SetText(TextSetOptions.None, "ab");
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(4, 4);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Text_Rebase_Preserves_Backward_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				RaiseKey(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);

				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) => changingCount++;
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.GetRange(0, 0).Text = "x";

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(4, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Formatting_Undo_Preserves_Backward_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				RaiseKey(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);

				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) => changingCount++;
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Undo();

				Assert.AreEqual(0, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Keyboard_Undo_Rebases_Selection_Without_Forced_Collapse()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				SUT.Document.Selection.SetRange(3, 3);
				RaiseKey(SUT, VirtualKey.X, unicodeKey: 'x');
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);

				var changingCount = 0;
				SUT.SelectionChanging += (s, e) => changingCount++;
				RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abc", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(3, SUT.SelectionStartForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Undo_Rebases_Repeated_Text_At_Exact_Edit()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "aaa");
				var tracked = SUT.Document.GetRange(2, 2);

				SUT.Document.GetRange(0, 0).Text = "a";
				Assert.AreEqual(3, tracked.StartPosition);

				SUT.Document.Undo();

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("aaa", text);
				Assert.AreEqual(2, tracked.StartPosition);
				Assert.AreEqual(2, tracked.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_UndoGroup_Rebases_Noncontiguous_Edits_In_Reverse_Order()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				var tracked = SUT.Document.GetRange(4, 4);

				SUT.Document.BeginUndoGroup();
				SUT.Document.GetRange(1, 1).Text = "X";
				SUT.Document.GetRange(6, 6).Text = "Y";
				SUT.Document.EndUndoGroup();
				Assert.AreEqual(5, tracked.StartPosition);

				SUT.Document.Undo();

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(4, tracked.StartPosition);
				Assert.AreEqual(4, tracked.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Delete_Multiple_Units_Raises_One_Cancellable_Proposal()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(1, 4);
				await WindowHelper.WaitForIdle();

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				Assert.AreEqual(2, SUT.Document.Selection.Delete(TextRangeUnit.Character, 2));

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("af", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Delete_Multiple_Units_Raises_One_Text_Notification()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(1, 4);
				await WindowHelper.WaitForIdle();

				var changingCount = 0;
				var changedCount = 0;
				SUT.TextChanging += (s, e) => changingCount++;
				SUT.TextChanged += (s, e) => changedCount++;

				Assert.AreEqual(2, SUT.Document.Selection.Delete(TextRangeUnit.Character, 2));
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(0, changedCount);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, changedCount);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Cut_Overrides_Cancel_Without_Recursion()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(1, 3);

				var changingCount = 0;
				var changedCount = 0;
				SUT.SelectionChanging += (s, e) =>
				{
					changingCount++;
					e.Cancel = true;
					SUT.Document.Selection.Cut();
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 4);

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abef", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Cut_Handler_Moves_Selection_Copies_And_Deletes_Same_Span()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(0, 2);
				SUT.CuttingToClipboard += (s, e) => SUT.Document.Selection.SetRange(3, 5);

				SUT.Document.Selection.Cut();
				await WindowHelper.WaitForIdle();

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abcf", text);
				Assert.AreEqual("de", await Clipboard.GetContent().GetTextAsync());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_Selection_Copy_Preserves_Backward_Direction()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				RaiseKey(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);

				SUT.Document.Selection.Copy();

				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Async_Range_Paste_Uses_Rebased_Operation_Range()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var dataPackage = new DataPackage();
				dataPackage.SetText("X");
				Clipboard.SetContent(dataPackage);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");

				var range = SUT.Document.GetRange(2, 4);
				range.Paste(0);
				SUT.Document.GetRange(0, 0).Text = "!";

				await WindowHelper.WaitFor(() =>
				{
					SUT.Document.GetText(TextGetOptions.None, out var text);
					return text == "!abXef";
				});
				Assert.AreEqual(4, range.StartPosition);
				Assert.AreEqual(4, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Async_Selection_Paste_Becomes_ReadOnly_Before_Continuation()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var dataPackage = new DataPackage();
				dataPackage.SetText("X");
				Clipboard.SetContent(dataPackage);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 2);

				SUT.Document.Selection.Paste(0);
				SUT.IsReadOnly = true;
				await WindowHelper.WaitForIdle();

				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("abc", text);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Control_Rich_Paste_TextChanging_Sees_Final_Formatting()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");
				SUT.Document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
				SUT.Document.Selection.SetRange(0, 2);
				SUT.Document.Selection.Copy();
				SUT.Document.SetText(TextSetOptions.None, "z");
				SUT.Document.Selection.SetRange(1, 1);

				var changingCount = 0;
				var observedBold = FormatEffect.Undefined;
				SUT.TextChanging += (s, e) =>
				{
					changingCount++;
					observedBold = SUT.Document.GetRange(1, 3).CharacterFormat.Bold;
				};

				SUT.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					SUT.Document.GetText(TextGetOptions.None, out var text);
					return text == "zab";
				});

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(FormatEffect.On, observedBold);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Cancelled_Programmatic_Selection_Preserves_Pending_Caret_Format()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 1);
				SUT.Document.Selection.CharacterFormat.Bold = FormatEffect.On;

				SUT.SelectionChanging += (s, e) => e.Cancel = true;
				SUT.Document.Selection.SetRange(0, 0);

				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(FormatEffect.On, SUT.Document.Selection.CharacterFormat.Bold);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_DefaultTabStop_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(36f, SUT.Document.DefaultTabStop);

			SUT.Document.DefaultTabStop = 48f;
			Assert.AreEqual(48f, SUT.Document.DefaultTabStop);
		}

		[TestMethod]
		public async Task When_CaretType_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(CaretType.Normal, SUT.Document.CaretType);

			SUT.Document.CaretType = CaretType.Null;
			Assert.AreEqual(CaretType.Null, SUT.Document.CaretType);
		}

		[TestMethod]
		public async Task When_Trailing_Whitespace_And_Spacing_Knobs_RoundTrip()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			Assert.IsFalse(SUT.Document.AlignmentIncludesTrailingWhitespace);
			Assert.IsFalse(SUT.Document.IgnoreTrailingCharacterSpacing);

			SUT.Document.AlignmentIncludesTrailingWhitespace = true;
			SUT.Document.IgnoreTrailingCharacterSpacing = true;

			Assert.IsTrue(SUT.Document.AlignmentIncludesTrailingWhitespace);
			Assert.IsTrue(SUT.Document.IgnoreTrailingCharacterSpacing);
		}

		[TestMethod]
		public async Task When_CanCopy_Reflects_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");

			SUT.Document.Selection.SetRange(0, 0);
			Assert.IsFalse(SUT.Document.CanCopy());

			SUT.Document.Selection.SetRange(0, 5);
			Assert.IsTrue(SUT.Document.CanCopy());
		}

		[TestMethod]
		public async Task When_CanPaste_True_With_Text_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var dp = new DataPackage();
			dp.SetText("clip");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.CanPaste());
		}

		[TestMethod]
		public async Task When_ClearUndoRedoHistory_Clears_Stacks()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "one");
			SUT.Document.SetText(TextSetOptions.None, "two");
			Assert.IsTrue(SUT.Document.CanUndo());

			SUT.Document.Undo();
			Assert.IsTrue(SUT.Document.CanRedo());

			SUT.Document.ClearUndoRedoHistory();
			Assert.IsFalse(SUT.Document.CanUndo());
			Assert.IsFalse(SUT.Document.CanRedo());
		}

		[TestMethod]
		public async Task When_TextChanging_Raised_Before_TextChanged()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var order = new System.Collections.Generic.List<string>();
			var contentChanging = false;
			SUT.TextChanging += (s, e) =>
			{
				order.Add("changing");
				contentChanging = e.IsContentChanging;
			};
			SUT.TextChanged += (s, e) => order.Add("changed");

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { "changing", "changed" }, order);
			Assert.IsTrue(contentChanging);
		}

		[TestMethod]
		public async Task When_TextChanging_Mutates_Document_Then_Changed_Uses_Final_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var changingCount = 0;
			var changedCount = 0;
			SUT.TextChanging += (s, e) =>
			{
				changingCount++;
				SUT.Document.SetText(TextSetOptions.None, "final");
			};
			SUT.TextChanged += (s, e) =>
			{
				changedCount++;
				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("final", text);
			};

			SUT.Document.SetText(TextSetOptions.None, "intermediate");

			Assert.AreEqual(1, changingCount);
			Assert.AreEqual(0, changedCount);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, changedCount);
		}

		[TestMethod]
		public async Task When_TextChanging_Not_Raised_On_Format_Only_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanging += (s, e) => count++;

			// A format-only change re-renders but leaves the plain text unchanged, so TextChanging
			// (paired with TextChanged) must not fire.
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public async Task When_SelectionChanging_Raised_On_Interactive_Move()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");

			var count = 0;
			var proposedStart = -1;
			var proposedLength = -1;
			SUT.SelectionChanging += (s, e) =>
			{
				count++;
				proposedStart = e.SelectionStart;
				proposedLength = e.SelectionLength;
			};

			// Caret is at 3; moving left proposes a collapsed selection at 2.
			RaiseKey(SUT, VirtualKey.Left);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count, $"SelectionChanging should raise exactly once on an interactive move, count was {count}.");
			Assert.AreEqual(2, proposedStart);
			Assert.AreEqual(0, proposedLength);
		}

		[TestMethod]
		public async Task When_SelectionChanging_Cancel_Prevents_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition);

			var raised = false;
			SUT.SelectionChanging += (s, e) =>
			{
				raised = true;
				e.Cancel = true;
			};

			// Home would move the caret to 0, but the cancelling handler must prevent it.
			RaiseKey(SUT, VirtualKey.Home);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(raised, "SelectionChanging should have been raised for the Home key.");
			Assert.AreEqual(3, SUT.Document.Selection.StartPosition, "The cancelled selection change must not move the caret.");
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
		}

		[TestMethod]
		public async Task When_CopyingToClipboard_Raised()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.CopyingToClipboard += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public async Task When_CopyingToClipboard_Empty_Selection_Does_Not_Raise()
		{
			// A copy with a collapsed (empty) selection is a no-op and raises no CopyingToClipboard event,
			// consistent with CuttingToClipboard and TextBox.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			// Collapse the selection to a caret (End leaves a zero-length selection).
			RaiseKey(SUT, VirtualKey.End);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.CopyingToClipboard += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, count, "Empty-selection copy should not raise CopyingToClipboard.");
		}

		[TestMethod]
		public async Task When_CopyingToClipboard_Handled_Suppresses_Copy()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Seed the clipboard with a sentinel; a suppressed copy must leave it untouched.
			var seed = new DataPackage();
			seed.SetText("SENTINEL");
			Clipboard.SetContent(seed);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			SUT.CopyingToClipboard += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var text = await content.GetTextAsync();
			Assert.AreEqual("SENTINEL", text);
		}

		[TestMethod]
		public async Task When_CuttingToClipboard_Raised_And_Cuts()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.CuttingToClipboard += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.X, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		[TestMethod]
		public async Task When_CuttingToClipboard_Handled_Suppresses_Cut()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			SUT.CuttingToClipboard += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.X, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			// A suppressed cut must leave the document content intact.
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_Paste_Raised()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.Paste += (s, e) => count++;

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public async Task When_Paste_Handled_Suppresses_Paste()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XYZ");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			SUT.Paste += (s, e) => e.Handled = true;

			RaiseKey(SUT, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			// A suppressed paste must not replace the selected text.
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abc", text);
		}

		[TestMethod]
		public async Task When_Selection_Copy_Programmatic_Raises_CopyingToClipboard()
		{
			// Mirrors RichEditBoxTOMTests.cpp VerifyProgrammaticSelectionCopyRaisesRichEditBoxEvent (~1936):
			// Document.Selection.Copy() raises CopyingToClipboard (and NOT CuttingToClipboard), unfocused.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "text to copy");
			SUT.Document.Selection.SetRange(0, "text to copy".Length);
			await WindowHelper.WaitForIdle();

			var copyCount = 0;
			var cutCount = 0;
			SUT.CopyingToClipboard += (s, e) => copyCount++;
			SUT.CuttingToClipboard += (s, e) => cutCount++;

			SUT.Document.Selection.Copy();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, copyCount, "Selection.Copy must raise CopyingToClipboard.");
			Assert.AreEqual(0, cutCount, "Selection.Copy must not raise CuttingToClipboard.");
		}

		[TestMethod]
		public async Task When_Selection_Cut_Programmatic_Raises_CuttingToClipboard()
		{
			// Mirrors VerifyProgrammaticSelectionCutRaisesRichEditBoxEvent (~1868): Selection.Cut() raises
			// CuttingToClipboard (not CopyingToClipboard) and removes the text, unfocused.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "text to cut");
			SUT.Document.Selection.SetRange(0, "text to cut".Length);
			await WindowHelper.WaitForIdle();

			var copyCount = 0;
			var cutCount = 0;
			SUT.CopyingToClipboard += (s, e) => copyCount++;
			SUT.CuttingToClipboard += (s, e) => cutCount++;

			SUT.Document.Selection.Cut();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, cutCount, "Selection.Cut must raise CuttingToClipboard.");
			Assert.AreEqual(0, copyCount, "Selection.Cut must not raise CopyingToClipboard.");
			SUT.Document.GetText(TextGetOptions.None, out var cutText);
			Assert.AreEqual(string.Empty, cutText);
		}

		[TestMethod]
		public async Task When_Selection_Paste_Programmatic_Raises_Paste()
		{
			// Mirrors VerifyProgrammaticSelectionPasteRaisesRichEditBoxEvent (~2004): Selection.Paste(0)
			// raises the Paste event, unfocused.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("text to paste");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "abc");
			SUT.Document.Selection.SetRange(0, 3);
			await WindowHelper.WaitForIdle();

			var pasteCount = 0;
			SUT.Paste += (s, e) => pasteCount++;

			SUT.Document.Selection.Paste(0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, pasteCount, "Selection.Paste must raise the Paste event.");
		}

		[TestMethod]
		public async Task When_Range_Cut_Programmatic_Does_Not_Raise_Control_Event()
		{
			// WinUI raises the clipboard events for the SELECTION, but not for a plain (non-selection)
			// range — GetRange(...).Cut() removes the text silently (keeps the base UnoTextRange behavior).
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "abcdef");

			var cutCount = 0;
			SUT.CuttingToClipboard += (s, e) => cutCount++;

			SUT.Document.GetRange(0, 3).Cut();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, cutCount, "A plain range cut must not raise CuttingToClipboard.");
			SUT.Document.GetText(TextGetOptions.None, out var rangeCutText);
			Assert.AreEqual("def", rangeCutText);
		}

		[TestMethod]
		public async Task When_Document_SetText_Clamps_To_MaxLength()
		{
			// Mirrors RichEditBoxTOMTests.cpp SetTextAdheresToMaxLength (~1239): a programmatic SetText is
			// clamped to the control's MaxLength.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.MaxLength = 2;
			SUT.Document.SetText(TextSetOptions.None, "hello world");

			SUT.Document.GetText(TextGetOptions.None, out var clampedText);
			Assert.AreEqual(2, clampedText.Length);
			Assert.AreEqual("he", clampedText);
		}

		[TestMethod]
		public async Task When_Range_FindText_Backward_With_Negative_ScanLength()
		{
			// A negative scanLength searches backward from the range start (per the ITextRange::FindText
			// contract), returning the occurrence nearest the start.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "one two one");

			var range = SUT.Document.GetRange(11, 11);
			var found = range.FindText("one", -11, FindOptions.None);

			Assert.AreEqual(3, found);
			Assert.AreEqual(8, range.StartPosition);
			Assert.AreEqual(11, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Range_Character_Set_Replaces_Character()
		{
			// ITextRange.Character setter overwrites the single character at the range start.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "cat");
			var range = SUT.Document.GetRange(1, 2);
			range.Character = 'u';
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var charText);
			Assert.AreEqual("cut", charText);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Center_Sets_Override()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			await WindowHelper.WaitForIdle();

			// A fresh (Left) document does not override the control-level TextAlignment.
			Assert.IsNull(SUT.ParagraphAlignmentOverride);

			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Center, SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Right_Sets_Override()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Right, SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Mixed_Leaves_Override_Null()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");

			// Two paragraphs with different alignments cannot be rendered on a single TextBlock, so no
			// uniform override is projected.
			SUT.Document.GetRange(0, 0).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Cleared_Restores_Null()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(TextAlignment.Center, SUT.ParagraphAlignmentOverride);

			// Returning to the (default) Left alignment relinquishes the override back to the control DP.
			SUT.Document.GetRange(0, 5).ParagraphFormat.Alignment = ParagraphAlignment.Left;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
		}

		[TestMethod]
		public async Task When_ParagraphAlignment_Projects_To_DisplayBlock()
		{
			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hi");
			await WindowHelper.WaitForIdle();

			var block = FindDisplayBlock(SUT);
			Assert.IsNotNull(block, "The DisplayBlock hosting the text should be in the visual tree.");

			// Baseline: no paragraph-alignment override is active, so IsTextAlignmentSetToDefault is true.
			// While it is true, the shared TextBlock renders with its own default alignment regardless of
			// the block's TextAlignment property value (see GetAdjustedTextAlignment), which mirrors the
			// control-level TextAlignment DP. Capture that default rather than hard-coding it: the DP's
			// metadata default is default(TextAlignment) == Center, which is a framework-wide constant, not
			// something this feature controls.
			Assert.IsTrue(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);
			var controlDefaultAlignment = ((ITextBoxViewHost)SUT).TextAlignment;
			Assert.AreEqual(controlDefaultAlignment, block.TextAlignment);

			// Applying a uniform Right paragraph alignment projects Right onto the DisplayBlock (the exact
			// value the shared TextBlock renders with) and disables the deferral to the default so it takes
			// effect. Right is deliberately distinct from the control DP default so the projection is
			// unambiguous. This is the identical render mechanism TextBox uses for its own TextAlignment.
			SUT.Document.GetRange(0, 2).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Right, SUT.ParagraphAlignmentOverride);
			Assert.AreEqual(TextAlignment.Right, block.TextAlignment);
			Assert.IsFalse(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);

			// Relinquishing the paragraph alignment (the model default is Left) clears the override and
			// hands the block back to the control-level DP default.
			SUT.Document.GetRange(0, 2).ParagraphFormat.Alignment = ParagraphAlignment.Left;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.ParagraphAlignmentOverride);
			Assert.IsTrue(((ITextBoxViewHost)SUT).IsTextAlignmentSetToDefault);
			Assert.AreEqual(controlDefaultAlignment, block.TextAlignment);
		}

		[TestMethod]
		public async Task When_IME_Composition_Inserts_And_Commits()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var composing);
			Assert.AreEqual("ni", composing);

			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var committed);
			Assert.AreEqual("nihao", committed);
		}

		[TestMethod]
		public async Task When_IME_ReadOnly_Ignores_Composition()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox { IsReadOnly = true };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Original");
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, fake.StartImeSessionCallCount);
				Assert.IsFalse(SUT.IsCaretRenderedForTesting);

				fake.SimulateCompositionStart();
				fake.SimulateCompositionUpdate("ni");
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(SUT.IsComposing);
				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("Original", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		public async Task When_IME_Composition_Is_Single_Undo_Entry()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("n");
			fake.SimulateCompositionUpdate("ni");
			fake.SimulateCompositionUpdate("nihao");
			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var afterCommit);
			Assert.AreEqual("nihao", afterCommit);

			// The whole composition collapses into ONE undo entry (matching WinUI).
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("", afterUndo);
		}

		[TestMethod]
		public async Task When_IME_Composition_Cancel_Keeps_Text()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(SUT.IsComposing);

			fake.SimulateCompositionCancel();
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ni", text);
		}

		[TestMethod]
		public async Task When_IME_Composition_Events_Raised()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var started = 0;
			var changed = 0;
			var ended = 0;
			SUT.TextCompositionStarted += (s, e) => started++;
			SUT.TextCompositionChanged += (s, e) => changed++;
			SUT.TextCompositionEnded += (s, e) => ended++;

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, started);
			Assert.IsTrue(changed >= 1);
			Assert.AreEqual(1, ended);
		}

		[TestMethod]
		public async Task When_IME_Composing_Swallows_Char_Key_Guard()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.ShouldSwallowKeyDuringComposition);

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ShouldSwallowKeyDuringComposition);

			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.ShouldSwallowKeyDuringComposition);
		}

		[TestMethod]
		public async Task When_IME_Composition_Preserves_Existing_Formatting()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			SUT.Document.Selection.SetRange(2, 2);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("xy");
			fake.SimulateCompositionComplete("xy");
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("ABxy", text);

			// The pre-existing bold run on "A" survives the composition edit.
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_IME_External_Change_Cancels_Composition()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("ni");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(SUT.IsComposing);

			fake.EndImeSessionCalled = false;
			SUT.Document.SetText(TextSetOptions.None, "Replaced");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Replaced", text);
			Assert.IsTrue(fake.EndImeSessionCalled, "EndImeSession should be called when composition is cancelled by external text change");
		}

		[TestMethod]
		public async Task When_ReadOnly_Transition_Ends_Composition_And_Restarts_Ime()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, fake.StartImeSessionCallCount);
				fake.SimulateCompositionStart();
				fake.SimulateCompositionUpdate("ni");
				Assert.IsTrue(SUT.IsComposing);

				fake.EndImeSessionCalled = false;
				SUT.IsReadOnly = true;
				Assert.IsTrue(fake.EndImeSessionCalled);
				Assert.IsFalse(SUT.IsComposing);
				SUT.Document.GetText(TextGetOptions.None, out var text);
				Assert.AreEqual("ni", text);

				SUT.IsReadOnly = false;
				Assert.AreEqual(2, fake.StartImeSessionCallCount);
			}
			finally
			{
				WindowHelper.WindowContent = null;
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		public void When_Inactive_IME_Host_Ends_Session_Active_Host_Remains()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
			var activeHost = (IImeSessionHost)new RichEditBox();
			var inactiveHost = (IImeSessionHost)new RichEditBox();

			ImeSessionCoordinator.StartSession(activeHost);
			try
			{
				fake.EndImeSessionCalled = false;
				ImeSessionCoordinator.EndSession(inactiveHost);

				Assert.IsFalse(fake.EndImeSessionCalled);
				Assert.AreSame(activeHost, ImeSessionCoordinator.ActiveHost);
			}
			finally
			{
				ImeSessionCoordinator.EndSession(activeHost);
			}
		}

		private class FakeImeTextBoxExtension : IImeTextBoxExtension
		{
			public bool IsComposing { get; private set; }
			public bool EndImeSessionCalled { get; set; }
			public int StartImeSessionCallCount { get; private set; }

			public event EventHandler CompositionStarted;
			public event EventHandler<ImeCompositionEventArgs> CompositionUpdated;
			public event EventHandler<ImeCompositionEventArgs> CompositionCompleted;
			public event EventHandler CompositionEnded;

			public void StartImeSession(IImeSessionHost host) => StartImeSessionCallCount++;

			public void EndImeSession()
			{
				EndImeSessionCalled = true;
				if (IsComposing)
				{
					IsComposing = false;
					CompositionEnded?.Invoke(this, EventArgs.Empty);
				}
			}

			public void SimulateCompositionStart()
			{
				IsComposing = true;
				CompositionStarted?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCompositionUpdate(string text, int cursorPosition = -1)
			{
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, cursorPosition));
			}

			public void SimulateCompositionComplete(string text)
			{
				IsComposing = false;
				CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCompositionCancel()
			{
				IsComposing = false;
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}
		}

		[TestMethod]
		public async Task When_CtrlB_Toggles_Bold_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(1, 4);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);
			// The unselected characters stay unformatted.
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);

			// A second toggle over the same (fully bold) selection turns it back off.
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 4).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_CtrlI_Toggles_Italic_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 3);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Italic);

			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 3).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_CtrlU_Toggles_Underline_On_Selection()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.U, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(UnderlineType.Single, SUT.Document.GetRange(0, 5).CharacterFormat.Underline);

			RaiseKey(SUT, VirtualKey.U, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(UnderlineType.None, SUT.Document.GetRange(0, 5).CharacterFormat.Underline);
		}

		[TestMethod]
		public async Task When_CtrlB_On_Mixed_Selection_Makes_All_Bold()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");

			// Make only the first two characters bold, leaving the rest unformatted (a mixed range).
			SUT.Document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.Undefined, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			// A mixed selection toggles to fully-on (matching "make it all bold").
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_DisabledFormattingAccelerators_Suppresses_Bold_Only()
		{
			var SUT = new RichEditBox { DisabledFormattingAccelerators = DisabledFormattingAccelerators.Bold };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			// Bold is disabled, so Ctrl+B does nothing.
			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			// Italic is still enabled and applies normally.
			RaiseKey(SUT, VirtualKey.I, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_CtrlB_Is_Single_Undo_Entry()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);

			// A single undo reverts the whole formatting toggle while keeping the text.
			RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("abcde", text);
		}

		[TestMethod]
		public async Task When_ReadOnly_Suppresses_Formatting_Accelerator()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "abcde");
			SUT.Document.Selection.SetRange(0, 5);
			await WindowHelper.WaitForIdle();

			SUT.IsReadOnly = true;
			await WindowHelper.WaitForIdle();

			RaiseKey(SUT, VirtualKey.B, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(0, 5).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_ReadOnly_Blocks_Keyboard_Undo_Redo()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				SUT.Document.SetText(TextSetOptions.None, "A");
				SUT.Document.SetText(TextSetOptions.None, "AB");
				RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
				SUT.Document.GetText(TextGetOptions.None, out var textWithRedoEntry);
				Assert.AreEqual("A", textWithRedoEntry);

				SUT.IsReadOnly = true;
				RaiseKey(SUT, VirtualKey.Y, VirtualKeyModifiers.Control);
				SUT.Document.GetText(TextGetOptions.None, out var afterBlockedRedo);
				Assert.AreEqual("A", afterBlockedRedo);

				RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
				SUT.Document.GetText(TextGetOptions.None, out var afterBlockedUndo);
				Assert.AreEqual("A", afterBlockedUndo);

				SUT.IsReadOnly = false;
				RaiseKey(SUT, VirtualKey.Y, VirtualKeyModifiers.Control);
				SUT.Document.GetText(TextGetOptions.None, out var editableText);
				Assert.AreEqual("AB", editableText);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ReadOnly_Toggle_Updates_Caret()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "hi");
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				SUT.Document.Selection.SetRange(2, 2);
				Assert.IsTrue(SUT.IsCaretRenderedForTesting);

				SUT.IsReadOnly = true;
				Assert.IsFalse(SUT.IsCaretRenderedForTesting);

				SUT.IsReadOnly = false;
				Assert.IsTrue(SUT.IsCaretRenderedForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ReadOnly_Toggle_Preserves_Backward_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				RaiseKey(SUT, VirtualKey.Left, VirtualKeyModifiers.Shift);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
				Assert.AreEqual(1, SUT.SelectionLengthForTesting);

				SUT.IsReadOnly = true;
				SUT.IsReadOnly = false;

				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
				Assert.AreEqual(1, SUT.SelectionLengthForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ReadOnly_Changes_Automation_IsReadOnly_Property()
		{
			var SUT = new RichEditBox();
			var listener = new ReadOnlyPropertyChangedListener();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				Assert.IsNotNull(peer);

				AutomationPeer.TestAutomationPeerListener = listener;
				SUT.IsReadOnly = true;

				Assert.AreEqual(1, listener.NotificationCount);
				Assert.AreSame(peer, listener.Peer);
				Assert.AreSame(ValuePatternIdentifiers.IsReadOnlyProperty, listener.Property);
				Assert.AreEqual(false, listener.OldValue);
				Assert.AreEqual(true, listener.NewValue);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = null;
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Range_Copy_Puts_Text_On_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.GetRange(0, 5).Copy();
			await WindowHelper.WaitForIdle();

			var content = Clipboard.GetContent();
			var clipboardText = await content.GetTextAsync();
			Assert.AreEqual("Hello", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Copy_Empty_Is_NoOp()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var seed = new DataPackage();
			seed.SetText("SEED");
			Clipboard.SetContent(seed);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			// A degenerate (caret) range has nothing to copy, so the clipboard is untouched.
			SUT.Document.GetRange(3, 3).Copy();
			await WindowHelper.WaitForIdle();

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("SEED", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Cut_Removes_And_Copies()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(0, 6);
			range.Cut();
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(0, range.EndPosition, "Cut should collapse the range to its start.");

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		public async Task When_Range_Paste_Replaces_Range()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "AB");
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("XY");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(1, 1);
			range.Paste(0);

			await WindowHelper.WaitFor(() =>
			{
				SUT.Document.GetText(TextGetOptions.None, out var t);
				return t == "AXYB";
			});

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition, "Paste should collapse the range past the inserted text.");
		}

		[TestMethod]
		public async Task When_Range_CanPaste_Reflects_Clipboard()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			await WindowHelper.WaitForIdle();

			var dp = new DataPackage();
			dp.SetText("paste me");
			Clipboard.SetContent(dp);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.Document.GetRange(0, 0).CanPaste(0));
		}

		[TestMethod]
		public async Task When_Range_GetCharacterUtf32_Returns_CodePoint()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(1, 1);
			range.GetCharacterUtf32(out var codePoint, 0);
			Assert.AreEqual((uint)'e', codePoint);

			// Reading past the end of the story yields 0.
			range.GetCharacterUtf32(out var oob, 100);
			Assert.AreEqual(0u, oob);
		}

		[TestMethod]
		public async Task When_Range_MatchSelection_Moves_Selection_To_Range()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.Document.Selection.SetRange(2, 4);
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(8, 10);
			range.MatchSelection();

			// WinUI: MatchSelection moves the ACTIVE SELECTION onto this range (verified against
			// RichEditBoxTOMTests, which assert the selection takes the range's positions), so the
			// selection becomes (8,10) while this range is left unchanged.
			Assert.AreEqual(8, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(10, SUT.Document.Selection.EndPosition);
			Assert.AreEqual(8, range.StartPosition);
			Assert.AreEqual(10, range.EndPosition);
		}

		[TestMethod]
		public async Task When_Copy_Default_Preserves_Character_Formatting_On_Paste()
		{
			// Mirrors RichEditBoxTOMTests.cpp TestClipboardCopyFormats (~380-420): a default copy
			// (ClipboardCopyFormat.AllFormats) preserves the italic run across copy -> Ctrl+V paste.
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);
			await WindowHelper.WaitForIdle();

			source.Document.SetText(TextSetOptions.None, "world hello");
			source.Document.Selection.SetRange(0, 11);
			source.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			source.Document.Selection.Copy();
			await WindowHelper.WaitForIdle();

			target.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			RaiseKey(target, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitFor(() =>
			{
				target.Document.GetText(TextGetOptions.None, out var t);
				return t == "world hello";
			});

			target.Document.Selection.SetRange(0, 11);
			Assert.AreEqual(FormatEffect.On, target.Document.Selection.CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_Copy_PlainText_Drops_Character_Formatting_On_Paste()
		{
			// Mirrors RichEditBoxTOMTests.cpp TestClipboardCopyFormats (~437-447): ClipboardCopyFormat
			// PlainText drops formatting, so the pasted text is not italic.
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);
			await WindowHelper.WaitForIdle();

			source.Document.SetText(TextSetOptions.None, "world hello");
			source.Document.Selection.SetRange(0, 11);
			source.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			source.ClipboardCopyFormat = RichEditClipboardFormat.PlainText;
			source.Document.Selection.Copy();
			await WindowHelper.WaitForIdle();

			target.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			RaiseKey(target, VirtualKey.V, VirtualKeyModifiers.Control);
			await WindowHelper.WaitFor(() =>
			{
				target.Document.GetText(TextGetOptions.None, out var t);
				return t == "world hello";
			});

			target.Document.Selection.SetRange(0, 11);
			Assert.AreEqual(FormatEffect.Off, target.Document.Selection.CharacterFormat.Italic);
		}

		private static TextBlock FindDisplayBlock(DependencyObject root)
		{
			var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
				if (child is TextBlock { Text: "hi" } tb)
				{
					return tb;
				}

				var found = FindDisplayBlock(child);
				if (found is not null)
				{
					return found;
				}
			}

			return null;
		}

		private sealed class ReadOnlyPropertyChangedListener : IAutomationPeerListener
		{
			public int NotificationCount { get; private set; }
			public AutomationPeer Peer { get; private set; }
			public AutomationProperty Property { get; private set; }
			public object OldValue { get; private set; }
			public object NewValue { get; private set; }

			public bool ListenerExistsHelper(AutomationEvents eventId) => true;

			public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }

			public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }

			public void NotifyInvalidatePeer(AutomationPeer peer) { }

			public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
			{
				if (ReferenceEquals(automationProperty, ValuePatternIdentifiers.IsReadOnlyProperty))
				{
					NotificationCount++;
					Peer = peer;
					Property = automationProperty;
					OldValue = oldValue;
					NewValue = newValue;
				}
			}

			public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
		}
	}
}
