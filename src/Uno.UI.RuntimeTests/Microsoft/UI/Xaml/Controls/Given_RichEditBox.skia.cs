using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{

		[TestMethod]
		public async Task When_RichEditBox_Uses_Managed_Renderer()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "managed");
				editor.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var displayBlock = GetDisplayBlock(editor);
				Assert.AreEqual(1, displayBlock.Opacity);
				Assert.AreEqual("managed", displayBlock.Text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("a\u0301b")]
		[DataRow("\U0001F469\u200D\U0001F4BB")]
		[DataRow("\U0001F1E8\U0001F1E6")]
		[DataRow("\r\n")]
		public void When_Text_Element_Boundary_Cache_Matches_StringInfo(string text)
		{
			var cache = new TextElementBoundaryCache();
			var actual = cache.Get(text, version: 1);
			var starts = StringInfo.ParseCombiningCharacters(text);

			Assert.AreEqual(starts.Length + 1, actual.Count);
			for (var i = 0; i < starts.Length; i++)
			{
				Assert.AreEqual(starts[i], actual[i]);
			}
			Assert.AreEqual(text.Length, actual[actual.Count - 1]);
		}

		[TestMethod]
		public async Task When_Text_Element_Boundary_Cache_Invalidates_After_Edit_And_Undo()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "a\u0301b");

				Assert.AreEqual(0, SUT.Document.GetTextElementStart(1));
				Assert.AreEqual(2, SUT.Document.GetTextElementEnd(1));

				SUT.Document.GetRange(1, 2).Text = "x";
				Assert.AreEqual(1, SUT.Document.GetTextElementStart(1));
				Assert.AreEqual(1, SUT.Document.GetTextElementEnd(1));

				SUT.Document.Undo();
				Assert.AreEqual(0, SUT.Document.GetTextElementStart(1));
				Assert.AreEqual(2, SUT.Document.GetTextElementEnd(1));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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
				Assert.AreEqual(TextAlignment.DetectFromContent, displayBlock.TextAlignment);
				Assert.AreEqual(TextReadingOrder.DetectFromContent, displayBlock.TextReadingOrder);
				Assert.IsTrue(displayBlock.IsColorFontEnabled);
				Assert.AreEqual(Microsoft.UI.Colors.Transparent, displayBlock.SelectionHighlightColor.Color);

				SUT.TextWrapping = TextWrapping.NoWrap;
				SUT.HorizontalTextAlignment = TextAlignment.Right;
				SUT.TextReadingOrder = TextReadingOrder.UseFlowDirection;
				SUT.IsColorFontEnabled = false;
				var selectionBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
				SUT.SelectionHighlightColor = selectionBrush;

				Assert.AreEqual(TextWrapping.NoWrap, displayBlock.TextWrapping);
				Assert.AreEqual(TextAlignment.Right, SUT.TextAlignment);
				Assert.AreEqual(TextAlignment.Right, displayBlock.TextAlignment);
				Assert.AreEqual(TextReadingOrder.UseFlowDirection, displayBlock.TextReadingOrder);
				Assert.IsFalse(displayBlock.IsColorFontEnabled);
				Assert.AreEqual(Microsoft.UI.Colors.Transparent, displayBlock.SelectionHighlightColor.Color);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
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
		public async Task When_Native_Text_Input_Updates_Document_And_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");
				SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;

				SUT.UpdateTextFromNative("aXYZb", 4, 0);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aXYZb", text);
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(4, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);
				Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(4, 5).CharacterFormat.Bold);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Text_Input_Is_Rejected_While_ReadOnly()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "original");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.IsReadOnly = true;

				SUT.UpdateTextFromNative("changed", 7, 0);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("original", text);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Text_Input_Applies_Casing_And_MaxLength()
		{
			var SUT = new RichEditBox
			{
				CharacterCasing = CharacterCasing.Upper,
				MaxLength = 3,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "a");

				SUT.UpdateTextFromNative("abcd", 4, 0);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aBC", text, "Only the native insertion should be cased and it should respect MaxLength.");
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition, "The native caret should be rebased after MaxLength truncates the insertion.");
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_TextBoxView_Native_Input_Uses_RichEditBox_Pipeline()
		{
			var SUT = new RichEditBox
			{
				CharacterCasing = CharacterCasing.Upper,
				MaxLength = 2,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var view = ((IImeSessionHost)SUT).TextBoxView;
				Assert.IsNotNull(view);

				var eventOrder = new List<string>();
				SUT.TextChanging += (_, _) =>
				{
					eventOrder.Add("changing");
					SUT.Document.GetRange(0, SUT.GetPlainTextLength()).CharacterFormat.Bold = FormatEffect.On;
				};
				SUT.TextChanged += (_, _) => eventOrder.Add("changed");

				view.UpdateTextFromNative("a😀");
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("A😀", text, "Interactive MaxLength must not split a surrogate pair.");
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, text.Length).CharacterFormat.Bold);
				CollectionAssert.AreEqual(new[] { "changing", "changed" }, eventOrder);
				Assert.AreEqual(text, view.DisplayBlock.Text);
				var placeholder = SUT.FindFirstChild<FrameworkElement>(element => element.Name == "PlaceholderTextContentPresenter");
				Assert.IsTrue(placeholder is null || placeholder.Visibility == Visibility.Collapsed);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Host_Input_Respects_Protection_And_Selection_Cancellation()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(3, 3);
				SUT.Document.GetRange(1, 2).CharacterFormat.ProtectedText = FormatEffect.On;

				((IImeSessionHost)SUT).UpdateTextFromNative("axc", 2, 0);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abc", text);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);

				SUT.Document.GetRange(1, 2).CharacterFormat.ProtectedText = FormatEffect.Off;
				SUT.SelectionChanging += (_, args) => args.Cancel = true;
				((IImeSessionHost)SUT).UpdateTextFromNative("abcd", 4, 0);

				GetTextWithoutFinalEop(SUT.Document, out text);
				Assert.AreEqual("abcd", text);
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Backward_Selection_Is_Rebased_After_Input_Correction()
		{
			var SUT = new RichEditBox { MaxLength = 4 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");

				((IImeSessionHost)SUT).UpdateTextFromNative("aXYZb", 4, -3);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aXYb", text);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.NativeSelectionIsBackward);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Browser_Apple_Android_Native_Host_Typing_Coalesces_Undo()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.ClearUndoRedoHistory();
				var host = (IImeSessionHost)SUT;

				host.UpdateTextFromNative("a", 1, 0);
				host.UpdateTextFromNative("ab", 2, 0);
				host.UpdateTextFromNative("abc", 3, 0);

				Assert.IsTrue(SUT.Document.CanUndo());
				SUT.Document.Undo();
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual(string.Empty, text);
				Assert.IsFalse(SUT.Document.CanUndo(), "Sequential native typing should be one undo action.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Newline_Is_Normalized()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				((IImeSessionHost)SUT).UpdateTextFromNative("a\nb", 3, 0);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("a\rb", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Paste_Replaces_Selection_And_Preserves_Rich_State()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
				SUT.Document.GetRange(3, 4).CharacterFormat.Italic = FormatEffect.On;
				SUT.Document.Selection.SetRange(1, 3);
				var pasteCount = 0;
				SUT.Paste += (_, _) => pasteCount++;

				SUT.PasteFromClipboard("XY\nZ");

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aXY\rZd", text);
				Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, pasteCount);
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 1).CharacterFormat.Bold);
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(5, 6).CharacterFormat.Italic);
				Assert.IsTrue(SUT.Document.CanUndo());

				SUT.Document.Undo();
				GetTextWithoutFinalEop(SUT.Document, out text);
				Assert.AreEqual("abcd", text);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);

				SUT.Document.Redo();
				GetTextWithoutFinalEop(SUT.Document, out text);
				Assert.AreEqual("aXY\rZd", text);
				Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Native_Paste_Respects_ReadOnly_And_Handled()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(1, 2);
				var pasteCount = 0;

				SUT.IsReadOnly = true;
				SUT.Paste += (_, args) =>
				{
					pasteCount++;
					args.Handled = true;
				};
				SUT.PasteFromClipboard("X");
				Assert.AreEqual(0, pasteCount, "Read-only paste should be rejected before raising Paste.");

				SUT.IsReadOnly = false;
				SUT.PasteFromClipboard("X");
				Assert.AreEqual(1, pasteCount);
				GetTextWithoutFinalEop(SUT.Document, out var text);
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		public async Task When_Touch_Tap_Selects_Word_And_Shows_Grippers()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var finger = injector.GetFinger();
				finger.Press(GetTextPoint(SUT, 8));
				finger.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(6, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(11, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(RichEditBox.RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing, SUT.CaretMode);
				Assert.IsNotNull(SUT.SelectionGrippersForTesting);

				finger.Press(GetTextPoint(SUT, 8));
				finger.Release();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(6, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(11, SUT.Document.Selection.EndPosition);

				finger.Press(GetTextPoint(SUT, 1));
				finger.Release();
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(0, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(RichEditBox.RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing, SUT.CaretMode);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Touch_End_Gripper_Drag_Extends_Selection()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world again");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var finger = injector.GetFinger();
				finger.Press(GetTextPoint(SUT, 2));
				finger.Release();
				await WindowHelper.WaitForIdle();

				var grippers = SUT.SelectionGrippersForTesting;
				Assert.IsNotNull(grippers);
				finger.Press(grippers.Value.end.GetAbsoluteBounds().GetCenter());
				var endPoint = GetTextPoint(SUT, 17);
				finger.MoveTo(new Windows.Foundation.Point(endPoint.X + 12, endPoint.Y), stepOffsetInMilliseconds: 20);
				finger.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(17, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Touch_Selection_Shows_Selection_Flyout()
		{
			// Mirrors WinUI TextControlHelper.h:22 VerifySelectingTextWithTouchShowsSelectionFlyout, reached
			// from RichEditBoxIntegrationTests.cpp:165: a touch selection opens the SelectionFlyout, and
			// collapsing the selection back to a caret closes it.
			var flyout = new MenuFlyout();
			flyout.Items.Add(new MenuFlyoutItem { Text = "Selection" });
			var SUT = new RichEditBox { Width = 400, SelectionFlyout = flyout };
			var opened = 0;
			var closed = 0;
			flyout.Opened += (_, _) => opened++;
			flyout.Closed += (_, _) => closed++;
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var finger = injector.GetFinger();
				finger.Press(GetTextPoint(SUT, 8));
				finger.Release();
				await WindowHelper.WaitForIdle();

				Assert.IsGreaterThan(
					SUT.Document.Selection.StartPosition,
					SUT.Document.Selection.EndPosition,
					"A touch tap on a word should produce a non-empty selection.");
				await WindowHelper.WaitFor(() => opened == 1);
				Assert.AreEqual(0, closed);

				flyout.Hide();
				await WindowHelper.WaitFor(() => closed == 1);

				finger.Press(GetTextPoint(SUT, 1));
				finger.Release();
				await WindowHelper.WaitForIdle();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(SUT.Document.Selection.StartPosition, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, opened, "A caret-only touch tap must not reopen the SelectionFlyout.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Mouse_Triple_Click_Selects_Logical_Line()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "first\rsecond\rthird");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				mouse.MoveTo(GetTextPoint(SUT, 8));
				mouse.Press();
				mouse.Release();
				mouse.Press();
				mouse.Release();
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(6, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(13, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Mouse_Double_Click_Drag_Extends_By_Word()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				mouse.MoveTo(GetTextPoint(SUT, 8));
				mouse.Press();
				mouse.Release();
				mouse.Press();
				await WindowHelper.WaitFor(() => SUT.Document.Selection.StartPosition == 6 && SUT.Document.Selection.EndPosition == 11);

				mouse.MoveTo(GetTextPoint(SUT, 1));
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(11, SUT.Document.Selection.EndPosition);
#if HAS_UNO
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static Windows.Foundation.Point GetTextPoint(RichEditBox editor, int index)
		{
			var displayBlock = editor.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			var textLength = editor.GetPlainTextContent().Length;
			index = Math.Clamp(index, 0, textLength);
			var start = displayBlock.ParsedText.GetRectForIndex(index);
			var end = displayBlock.ParsedText.GetRectForIndex(Math.Min(index + 1, textLength));
			var point = new Windows.Foundation.Point(
				(start.X + end.X) / 2 + displayBlock.Padding.Left,
				start.Y + start.Height / 2 + displayBlock.Padding.Top);
			return displayBlock.TransformToVisual(null).TransformPoint(point);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Pointer_Click_Places_Caret()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Pointer_Drag_Selects_Text()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Shift_Click_Extends_Selection()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(0, SUT.Document.Selection.EndPosition);

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var t);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			GetTextWithoutFinalEop(SUT.Document, out var acceptedAtOne);
			Assert.AreEqual("\U0001F600", acceptedAtOne);

			SUT.MaxLength = 2;
			await TypeAsync(SUT, "\U0001F600");
			GetTextWithoutFinalEop(SUT.Document, out var accepted);
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
			GetTextWithoutFinalEop(SUT.Document, out var afterBackspace);
			Assert.AreEqual(string.Empty, afterBackspace);

			await TypeAsync(SUT, "\U0001F600X");
			RaiseKey(SUT, VirtualKey.Home);
			RaiseKey(SUT, VirtualKey.Delete);
			GetTextWithoutFinalEop(SUT.Document, out var afterDelete);
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
			GetTextWithoutFinalEop(SUT.Document, out var afterBackspace);
			Assert.AreEqual("X", afterBackspace);

			SUT.Document.SetText(TextSetOptions.None, "\U0001F600X");
			SUT.Document.Selection.SetRange(1, 1);
			RaiseKey(SUT, VirtualKey.Delete);
			GetTextWithoutFinalEop(SUT.Document, out var afterDelete);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("Xc", text);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var t);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			GetTextWithoutFinalEop(SUT.Document, out var afterUpper);
			Assert.AreEqual("A", afterUpper);

			SUT.CharacterCasing = CharacterCasing.Lower;
			await TypeAsync(SUT, "B");
			GetTextWithoutFinalEop(SUT.Document, out var afterLower);
			Assert.AreEqual("Ab", afterLower);

			SUT.CharacterCasing = CharacterCasing.Normal;
			await TypeAsync(SUT, "aB");
			GetTextWithoutFinalEop(SUT.Document, out var afterNormal);
			Assert.AreEqual("AbaB", afterNormal);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var t);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("axb", text);
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 2).CharacterFormat.Bold, "Pending bold should clear once the caret moves.");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var t);
				return t == "HelloHello";
			});
		}

		[TestMethod]
		public async Task When_Cut_Is_NoOp_When_ReadOnly()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			SUT.IsReadOnly = true;
			SUT.Document.Selection.SetRange(0, 5);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.CutSelectionToClipboard();
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("Hello world", text);
		}

		[TestMethod]
		public async Task When_Empty_Paragraph_Format_Projects_And_Is_Inherited_By_Typing()
		{
			var SUT = new RichEditBox { Width = 240 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var empty = SUT.Document.GetRange(0, 0).ParagraphFormat;
				empty.SetIndents(6, 12, 0);
				empty.SpaceBefore = 3;
				empty.SetLineSpacing(LineSpacingRule.Exactly, 24);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(SUT);
				var caret = block.ParsedText.GetRectForIndex(0);
				Assert.AreEqual(24, caret.X, 1.5);
				Assert.AreEqual(4, caret.Y, 1.5);
				Assert.AreEqual(32, caret.Height, 1.5);

				SUT.Document.Selection.TypeText("x");
				var typed = SUT.Document.GetRange(0, 0).ParagraphFormat;
				Assert.AreEqual(6f, typed.FirstLineIndent);
				Assert.AreEqual(12f, typed.LeftIndent);
				Assert.AreEqual(LineSpacingRule.Exactly, typed.LineSpacingRule);

				SUT.Document.Undo();
				Assert.AreEqual(0, SUT.Document.TextLength);
				Assert.AreEqual(12f, SUT.Document.GetRange(0, 0).ParagraphFormat.LeftIndent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Terminal_Paragraph_Format_Renders_RoundTrips_Rtf_And_Undoes()
		{
			var source = new RichEditBox { Width = 240 };
			var target = new RichEditBox { Width = 240 };
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				source.Document.SetText(TextSetOptions.None, "a\r");
				source.Document.BeginUndoGroup();
				var terminal = source.Document.GetRange(2, 2).ParagraphFormat;
				terminal.SetIndents(6, 12, 0);
				terminal.SpaceBefore = 3;
				terminal.SetLineSpacing(LineSpacingRule.Exactly, 24);
				terminal.ListType = MarkerType.Arabic;
				terminal.ListStyle = MarkerStyle.Period;
				terminal.ListLevelIndex = 1;
				terminal.ListStart = 1;
				terminal.ListTab = 18;
				source.Document.EndUndoGroup();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0f, source.Document.GetRange(0, 0).ParagraphFormat.LeftIndent);
				var block = GetDisplayBlock(source);
				Assert.AreEqual("1.", block.EndingParagraphLayout?.MarkerText);
				var terminalCaret = block.ParsedText.GetRectForIndex(2);
				Assert.AreEqual(48, terminalCaret.X, 1.5, "The 24-DIP indent plus 24-DIP list tab should position terminal text at 48 DIPs.");
				Assert.AreEqual(32, terminalCaret.Height, 1.5);

				source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
				target.Document.SetText(TextSetOptions.FormatRtf, rtf);
				var imported = target.Document.GetRange(2, 2).ParagraphFormat;
				Assert.AreEqual(12f, imported.LeftIndent);
				Assert.AreEqual(LineSpacingRule.Exactly, imported.LineSpacingRule);
				Assert.AreEqual(MarkerType.Arabic, imported.ListType);
				Assert.AreEqual(1, imported.ListLevelIndex);

				source.Document.Undo();
				Assert.AreEqual(0f, source.Document.GetRange(2, 2).ParagraphFormat.LeftIndent);
				Assert.AreEqual(MarkerType.Undefined, source.Document.GetRange(2, 2).ParagraphFormat.ListType);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Logical_Line_Chunks_Handle_Crlf_And_Unicode_Separators()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				const string text = "a\r\nb\u2028c\u2029d";
				Assert.AreEqual(2, TextUnitNavigation.GetHardLineBreakLengthEndingAt(text, 3));
				Assert.AreEqual((0, 3), TextUnitNavigation.GetLogicalLineChunk(text, 1));
				Assert.AreEqual((3, 2), TextUnitNavigation.GetLogicalLineChunk(text, 3));
				Assert.AreEqual((5, 2), TextUnitNavigation.GetLogicalLineChunk(text, 5));
				Assert.AreEqual((7, 1), TextUnitNavigation.GetLogicalLineChunk(text, 7));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Hanging_Indent_Projects_Differently_On_Wrapped_Lines()
		{
			var SUT = new RichEditBox { Width = 100, TextWrapping = TextWrapping.Wrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda");
				SUT.Document.GetRange(0, 0).ParagraphFormat.SetIndents(-12, 24, 0);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(SUT);
				var firstLine = block.ParsedText.GetLineAt(0);
				Assert.IsFalse(firstLine.lastLine, "The test text should wrap under the constrained width.");
				var first = block.ParsedText.GetRectForIndex(0);
				var continuation = block.ParsedText.GetRectForIndex(firstLine.start + firstLine.length);

				Assert.AreEqual(16, continuation.X - first.X, 1.5, "The continuation line should use the full left indent while the first line hangs by 12 points.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Nested_Lists_Number_And_Render_Markers()
		{
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
				Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "outer\rnested\rresume");

				ConfigureListParagraph(SUT.Document.GetRange(0, 0).ParagraphFormat, level: 1, leftIndent: 24);
				ConfigureListParagraph(SUT.Document.GetRange(6, 6).ParagraphFormat, level: 2, leftIndent: 36);
				ConfigureListParagraph(SUT.Document.GetRange(13, 13).ParagraphFormat, level: 1, leftIndent: 24);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(SUT);
				var markers = block.Inlines
					.OfType<Run>()
					.Select(run => run.ParagraphLayout?.MarkerText)
					.Where(marker => marker is not null)
					.ToArray();
				CollectionAssert.AreEqual(new[] { "1.", "1.", "2." }, markers);

				var textStart = block.ParsedText.GetRectForIndex(0).X + block.Padding.Left;
				var screenshot = await UITestHelper.ScreenShot(block);
				var redBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 15);
				Assert.IsTrue(redBounds.Left < textStart - 4, $"The marker should render before the indexed text origin; marker/text bounds began at {redBounds.Left}, text at {textStart}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Line_Separator_Does_Not_Restart_Paragraph_Indent()
		{
			var SUT = new RichEditBox { Width = 260, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "a\u2028b\u2029c");
				SUT.Document.GetRange(0, 0).ParagraphFormat.SetIndents(9, 18, 0);
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(SUT);
				var first = block.ParsedText.GetRectForIndex(0);
				var afterLineSeparator = block.ParsedText.GetRectForIndex(2);
				var afterParagraphSeparator = block.ParsedText.GetRectForIndex(4);

				Assert.AreEqual(12, first.X - afterLineSeparator.X, 1.5, "U+2028 should continue the paragraph and drop only the first-line indent.");
				Assert.AreEqual(24, afterLineSeparator.X - afterParagraphSeparator.X, 1.5, "U+2029 should begin a separately formatted paragraph.");
				Assert.AreEqual(0, SUT.Document.GetRange(4, 4).ParagraphFormat.LeftIndent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Rtl_Paragraph_List_Projects_Direction_And_Mirrored_Indents()
		{
			var SUT = new RichEditBox { Width = 300, TextWrapping = TextWrapping.Wrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ltr\rאבג");
				var rtl = SUT.Document.GetRange(4, 4).ParagraphFormat;
				rtl.RightToLeft = FormatEffect.On;
				rtl.Alignment = ParagraphAlignment.Right;
				rtl.SetIndents(6, 12, 18);
				rtl.ListType = MarkerType.Arabic;
				rtl.ListStyle = MarkerStyle.Period;
				rtl.ListLevelIndex = 1;
				rtl.ListStart = 1;
				rtl.ListTab = 12;
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(SUT);
				var rtlRun = block.Inlines.OfType<Run>().First(run => run.Text.Contains("אבג", StringComparison.Ordinal));
				Assert.AreEqual(FlowDirection.RightToLeft, rtlRun.FlowDirection);
				Assert.IsTrue(rtlRun.ParagraphLayout?.RightToLeft);
				Assert.AreEqual("1.", rtlRun.ParagraphLayout?.MarkerText);

				var logicalStart = block.ParsedText.GetRectForIndex(4);
				var logicalEnd = block.ParsedText.GetRectForIndex(7);
				Assert.IsTrue(
					logicalStart.X > logicalEnd.X,
					$"Logical RTL text should progress from the right edge toward the left. Parsed={block.ParsedText.GetType().Name}, TextLength={block.Text.Length}, Start={logicalStart}, End={logicalEnd}.");
				Assert.IsTrue(logicalStart.X <= 300 - 24 - 8, $"The RTL first line should honor its right + first-line indents, got X={logicalStart.X}.");
				Assert.IsTrue(double.IsFinite(logicalStart.X) && double.IsFinite(logicalEnd.X));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Justified_Paragraph_Expands_Wrapped_Nonfinal_Lines()
		{
			const string text = "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda";
			var left = new RichEditBox { Width = 150, TextWrapping = TextWrapping.Wrap };
			var justified = new RichEditBox { Width = 150, TextWrapping = TextWrapping.Wrap };
			var panel = new StackPanel();
			panel.Children.Add(left);
			panel.Children.Add(justified);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				left.Document.SetText(TextSetOptions.None, text);
				justified.Document.SetText(TextSetOptions.None, text);
				justified.Document.GetRange(0, 0).ParagraphFormat.Alignment = ParagraphAlignment.Justify;
				await WindowHelper.WaitForIdle();

				var leftText = GetDisplayBlock(left).ParsedText;
				var justifiedText = GetDisplayBlock(justified).ParsedText;
				var firstLine = justifiedText.GetLineAt(0);
				Assert.IsFalse(firstLine.lastLine);
				var lastCharacter = firstLine.start + firstLine.length - 1;
				var leftRect = leftText.GetRectForIndex(lastCharacter);
				var justifiedRect = justifiedText.GetRectForIndex(lastCharacter);
				Assert.IsGreaterThan(
					leftRect.Right + 5,
					justifiedRect.Right,
					$"Justification should expand the first wrapped line toward the right content edge, got {leftRect.Right} and {justifiedRect.Right}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Paragraph_Marker_Families_Project_Correct_Glyphs()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "black\rwhite\rjapanese");
				ConfigureMarker(SUT.Document.GetRange(0, 0).ParagraphFormat, MarkerType.BlackCircleWingding);
				ConfigureMarker(SUT.Document.GetRange(6, 6).ParagraphFormat, MarkerType.WhiteCircleWingding);
				ConfigureMarker(SUT.Document.GetRange(12, 12).ParagraphFormat, MarkerType.JapanSimplifiedChinese);
				await WindowHelper.WaitForIdle();

				var markers = GetDisplayBlock(SUT).Inlines.OfType<Run>()
					.Select(run => run.ParagraphLayout?.MarkerText)
					.Where(marker => marker is not null)
					.ToArray();
				CollectionAssert.AreEqual(new[] { "➊.", "➀.", "一．" }, markers);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}

			static void ConfigureMarker(ITextParagraphFormat format, MarkerType type)
			{
				format.SetIndents(0, 18, 0);
				format.ListType = type;
				format.ListStyle = MarkerStyle.Period;
				format.ListLevelIndex = 1;
				format.ListStart = 1;
				format.ListTab = 12;
			}
		}

		private static TextBlock GetDisplayBlock(RichEditBox editor)
		{
			var block = editor.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
			Assert.IsNotNull(block);
			return block;
		}

		[TestMethod]
		public async Task When_ScrollIntoView_Honors_Endpoint_And_Axis_Options()
		{
			var SUT = new RichEditBox { Width = 120, Height = 60, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 20).Select(value => $"Line {value:D2} with long trailing content"));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);
				var lastLineStart = text.LastIndexOf("Line", StringComparison.Ordinal);
				var range = SUT.Document.GetRange(0, text.Length);

				range.ScrollIntoView(PointOptions.NoHorizontalScroll);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, scrollViewer.HorizontalOffset, 0.5, "NoHorizontalScroll must preserve horizontal offset.");
				Assert.IsTrue(scrollViewer.VerticalOffset > 0, "The range end should scroll vertically into view.");

				scrollViewer.ChangeView(0, 0, null, disableAnimation: true);
				await WindowHelper.WaitForIdle();
				var endRange = SUT.Document.GetRange(lastLineStart, text.Length);
				endRange.ScrollIntoView(PointOptions.Start | PointOptions.NoVerticalScroll);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(0, scrollViewer.VerticalOffset, 0.5, "NoVerticalScroll must preserve vertical offset.");
				Assert.IsTrue(scrollViewer.HorizontalOffset <= 1, "The selected start endpoint is at the line's left edge.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Automation_ScrollIntoView_Aligns_Viewport_Edge()
		{
			var SUT = new RichEditBox { Width = 120, Height = 60, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 20).Select(value => $"Line {value:D2} with long trailing content"));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				var displayBlock = scrollViewer?.Content as TextBlock;
				Assert.IsNotNull(scrollViewer);
				Assert.IsNotNull(displayBlock);
				var rangeStart = text.IndexOf("Line 10", StringComparison.Ordinal);
				var rangeEnd = rangeStart + "Line 10".Length;
				SUT.Document.Selection.SetRange(rangeStart, rangeEnd);
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var provider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
				Assert.IsNotNull(provider);
				var range = provider.GetSelection()[0];

				var startRect = displayBlock.ParsedText.GetRectForIndex(rangeStart);
				range.ScrollIntoView(alignToTop: true);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(
					Math.Clamp(startRect.Top, 0, scrollViewer.ScrollableHeight),
					scrollViewer.VerticalOffset,
					1);

				var endRect = displayBlock.ParsedText.GetRectForIndex(rangeEnd);
				range.ScrollIntoView(alignToTop: false);
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(
					Math.Clamp(endRect.Bottom - scrollViewer.ViewportHeight, 0, scrollViewer.ScrollableHeight),
					scrollViewer.VerticalOffset,
					1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Automation_Bounding_Rectangles_Are_Per_Visual_Line()
		{
			var SUT = new RichEditBox { Width = 180, Height = 120, TextWrapping = TextWrapping.Wrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "First line\rSecond line\rThird line");
				SUT.Document.Selection.SetRange(0, SUT.Document.TextLength);
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var provider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
				Assert.IsNotNull(provider);
				provider.GetSelection()[0].GetBoundingRectangles(out var rectangles);

				Assert.AreEqual(0, rectangles.Length % 4);
				Assert.IsGreaterThanOrEqualTo(12, rectangles.Length);
				Assert.IsLessThan(rectangles[5], rectangles[1]);
				for (var i = 0; i < rectangles.Length; i += 4)
				{
					Assert.IsGreaterThanOrEqualTo(0, rectangles[i + 2]);
					Assert.IsGreaterThan(0, rectangles[i + 3]);
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Keyboard_Caret_Movement_Scrolls_Internal_Viewport()
		{
			var SUT = new RichEditBox
			{
				Width = 180,
				Height = 80,
				TextWrapping = TextWrapping.Wrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 40).Select(value => $"Line {value:D2} with wrapped content"));
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.Selection.SetRange(0, 0);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);
				Assert.AreEqual(0, scrollViewer.VerticalOffset, 0.5);

				RaiseDocumentEndKey(SUT);
				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "Keyboard movement to the document end should scroll the internal viewport.");
				Assert.AreEqual(text.Length, SUT.Document.Selection.StartPosition);

				RaiseDocumentHomeKey(SUT);
				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset <= 0.5,
					timeoutMS: 5000,
					message: "Keyboard movement to the document start should restore the internal viewport.");
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Keyboard_Caret_Movement_Scrolls_Horizontally()
		{
			var SUT = new RichEditBox
			{
				Width = 140,
				Height = 60,
				TextWrapping = TextWrapping.NoWrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				const string text = "A long line whose final caret is well beyond the viewport width";
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.Selection.SetRange(0, 0);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);
				Assert.AreEqual(0, scrollViewer.HorizontalOffset, 0.5);

				RaiseDocumentEndKey(SUT);
				await WindowHelper.WaitFor(
					() => scrollViewer.HorizontalOffset > 0,
					timeoutMS: 5000,
					message: "Keyboard movement to the line end should scroll horizontally.");
				var originalOffset = scrollViewer.HorizontalOffset;

				SUT.FontSize *= 2;
				await WindowHelper.WaitFor(
					() => scrollViewer.HorizontalOffset > originalOffset + 1,
					timeoutMS: 5000,
					message: "A font geometry change should keep the unchanged document-end caret visible.");

				RaiseDocumentHomeKey(SUT);
				await WindowHelper.WaitFor(
					() => scrollViewer.HorizontalOffset <= 0.5,
					timeoutMS: 5000,
					message: "Keyboard movement to the line start should restore the horizontal viewport.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Disabled_Internal_Scrolling_Brings_Outer_Viewport_To_Caret()
		{
			var SUT = new RichEditBox
			{
				Width = 220,
				Height = 900,
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Top,
			};
			ScrollViewer.SetVerticalScrollMode(SUT, ScrollMode.Disabled);
			ScrollViewer.SetVerticalScrollBarVisibility(SUT, ScrollBarVisibility.Disabled);
			var outerScrollViewer = new ScrollViewer
			{
				Height = 140,
				Content = SUT,
			};
			try
			{
				WindowHelper.WindowContent = outerScrollViewer;
				await WindowHelper.WaitForLoaded(outerScrollViewer);
				var text = string.Join('\r', Enumerable.Range(0, 45).Select(value => $"Outer line {value:D2}"));
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.Selection.SetRange(0, 0);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				RaiseDocumentEndKey(SUT);
				await WindowHelper.WaitFor(
					() => outerScrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "The outer ScrollViewer should bring the document-end caret into view.");
				Assert.IsTrue(outerScrollViewer.VerticalOffset < outerScrollViewer.ScrollableHeight + 0.5);
				var endOffset = outerScrollViewer.VerticalOffset;

				RaiseDocumentHomeKey(SUT);
				await WindowHelper.WaitFor(
					() => outerScrollViewer.VerticalOffset < endOffset - 1,
					timeoutMS: 5000,
					message: "The outer ScrollViewer should scroll upward toward the document-start caret.");
				var displayBlock = GetDisplayBlock(SUT);
				var caretRect = displayBlock.ParsedText.GetRectForIndex(0);
				var caretInViewport = displayBlock.TransformToVisual(outerScrollViewer).TransformBounds(caretRect);
				Assert.IsTrue(
					caretInViewport.Bottom >= 0 && caretInViewport.Top <= outerScrollViewer.ActualHeight,
					$"The document-start caret should be inside the outer viewport, but was {caretInViewport}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_StartActive_Selection_Scrolls_To_Start()
		{
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(new FakeImeTextBoxExtension());
			var SUT = new RichEditBox
			{
				Width = 180,
				Height = 80,
				TextWrapping = TextWrapping.NoWrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 40).Select(value => $"Line {value:D2}"));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);
				SUT.Document.Selection.SetRange(text.Length, text.Length);
				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "Moving the programmatic caret to the end should scroll downward.");

				SUT.Document.Selection.SetRange(0, text.Length);
				SUT.Document.Selection.Options |= SelectionOptions.StartActive;
				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset <= 0.5,
					timeoutMS: 5000,
					message: "A StartActive programmatic selection should scroll to its active start endpoint.");
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				Assert.AreEqual(0, SUT.SelectionStartForTesting);
				Assert.AreEqual(text.Length, SUT.SelectionLengthForTesting);

				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting, "Focusing must preserve the TOM's active start endpoint.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Backward_Selection_Gripper_Scrolls_To_Physical_Endpoint()
		{
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(new FakeImeTextBoxExtension());
			var SUT = new RichEditBox
			{
				Width = 220,
				Height = 900,
				TextWrapping = TextWrapping.NoWrap,
				VerticalAlignment = VerticalAlignment.Top,
			};
			ScrollViewer.SetVerticalScrollMode(SUT, ScrollMode.Disabled);
			ScrollViewer.SetVerticalScrollBarVisibility(SUT, ScrollBarVisibility.Disabled);
			var outerScrollViewer = new ScrollViewer
			{
				Height = 140,
				Content = SUT,
			};
			try
			{
				WindowHelper.WindowContent = outerScrollViewer;
				await WindowHelper.WaitForLoaded(outerScrollViewer);
				var text = string.Join('\r', Enumerable.Range(0, 45).Select(value => $"Gripper line {value:D2}"));
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Focus(FocusState.Programmatic);
				SUT.Document.Selection.SetRange(0, text.Length);
				SUT.Document.Selection.Options |= SelectionOptions.StartActive;
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);

				((ITextSelectionGripperHost)SUT).ScrollForGripper(isEndGripper: true);
				await WindowHelper.WaitFor(
					() => outerScrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "The upper-index gripper should scroll to the document end even for a backward selection.");
				var endOffset = outerScrollViewer.VerticalOffset;

				((ITextSelectionGripperHost)SUT).ScrollForGripper(isEndGripper: false);
				await WindowHelper.WaitFor(
					() => outerScrollViewer.VerticalOffset < endOffset - 1,
					timeoutMS: 5000,
					message: "The lower-index gripper should scroll back toward the document start.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Preconfigured_Caret_Scrolls_After_Load()
		{
			var SUT = new RichEditBox
			{
				Width = 180,
				Height = 80,
				TextWrapping = TextWrapping.NoWrap,
			};
			var text = string.Join('\r', Enumerable.Range(0, 40).Select(value => $"Preconfigured line {value:D2}"));
			SUT.Document.SetText(TextSetOptions.None, text);
			SUT.Document.Selection.SetRange(text.Length, text.Length);
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);
				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "A caret configured before templating should be brought into view after load.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Batched_Caret_Scrolls_After_Display_Updates()
		{
			var SUT = new RichEditBox
			{
				Width = 180,
				Height = 80,
				TextWrapping = TextWrapping.NoWrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Initial");
				await WindowHelper.WaitForIdle();
				var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				Assert.IsNotNull(scrollViewer);

				var text = string.Join('\r', Enumerable.Range(0, 40).Select(value => $"Batched line {value:D2}"));
				SUT.Document.BatchDisplayUpdates();
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.Selection.SetRange(0, text.Length);
				SUT.Document.Selection.Options |= SelectionOptions.StartActive;
				((ITextSelectionGripperHost)SUT).ScrollForGripper(isEndGripper: true);
				Assert.AreEqual(0, SUT.Document.ApplyDisplayUpdates());

				await WindowHelper.WaitFor(
					() => scrollViewer.VerticalOffset > 0,
					timeoutMS: 5000,
					message: "Applying batched display updates should preserve the requested upper gripper target.");
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(text.Length, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static void RaiseDocumentEndKey(RichEditBox editor)
		{
			var key = Uno.UI.Helpers.DeviceTargetHelper.UsesAppleKeyboardLayout ? VirtualKey.Down : VirtualKey.End;
			RaiseKey(editor, key, VirtualKeyModifiers.Control);
		}

		private static void RaiseDocumentHomeKey(RichEditBox editor)
		{
			var key = Uno.UI.Helpers.DeviceTargetHelper.UsesAppleKeyboardLayout ? VirtualKey.Up : VirtualKey.Home;
			RaiseKey(editor, key, VirtualKeyModifiers.Control);
		}

		[TestMethod]
		public async Task When_AdvancedCharacterFormat_Persists_And_Renders_FontStretch()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "format");

				var range = SUT.Document.GetRange(0, 6);
				var value = range.CharacterFormat.GetClone();
				value.AllCaps = FormatEffect.On;
				value.BackgroundColor = Microsoft.UI.Colors.Gold;
				value.FontStretch = global::Windows.UI.Text.FontStretch.Expanded;
				value.Hidden = FormatEffect.On;
				value.Kerning = 8;
				value.LanguageTag = "el-GR";
				value.Outline = FormatEffect.On;
				value.Position = 2.5f;
				value.ProtectedText = FormatEffect.On;
				value.SmallCaps = FormatEffect.On;
				value.Spacing = 1.5f;
				value.Subscript = FormatEffect.On;
				value.Superscript = FormatEffect.Off;
				value.TextScript = TextScript.Greek;
				range.CharacterFormat.SetClone(value);
				await WindowHelper.WaitForIdle();

				var actual = range.CharacterFormat;
				Assert.AreEqual(FormatEffect.On, actual.AllCaps);
				Assert.AreEqual(Microsoft.UI.Colors.Gold, actual.BackgroundColor);
				Assert.AreEqual(global::Windows.UI.Text.FontStretch.Expanded, actual.FontStretch);
				Assert.AreEqual(FormatEffect.On, actual.Hidden);
				Assert.AreEqual(8f, actual.Kerning);
				Assert.AreEqual("el-GR", actual.LanguageTag);
				Assert.AreEqual(FormatEffect.On, actual.Outline);
				Assert.AreEqual(2.5f, actual.Position);
				Assert.AreEqual(FormatEffect.On, actual.ProtectedText);
				Assert.AreEqual(FormatEffect.On, actual.SmallCaps);
				Assert.AreEqual(1.5f, actual.Spacing);
				Assert.AreEqual(FormatEffect.On, actual.Subscript);
				Assert.AreEqual(FormatEffect.Off, actual.Superscript);
				Assert.AreEqual(TextScript.Greek, actual.TextScript);

				actual.ProtectedText = FormatEffect.Off;
				actual.Hidden = FormatEffect.Off;
				await WindowHelper.WaitForIdle();

				var contentElement = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				var block = contentElement?.Content as TextBlock;
				Assert.IsNotNull(block);
				var run = block.Inlines.OfType<Run>().Single();
				Assert.AreEqual(global::Windows.UI.Text.FontStretch.Expanded, run.FontStretch);
				Assert.AreEqual(Microsoft.UI.Colors.Gold, run.CharacterBackground);

				var screenshot = await UITestHelper.ScreenShot(SUT);
				var backgroundBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Gold, tolerance: 10);
				Assert.IsTrue(backgroundBounds is { Width: > 5, Height: > 5 }, $"The character background should be visible, bounds were {backgroundBounds}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_CharacterFormat_Weight_Preserves_Exact_OpenType_Value()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);
			source.Document.SetText(TextSetOptions.None, "ab");

			var first = source.Document.GetRange(0, 1);
			for (var weight = 0; weight <= 950; weight += 50)
			{
				first.CharacterFormat.Weight = weight;
				Assert.AreEqual(weight, first.CharacterFormat.Weight);
			}

			first.CharacterFormat.Weight = 350;
			source.Document.GetRange(1, 2).CharacterFormat.Weight = 900;
			Assert.AreEqual(TextConstants.UndefinedInt32Value, source.Document.GetRange(0, 2).CharacterFormat.Weight);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => first.CharacterFormat.Weight = 1000);

			await WindowHelper.WaitForIdle();
			var contentElement = source.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			var block = contentElement?.Content as TextBlock;
			Assert.IsNotNull(block);
			var runs = block.Inlines.OfType<Run>().ToArray();
			Assert.AreEqual((ushort)350, runs[0].FontWeight.Weight);
			Assert.AreEqual((ushort)900, runs[1].FontWeight.Weight);

			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
			Assert.IsTrue(rtf.Contains(",350,", StringComparison.Ordinal), $"The exact weight metadata is missing from: {rtf}");
			target.Document.SetText(TextSetOptions.FormatRtf, rtf);
			Assert.AreEqual(350, target.Document.GetRange(0, 1).CharacterFormat.Weight, $"RTF: {rtf}");
			Assert.AreEqual(900, target.Document.GetRange(1, 2).CharacterFormat.Weight, $"RTF: {rtf}");
		}

		[TestMethod]
		public async Task When_NoOp_Replacement_Synchronizes_Interactive_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(2, 4);
				Assert.AreEqual(2, SUT.NativeSelectionStart);
				Assert.AreEqual(2, SUT.NativeSelectionLength);

				SUT.Document.GetRange(1, 5).Text = "bcde";

				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, SUT.NativeSelectionStart);
				Assert.AreEqual(0, SUT.NativeSelectionLength);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Explicit_Weight_400_Overrides_Bold_Control_Font()
		{
			var SUT = new RichEditBox { FontWeight = Microsoft.UI.Text.FontWeights.Bold };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "normal");
				SUT.Document.GetRange(0, 6).CharacterFormat.Weight = 400;
				await WindowHelper.WaitForIdle();

				var block = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
				Assert.IsNotNull(block);
				Assert.AreEqual((ushort)400, block.Inlines.OfType<Run>().Single().FontWeight.Weight);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
		public async Task When_Malformed_Rtf_Clipboard_Falls_Back_To_Plain_Text()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var package = new DataPackage();
				package.SetRtf(@"{\rtf1\red999999999999999999999 broken");
				package.SetText("fallback");
				Clipboard.SetContent(package);

				SUT.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(SUT.Document, out var text);
					return text == "fallback";
				});
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Clipboard_Text_Normalizes_Line_Endings_Before_MaxLength()
		{
			var SUT = new RichEditBox { MaxLength = 2 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual("\rx", SUT.Document.NormalizeImportedPlainText("\r\nx", 0, 0));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Rtf_Unsafe_Hyperlink_Metadata_Is_Preserved_But_Not_Activated()
		{
			var SUT = new RichEditBox();
			var confirmationCount = 0;
			var launchCount = 0;
			SUT.LinkConfirmationForTesting = _ =>
			{
				confirmationCount++;
				return Task.FromResult(false);
			};
			SUT.LinkLauncherForTesting = _ =>
			{
				launchCount++;
				return Task.FromResult(true);
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1{\field{\*\fldinst HYPERLINK ""javascript:alert(1)""}{\fldrslt unsafe}}}");

				Assert.AreEqual("\"javascript:alert(1)\"", SUT.Document.GetRange(0, 6).Link);
				Assert.IsTrue(SUT.TryNavigateLinkAt(1));
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, confirmationCount);
				Assert.AreEqual(0, launchCount);
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("unsafe", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow("mailto:user@example.com", "mailto")]
		[DataRow("tel:+15555550100", "tel")]
		[DataRow("ms-settings:privacy", "ms-settings")]
		[DataRow("contoso-shell:open", "contoso-shell")]
		public void When_Programmatic_Absolute_Link_Is_Parsed(string target, string scheme)
		{
			Assert.IsTrue(RichEditBox.TryGetLinkUri($"\"{target}\"", out var uri));
			Assert.AreEqual(scheme, uri.Scheme);
		}

		[TestMethod]
		public void When_Rtf_Group_Budget_Is_Exceeded()
		{
			var rtf = new StringBuilder(@"{\rtf1");
			for (var i = 0; i < 65_537; i++)
			{
				rtf.Append("{}");
			}
			rtf.Append('}');

			Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf.ToString()));
		}

		[TestMethod]
		public void When_Rtf_Format_Run_Budget_Is_Exceeded()
		{
			var rtf = new StringBuilder(@"{\rtf1 ");
			for (var i = 0; i < 65_537; i++)
			{
				rtf.Append(i % 2 == 0 ? @"\b x" : @"\b0 x");
			}
			rtf.Append('}');

			Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf.ToString()));
		}

		[TestMethod]
		public async Task When_Many_Rtf_Format_Runs_Render_As_Bounded_Plain_Text()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var rtf = new StringBuilder(@"{\rtf1 ");
				for (var i = 0; i < 4100; i++)
				{
					rtf.Append(@"\b a\b0 b");
				}
				rtf.Append('}');

				SUT.Document.SetText(TextSetOptions.FormatRtf, rtf.ToString());
				await WindowHelper.WaitForIdle();

				var displayBlock = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
				Assert.IsNotNull(displayBlock);
				Assert.IsTrue(SUT.UsesBoundedRichLayout);
				Assert.HasCount(0, displayBlock.Inlines);
				Assert.AreEqual(8200, SUT.Document.TextLength);
				Assert.IsGreaterThan(0, displayBlock.ParsedText.GetRectForIndex(SUT.Document.TextLength - 1).Height);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public void When_Rtf_Control_Token_Budget_Is_Exceeded()
		{
			var rtf = new StringBuilder(@"{\rtf1 ");
			for (var i = 0; i < 262_145; i++)
			{
				rtf.Append(@"\a");
			}
			rtf.Append('}');

			Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf.ToString()));
		}

		[TestMethod]
		public void When_Rtf_Color_Budget_Is_Exceeded()
		{
			var rtf = new StringBuilder(@"{\rtf1{\colortbl;");
			for (var i = 0; i < 4_097; i++)
			{
				rtf.Append(@"\red0\green0\blue0;");
			}
			rtf.Append("}x}");

			Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf.ToString()));
		}

		[TestMethod]
		public void When_Rtf_Paragraph_Metrics_Are_Bounded()
		{
			const string rtf = @"{\rtf1\li2147483647{\*\unopara 0,0,3.4028235E+38,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,}x}";

			var fragment = RichTextRtfCodec.Read(rtf);

			Assert.AreEqual(4096f, fragment.GetParagraphFormatAt(0).LeftIndent);
		}

		[TestMethod]
		public void When_Rtf_Malformed_Picture_Attempt_Budget_Is_Exceeded()
		{
			var rtf = new StringBuilder(@"{\rtf1");
			for (var i = 0; i < 4096; i++)
			{
				rtf.Append(@"{\pict\pngblip invalid}");
			}
			rtf.Append('}');

			var (_, clones) = TrackFormattingClones(() =>
			{
				Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf.ToString()));
				return true;
			});
			Assert.IsLessThan(512, clones.Character);
			Assert.IsLessThan(512, clones.Paragraph);
		}

		[TestMethod]
		public void When_Clipboard_Rtf_Protection_Is_Stripped()
		{
			var fragment = RichTextRtfCodec.Read(@"{\rtf1\protect protected}");

			fragment = RichEditTextDocument.SanitizeClipboardFragment(fragment);

			Assert.IsFalse(fragment.GetCharacterFormatAt(0).ProtectedText);
		}

		[TestMethod]
		public void When_Rtf_Parser_Rejects_Projected_Text_Above_Limit()
		{
			var rtf = @"{\rtf1 " + new string('a', 262_145) + "}";

			Assert.ThrowsExactly<ArgumentException>(() => RichTextRtfCodec.Read(rtf, maxCharacters: 262_144));
		}

		[TestMethod]
		public async Task When_Rtf_Exact_Character_Cap_Still_Parses_Terminal_Paragraph_Metadata()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				const string rtf = @"{\rtf1 a{\*\unopara 3,0,12,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,}}";
				var fragment = RichTextRtfCodec.Read(rtf, maxCharacters: 1);

				Assert.AreEqual("a", fragment.Text);
				Assert.IsTrue(fragment.HasExplicitTerminalParagraphState);
				Assert.AreEqual(ParagraphAlignment.Right, fragment.TerminalParagraphState.Alignment);
				Assert.AreEqual(12f, fragment.TerminalParagraphState.LeftIndent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_MathML_Rejects_Projected_Text_Above_Hard_Limit()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);
				var beforeStory = SUT.Document.GetRange(0, int.MaxValue).Text;
				var beforeProjection = SUT.Document.MathProjection;
				SUT.Document.GetMathML(out var beforeMathML);
				var mathML = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtext>"
					+ new string('a', 262_145)
					+ "</mtext></math>";

				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetMathML(mathML));
				Assert.AreEqual(beforeStory, SUT.Document.GetRange(0, int.MaxValue).Text);
				Assert.AreEqual(beforeProjection, SUT.Document.MathProjection);
				SUT.Document.GetMathML(out var afterMathML);
				Assert.AreEqual(beforeMathML, afterMathML);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_TextChanging_HandlerThrows_PropagatesAndRendersFinalState()
		{
			var SUT = new RichEditBox();
			var failure = new InvalidOperationException("TextChanging handler");
			var changingCount = 0;
			global::Windows.Foundation.TypedEventHandler<RichEditBox, RichEditBoxTextChangingEventArgs> handler = (_, _) =>
			{
				if (++changingCount == 1)
				{
					SUT.Document.SetText(TextSetOptions.None, "nested");
					throw failure;
				}
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.TextChanging += handler;

				Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Document.SetText(TextSetOptions.None, "abc")));
				GetTextWithoutFinalEop(SUT.Document, out var nested);
				Assert.AreEqual("nested", nested);
				Assert.AreEqual("nested", GetDisplayBlock(SUT).Text);
				Assert.AreEqual(1, changingCount);

				SUT.Document.SetText(TextSetOptions.None, "next");
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("next", text);
				Assert.AreEqual("next", GetDisplayBlock(SUT).Text);
				Assert.AreEqual(2, changingCount);
			}
			finally
			{
				SUT.TextChanging -= handler;
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Interactive_Edit_Over_Mixed_Protected_Selection_Is_Rejected()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.GetRange(2, 4).CharacterFormat.ProtectedText = FormatEffect.On;
				SUT.Document.Selection.SetRange(1, 5);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				RaiseKey(SUT, VirtualKey.X, unicodeKey: 'x');
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
				Assert.AreEqual(4, SUT.SelectionLengthForTesting);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Ime_Edit_Over_Protected_Selection_Is_Rejected()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.GetRange(2, 4).CharacterFormat.ProtectedText = FormatEffect.On;
				SUT.Document.Selection.SetRange(1, 5);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				fake.SimulateCompositionStart();
				fake.SimulateCompositionUpdate("x");
				fake.SimulateCompositionComplete("x");
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_CharacterFormat_Spacing_Affects_Layout_And_HitTesting()
		{
			var SUT = new RichEditBox { Width = 400, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ABCD");
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(0, 4);
				range.GetRect(PointOptions.ClientCoordinates, out var before, out _);
				range.CharacterFormat.Spacing = 8;
				await WindowHelper.WaitForIdle();
				range.GetRect(PointOptions.ClientCoordinates, out var after, out _);
				Assert.IsTrue(after.Width > before.Width + 20, $"Character spacing should widen the range, was {before.Width} then {after.Width}.");

				var contentElement = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				var block = contentElement?.Content as TextBlock;
				Assert.IsNotNull(block);
				Assert.IsTrue(block.Inlines.OfType<Run>().Single().CharacterSpacing > 0);

				SUT.Document.GetRange(3, 3).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.ClientCoordinates, out var point);
				var recovered = SUT.Document.GetRangeFromPoint(point, PointOptions.ClientCoordinates);
				Assert.IsTrue(Math.Abs(recovered.StartPosition - 3) <= 1, $"Spaced text hit testing should recover the final character, was {recovered.StartPosition}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_TextChanged_Handler_Reapplies_CharacterFormat_Typing_Stays_Stable()
		{
			// Mirrors WinUI RichEditBoxTests.cs:43 UpdateCharacterFormatForTextChangedEvent (the Sticky Notes
			// pattern): the handler toggles CharacterFormat from inside TextChanged for every keystroke. The
			// reentrant document mutation must not recurse indefinitely nor drop typed characters.
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			var oldText = string.Empty;
			var handlerInvocations = 0;
			SUT.TextChanged += (s, e) =>
			{
				handlerInvocations++;
				var document = SUT.Document;
				document.GetText(TextGetOptions.None, out var currentText);
				if (currentText != oldText)
				{
					oldText = currentText;
					var range = document.GetRange(0, 0);
					range.Expand(TextRangeUnit.CharacterFormat);
					var format = range.CharacterFormat;
					format.Bold = FormatEffect.Toggle;
					range.CharacterFormat = format;
				}
			};

			const int keystrokes = 25;
			await TypeAsync(SUT, new string('a', keystrokes));

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual(new string('a', keystrokes), text);
			Assert.IsGreaterThanOrEqualTo(keystrokes, handlerInvocations);
			// A reentrant format toggle must settle instead of re-entering once per nested change.
			Assert.IsLessThanOrEqualTo(keystrokes * 4, handlerInvocations);
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		// Android and UIKit clipboard backends do not expose custom RTF formats.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaUIKit)]
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
					GetTextWithoutFinalEop(SUT.Document, out var text);
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
		public async Task When_CaretType_Null_Suppresses_Visual_Caret()
		{
			var SUT = new RichEditBox { Width = 240 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				SUT.Document.Selection.SetRange(1, 1);

				Assert.AreEqual(CaretType.Normal, SUT.Document.CaretType);
				Assert.IsTrue(SUT.IsCaretRenderedForTesting);

				SUT.Document.CaretType = CaretType.Null;
				Assert.AreEqual(CaretType.Null, SUT.Document.CaretType);
				Assert.IsFalse(SUT.IsCaretRenderedForTesting);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abc", text);

				SUT.Document.CaretType = CaretType.Normal;
				Assert.IsTrue(SUT.IsCaretRenderedForTesting);
				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.CaretType = (CaretType)int.MaxValue);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow(TextAlignment.Left, 0d)]
		[DataRow(TextAlignment.Center, 0.5d)]
		[DataRow(TextAlignment.Right, 1d)]
		public async Task When_AlignmentIncludesTrailingWhitespace_Changes_Aligned_Extent(TextAlignment alignment, double expectedShiftFactor)
		{
			const string text = "A   ";
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
				TextAlignment = alignment,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var parsed = GetDisplayBlock(SUT).ParsedText;
				var excludedStart = parsed.GetRectForIndex(0).X;
				var trailingWidth = parsed.GetRectForIndex(text.Length).X - parsed.GetRectForIndex(1).X;
				Assert.IsTrue(trailingWidth > 1, $"The test text should have measurable trailing whitespace, measured {trailingWidth}.");

				SUT.Document.AlignmentIncludesTrailingWhitespace = true;
				await WindowHelper.WaitForIdle();
				var includedStart = GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X;
				Assert.AreEqual(
					trailingWidth * expectedShiftFactor,
					excludedStart - includedStart,
					1,
					$"{alignment} alignment should shift by the matching fraction of trailing whitespace.");

				SUT.Document.AlignmentIncludesTrailingWhitespace = false;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(excludedStart, GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow("   A")]
		[DataRow("A A")]
		public async Task When_AlignmentIncludesTrailingWhitespace_Does_Not_Move_NonTrailing_Spaces(string text)
		{
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
				TextAlignment = TextAlignment.Center,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var before = GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X;
				SUT.Document.AlignmentIncludesTrailingWhitespace = true;
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(before, GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_AlignmentIncludesTrailingWhitespace_Mirrors_Rtl_Trailing_Spaces()
		{
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
				TextAlignment = TextAlignment.Center,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "אבג   ");
				SUT.Document.GetRange(0, 0).ParagraphFormat.RightToLeft = FormatEffect.On;
				await WindowHelper.WaitForIdle();

				var excludedStart = GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X;
				SUT.Document.AlignmentIncludesTrailingWhitespace = true;
				await WindowHelper.WaitForIdle();
				var includedStart = GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X;
				Assert.IsTrue(
					includedStart > excludedStart + 1,
					$"Including visual-left RTL trailing spaces should move the full centered run right, from {excludedStart} to {includedStart}.");

				SUT.Document.AlignmentIncludesTrailingWhitespace = false;
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(excludedStart, GetDisplayBlock(SUT).ParsedText.GetRectForIndex(0).X, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[DataRow(TextAlignment.Left, 0d, 1d)]
		[DataRow(TextAlignment.Center, 0.5d, 0.5d)]
		[DataRow(TextAlignment.Right, 1d, 0d)]
		public async Task When_IgnoreTrailingCharacterSpacing_Changes_Aligned_Extent(
			TextAlignment alignment,
			double expectedStartShiftFactor,
			double expectedEndShiftFactor)
		{
			const float spacingInPoints = 12;
			const double expectedSpacingInDips = spacingInPoints * 4d / 3d;
			const string text = "AAAA";
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
				TextAlignment = alignment,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				var range = SUT.Document.GetRange(0, text.Length);
				range.CharacterFormat.Spacing = spacingInPoints;
				range.CharacterFormat.Underline = UnderlineType.Single;
				range.CharacterFormat.Strikethrough = FormatEffect.On;
				await WindowHelper.WaitForIdle();

				var parsed = GetDisplayBlock(SUT).ParsedText;
				var spacedStart = parsed.GetRectForIndex(0).X;
				var spacedEnd = parsed.GetRectForIndex(text.Length).X;

				SUT.Document.IgnoreTrailingCharacterSpacing = true;
				await WindowHelper.WaitForIdle();
				parsed = GetDisplayBlock(SUT).ParsedText;
				var ignoredStart = parsed.GetRectForIndex(0).X;
				var ignoredEnd = parsed.GetRectForIndex(text.Length).X;

				Assert.AreEqual(expectedSpacingInDips * expectedStartShiftFactor, ignoredStart - spacedStart, 0.25);
				Assert.AreEqual(expectedSpacingInDips * expectedEndShiftFactor, spacedEnd - ignoredEnd, 0.25);
				var endRect = parsed.GetRectForIndex(text.Length);
				Assert.AreEqual(
					text.Length,
					parsed.GetIndexAt(new Windows.Foundation.Point(endRect.X - 0.25, endRect.Y + endRect.Height / 2), false, true));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_IgnoreTrailingCharacterSpacing_Applies_To_Each_Line()
		{
			const float spacingInPoints = 12;
			const double expectedSpacingInDips = spacingInPoints * 4d / 3d;
			const string text = "AAAA\rbbbb";
			var SUT = new RichEditBox
			{
				Width = 320,
				TextWrapping = TextWrapping.NoWrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.GetRange(0, text.Length).CharacterFormat.Spacing = spacingInPoints;
				await WindowHelper.WaitForIdle();

				var parsed = GetDisplayBlock(SUT).ParsedText;
				var spacedFirstEnd = parsed.GetRectForIndex(4).X;
				var spacedSecondEnd = parsed.GetRectForIndex(text.Length).X;

				SUT.Document.IgnoreTrailingCharacterSpacing = true;
				await WindowHelper.WaitForIdle();
				parsed = GetDisplayBlock(SUT).ParsedText;
				var ignoredFirstEnd = parsed.GetRectForIndex(4).X;
				var ignoredSecondEnd = parsed.GetRectForIndex(text.Length).X;

				Assert.AreEqual(expectedSpacingInDips, spacedFirstEnd - ignoredFirstEnd, 0.25);
				Assert.AreEqual(expectedSpacingInDips, spacedSecondEnd - ignoredSecondEnd, 0.25);
				Assert.IsTrue(parsed.GetRectForIndex(4).Y < parsed.GetRectForIndex(5).Y);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_IgnoreTrailingCharacterSpacing_Applies_To_Each_Wrapped_Line()
		{
			const float spacingInPoints = 6;
			const double expectedSpacingInDips = spacingInPoints * 4d / 3d;
			const string text = "alpha beta gamma delta epsilon zeta eta theta";
			var SUT = new RichEditBox
			{
				Width = 140,
				TextWrapping = TextWrapping.Wrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.GetRange(0, text.Length).CharacterFormat.Spacing = spacingInPoints;
				await WindowHelper.WaitForIdle();

				var parsed = GetDisplayBlock(SUT).ParsedText;
				var firstLine = parsed.GetLineAt(0);
				Assert.IsFalse(firstLine.lastLine, "The constrained editor should create multiple visual lines.");
				var firstLineEnd = firstLine.start + firstLine.length;
				var firstSpacedEnd = parsed.GetRectForIndex(firstLineEnd - 1).Right;
				var finalSpacedEnd = parsed.GetRectForIndex(text.Length).X;

				SUT.Document.IgnoreTrailingCharacterSpacing = true;
				await WindowHelper.WaitForIdle();
				parsed = GetDisplayBlock(SUT).ParsedText;
				var ignoredFirstLine = parsed.GetLineAt(0);
				Assert.AreEqual(firstLineEnd, ignoredFirstLine.start + ignoredFirstLine.length);
				Assert.AreEqual(expectedSpacingInDips, firstSpacedEnd - parsed.GetRectForIndex(firstLineEnd - 1).Right, 0.25);
				Assert.AreEqual(expectedSpacingInDips, finalSpacedEnd - parsed.GetRectForIndex(text.Length).X, 0.25);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_IgnoreTrailingCharacterSpacing_Does_Not_Cross_Empty_Line_Boundaries()
		{
			const float spacingInPoints = 6;
			const double expectedSpacingInDips = spacingInPoints * 4d / 3d;
			const string text = "A\r\rB";
			var SUT = new RichEditBox
			{
				Width = 240,
				TextWrapping = TextWrapping.NoWrap,
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Document.GetRange(0, text.Length).CharacterFormat.Spacing = spacingInPoints;
				await WindowHelper.WaitForIdle();

				var parsed = GetDisplayBlock(SUT).ParsedText;
				var firstSpacedEnd = parsed.GetRectForIndex(1).Right;
				var emptyLineCaret = parsed.GetRectForIndex(2);
				var finalSpacedEnd = parsed.GetRectForIndex(text.Length).X;

				SUT.Document.IgnoreTrailingCharacterSpacing = true;
				await WindowHelper.WaitForIdle();
				parsed = GetDisplayBlock(SUT).ParsedText;

				Assert.AreEqual(expectedSpacingInDips, firstSpacedEnd - parsed.GetRectForIndex(1).Right, 0.25);
				Assert.AreEqual(emptyLineCaret.X, parsed.GetRectForIndex(2).X, 0.1);
				Assert.AreEqual(expectedSpacingInDips, finalSpacedEnd - parsed.GetRectForIndex(text.Length).X, 0.25);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Link_Renders_As_Hyperlink_Inline()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "link plain");
			SUT.Document.GetRange(0, 4).Link = "\"https://contoso.example\"";
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			var displayBlock = contentElement?.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.IsInstanceOfType<Hyperlink>(displayBlock.Inlines[0]);
			var hyperlink = (Hyperlink)displayBlock.Inlines[0];
			Assert.AreEqual("link", ((Run)hyperlink.Inlines[0]).Text);
		}

		[TestMethod]
		public async Task When_Rtf_Font_Size_In_Points_Renders_In_Dips()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\fs24 size}");
				await WindowHelper.WaitForIdle();

				var block = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement")?.Content as TextBlock;
				var run = block?.Inlines.OfType<Run>().Single();
				Assert.IsNotNull(run);
				Assert.AreEqual(12f, SUT.Document.GetRange(0, 4).CharacterFormat.Size);
				Assert.AreEqual(16d, run.FontSize, 0.01);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_MathMode_Changes_Clear_Content_And_Undo_History()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "before");
				Assert.IsTrue(SUT.Document.CanUndo());

				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);
				Assert.AreEqual(0, SUT.Document.TextLength);
				Assert.AreEqual("\r", SUT.Document.GetRange(0, int.MaxValue).Text);
				Assert.IsNull(SUT.Document.MathProjection);
				Assert.IsFalse(SUT.Document.CanUndo());
				Assert.AreEqual(RichEditMathMode.MathOnly, SUT.Document.GetMathMode());

				SUT.Document.SetText(TextSetOptions.None, "math");
				Assert.IsTrue(SUT.Document.CanUndo());
				SUT.Document.SetMathMode(RichEditMathMode.NoMath);
				Assert.AreEqual(0, SUT.Document.TextLength);
				Assert.AreEqual("\r", SUT.Document.GetRange(0, int.MaxValue).Text);
				Assert.IsNull(SUT.Document.MathProjection);
				Assert.IsFalse(SUT.Document.CanUndo());
				Assert.AreEqual(RichEditMathMode.NoMath, SUT.Document.GetMathMode());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetMathML_Projects_Presentation_Math_And_Canonicalizes_Source()
		{
			const string mathML = "<mml:math xmlns:mml=\"http://www.w3.org/1998/Math/MathML\" display=\"block\">"
				+ "<mml:msup><mml:mi mathcolor=\"#FF0000\">x</mml:mi><mml:mn>3</mml:mn></mml:msup>"
				+ "<mml:mo>+</mml:mo><mml:mfrac><mml:mn>1</mml:mn><mml:mn>2</mml:mn></mml:mfrac>"
				+ "</mml:math>";
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);

				SUT.Document.SetMathML(mathML);
				await WindowHelper.WaitForIdle();

				var projection = SUT.Document.MathProjection;
				Assert.IsNotNull(projection);
				Assert.AreEqual(projection + "\r", SUT.Document.GetRange(0, int.MaxValue).Text);
				SUT.Document.GetMathML(out var roundTripped);
				var canonical = System.Xml.Linq.XDocument.Parse(roundTripped);
				Assert.AreEqual("block", canonical.Root?.Attribute("display")?.Value);
				Assert.IsTrue(projection.Contains("\U0001D465", StringComparison.Ordinal));
				Assert.IsTrue(projection.Contains('3'));
				Assert.IsTrue(projection.Contains('1'));
				Assert.IsTrue(projection.Contains('2'));
				Assert.IsTrue(canonical.Descendants().Any(element => element.Name.LocalName == "mfrac"));
				var xAtom = SUT.Document.MathAtoms.Single(atom =>
					atom.Atom.ProjectionText.Contains("\U0001D465", StringComparison.Ordinal));
				Assert.AreEqual(
					Microsoft.UI.Colors.Red,
					SUT.Document.GetRange(xAtom.Span.Start, xAtom.Span.End).CharacterFormat.ForegroundColor);

				var contentElement = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
				var block = contentElement?.Content as TextBlock;
				Assert.IsNotNull(block);
				Assert.AreEqual(RichEditTextDocument.MathRenderingFontFamilyName, block.FontFamily.Source);
				Assert.IsTrue(block.Inlines.OfType<Run>().All(run =>
					run.FontFamily.Source == RichEditTextDocument.MathRenderingFontFamilyName));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SetMathML_Invalid_Or_Unsafe_Policy_Input_Is_Atomic()
		{
			const string validMathML = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>x</mi></math>";
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);
				SUT.Document.SetMathML(validMathML);
				SUT.Document.GetMathML(out var canonical);
				var projection = SUT.Document.MathProjection;
				var story = SUT.Document.GetRange(0, int.MaxValue).Text;
				SUT.Document.Selection.SetRange(0, 1);
				SUT.Document.ClearUndoRedoHistory();

				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetMathML("<math><mi>x</mi></math>"));
				Assert.AreEqual(projection, SUT.Document.MathProjection);
				Assert.AreEqual(story, SUT.Document.GetRange(0, int.MaxValue).Text);
				SUT.Document.GetMathML(out var afterInvalidMathML);
				Assert.AreEqual(canonical, afterInvalidMathML);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.IsFalse(SUT.Document.CanUndo());

				const string unsafeMathML = "<!DOCTYPE math [<!ENTITY value SYSTEM \"file:///etc/passwd\">]>"
					+ "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtext>&value;</mtext></math>";
				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetMathML(unsafeMathML));
				Assert.AreEqual(projection, SUT.Document.MathProjection);
				Assert.AreEqual(story, SUT.Document.GetRange(0, int.MaxValue).Text);
				SUT.Document.GetMathML(out var afterUnsafeMathML);
				Assert.AreEqual(canonical, afterUnsafeMathML);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
				Assert.IsFalse(SUT.Document.CanUndo());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_MathML_Undo_Redo_Restores_Source_Document()
		{
			const string first = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>x</mi></math>";
			const string second = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>y</mi></math>";
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);
				SUT.Document.SetMathML(first);
				SUT.Document.GetMathML(out var firstCanonical);
				var firstProjection = SUT.Document.MathProjection;
				var firstStory = SUT.Document.GetRange(0, int.MaxValue).Text;
				SUT.Document.ClearUndoRedoHistory();
				SUT.Document.SetMathML(second);
				SUT.Document.GetMathML(out var secondCanonical);
				var secondProjection = SUT.Document.MathProjection;
				var secondStory = SUT.Document.GetRange(0, int.MaxValue).Text;

				SUT.Document.Undo();
				SUT.Document.GetMathML(out var afterUndo);
				Assert.AreEqual(firstCanonical, afterUndo);
				Assert.AreEqual(firstProjection, SUT.Document.MathProjection);
				Assert.AreEqual(firstStory, SUT.Document.GetRange(0, int.MaxValue).Text);

				SUT.Document.Redo();
				SUT.Document.GetMathML(out var afterRedo);
				Assert.AreEqual(secondCanonical, afterRedo);
				Assert.AreEqual(secondProjection, SUT.Document.MathProjection);
				Assert.AreEqual(secondStory, SUT.Document.GetRange(0, int.MaxValue).Text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		// Android and UIKit clipboard backends do not expose custom RTF formats.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaUIKit)]
		public async Task When_RtfOnly_Clipboard_Paste_Preserves_Formatting_Cross_Process()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			try
			{
				panel.Children.Add(source);
				panel.Children.Add(target);
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);

				source.Document.SetText(TextSetOptions.None, "rich");
				source.Document.GetRange(0, 4).CharacterFormat.Bold = FormatEffect.On;
				source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
				var package = new DataPackage();
				package.SetRtf(rtf);
				Clipboard.SetContent(package);
				await WindowHelper.WaitForIdle();

				target.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(target.Document, out var text);
					return text == "rich";
				});
				Assert.AreEqual(FormatEffect.On, target.Document.GetRange(0, 4).CharacterFormat.Bold);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		public async Task When_InputScope_RoundTrips()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				Assert.IsNull(SUT.InputScope);
				Assert.AreEqual(Microsoft.UI.Xaml.Input.InputScopeNameValue.Default, ((IImeSessionHost)SUT).InputScope.Names[0].NameValue);

				var scope = new Microsoft.UI.Xaml.Input.InputScope();
				scope.Names.Add(new Microsoft.UI.Xaml.Input.InputScopeName
				{
					NameValue = Microsoft.UI.Xaml.Input.InputScopeNameValue.Url,
				});
				SUT.InputScope = scope;

				Assert.AreSame(scope, SUT.InputScope);
				Assert.AreSame(scope, ((IImeSessionHost)SUT).InputScope);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Adjacent_Identical_Images_Render_As_Separate_Objects()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var bytes = Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(2, 2, Microsoft.UI.Colors.Blue);
				SUT.Document.GetRange(0, 0).InsertImage(20, 15, 10, VerticalCharacterAlignment.Baseline, "one", new MemoryStream(bytes).AsRandomAccessStream());
				SUT.Document.GetRange(1, 1).InsertImage(20, 15, 10, VerticalCharacterAlignment.Baseline, "one", new MemoryStream(bytes).AsRandomAccessStream());
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(2, SUT.Document.CharacterRunCount);
				Assert.AreEqual(2, GetDisplayBlock(SUT).Inlines.Count);
				SUT.Document.GetRange(0, 1).GetRect(PointOptions.ClientCoordinates, out var first, out _);
				SUT.Document.GetRange(1, 2).GetRect(PointOptions.ClientCoordinates, out var second, out _);
				Assert.AreEqual(20, first.Width, 1);
				Assert.AreEqual(20, second.Width, 1);
				Assert.AreEqual(first.Right, second.X, 1, "Adjacent image objects should occupy consecutive independent advances.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_CopyingToClipboard_Handled_Suppresses_Copy()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Seed the clipboard with a sentinel; a suppressed copy must leave it untouched.
			var seed = new DataPackage();
			seed.SetText("SENTINEL");
			var hasSystemClipboard = RuntimeTestsPlatformHelper.CurrentPlatform != RuntimeTestPlatforms.SkiaTvOS;
			if (hasSystemClipboard)
			{
				Clipboard.SetContent(seed);
			}
			await WindowHelper.WaitForIdle();

			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await TypeAsync(SUT, "abc");
			RaiseKey(SUT, VirtualKey.A, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.CopyingToClipboard += (s, e) =>
			{
				count++;
				e.Handled = true;
			};

			RaiseKey(SUT, VirtualKey.C, VirtualKeyModifiers.Control);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			if (hasSystemClipboard)
			{
				var content = Clipboard.GetContent();
				var text = await content.GetTextAsync();
				Assert.AreEqual("SENTINEL", text);
			}
			GetTextWithoutFinalEop(SUT.Document, out var documentText);
			Assert.AreEqual("abc", documentText);
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("abc", text);
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

			// Mixed alignments use per-line metadata, so no block-level uniform override is projected.
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
		public async Task When_ContextFlyout_With_Selection_Populates_RichEdit_Commands()
		{
			var focusedSelectionBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
			var SUT = new RichEditBox
			{
				Width = 200,
				SelectionHighlightColor = focusedSelectionBrush,
				SelectionHighlightColorWhenNotFocused =
					new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
			};
			TextCommandBarFlyout flyout = null;
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "Test content");
				SUT.Document.Selection.SetRange(0, 4);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				flyout = SUT.ContextFlyout as TextCommandBarFlyout;
				Assert.IsNotNull(flyout);
				TextControlFlyoutHelper.ShowAt(
					flyout,
					SUT,
					new Windows.Foundation.Point(10, 10),
					default,
					FlyoutShowMode.Standard);
				await WindowHelper.WaitForIdle();

				var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
				var displayBlock = contentElement?.Content as TextBlock;
				Assert.IsNotNull(displayBlock);
				Assert.AreEqual(FocusState.Unfocused, SUT.FocusState);
				Assert.AreSame(focusedSelectionBrush, displayBlock.SelectionHighlightColor);

				var commandModifier = Uno.UI.Helpers.DeviceTargetHelper.PlatformCommandModifier;
				var buttons = flyout.PrimaryCommands.Concat(flyout.SecondaryCommands).OfType<AppBarButton>().ToList();
				Assert.IsTrue(buttons.Any(button => button.KeyboardAccelerators.Any(accelerator => accelerator.Key == VirtualKey.X && accelerator.Modifiers.HasFlag(commandModifier))), "Cut should be available for an editable selection.");
				Assert.IsTrue(buttons.Any(button => button.KeyboardAccelerators.Any(accelerator => accelerator.Key == VirtualKey.C && accelerator.Modifiers.HasFlag(commandModifier))), "Copy should be available for a selection.");
				Assert.IsTrue(buttons.Any(button => button.KeyboardAccelerators.Any(accelerator => accelerator.Key == VirtualKey.A && accelerator.Modifiers.HasFlag(commandModifier))), "Select All should be available.");
			}
			finally
			{
				flyout?.Hide();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ContextMenuOpening_Handled_Suppresses_Text_Flyout()
		{
			var SUT = new RichEditBox();
			var flyout = new MenuFlyout();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var eventCount = 0;
				SUT.ContextMenuOpening += (_, args) =>
				{
					eventCount++;
					args.Handled = true;
				};

				TextControlFlyoutHelper.ShowAt(
					flyout,
					SUT,
					new Windows.Foundation.Point(10, 10),
					default,
					FlyoutShowMode.Standard);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, eventCount);
				Assert.IsFalse(flyout.IsOpen);
			}
			finally
			{
				flyout.Hide();
				WindowHelper.WindowContent = null;
			}
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
			GetTextWithoutFinalEop(SUT.Document, out var composing);
			Assert.AreEqual("ni", composing);

			fake.SimulateCompositionComplete("nihao");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsComposing);
			GetTextWithoutFinalEop(SUT.Document, out var committed);
			Assert.AreEqual("nihao", committed);
		}

		[TestMethod]
		public async Task When_IME_ReadOnly_Ignores_Composition()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Original");
				SUT.IsReadOnly = true;
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, fake.StartImeSessionCallCount);
				Assert.IsFalse(SUT.IsCaretRenderedForTesting);

				fake.SimulateCompositionStart();
				fake.SimulateCompositionUpdate("ni");
				await WindowHelper.WaitForIdle();

				Assert.IsFalse(SUT.IsComposing);
				GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var afterCommit);
			Assert.AreEqual("nihao", afterCommit);

			// The whole composition collapses into ONE undo entry (matching WinUI).
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(SUT.Document, out var afterUndo);
			Assert.AreEqual("", afterUndo);
		}

		[TestMethod]
		public async Task When_IME_Composition_Cancel_Removes_Preedit()
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("", text);
			Assert.IsFalse(SUT.Document.CanUndo());
		}

		[TestMethod]
		public async Task When_IME_Partial_Result_Preserves_Committed_Prefix()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.ClearUndoRedoHistory();
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("nihao");
			fake.SimulateCompositionPartialCommit("你", "hao", cursorPosition: 3);
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("A你haoB", text);
			Assert.IsTrue(SUT.IsComposing);
			Assert.AreEqual(2, SUT.CompositionStartIndex);
			Assert.AreEqual(3, SUT.CompositionLength);

			fake.SimulateCompositionUpdate("ha");
			fake.SimulateCompositionComplete("好");
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(SUT.Document, out text);
			Assert.AreEqual("A你好B", text);
			Assert.IsFalse(SUT.IsComposing);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out text);
			Assert.AreEqual("AB", text);
		}

		[TestMethod]
		public async Task When_IME_Partial_Result_Then_Cancel_Keeps_Only_Committed_Prefix()
		{
			var fake = new FakeImeTextBoxExtension();
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);

			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "AB");
			SUT.Document.ClearUndoRedoHistory();
			SUT.Document.Selection.SetRange(1, 1);
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			fake.SimulateCompositionStart();
			fake.SimulateCompositionUpdate("nihao");
			fake.SimulateCompositionPartialCommit("你", "hao", cursorPosition: 3);
			fake.SimulateCompositionCancel();
			await WindowHelper.WaitForIdle();

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("A你B", text);
			Assert.IsFalse(SUT.IsComposing);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out text);
			Assert.AreEqual("AB", text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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
				GetTextWithoutFinalEop(SUT.Document, out var text);
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
			public int StartFailuresRemaining { get; set; }
			public ImeSessionActivation LastActivation { get; private set; }
			public List<ImeSessionActivation> Activations { get; } = new();
			public List<ImeSessionUpdate> Updates { get; } = new();
			public IReadOnlyList<string> LinguisticAlternatives { get; set; } = Array.Empty<string>();
			public Func<string, CancellationToken, Task<IReadOnlyList<string>>> LinguisticAlternativesFactory { get; set; }
			public string LastCompositionText { get; private set; }

			public event EventHandler CompositionStarted;
			public event EventHandler<ImeCompositionEventArgs> CompositionUpdated;
			public event EventHandler<ImeCompositionEventArgs> CompositionCompleted;
			public event EventHandler<ImePartialCompositionEventArgs> CompositionPartiallyCommitted;
			public event EventHandler<ImeCompositionEventArgs> CompositionCanceled;
			public event EventHandler CompositionEnded;
			public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs> CandidateWindowBoundsChanged;

			public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
			{
				StartImeSessionCallCount++;
				if (StartFailuresRemaining > 0)
				{
					StartFailuresRemaining--;
					throw new InvalidOperationException("Synthetic IME start failure.");
				}
				LastActivation = activation;
				Activations.Add(activation);
			}

			public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update) => Updates.Add(update);

			public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				LastCompositionText = compositionText;
				return LinguisticAlternativesFactory?.Invoke(compositionText, cancellationToken)
					?? Task.FromResult(LinguisticAlternatives);
			}

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

			public void SimulateCompositionUpdate(
				string text,
				int cursorPosition = -1,
				int resolvedLength = 0,
				bool textAlreadyApplied = false)
			{
				CompositionUpdated?.Invoke(
					this,
					new ImeCompositionEventArgs(text, cursorPosition, resolvedLength, textAlreadyApplied));
			}

			public void SimulateCompositionComplete(string text, bool textAlreadyApplied = false)
			{
				IsComposing = false;
				CompositionCompleted?.Invoke(
					this,
					new ImeCompositionEventArgs(text, textAlreadyApplied: textAlreadyApplied));
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCompositionPartialCommit(
				string committedText,
				string compositionText,
				int cursorPosition = -1,
				int resolvedLength = 0,
				bool textAlreadyApplied = false)
			{
				CompositionPartiallyCommitted?.Invoke(
					this,
					new ImePartialCompositionEventArgs(
						committedText,
						compositionText,
						cursorPosition,
						resolvedLength,
						textAlreadyApplied));
			}

			public void SimulateCompositionCancel()
			{
				IsComposing = false;
				CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}

			public void SimulateCandidateWindowBoundsChanged(Windows.Foundation.Rect bounds)
				=> CandidateWindowBoundsChanged?.Invoke(this, new ImeCandidateWindowBoundsChangedEventArgs(bounds));
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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
				GetTextWithoutFinalEop(SUT.Document, out var textWithRedoEntry);
				Assert.AreEqual("A", textWithRedoEntry);

				SUT.IsReadOnly = true;
				RaiseKey(SUT, VirtualKey.Y, VirtualKeyModifiers.Control);
				GetTextWithoutFinalEop(SUT.Document, out var afterBlockedRedo);
				Assert.AreEqual("A", afterBlockedRedo);

				RaiseKey(SUT, VirtualKey.Z, VirtualKeyModifiers.Control);
				GetTextWithoutFinalEop(SUT.Document, out var afterBlockedUndo);
				Assert.AreEqual("A", afterBlockedUndo);

				SUT.IsReadOnly = false;
				RaiseKey(SUT, VirtualKey.Y, VirtualKeyModifiers.Control);
				GetTextWithoutFinalEop(SUT.Document, out var editableText);
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
			using var imeDisposable = RichEditBox.SetImeExtensionForTesting(new FakeImeTextBoxExtension());
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
		// Android and UIKit clipboard backends do not expose custom RTF formats.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaUIKit)]
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
				GetTextWithoutFinalEop(target.Document, out var t);
				return t == "world hello";
			});

			target.Document.Selection.SetRange(0, 11);
			Assert.AreEqual(FormatEffect.On, target.Document.Selection.CharacterFormat.Italic);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(target.Document, out var t);
				return t == "world hello";
			});

			target.Document.Selection.SetRange(0, 11);
			Assert.AreEqual(FormatEffect.Off, target.Document.Selection.CharacterFormat.Italic);
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

			public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer child) { }

			public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData) { }

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
