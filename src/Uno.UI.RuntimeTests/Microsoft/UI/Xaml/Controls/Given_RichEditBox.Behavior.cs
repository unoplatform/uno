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
#if HAS_UNO
using Microsoft.UI.Xaml.Internal;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.DevTools.Input;
#if HAS_UNO
using Uno.UI.Xaml.Controls.Extensions;
#endif
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
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		[DataRow(typeof(UIElement), nameof(UIElement.UpdateLayout))]
		[DataRow(typeof(Control), "get_FontSize")]
		[DataRow(typeof(Control), "get_FontWeight")]
		[DataRow(typeof(Control), "get_FontStyle")]
		[DataRow(typeof(Control), "get_FontStretch")]
		[DataRow(typeof(RichEditBox), "get_IsColorFontEnabled")]
		[DataRow(typeof(RichEditBox), "get_IsSpellCheckEnabled")]
		[DataRow(typeof(RichEditBox), "get_TextReadingOrder")]
		[DataRow(typeof(RichEditBox), "get_TextWrapping")]
		public void When_Internal_View_Host_Preserves_Public_Nonvirtual_API(Type declaringType, string methodName)
		{
			var method = declaringType.GetMethod(methodName);
			Assert.IsNotNull(method);
			Assert.IsFalse(method.IsVirtual, "An internal view-host adapter must not change an existing public method's CLR dispatch contract.");
		}

		[TestMethod]
		public void When_TextConstants_Match_WinUIEdit()
		{
			Assert.AreEqual(global::Windows.UI.Color.FromArgb(0, 0, 0, 1), TextConstants.AutoColor);
			Assert.AreEqual(-1_073_741_823, TextConstants.MinUnitCount);
			Assert.AreEqual(1_073_741_823, TextConstants.MaxUnitCount);
			Assert.AreEqual(global::Windows.UI.Color.FromArgb(0, 0, 0, 2), TextConstants.UndefinedColor);
			Assert.AreEqual(-9_999_999f, TextConstants.UndefinedFloatValue);
			Assert.AreEqual((global::Windows.UI.Text.FontStretch)(-9_999_999), TextConstants.UndefinedFontStretch);
			Assert.AreEqual((global::Windows.UI.Text.FontStyle)(-9_999_999), TextConstants.UndefinedFontStyle);
			Assert.AreEqual(-9_999_999, TextConstants.UndefinedInt32Value);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Unfocused_Selection_Highlight_Is_Transparent_Text_Remains_Visible()
		{
			var editor = new RichEditBox
			{
				Width = 240,
				Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
				Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
			};
			var other = new Button();
			var panel = new StackPanel
			{
				Children =
				{
					editor,
					other,
				},
			};
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				editor.Document.SetText(TextSetOptions.None, "Selected text");
				editor.Document.Selection.SetRange(0, 13);
				editor.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				other.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();

				var selected = await UITestHelper.ScreenShot(editor);
				editor.Document.Selection.SetRange(0, 0);
				await WindowHelper.WaitForIdle();
				var unselected = await UITestHelper.ScreenShot(editor);

				await ImageAssert.AreEqualAsync(selected, unselected);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Pointer_Click_In_Empty_Area_Focuses_Control()
		{
			var SUT = new RichEditBox { Width = 220, Height = 140 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForIdle();

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();
				var bounds = SUT.GetAbsoluteBounds();
				mouse.MoveTo(new Windows.Foundation.Point(bounds.X + bounds.Width / 2, bounds.Bottom - 12));
				mouse.Press();
				mouse.Release();
				await WindowHelper.WaitForIdle();

				Assert.AreNotEqual(FocusState.Unfocused, SUT.FocusState);
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(0, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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

#if HAS_UNO
			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
#endif
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

#if HAS_UNO
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
#endif
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

#if HAS_UNO
			Assert.AreEqual(2, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
#endif
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

#if HAS_UNO
			Assert.AreEqual(4, SUT.SelectionStartForTesting);
			Assert.AreEqual(5, SUT.SelectionLengthForTesting);
#endif
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
		public async Task When_SetIndex_With_Extend_Selects_Whole_Unit()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello World foo");

				var range = SUT.Document.GetRange(2, 3);
				range.SetIndex(TextRangeUnit.Word, 2, true);

				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(12, range.EndPosition);

				range.SetRange(13, 14);
				range.SetIndex(TextRangeUnit.Word, 1, true);
				Assert.AreEqual(0, range.StartPosition);
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
				Assert.AreEqual(0, range.StartPosition);
				Assert.AreEqual(7, range.EndPosition);
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

#if HAS_UNO
			Assert.AreEqual(0, SUT.SelectionStartForTesting);
			Assert.AreEqual(6, SUT.SelectionLengthForTesting);
#endif
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
		public async Task When_EndOf_Line_Moves_Past_Paragraph_Mark()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(4, 4);
				Assert.AreEqual(2, range.EndOf(TextRangeUnit.Line, false));
				Assert.AreEqual(6, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);

				range.SetRange(4, 4);
				Assert.AreEqual(2, range.EndOf(TextRangeUnit.Line, true));
				Assert.AreEqual(4, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);

				range.SetRange(7, 7);
				Assert.AreEqual(1, range.EndOf(TextRangeUnit.Line, false));
				Assert.AreEqual(8, range.StartPosition);

				// The selection's End key still stops before the paragraph mark.
				SUT.Document.Selection.SetRange(4, 4);
				Assert.AreEqual(1, SUT.Document.Selection.EndKey(TextRangeUnit.Line, false));
				Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Expand_Line_Selects_Whole_Line()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(4, 4);
				Assert.AreEqual(3, range.Expand(TextRangeUnit.Line));

				Assert.AreEqual(3, range.StartPosition);
				Assert.AreEqual(6, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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
#if HAS_UNO
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
#endif
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
#if HAS_UNO
			Assert.AreEqual(5, SUT.SelectionStartForTesting);
			Assert.AreEqual(0, SUT.SelectionLengthForTesting);
#endif
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_MoveDown_Moves_Caret_To_Next_Line()
		{
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
#if HAS_UNO
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
#endif
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_MoveUp_Moves_Caret_To_Previous_Line()
		{
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
#if HAS_UNO
			Assert.AreEqual(3, SUT.SelectionStartForTesting);
#endif
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_MoveDown_Screen_Moves_By_Viewport()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "00\r11\r22\r33\r44\r55\r66\r77\r88\r99");
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				SUT.Document.Selection.SetRange(0, 0);

				var moved = SUT.Document.Selection.MoveDown(TextRangeUnit.Screen, 1, false);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1, moved);
				Assert.IsTrue(SUT.Document.Selection.StartPosition > 3, $"A screen move should advance farther than one line, was {SUT.Document.Selection.StartPosition}.");
				Assert.AreEqual(SUT.Document.Selection.StartPosition, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Selection_Move_Window_Uses_Visible_Range()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				const string text = "00\r11\r22\r33\r44\r55\r66\r77\r88\r99";
				SUT.Document.SetText(TextSetOptions.None, text);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				SUT.Document.Selection.SetRange(0, 0);

				var moved = SUT.Document.Selection.MoveDown(TextRangeUnit.Window, 1, false);

				Assert.AreEqual(1, moved);
				Assert.IsTrue(SUT.Document.Selection.StartPosition > 0);
				Assert.IsTrue(SUT.Document.Selection.StartPosition < text.Length, "The initial viewport should not include the entire story.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Move_Paragraph_Uses_Paragraph_Starts()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc\rdd");
				SUT.Document.Selection.SetRange(1, 1);

				Assert.AreEqual(2, SUT.Document.Selection.MoveDown(TextRangeUnit.Paragraph, 2, false));
				Assert.AreEqual(6, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.MoveUp(TextRangeUnit.Paragraph, 1, false));
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Selection_Vertical_Move_Uses_The_Active_End()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
				SUT.Document.Selection.SetRange(0, 1);

				Assert.AreEqual(1, SUT.Document.Selection.MoveDown(TextRangeUnit.Line, 1, false));
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(4, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_Vertical_Extend_Tracks_Active_Start()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "aa\rbb\rcc");
				SUT.Document.Selection.SetRange(6, 6);

				Assert.AreEqual(1, SUT.Document.Selection.MoveUp(TextRangeUnit.Line, 1, true));
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(6, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));

				Assert.AreEqual(1, SUT.Document.Selection.MoveUp(TextRangeUnit.Line, 1, true));
				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(6, SUT.Document.Selection.EndPosition);

				Assert.AreEqual(1, SUT.Document.Selection.MoveDown(TextRangeUnit.Line, 1, true));
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(6, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Selection_Horizontal_Extend_Tracks_Active_Endpoint()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.Selection.SetRange(4, 4);

				Assert.AreEqual(2, SUT.Document.Selection.MoveLeft(TextRangeUnit.Character, 2, true));
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(4, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));
#if HAS_UNO
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
#endif

				Assert.AreEqual(1, SUT.Document.Selection.MoveRight(TextRangeUnit.Character, 1, true));
				Assert.AreEqual(3, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(4, SUT.Document.Selection.EndPosition);
				Assert.IsTrue(SUT.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));

				Assert.AreEqual(2, SUT.Document.Selection.MoveRight(TextRangeUnit.Character, 2, true));
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
				Assert.IsFalse(SUT.Document.Selection.Options.HasFlag(SelectionOptions.StartActive));
#if HAS_UNO
				Assert.IsFalse(SUT.IsSelectionBackwardForTesting);
#endif

				SUT.Document.Selection.Options |= SelectionOptions.StartActive;
#if HAS_UNO
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting, "Changing StartActive alone should update the interactive active endpoint.");
#endif
				SUT.Document.Selection.Options &= ~SelectionOptions.StartActive;
#if HAS_UNO
				Assert.IsFalse(SUT.IsSelectionBackwardForTesting);
#endif

				SUT.Document.Selection.SetRange(1, 3);
				SUT.SelectionChanging += (s, e) => e.Cancel = true;
				Assert.AreEqual(0, SUT.Document.Selection.MoveLeft(TextRangeUnit.Character, 1, false));
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
				Assert.AreEqual(0, SUT.Document.Selection.EndKey(TextRangeUnit.Story, false));
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(3, SUT.Document.Selection.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Range_Move_Screen_Throws_ENotImpl()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "00\r11\r22\r33\r44\r55\r66\r77\r88\r99");
				await WindowHelper.WaitForIdle();
				var range = SUT.Document.GetRange(0, 0);

				AssertTomException<NotImplementedException>(
					() => range.Move(TextRangeUnit.Screen, 1),
					unchecked((int)0x80004001));
				Assert.AreEqual(0, range.StartPosition);
				Assert.AreEqual(0, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Range_Move_Screen_Nondegenerate_Throws_ENotImpl()
		{
			var SUT = new RichEditBox { Width = 200, Height = 60 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var text = string.Join('\r', Enumerable.Range(0, 30).Select(value => value.ToString("D2")));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();
				var range = SUT.Document.GetRange(0, 3);

				AssertTomException<NotImplementedException>(
					() => range.Move(TextRangeUnit.Screen, 100),
					unchecked((int)0x80004001));
				Assert.AreEqual(0, range.StartPosition);
				Assert.AreEqual(3, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Range_Window_Operations_Use_Visible_Bounds()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var text = string.Join('\r', Enumerable.Range(0, 20).Select(value => value.ToString("D2")));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var visible = SUT.Document.GetRange(1, 1);
				visible.Expand(TextRangeUnit.Window);
				var visibleStart = visible.StartPosition;
				var visibleEnd = visible.EndPosition;
				Assert.AreEqual(0, visibleStart);
				Assert.IsTrue(visibleEnd > visibleStart && visibleEnd < text.Length, $"The viewport should contain a proper story subset, was [{visibleStart},{visibleEnd}).");
				Assert.AreEqual(1, visible.GetIndex(TextRangeUnit.Window));

				var range = SUT.Document.GetRange(visibleStart, visibleStart);
				Assert.AreEqual(1, range.Move(TextRangeUnit.Window, 1));
				Assert.AreEqual(visibleEnd, range.StartPosition);
				Assert.AreEqual(-1, range.Move(TextRangeUnit.Window, -1));
				Assert.AreEqual(visibleStart, range.StartPosition);

				range.SetRange(visibleStart, visibleStart);
				Assert.AreEqual(1, range.MoveEnd(TextRangeUnit.Window, 1));
				Assert.AreEqual(visibleEnd, range.EndPosition);
				range.SetRange(visibleEnd, visibleEnd);
				Assert.AreEqual(-1, range.MoveStart(TextRangeUnit.Window, -1));
				Assert.AreEqual(visibleStart, range.StartPosition);
				Assert.AreEqual(visibleEnd, range.EndPosition);

				range.SetIndex(TextRangeUnit.Window, 1, false);
				Assert.AreEqual(visibleStart, range.StartPosition);
				range.SetIndex(TextRangeUnit.Window, 1, true);
				Assert.AreEqual(visibleEnd, range.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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

			GetTextWithoutFinalEop(SUT.Document, out var afterGroup);
			Assert.AreEqual("startAB", afterGroup);

			// A single undo reverts the whole group (both A and B), not just the last edit.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			// The pre-group SetText remains a separate undo entry.
			Assert.IsTrue(SUT.Document.CanUndo());
			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var afterSecondUndo);
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
			GetTextWithoutFinalEop(SUT.Document, out var afterUndo);
			Assert.AreEqual("start", afterUndo);

			Assert.IsTrue(SUT.Document.CanRedo());
			SUT.Document.Redo();
			GetTextWithoutFinalEop(SUT.Document, out var afterRedo);
			Assert.AreEqual("startAB", afterRedo);
		}

		[TestMethod]
		public async Task When_Nested_UndoGroup_Ends_On_First_End()
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

			GetTextWithoutFinalEop(SUT.Document, out var afterGroup);
			Assert.AreEqual("x123", afterGroup);

			// BeginUndoGroup is idempotent rather than nested; the first EndUndoGroup closes it.
			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var afterUndo);
			Assert.AreEqual("x12", afterUndo);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var afterSecondUndo);
			Assert.AreEqual("x", afterSecondUndo);
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

			GetTextWithoutFinalEop(SUT.Document, out var typed);
			Assert.AreEqual("ab", typed);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var afterUndo);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
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
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(MarkerType.None, SUT.Document.GetDefaultParagraphFormat().ListType);
				SUT.Document.SetText(TextSetOptions.None, "one\rtwo");
				Assert.AreEqual(MarkerType.None, SUT.Document.GetRange(4, 7).ParagraphFormat.ListType);

				var first = SUT.Document.GetRange(0, 0).ParagraphFormat;
				first.ListType = MarkerType.Bullet;

				Assert.AreEqual(MarkerType.Bullet, SUT.Document.GetRange(0, 3).ParagraphFormat.ListType);
				Assert.AreEqual(MarkerType.None, SUT.Document.GetRange(4, 7).ParagraphFormat.ListType);
				Assert.AreEqual(MarkerType.Undefined, SUT.Document.GetRange(0, 7).ParagraphFormat.ListType);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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
			GetTextWithoutFinalEop(SUT.Document, out var text);
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("aaa\rbbbX", text);
			Assert.AreEqual(ParagraphAlignment.Center, SUT.Document.GetRange(5, 5).ParagraphFormat.Alignment);
		}

		[TestMethod]
		public async Task When_Inserting_At_Paragraph_Start_Preserves_That_Paragraph_Format()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");
				var second = SUT.Document.GetRange(4, 4).ParagraphFormat;
				second.Alignment = ParagraphAlignment.Right;
				second.SetIndents(3, 9, 0);

				SUT.Document.Selection.SetRange(4, 4);
				SUT.Document.Selection.TypeText("X");

				Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 0).ParagraphFormat.Alignment);
				var actual = SUT.Document.GetRange(4, 4).ParagraphFormat;
				Assert.AreEqual(ParagraphAlignment.Right, actual.Alignment);
				Assert.AreEqual(3f, actual.FirstLineIndent);
				Assert.AreEqual(9f, actual.LeftIndent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Mixed_Paragraph_Alignments_Render_Per_Paragraph()
		{
			var SUT = new RichEditBox { Width = 300, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "left\rright");
				SUT.Document.GetRange(0, 4).ParagraphFormat.Alignment = ParagraphAlignment.Left;
				SUT.Document.GetRange(5, 10).ParagraphFormat.Alignment = ParagraphAlignment.Right;
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(0, 0).GetRect(PointOptions.ClientCoordinates, out var firstParagraphCaret, out _);
				SUT.Document.GetRange(5, 5).GetRect(PointOptions.ClientCoordinates, out var secondParagraphCaret, out _);

				Assert.IsTrue(
					secondParagraphCaret.X > firstParagraphCaret.X + 100,
					$"The right-aligned paragraph should have an independent horizontal offset, was {firstParagraphCaret.X} then {secondParagraphCaret.X}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Paragraph_Indents_Project_To_Caret_Geometry()
		{
			var SUT = new RichEditBox { Width = 320, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "first\rsecond");
				SUT.Document.GetRange(0, 0).ParagraphFormat.SetIndents(12, 24, 18);
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(0, 0).GetRect(PointOptions.ClientCoordinates, out var indented, out _);
				SUT.Document.GetRange(6, 6).GetRect(PointOptions.ClientCoordinates, out var baseline, out _);

				Assert.AreEqual(48, indented.X - baseline.X, 1.5, "TOM points should project to DIPs and combine left + first-line indents.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Paragraph_And_Line_Spacing_Project_To_Vertical_Geometry()
		{
			var SUT = new RichEditBox { Width = 240 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "a\rb");
				var firstFormat = SUT.Document.GetRange(0, 0).ParagraphFormat;
				firstFormat.SetLineSpacing(LineSpacingRule.Exactly, 30);
				firstFormat.SpaceAfter = 9;
				SUT.Document.GetRange(2, 2).ParagraphFormat.SpaceBefore = 6;
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(0, 0).GetRect(PointOptions.ClientCoordinates, out var first, out _);
				SUT.Document.GetRange(2, 2).GetRect(PointOptions.ClientCoordinates, out var second, out _);

				Assert.AreEqual(40, first.Height, 1.5, "Exactly 30 points should produce a 40-DIP line box.");
				Assert.AreEqual(60, second.Y - first.Y, 2, "The second line should follow the 40-DIP line plus 9pt after and 6pt before spacing.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Default_Paragraph_Between_Formatted_Paragraphs_Resets_Layout()
		{
			var SUT = new RichEditBox { Width = 300, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "one\rtwo\rthree");
				SUT.Document.GetRange(0, 0).ParagraphFormat.SetIndents(0, 24, 0);
				SUT.Document.GetRange(8, 8).ParagraphFormat.SetIndents(0, 12, 0);
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(0, 0).GetRect(PointOptions.ClientCoordinates, out var first, out _);
				SUT.Document.GetRange(4, 4).GetRect(PointOptions.ClientCoordinates, out var middle, out _);
				SUT.Document.GetRange(8, 8).GetRect(PointOptions.ClientCoordinates, out var last, out _);

				Assert.AreEqual(32, first.X - middle.X, 1.5);
				Assert.AreEqual(16, last.X - middle.X, 1.5);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Percent_Line_Spacing_Is_Rejected()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			var format = SUT.Document.GetDefaultParagraphFormat();
			Assert.ThrowsExactly<ArgumentException>(() => format.SetLineSpacing(LineSpacingRule.Percent, 150));
			Assert.ThrowsExactly<ArgumentException>(() => format.SetIndents(float.NaN, 0, 0));
			Assert.ThrowsExactly<ArgumentException>(() => format.SpaceBefore = float.PositiveInfinity);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => format.ListTab = -1);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => format.ListLevelIndex = -1);
			Assert.ThrowsExactly<ArgumentException>(() => format.ListType = (MarkerType)999);
		}

		private static void ConfigureListParagraph(ITextParagraphFormat format, int level, float leftIndent)
		{
			format.SetIndents(0, leftIndent, 0);
			format.ListType = MarkerType.Arabic;
			format.ListStyle = MarkerStyle.Period;
			format.ListAlignment = MarkerAlignment.Right;
			format.ListLevelIndex = level;
			format.ListStart = 1;
			format.ListTab = 18;
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GetRect_Returns_Caret_Geometry()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GetRect_Range_Has_Positive_Width()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_GetRect_ScreenCoordinates_Apply_RasterizationScale()
		{
			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(2, 5);
			range.GetRect(PointOptions.None, out var screenRect, out _);
			range.GetRect(PointOptions.ClientCoordinates, out var clientRect, out _);

			var scale = SUT.XamlRoot.RasterizationScale;
			Assert.AreEqual(clientRect.Width * scale, screenRect.Width, 1, "Screen width should be expressed in physical pixels.");
			Assert.AreEqual(clientRect.Height * scale, screenRect.Height, 1, "Screen height should be expressed in physical pixels.");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_GetRect_Multiline_Range_Includes_Intermediate_Lines()
		{
			var SUT = new RichEditBox { Width = 600 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				const string text = "x\rThis intermediate line is deliberately much wider\rz";
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var middleStart = text.IndexOf('T');
				var middleEnd = text.LastIndexOf('\r');
				SUT.Document.GetRange(0, text.Length).GetRect(PointOptions.ClientCoordinates, out var fullRect, out _);
				SUT.Document.GetRange(middleStart, middleEnd).GetRect(PointOptions.ClientCoordinates, out var middleRect, out _);

				Assert.IsTrue(fullRect.Width >= middleRect.Width - 0.5, $"The full range should include the widest intermediate line, was {fullRect.Width} vs {middleRect.Width}.");
				Assert.IsTrue(fullRect.Height > middleRect.Height, "The full range should span all visual lines.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GetPoint_Vertical_Alignment_Orders_Points()
		{
			var SUT = new RichEditBox { Width = 400 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");
			await WindowHelper.WaitForIdle();

			var range = SUT.Document.GetRange(4, 4);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.None, out var topPoint);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Baseline, PointOptions.None, out var baselinePoint);
			range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Bottom, PointOptions.None, out var bottomPoint);

			Assert.AreEqual(topPoint.X, bottomPoint.X, 0.5, "Left alignment should give the same X for both vertical anchors.");
			Assert.IsTrue(baselinePoint.Y > topPoint.Y, $"Baseline should be below the line top, was {baselinePoint.Y} vs {topPoint.Y}.");
			Assert.IsTrue(baselinePoint.Y < bottomPoint.Y, $"Baseline should be above the line bottom, was {baselinePoint.Y} vs {bottomPoint.Y}.");
			Assert.IsTrue(bottomPoint.Y > topPoint.Y, $"Bottom anchor should be below the top anchor, was {bottomPoint.Y} vs {topPoint.Y}.");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GetPoint_GetRangeFromPoint_RoundTrips()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GetPoint_GetRangeFromPoint_ClientCoordinates_RoundTrips()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(6, 6).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.ClientCoordinates, out var point);
				var recovered = SUT.Document.GetRangeFromPoint(point, PointOptions.ClientCoordinates);

				Assert.IsTrue(Math.Abs(recovered.StartPosition - 6) <= 1, $"Client-coordinate hit testing should recover the origin index, was {recovered.StartPosition}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_GetRangeFromPoint_OffClient_Returns_Nearest_Text()
		{
			var SUT = new RichEditBox { Width = 300, Height = 80, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "first\rsecond");
				await WindowHelper.WaitForIdle();

				var above = SUT.Document.GetRangeFromPoint(new Windows.Foundation.Point(10, -100), PointOptions.ClientCoordinates);
				var below = SUT.Document.GetRangeFromPoint(new Windows.Foundation.Point(250, 1000), PointOptions.ClientCoordinates);
				var left = SUT.Document.GetRangeFromPoint(new Windows.Foundation.Point(-100, 10), PointOptions.ClientCoordinates);
				var right = SUT.Document.GetRangeFromPoint(new Windows.Foundation.Point(1000, 10), PointOptions.ClientCoordinates);
				Assert.AreEqual(0, above.StartPosition);
				Assert.AreEqual(12, below.StartPosition);
				Assert.AreEqual(0, left.StartPosition);
				Assert.IsTrue(right.StartPosition >= 5, $"A point to the right should map to the first line end, was {right.StartPosition}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SetPoint_Moves_Caret()
		{
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_PointOptions_Start_Selects_Start_Endpoint()
		{
			var SUT = new RichEditBox { Width = 400 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(2, 8);
				range.GetRect(PointOptions.ClientCoordinates | PointOptions.Start, out var startRect, out _);
				range.GetRect(PointOptions.ClientCoordinates, out var expectedRangeRect, out _);
				Assert.AreEqual(expectedRangeRect.X, startRect.X, 0.5);
				Assert.AreEqual(expectedRangeRect.Width, startRect.Width, 0.5);

				SUT.Document.GetRange(5, 5).GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Top, PointOptions.ClientCoordinates, out var point);
				range.SetPoint(point, PointOptions.ClientCoordinates | PointOptions.Start, extend: true);
				Assert.IsTrue(Math.Abs(range.StartPosition - 5) <= 1, $"Start endpoint should move to the hit index, was {range.StartPosition}.");
				Assert.AreEqual(8, range.EndPosition, "Moving Start must preserve End when the endpoints do not cross.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Automation_GetVisibleRanges_Uses_Viewport()
		{
			var SUT = new RichEditBox { Width = 160, Height = 70, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 30).Select(index => $"Line {index:D2}"));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SUT);
				var provider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
				Assert.IsNotNull(provider);
				var visible = provider.GetVisibleRanges();

				Assert.HasCount(1, visible);
				var visibleText = visible[0].GetText(-1);
				Assert.IsFalse(string.IsNullOrEmpty(visibleText));
				Assert.IsLessThan(text.Length, visibleText.Length);
				Assert.IsTrue(text.StartsWith(visibleText, StringComparison.Ordinal));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_DefaultCharacterFormat_Applies_To_Typed_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultCharacterFormat().Italic = FormatEffect.On;
			SUT.Document.Selection.TypeText("hi");

			GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Window_Movement_Does_Not_Reverse_Direction_Outside_Viewport()
		{
			var SUT = new RichEditBox { Width = 200, Height = 80 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var text = string.Join('\r', Enumerable.Range(0, 20).Select(value => value.ToString("D2")));
				SUT.Document.SetText(TextSetOptions.None, text);
				await WindowHelper.WaitForIdle();

				var range = SUT.Document.GetRange(text.Length, text.Length);
				Assert.AreEqual(0, range.Move(TextRangeUnit.Window, 1));
				Assert.AreEqual(text.Length, range.StartPosition);

				SUT.Document.Selection.SetRange(text.Length, text.Length);
				Assert.AreEqual(0, SUT.Document.Selection.MoveDown(TextRangeUnit.Window, 1, false));
				Assert.AreEqual(text.Length, SUT.Document.Selection.StartPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_AdvancedCharacterFormat_Mixed_Range_Reports_Undefined()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "ab");

			SUT.Document.GetRange(0, 1).CharacterFormat.AllCaps = FormatEffect.On;
			SUT.Document.GetRange(1, 2).CharacterFormat.AllCaps = FormatEffect.Off;
			SUT.Document.GetRange(0, 1).CharacterFormat.FontStretch = global::Windows.UI.Text.FontStretch.Expanded;
			SUT.Document.GetRange(1, 2).CharacterFormat.FontStretch = global::Windows.UI.Text.FontStretch.Condensed;

			var mixed = SUT.Document.GetRange(0, 2).CharacterFormat;
			Assert.AreEqual(FormatEffect.Undefined, mixed.AllCaps);
			Assert.AreEqual(TextConstants.UndefinedFontStretch, mixed.FontStretch);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Bold_Toggle_On_Mixed_Weights_Uses_Aggregate_State()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");
				SUT.Document.GetRange(0, 1).CharacterFormat.Weight = 350;
				SUT.Document.GetRange(1, 2).CharacterFormat.Weight = 900;

				SUT.Document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.Toggle;

				Assert.AreEqual(400, SUT.Document.GetRange(0, 1).CharacterFormat.Weight);
				Assert.AreEqual(400, SUT.Document.GetRange(1, 2).CharacterFormat.Weight);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Character_Format_Toggle_Can_Be_Reused()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.Document.GetRange(1, 2).CharacterFormat.Bold = FormatEffect.On;

				var toggle = SUT.Document.GetRange(2, 4).CharacterFormat.GetClone();
				toggle.Bold = FormatEffect.Toggle;
				SUT.Document.GetRange(0, 2).CharacterFormat = toggle;

				Assert.AreEqual(FormatEffect.Toggle, toggle.Bold);
				Assert.AreEqual(400, SUT.Document.GetRange(0, 2).CharacterFormat.Weight);

				SUT.Document.GetRange(2, 4).CharacterFormat = toggle;
				Assert.AreEqual(700, SUT.Document.GetRange(2, 4).CharacterFormat.Weight);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Hidden_Text_Has_No_Visual_Advance_But_Retains_Tom_Positions()
		{
			var SUT = new RichEditBox { Width = 300, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "AXB");
				SUT.Document.GetRange(1, 2).CharacterFormat.Hidden = FormatEffect.On;
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(1, 1).GetRect(PointOptions.ClientCoordinates, out var hiddenStart, out _);
				SUT.Document.GetRange(2, 2).GetRect(PointOptions.ClientCoordinates, out var hiddenEnd, out _);
				SUT.Document.GetRange(1, 2).GetRect(PointOptions.ClientCoordinates, out var hiddenRange, out _);
				Assert.AreEqual(hiddenStart.X, hiddenEnd.X, 0.5, "Hidden text must consume no horizontal advance.");
				Assert.IsTrue(hiddenRange.Width <= 1, $"A hidden range should have caret-width geometry, was {hiddenRange.Width}.");

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("AXB", text);
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(1, 2).CharacterFormat.Hidden);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_NoHidden_Filters_Plain_And_Rtf_Text()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				source.Document.SetText(TextSetOptions.None, "visible hidden tail");
				source.Document.GetRange(8, 14).CharacterFormat.Hidden = FormatEffect.On;

				GetTextWithoutFinalEop(source.Document, TextGetOptions.NoHidden, out var plain);
				Assert.AreEqual("visible  tail", plain);

				source.Document.GetText(TextGetOptions.NoHidden | TextGetOptions.FormatRtf, out var rtf);
				target.Document.SetText(TextSetOptions.FormatRtf, rtf);
				GetTextWithoutFinalEop(target.Document, out var richText);
				Assert.AreEqual("visible  tail", richText);
				Assert.AreEqual(FormatEffect.Off, target.Document.GetRange(0, richText.Length).CharacterFormat.Hidden);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Unhide_Prevents_Plain_And_Rtf_Insertions_From_Remaining_Hidden()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");
				SUT.Document.GetRange(0, 2).CharacterFormat.Hidden = FormatEffect.On;

				var plain = SUT.Document.GetRange(1, 1);
				plain.SetText(TextSetOptions.Unhide, "X");
				Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 2).CharacterFormat.Hidden);

				const string hiddenRtf = @"{\rtf1\ansi\v Y}";
				var rich = SUT.Document.GetRange(2, 2);
				rich.SetText(TextSetOptions.FormatRtf | TextSetOptions.Unhide, hiddenRtf);
				Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(2, 3).CharacterFormat.Hidden);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aXYb", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Protected_Tom_Mutations_Are_Rejected_Atomically()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.GetRange(2, 4).CharacterFormat.ProtectedText = FormatEffect.On;
				SUT.Document.ClearUndoRedoHistory();

				var mixed = SUT.Document.GetRange(1, 5);
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => mixed.CharacterFormat.Italic = FormatEffect.On);
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => mixed.ParagraphFormat.Alignment = ParagraphAlignment.Center);
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => mixed.Link = "\"https://contoso.example\"");
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.SetText(TextSetOptions.None, "replaced"));
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1 replaced}"));

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(1, 2).CharacterFormat.Italic);
				Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(1, 5).ParagraphFormat.Alignment);
				Assert.AreEqual(string.Empty, SUT.Document.GetRange(1, 5).Link);
				Assert.IsFalse(SUT.Document.CanUndo());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Protected_Cut_And_Paste_Reject_Before_Side_Effects()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var seed = new DataPackage();
				seed.SetText("SEED");
				var hasSystemClipboard = RuntimeTestsPlatformHelper.CurrentPlatform != RuntimeTestPlatforms.SkiaTvOS;
				if (hasSystemClipboard)
				{
					Clipboard.SetContent(seed);
				}
				await WindowHelper.WaitForIdle();

				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.GetRange(2, 4).CharacterFormat.ProtectedText = FormatEffect.On;
				var mixed = SUT.Document.GetRange(1, 5);
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => mixed.Cut());
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => mixed.Paste(0));
				if (hasSystemClipboard)
				{
					Assert.AreEqual("SEED", await Clipboard.GetContent().GetTextAsync());
				}

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(1, mixed.StartPosition);
				Assert.AreEqual(5, mixed.EndPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Protection_Is_Explicitly_Removed_Mutation_Succeeds()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				var range = SUT.Document.GetRange(0, 3);
				range.CharacterFormat.ProtectedText = FormatEffect.On;

				range.CharacterFormat.ProtectedText = FormatEffect.Off;
				Assert.AreEqual(FormatEffect.Off, range.CharacterFormat.ProtectedText);
				range.Text = "updated";

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("updated", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ReadOnly_Tom_Mutations_Are_Rejected_Atomically()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.ClearUndoRedoHistory();
				SUT.IsReadOnly = true;

				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.SetText(TextSetOptions.None, "replaced"));
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.GetRange(0, 1).Text = "x");
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On);
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => SUT.Document.GetRange(0, 1).ParagraphFormat.Alignment = ParagraphAlignment.Center);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abc", text);
				Assert.IsFalse(SUT.Document.CanUndo());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ReadOnly_Undo_And_Redo_Are_Ignored()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.ClearUndoRedoHistory();
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.IsReadOnly = true;

				SUT.Document.Undo();
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcd", text);
				Assert.IsTrue(SUT.Document.CanUndo());

				SUT.IsReadOnly = false;
				SUT.Document.Undo();
				SUT.IsReadOnly = true;
				SUT.Document.Redo();
				GetTextWithoutFinalEop(SUT.Document, out text);
				Assert.AreEqual("abc", text);
				Assert.IsTrue(SUT.Document.CanRedo());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Protected_Caret_Text_Edit_Ignores_Range_Gravity()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcdef");
				SUT.Document.GetRange(2, 4).CharacterFormat.ProtectedText = FormatEffect.On;

				var inside = SUT.Document.GetRange(3, 3);
				inside.Text = "X";
				var forwardBoundary = SUT.Document.GetRange(2, 2);
				forwardBoundary.Gravity = RangeGravity.Forward;
				forwardBoundary.Text = "Y";
				var backwardBoundary = SUT.Document.GetRange(2, 2);
				backwardBoundary.Gravity = RangeGravity.Backward;
				backwardBoundary.Text = "Z";
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abZYcXdef", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Deleting_Paragraph_Break_Normalizes_Merged_Paragraph_Format()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "aaa\rbbb");
				SUT.Document.GetRange(0, 3).ParagraphFormat.Alignment = ParagraphAlignment.Left;
				SUT.Document.GetRange(4, 7).ParagraphFormat.Alignment = ParagraphAlignment.Right;

				SUT.Document.GetRange(3, 4).Delete(TextRangeUnit.Character, 1);

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("aaabbb", text);
				Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(0, 6).ParagraphFormat.Alignment);

				SUT.Document.GetRange(6, 6).Text = "x";
				Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(6, 7).ParagraphFormat.Alignment);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Live_Format_Handles_Do_Not_Overwrite_Unrelated_Changes()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");

				var staleCharacter = SUT.Document.GetRange(0, 3).CharacterFormat;
				SUT.Document.GetRange(0, 3).CharacterFormat.Italic = FormatEffect.On;
				staleCharacter.Bold = FormatEffect.On;
				Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(0, 3).CharacterFormat.Italic);

				var staleParagraph = SUT.Document.GetRange(0, 3).ParagraphFormat;
				SUT.Document.GetRange(0, 3).ParagraphFormat.SpaceBefore = 12;
				staleParagraph.Alignment = ParagraphAlignment.Center;
				var paragraph = SUT.Document.GetRange(0, 3).ParagraphFormat;
				Assert.AreEqual(12f, paragraph.SpaceBefore);
				Assert.AreEqual(ParagraphAlignment.Center, paragraph.Alignment);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Mixed_Formats_Use_TextConstants_And_Sentinel_Setters_Are_NoOps()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "ab");
				SUT.Document.GetRange(0, 1).CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Red;
				SUT.Document.GetRange(1, 2).CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Blue;
				SUT.Document.GetRange(0, 1).CharacterFormat.Size = 12;
				SUT.Document.GetRange(1, 2).CharacterFormat.Size = 18;
				SUT.Document.GetRange(0, 1).CharacterFormat.Italic = FormatEffect.On;

				var mixed = SUT.Document.GetRange(0, 2).CharacterFormat;
				Assert.AreEqual(TextConstants.UndefinedColor, mixed.ForegroundColor);
				Assert.AreEqual(TextConstants.UndefinedFloatValue, mixed.Size);
				Assert.AreEqual(TextConstants.UndefinedFontStyle, mixed.FontStyle);
				mixed.ForegroundColor = TextConstants.UndefinedColor;
				mixed.Size = TextConstants.UndefinedFloatValue;
				Assert.AreEqual(Microsoft.UI.Colors.Red, SUT.Document.GetRange(0, 1).CharacterFormat.ForegroundColor);
				Assert.AreEqual(Microsoft.UI.Colors.Blue, SUT.Document.GetRange(1, 2).CharacterFormat.ForegroundColor);

				SUT.Document.GetRange(0, 2).CharacterFormat.ForegroundColor = TextConstants.AutoColor;
				Assert.AreEqual(TextConstants.AutoColor, SUT.Document.GetRange(0, 2).CharacterFormat.ForegroundColor);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Standard_Rtf_Picture_Imports_Without_Uno_Metadata()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				using var stream = CreateImageStream(Microsoft.UI.Colors.Purple);
				using var memory = new MemoryStream();
				stream.AsStreamForRead().CopyTo(memory);
				var hex = Convert.ToHexString(memory.ToArray());
				SUT.Document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1 A{{\pict\pngblip\picw2\pich2\picwgoal300\pichgoal150 {hex}}}B}}");
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				GetTextWithoutFinalEop(SUT.Document, TextGetOptions.UseObjectText, out var objectText);
				Assert.AreEqual("A\ufffcB", text);
				Assert.AreEqual("AB", objectText);
				SUT.Document.GetRange(1, 2).GetRect(PointOptions.ClientCoordinates, out var imageRect, out _);
				Assert.AreEqual(20, imageRect.Width, 1);
				Assert.IsTrue(imageRect.Height >= 10, $"The visual line should contain the 10-DIP picture, height was {imageRect.Height}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Range_Rtf_Stream_NoHidden_Removes_Hidden_Text()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				source.Document.SetText(TextSetOptions.None, "abc");
				source.Document.GetRange(1, 2).CharacterFormat.Hidden = FormatEffect.On;
				var stream = new InMemoryRandomAccessStream();
				source.Document.GetRange(0, 3).GetTextViaStream(TextGetOptions.FormatRtf | TextGetOptions.NoHidden, stream);
				target.Document.GetRange(0, 0).SetTextViaStream(TextSetOptions.FormatRtf, stream);

				GetTextWithoutFinalEop(target.Document, out var text);
				Assert.AreEqual("ac", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Forward_Gravity_Paste_At_Protected_Boundary_Is_Rejected()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.Document.GetRange(1, 3).CharacterFormat.ProtectedText = FormatEffect.On;
				var package = new DataPackage();
				package.SetText("X");
				Clipboard.SetContent(package);

				var range = SUT.Document.GetRange(1, 1);
				range.Gravity = RangeGravity.Forward;
				Assert.ThrowsExactly<UnauthorizedAccessException>(() => range.Paste(0));
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcd", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Rtf_Import_Honors_MaxLength_Before_Model_Allocation()
		{
			var SUT = new RichEditBox { MaxLength = 3 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.FormatRtf | TextSetOptions.CheckTextLimit, @"{\rtf1 abcdef}");

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abc", text);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Rtf_Truncation_Does_Not_Apply_Discarded_Paragraph_Format()
		{
			var SUT = new RichEditBox { MaxLength = 1 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(
					TextSetOptions.FormatRtf | TextSetOptions.CheckTextLimit,
					@"{\rtf1\ql a\par\qr b}");

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("a", text);
				Assert.AreEqual(ParagraphAlignment.Left, SUT.Document.GetRange(1, 1).ParagraphFormat.Alignment);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Import_Rejects_Paragraph_Tabs_Above_Hard_Limit()
		{
			var SUT = new RichEditBox();
			var tabs = string.Join(';', Enumerable.Range(0, 64).Select(index => $"{index}|0|0"));
			var encodedTabs = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tabs));
			var rtf = $@"{{\rtf1{{\*\unopara 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,{encodedTabs}}}text}}";

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, rtf));
			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Maximum_Rtf_Paragraph_Tabs_Remain_Bounded_Across_Many_Paragraphs()
		{
			var SUT = new RichEditBox();
			var tabs = string.Join(';', Enumerable.Range(0, 63).Select(index => $"{index}|0|0"));
			var encodedTabs = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tabs));
			var paragraphs = string.Concat(Enumerable.Repeat(@"\ql\par\qr\par", 16_384));
			var rtf = $@"{{\rtf1{{\*\unopara 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,{encodedTabs}}}{paragraphs}}}";
			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			var range = SUT.Document.GetRange(0, 32_768);
			var format = range.ParagraphFormat;
			Assert.AreEqual(63, format.TabCount);
			Assert.ThrowsExactly<ArgumentException>(() => format.AddTab(64, TabAlignment.Left, TabLeader.Spaces));
			SUT.Document.ClearUndoRedoHistory();
			format.AddTab(0, TabAlignment.Right, TabLeader.Dashes);

			var mutated = SUT.Document.GetRange(0, 0).ParagraphFormat;
			Assert.AreEqual(63, mutated.TabCount);
			mutated.GetTab(0, out var position, out var alignment, out var leader);
			Assert.AreEqual(0f, position);
			Assert.AreEqual(TabAlignment.Right, alignment);
			Assert.AreEqual(TabLeader.Dashes, leader);
			Assert.IsFalse(SUT.Document.CanUndo(), "The oversized pre-mutation snapshot must not be retained in undo history.");
		}

		[TestMethod]
		public void When_Paragraph_Tab_Mutation_Undo_Restores_Distinct_Paragraph_States()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "a\rb");
			SUT.Document.GetRange(0, 1).ParagraphFormat.Alignment = ParagraphAlignment.Left;
			SUT.Document.GetRange(2, 3).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			var format = SUT.Document.GetRange(0, 3).ParagraphFormat;
			format.AddTab(10, TabAlignment.Left, TabLeader.Spaces);
			SUT.Document.ClearUndoRedoHistory();

			format.AddTab(10, TabAlignment.Right, TabLeader.Dashes);
			SUT.Document.Undo();

			var first = SUT.Document.GetRange(0, 1).ParagraphFormat;
			var second = SUT.Document.GetRange(2, 3).ParagraphFormat;
			first.GetTab(0, out _, out var firstAlignment, out var firstLeader);
			second.GetTab(0, out _, out var secondAlignment, out var secondLeader);
			Assert.AreEqual(TabAlignment.Left, firstAlignment);
			Assert.AreEqual(TabLeader.Spaces, firstLeader);
			Assert.AreEqual(TabAlignment.Left, secondAlignment);
			Assert.AreEqual(TabLeader.Spaces, secondLeader);
			Assert.AreEqual(ParagraphAlignment.Left, first.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, second.Alignment);
		}

		[TestMethod]
		public void When_Paragraph_Tab_Rejects_Invalid_Positions()
		{
			var format = new RichEditBox().Document.GetDefaultParagraphFormat();

			Assert.ThrowsExactly<ArgumentException>(() => format.AddTab(-1, TabAlignment.Left, TabLeader.Spaces));
			Assert.ThrowsExactly<ArgumentException>(() => format.AddTab(float.NaN, TabAlignment.Left, TabLeader.Spaces));
			Assert.ThrowsExactly<ArgumentException>(() => format.AddTab(float.PositiveInfinity, TabAlignment.Left, TabLeader.Spaces));
			Assert.ThrowsExactly<ArgumentException>(() => format.DeleteTab(-1));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Import_Rejects_Language_Tag_Above_Hard_Limit()
		{
			var SUT = new RichEditBox();
			var language = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string('a', 257)));
			var rtf = $@"{{\rtf1{{\*\unochar 0,-,5,0,0,{language},0,0,0,0,0,0,0,0,400,0,1}}text}}";

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetText(TextSetOptions.FormatRtf, rtf));
			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Rtf_Export_Rejects_Metadata_Amplification_Before_Stream_Mutation()
		{
			var SUT = new RichEditBox();
			var language = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string('\u0800', 256)));
			var characterMetadata = $@"{{\*\unochar 0,-,5,0,0,{language},0,0,0,0,0,0,0,0,400,0,1}}";
			var alternatingText = string.Concat(Enumerable.Repeat(@"a{\plain b}", 32_700));
			SUT.Document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1{characterMetadata}{alternatingText}}}");

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.GetText(TextGetOptions.FormatRtf, out _));

			var stream = new InMemoryRandomAccessStream();
			var streamAdapter = stream.AsStreamForWrite();
			streamAdapter.WriteByte(42);
			streamAdapter.Flush();
			Assert.AreEqual(1ul, stream.Size);

			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SaveToStream(TextGetOptions.FormatRtf, stream));
			Assert.AreEqual(1ul, stream.Size);
		}

		[TestMethod]
		public async Task When_Same_Rtf_SetText_Synchronizes_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				SUT.Document.Selection.SetRange(2, 2);
				SUT.Focus(FocusState.Programmatic);
				await WindowHelper.WaitForIdle();
				SUT.Document.GetText(TextGetOptions.FormatRtf, out var rtf);

				SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

				Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
#if HAS_UNO
				Assert.AreEqual(0, SUT.SelectionStartForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Undefined_Indent_Clone_Does_Not_Overwrite_Existing_Indents()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				var range = SUT.Document.GetRange(0, 3);
				range.ParagraphFormat.SetIndents(1, 2, 3);
				var undefined = range.ParagraphFormat.GetClone();
				undefined.SetIndents(
					TextConstants.UndefinedFloatValue,
					TextConstants.UndefinedFloatValue,
					TextConstants.UndefinedFloatValue);

				range.ParagraphFormat.SetClone(undefined);

				var actual = range.ParagraphFormat;
				Assert.AreEqual(1f, actual.FirstLineIndent);
				Assert.AreEqual(2f, actual.LeftIndent);
				Assert.AreEqual(3f, actual.RightIndent);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Live_Paragraph_Tab_Handles_Merge_Operations()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abc");
				var first = SUT.Document.GetRange(0, 3).ParagraphFormat;
				var second = SUT.Document.GetRange(0, 3).ParagraphFormat;

				first.AddTab(10, TabAlignment.Left, TabLeader.Spaces);
				second.AddTab(20, TabAlignment.Right, TabLeader.Dashes);

				var actual = SUT.Document.GetRange(0, 3).ParagraphFormat;
				Assert.AreEqual(2, actual.TabCount);
				actual.GetTab(0, out var firstPosition, out _, out _);
				actual.GetTab(1, out var secondPosition, out _, out _);
				Assert.AreEqual(10f, firstPosition);
				Assert.AreEqual(20f, secondPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Vertical_Move_Uses_Actual_Target_Line_Height()
		{
			var SUT = new RichEditBox { Width = 200 };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "a\rb\rc");
				SUT.Document.GetRange(2, 3).CharacterFormat.Size = 80;
				await WindowHelper.WaitForIdle();
				SUT.Document.Selection.SetRange(0, 0);

				Assert.AreEqual(2, SUT.Document.Selection.MoveDown(TextRangeUnit.Line, 2, false));
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_AdvancedCharacterFormat_FormattedText_And_Undo_Preserve_State()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "abc");
			var sourceRange = source.Document.GetRange(0, 3);
			var value = sourceRange.CharacterFormat.GetClone();
			value.AllCaps = FormatEffect.On;
			value.BackgroundColor = Microsoft.UI.Colors.Coral;
			value.FontStretch = global::Windows.UI.Text.FontStretch.SemiExpanded;
			value.Spacing = 2;
			value.TextScript = TextScript.Ansi;
			sourceRange.CharacterFormat.SetClone(value);

			target.Document.GetRange(0, 0).FormattedText = sourceRange;
			var transferred = target.Document.GetRange(0, 3).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, transferred.AllCaps);
			Assert.AreEqual(Microsoft.UI.Colors.Coral, transferred.BackgroundColor);
			Assert.AreEqual(global::Windows.UI.Text.FontStretch.SemiExpanded, transferred.FontStretch);
			Assert.AreEqual(2f, transferred.Spacing);
			Assert.AreEqual(TextScript.Ansi, transferred.TextScript);

			source.Document.Undo();
			var restored = source.Document.GetRange(0, 3).CharacterFormat;
			Assert.AreEqual(FormatEffect.Off, restored.AllCaps);
			Assert.AreEqual(global::Windows.UI.Text.FontStretch.Normal, restored.FontStretch);
			Assert.AreEqual(0f, restored.Spacing);
			Assert.AreEqual(TextScript.Default, restored.TextScript);
		}

		[TestMethod]
		public async Task When_AdvancedCharacterFormat_At_Caret_Applies_To_Typed_Text()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "a");
			SUT.Document.Selection.SetRange(1, 1);

			var value = SUT.Document.Selection.CharacterFormat.GetClone();
			value.AllCaps = FormatEffect.On;
			value.FontStretch = global::Windows.UI.Text.FontStretch.ExtraExpanded;
			value.LanguageTag = "en-US";
			value.Spacing = 3;
			SUT.Document.Selection.CharacterFormat.SetClone(value);
			SUT.Document.Selection.TypeText("x");

			var inserted = SUT.Document.GetRange(1, 2).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, inserted.AllCaps);
			Assert.AreEqual(global::Windows.UI.Text.FontStretch.ExtraExpanded, inserted.FontStretch);
			Assert.AreEqual("en-US", inserted.LanguageTag);
			Assert.AreEqual(3f, inserted.Spacing);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_DefaultParagraphFormat_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.GetDefaultParagraphFormat().Alignment = ParagraphAlignment.Right;

			Assert.AreEqual(ParagraphAlignment.Right, SUT.Document.GetDefaultParagraphFormat().Alignment);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Same_Text_Is_Set_Text_Events_Are_Raised(bool observeTextChanging)
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				SUT.Document.SetText(TextSetOptions.None, "abc");
				await WindowHelper.WaitForIdle();

				var changing = 0;
				var changed = 0;
				SUT.TextChanged += (s, e) => changed++;
				if (observeTextChanging)
				{
					SUT.TextChanging += (s, e) => changing++;
				}

				SUT.Document.SetText(TextSetOptions.None, "abc");
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(1, changed);
				Assert.AreEqual(observeTextChanging ? 1 : 0, changing);

				SUT.Document.GetRange(0, 1).Text = "a";
				await WindowHelper.WaitForIdle();
				Assert.AreEqual(2, changed);
				Assert.AreEqual(observeTextChanging ? 2 : 0, changing);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_TextChanged_Raised_On_Format_Only_Change()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			SUT.TextChanged += (s, e) => count++;

			// WinUI raises TextChanged for formatting-only document changes as well as text changes.
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
					SUT.Document.Selection.Options |= SelectionOptions.StartActive;
				};
				SUT.SelectionChanged += (s, e) => changedCount++;

				SUT.Document.Selection.SetRange(2, 5);

				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(4, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(6, SUT.Document.Selection.EndPosition);
#if HAS_UNO
				Assert.AreEqual(4, SUT.SelectionStartForTesting);
				Assert.AreEqual(2, SUT.SelectionLengthForTesting);
				Assert.IsTrue(SUT.IsSelectionBackwardForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Programmatic_Same_Selection_Raises_No_Events()
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

				Assert.AreEqual(0, changingCount);
				Assert.AreEqual(0, changedCount);
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcdef", text);
				Assert.AreEqual(1, changingCount);
				Assert.AreEqual(1, changedCount);
				Assert.AreEqual(5, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(5, SUT.Document.Selection.EndPosition);
#if HAS_UNO
				Assert.AreEqual(5, SUT.SelectionStartForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
#if HAS_UNO
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_SelectionChanging_Handler_Explicit_Same_Proposal_Does_Not_Override_Cancel()
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
				Assert.AreEqual(0, changedCount);
				Assert.AreEqual(1, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(1, SUT.Document.Selection.EndPosition);
#if HAS_UNO
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
#endif
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
#if HAS_UNO
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
#if HAS_UNO
				Assert.AreEqual(1, SUT.SelectionStartForTesting);
#endif
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
#if HAS_UNO
				Assert.AreEqual(2, SUT.SelectionStartForTesting);
#endif
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("abcf", text);
				Assert.AreEqual("de", await Clipboard.GetContent().GetTextAsync());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS | RuntimeTestPlatforms.NativeWinUI)]
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
					GetTextWithoutFinalEop(SUT.Document, out var text);
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS | RuntimeTestPlatforms.NativeWinUI)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_DefaultTabStop_Changes_Tab_Layout()
		{
			var SUT = new RichEditBox { Width = 300, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "A\tB");
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(2, 2).GetRect(PointOptions.ClientCoordinates, out var defaultRect, out _);
				SUT.Document.DefaultTabStop = 72;
				await WindowHelper.WaitForIdle();
				SUT.Document.GetRange(2, 2).GetRect(PointOptions.ClientCoordinates, out var expandedRect, out _);

				Assert.AreEqual(48, expandedRect.X - defaultRect.X, 2, "Doubling the tab stop from 36pt to 72pt should add 48 DIPs before the following character.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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
		public async Task When_CanCopy_Does_Not_Depend_On_Selection()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.IsTrue(SUT.Document.CanCopy());

				SUT.Document.SetText(TextSetOptions.None, "Hello world");
				SUT.Document.Selection.SetRange(0, 0);
				Assert.IsTrue(SUT.Document.CanCopy());

				SUT.Document.Selection.SetRange(0, 5);
				Assert.IsTrue(SUT.Document.CanCopy());

				SUT.IsReadOnly = true;
				Assert.IsTrue(SUT.Document.CanCopy());
				Assert.IsFalse(SUT.Document.CanPaste());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("final", text);
			};

			SUT.Document.SetText(TextSetOptions.None, "intermediate");

			Assert.AreEqual(1, changingCount);
			Assert.AreEqual(0, changedCount);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, changedCount);
		}

		[TestMethod]
		public async Task When_TextChanging_Format_Only_Reports_Content_False()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			await WindowHelper.WaitForIdle();

			var count = 0;
			var isContentChanging = true;
			SUT.TextChanging += (s, e) =>
			{
				count++;
				isContentChanging = e.IsContentChanging;
			};

			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, count);
			Assert.IsFalse(isContentChanging);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_FormattedText_Copies_Character_And_Paragraph_Formatting()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "ab");
			source.Document.GetRange(0, 1).CharacterFormat.Bold = FormatEffect.On;
			source.Document.GetRange(0, 2).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			target.Document.SetText(TextSetOptions.None, "zz");

			var destination = target.Document.GetRange(0, 2);
			destination.FormattedText = source.Document.GetRange(0, 2);

			GetTextWithoutFinalEop(target.Document, out var text);
			Assert.AreEqual("ab", text);
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(0, 1).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.Off, target.Document.GetRange(1, 2).CharacterFormat.Bold);
			Assert.AreEqual(ParagraphAlignment.Right, target.Document.GetRange(0, 2).ParagraphFormat.Alignment);
			Assert.AreEqual(0, destination.StartPosition);
			Assert.AreEqual(2, destination.EndPosition);
		}

		[TestMethod]
		public async Task When_FormattedText_Overlap_Is_Snapshotted_And_Undoable()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			SUT.Document.GetRange(1, 3).CharacterFormat.Italic = FormatEffect.On;
			SUT.Document.ClearUndoRedoHistory();

			var destination = SUT.Document.GetRange(2, 5);
			destination.FormattedText = SUT.Document.GetRange(1, 4);

			GetTextWithoutFinalEop(SUT.Document, out var copied);
			Assert.AreEqual("abbcdf", copied);
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(2, 4).CharacterFormat.Italic);
			Assert.AreEqual(FormatEffect.Off, SUT.Document.GetRange(4, 5).CharacterFormat.Italic);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var restored);
			Assert.AreEqual("abcdef", restored);
			Assert.AreEqual(FormatEffect.On, SUT.Document.GetRange(1, 3).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_FormattedText_Get_Returns_Live_Clone()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			var formatted = SUT.Document.GetRange(1, 2).FormattedText;
			SUT.Document.GetRange(0, 0).Text = "x";

			Assert.AreEqual(2, formatted.StartPosition);
			Assert.AreEqual(3, formatted.EndPosition);
			Assert.AreEqual("b", formatted.Text);
		}

		[TestMethod]
		[DataRow(RangeGravity.Backward, 1, 4)]
		[DataRow(RangeGravity.Forward, 1, 4)]
		[DataRow(RangeGravity.Inward, 1, 4)]
		[DataRow(RangeGravity.Outward, 1, 4)]
		public async Task When_RangeGravity_Controls_Start_Insertion(RangeGravity gravity, int expectedStart, int expectedEnd)
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcd");
			var range = SUT.Document.GetRange(1, 3);
			range.Gravity = gravity;
			SUT.Document.GetRange(1, 1).Text = "X";

			Assert.AreEqual(expectedStart, range.StartPosition);
			Assert.AreEqual(expectedEnd, range.EndPosition);
		}

		[TestMethod]
		[DataRow(RangeGravity.Backward, 1, 3)]
		[DataRow(RangeGravity.Forward, 1, 3)]
		[DataRow(RangeGravity.Inward, 1, 3)]
		[DataRow(RangeGravity.Outward, 1, 3)]
		public async Task When_RangeGravity_Controls_End_Insertion(RangeGravity gravity, int expectedStart, int expectedEnd)
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abcd");
			var range = SUT.Document.GetRange(1, 3);
			range.Gravity = gravity;
			SUT.Document.GetRange(3, 3).Text = "X";

			Assert.AreEqual(expectedStart, range.StartPosition);
			Assert.AreEqual(expectedEnd, range.EndPosition);
		}

		[TestMethod]
		public async Task When_RangeGravity_Controls_Caret_Format_Basis()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "ab");
			SUT.Document.GetRange(1, 2).CharacterFormat.Bold = FormatEffect.On;
			var backward = SUT.Document.GetRange(1, 1);
			backward.Gravity = RangeGravity.Backward;
			var forward = SUT.Document.GetRange(1, 1);
			forward.Gravity = RangeGravity.Forward;

			Assert.AreEqual(FormatEffect.Off, backward.CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, forward.CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_Sentence_Units_Move_Expand_And_Index()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "One.  Two?\tThree!");
			var range = SUT.Document.GetRange(0, 0);
			Assert.AreEqual(1, range.Move(TextRangeUnit.Sentence, 1));
			Assert.AreEqual(6, range.StartPosition);
			Assert.AreEqual(2, range.GetIndex(TextRangeUnit.Sentence));

			range.Expand(TextRangeUnit.Sentence);
			Assert.AreEqual("Two?\t", range.Text);
			range.SetIndex(TextRangeUnit.Sentence, -1, false);
			Assert.AreEqual(11, range.StartPosition);
			Assert.AreEqual(3, range.GetIndex(TextRangeUnit.Sentence));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Link_Set_Get_Expands_To_Whole_Friendly_Name()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "go to Contoso now");
			SUT.Document.GetRange(6, 13).Link = "\"https://contoso.example\"";

			var partial = SUT.Document.GetRange(8, 9);
			Assert.AreEqual("\"https://contoso.example\"", partial.Link);
			Assert.AreEqual(6, partial.StartPosition);
			Assert.AreEqual(13, partial.EndPosition);
			Assert.AreEqual(LinkType.FriendlyLinkName, partial.CharacterFormat.LinkType);
			Assert.AreEqual(LinkType.NotALink, SUT.Document.GetRange(0, 2).CharacterFormat.LinkType);
		}

		[TestMethod]
		public async Task When_Link_Validates_Quoted_Nondegenerate_Input()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "abc");
			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.GetRange(0, 0).Link = "\"https://contoso.example\"");
			Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.GetRange(0, 3).Link = "https://contoso.example");

			SUT.Document.GetRange(0, 3).Link = "\ufddf\"https://contoso.example\"";
			Assert.AreEqual("\ufddf\"https://contoso.example\"", SUT.Document.GetRange(1, 2).Link);
		}

		[TestMethod]
		public async Task When_Link_Unlink_Undo_And_FormattedText_Preserve_Metadata()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "link");
			source.Document.GetRange(0, 4).Link = "\"https://contoso.example\"";
			target.Document.SetText(TextSetOptions.None, "xxxx");
			target.Document.GetRange(0, 4).FormattedText = source.Document.GetRange(0, 4);
			Assert.AreEqual("\"https://contoso.example\"", target.Document.GetRange(0, 4).Link);

			target.Document.ClearUndoRedoHistory();
			target.Document.GetRange(0, 4).Link = string.Empty;
			Assert.AreEqual(string.Empty, target.Document.GetRange(0, 4).Link);
			target.Document.Undo();
			Assert.AreEqual("\"https://contoso.example\"", target.Document.GetRange(0, 4).Link);
		}

		[TestMethod]
		public void When_Empty_Rtf_Replaces_Document_And_Range()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			SUT.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			SUT.Document.Selection.SetRange(4, 4);

			SUT.Document.SetText(TextSetOptions.FormatRtf, string.Empty);

			GetTextWithoutFinalEop(SUT.Document, out var cleared);
			Assert.AreEqual(string.Empty, cleared);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(0, SUT.Document.Selection.EndPosition);

			SUT.Document.SetText(TextSetOptions.None, "abcdef");
			var range = SUT.Document.GetRange(2, 4);
			range.SetText(TextSetOptions.FormatRtf, string.Empty);

			GetTextWithoutFinalEop(SUT.Document, out var rangeText);
			Assert.AreEqual("abef", rangeText);
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(2, range.EndPosition);
		}

		[TestMethod]
		public void When_Empty_Rtf_Stream_Clears_Document()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.None, "old");
			var stream = new InMemoryRandomAccessStream();

			SUT.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual(string.Empty, text);
		}

		[TestMethod]
		public void When_Word_Font_Table_Metadata_Is_Parsed()
		{
			const string rtf = @"{\rtf1\ansi\deff1{\fonttbl"
				+ @"{\f0\fbidi\fnil\fcharset0\fprq2{\*\panose 020b0604020202020204}Segoe UI{\*\falt Arial};}"
				+ @"{\f1\froman\fcharset0\fprq2 Times New Roman;}}"
				+ @"default \f0 sans {\f1 serif}\plain default-again}";
			var SUT = new RichEditBox();

			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("default sans serifdefault-again", text);
			Assert.AreEqual("Times New Roman", SUT.Document.GetRange(0, 7).CharacterFormat.Name);
			Assert.AreEqual("Segoe UI", SUT.Document.GetRange(8, 12).CharacterFormat.Name);
			Assert.AreEqual("Times New Roman", SUT.Document.GetRange(13, 18).CharacterFormat.Name);
			Assert.AreEqual("Times New Roman", SUT.Document.GetRange(18, 31).CharacterFormat.Name);
		}

		[TestMethod]
		[DataRow("ul", UnderlineType.Single)]
		[DataRow("ulw", UnderlineType.Words)]
		[DataRow("uldb", UnderlineType.Double)]
		[DataRow("uld", UnderlineType.Dotted)]
		[DataRow("uldash", UnderlineType.Dash)]
		[DataRow("uldashd", UnderlineType.DashDot)]
		[DataRow("uldashdd", UnderlineType.DashDotDot)]
		[DataRow("ulwave", UnderlineType.Wave)]
		[DataRow("ulth", UnderlineType.Thick)]
		[DataRow("ulhair", UnderlineType.Thin)]
		[DataRow("ululdbwave", UnderlineType.DoubleWave)]
		[DataRow("ulhwave", UnderlineType.HeavyWave)]
		[DataRow("ulldash", UnderlineType.LongDash)]
		[DataRow("ulthdash", UnderlineType.ThickDash)]
		[DataRow("ulthdashd", UnderlineType.ThickDashDot)]
		[DataRow("ulthdashdd", UnderlineType.ThickDashDotDot)]
		[DataRow("ulthd", UnderlineType.ThickDotted)]
		[DataRow("ulthldash", UnderlineType.ThickLongDash)]
		public void When_Standard_Rtf_Underline_Style_RoundTrips(string control, UnderlineType expected)
		{
			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.FormatRtf, $@"{{\rtf1\{control} text}}");
			Assert.AreEqual(expected, source.Document.GetRange(0, 4).CharacterFormat.Underline);

			source.Document.GetText(TextGetOptions.FormatRtf, out var exported);
			Assert.IsTrue(exported.Contains($@"\{control}", StringComparison.Ordinal));

			var target = new RichEditBox();
			target.Document.SetText(TextSetOptions.FormatRtf, exported);
			Assert.AreEqual(expected, target.Document.GetRange(0, 4).CharacterFormat.Underline);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Standard_Double_Wave_Underline_Alias_Is_Parsed()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\uldbwave text}");

			Assert.AreEqual(UnderlineType.DoubleWave, SUT.Document.GetRange(0, 4).CharacterFormat.Underline);
		}

		[TestMethod]
		public void When_Standard_Rtf_Underline_Resets_Are_Parsed()
		{
			var SUT = new RichEditBox();
			SUT.Document.SetText(TextSetOptions.FormatRtf, @"{\rtf1\ulth thick\ulth0 plain\ul double?\ulnone none}");

			Assert.AreEqual(UnderlineType.Thick, SUT.Document.GetRange(0, 5).CharacterFormat.Underline);
			Assert.AreEqual(UnderlineType.None, SUT.Document.GetRange(5, 10).CharacterFormat.Underline);
			Assert.AreEqual(UnderlineType.Single, SUT.Document.GetRange(10, 17).CharacterFormat.Underline);
			Assert.AreEqual(UnderlineType.None, SUT.Document.GetRange(17, 21).CharacterFormat.Underline);
		}

		[TestMethod]
		public async Task When_Thick_Underline_Renders_Thicker_Than_Single()
		{
			var single = new RichEditBox { Width = 240, Height = 70, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) };
			var thick = new RichEditBox { Width = 240, Height = 70, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) };
			var panel = new StackPanel();
			panel.Children.Add(single);
			panel.Children.Add(thick);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				const string spaces = "\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0\u00a0";
				ConfigureUnderline(single, UnderlineType.Single);
				ConfigureUnderline(thick, UnderlineType.Thick);
				await WindowHelper.WaitForIdle();

				var singleBitmap = await UITestHelper.ScreenShot(single);
				var thickBitmap = await UITestHelper.ScreenShot(thick);
				var singleRows = CountRedRows(singleBitmap);
				var thickRows = CountRedRows(thickBitmap);
				Assert.IsTrue(thickRows > singleRows, $"A thick underline should cover more pixel rows than a single underline, got {thickRows} and {singleRows}.");

				void ConfigureUnderline(RichEditBox editor, UnderlineType underline)
				{
					editor.Document.SetText(TextSetOptions.None, spaces);
					var format = editor.Document.GetRange(0, spaces.Length).CharacterFormat;
					format.ForegroundColor = Microsoft.UI.Colors.Red;
					format.Size = 24;
					format.Underline = underline;
				}

				static int CountRedRows(RawBitmap bitmap)
				{
					var rows = 0;
					for (var y = 0; y < bitmap.Height; y++)
					{
						var redPixels = 0;
						for (var x = 0; x < bitmap.Width; x++)
						{
							var pixel = bitmap.GetPixel(x, y);
							if (pixel is { A: > 200, R: > 180, G: < 100, B: < 100 })
							{
								redPixels++;
							}
						}

						if (redPixels >= 10)
						{
							rows++;
						}
					}

					return rows;
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public void When_Alternating_Rich_Text_Stream_Remains_Compact_And_RoundTrips()
		{
			var input = new System.Text.StringBuilder(@"{\rtf1\ansi ");
			for (var i = 0; i < 15_000; i++)
			{
				input.Append(@"{\b a}{\b0 b}");
			}
			input.Append('}');
			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.FormatRtf, input.ToString());
			var stream = new InMemoryRandomAccessStream();

			source.Document.SaveToStream(TextGetOptions.FormatRtf, stream);

			Assert.IsTrue(stream.Size is > 0 and < 4 * 1024 * 1024, $"Expected compact standard RTF output, size was {stream.Size} bytes.");
			var target = new RichEditBox();
			target.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);
			GetTextWithoutFinalEop(target.Document, out var text);
			Assert.AreEqual(30_000, text.Length);
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(0, 1).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.Off, target.Document.GetRange(1, 2).CharacterFormat.Bold);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Rtf_String_RoundTrips_Unicode_Formatting_Paragraph_And_Link()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "Héllo link");
			var formatted = source.Document.GetRange(0, 5).CharacterFormat;
			formatted.Bold = FormatEffect.On;
			formatted.ForegroundColor = Microsoft.UI.Colors.Red;
			formatted.Size = 20;
			source.Document.GetRange(0, 10).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			source.Document.GetRange(6, 10).Link = "\"https://contoso.example\"";

			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
			Assert.IsTrue(rtf.StartsWith("{\\rtf1", StringComparison.Ordinal));
			target.Document.SetText(TextSetOptions.FormatRtf, rtf);

			GetTextWithoutFinalEop(target.Document, out var text);
			Assert.AreEqual("Héllo link", text);
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(0, 5).CharacterFormat.Bold);
			Assert.AreEqual(Microsoft.UI.Colors.Red, target.Document.GetRange(0, 5).CharacterFormat.ForegroundColor);
			Assert.AreEqual(20f, target.Document.GetRange(0, 5).CharacterFormat.Size);
			Assert.AreEqual(ParagraphAlignment.Center, target.Document.GetRange(0, 10).ParagraphFormat.Alignment);
			Assert.AreEqual("\"https://contoso.example\"", target.Document.GetRange(7, 8).Link);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Rtf_AdvancedCharacterFormat_RoundTrips_Standard_And_Metadata_Controls()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "advanced");
			var range = source.Document.GetRange(0, 8);
			var value = range.CharacterFormat.GetClone();
			value.AllCaps = FormatEffect.On;
			value.BackgroundColor = Microsoft.UI.Colors.Coral;
			value.FontStretch = global::Windows.UI.Text.FontStretch.SemiExpanded;
			value.Hidden = FormatEffect.On;
			value.Kerning = 8;
			value.LanguageTag = "en-CA";
			value.Outline = FormatEffect.On;
			value.Position = 2.5f;
			value.ProtectedText = FormatEffect.On;
			value.SmallCaps = FormatEffect.On;
			value.Spacing = 2;
			value.Subscript = FormatEffect.On;
			value.Superscript = FormatEffect.Off;
			value.TextScript = TextScript.Ansi;
			range.CharacterFormat.SetClone(value);

			source.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
			Assert.IsTrue(rtf.Contains(@"\caps", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\highlight", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\expndtw40", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\kerning16", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\up5", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\sub", StringComparison.Ordinal));
			Assert.IsTrue(rtf.Contains(@"\*\unochar", StringComparison.Ordinal));

			target.Document.SetText(TextSetOptions.FormatRtf, rtf);
			var actual = target.Document.GetRange(0, 8).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, actual.AllCaps);
			Assert.AreEqual(Microsoft.UI.Colors.Coral, actual.BackgroundColor);
			Assert.AreEqual(global::Windows.UI.Text.FontStretch.SemiExpanded, actual.FontStretch);
			Assert.AreEqual(FormatEffect.On, actual.Hidden);
			Assert.AreEqual(8f, actual.Kerning);
			Assert.AreEqual("en-CA", actual.LanguageTag);
			Assert.AreEqual(FormatEffect.On, actual.Outline);
			Assert.AreEqual(2.5f, actual.Position);
			Assert.AreEqual(FormatEffect.On, actual.ProtectedText);
			Assert.AreEqual(FormatEffect.On, actual.SmallCaps);
			Assert.AreEqual(2f, actual.Spacing);
			Assert.AreEqual(FormatEffect.On, actual.Subscript);
			Assert.AreEqual(FormatEffect.Off, actual.Superscript);
			Assert.AreEqual(TextScript.Ansi, actual.TextScript);
		}

		[TestMethod]
		public async Task When_Rtf_Standard_AdvancedCharacter_Controls_Are_Parsed()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			const string rtf = @"{\rtf1\ansi{\colortbl ;\red255\green215\blue0;}\caps\highlight1\v\outl\protect\scaps\expndtw40\kerning16\up5\sub X}";
			SUT.Document.SetText(TextSetOptions.FormatRtf, rtf);

			var actual = SUT.Document.GetRange(0, 1).CharacterFormat;
			Assert.AreEqual(FormatEffect.On, actual.AllCaps);
			Assert.AreEqual(global::Windows.UI.Color.FromArgb(255, 255, 215, 0), actual.BackgroundColor);
			Assert.AreEqual(FormatEffect.On, actual.Hidden);
			Assert.AreEqual(FormatEffect.On, actual.Outline);
			Assert.AreEqual(FormatEffect.On, actual.ProtectedText);
			Assert.AreEqual(FormatEffect.On, actual.SmallCaps);
			Assert.AreEqual(2f, actual.Spacing);
			Assert.AreEqual(8f, actual.Kerning);
			Assert.AreEqual(2.5f, actual.Position);
			Assert.AreEqual(FormatEffect.On, actual.Subscript);
			Assert.AreEqual(FormatEffect.Off, actual.Superscript);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_MathML_APIs_Require_Valid_Math_Mode()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(RichEditMathMode.NoMath, SUT.Document.GetMathMode());
				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.GetMathML(out _));
				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetMathML("<math />"));
				Assert.ThrowsExactly<ArgumentException>(() => SUT.Document.SetMathMode((RichEditMathMode)42));
				Assert.AreEqual(RichEditMathMode.NoMath, SUT.Document.GetMathMode());
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_MathMode_Plain_Text_Edit_Exports_MathML()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetMathMode(RichEditMathMode.MathOnly);
				SUT.Document.SetText(TextSetOptions.None, "a < b & c");

				SUT.Document.GetMathML(out var mathML);
				var document = System.Xml.Linq.XDocument.Parse(mathML);
				Assert.AreEqual("http://www.w3.org/1998/Math/MathML", document.Root?.Name.NamespaceName);
				Assert.AreEqual("math", document.Root?.Name.LocalName);
				Assert.AreEqual("a < b & c", document.Root?.Value);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Range_Rtf_Stream_RoundTrips_Formatted_Subrange()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "abcde");
			source.Document.GetRange(1, 4).CharacterFormat.Italic = FormatEffect.On;
			var stream = new InMemoryRandomAccessStream();
			source.Document.GetRange(1, 4).GetTextViaStream(TextGetOptions.FormatRtf, stream);

			target.Document.SetText(TextSetOptions.None, "XX");
			target.Document.GetRange(1, 1).SetTextViaStream(TextSetOptions.FormatRtf, stream);

			GetTextWithoutFinalEop(target.Document, out var text);
			Assert.AreEqual("XbcdX", text);
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(1, 4).CharacterFormat.Italic);
		}

		[TestMethod]
		public async Task When_Document_Plain_And_Rtf_Streams_RoundTrip()
		{
			var source = new RichEditBox();
			var plainTarget = new RichEditBox();
			var richTarget = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(plainTarget);
			panel.Children.Add(richTarget);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "one\rtwö");
			source.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
			var plain = new InMemoryRandomAccessStream();
			var rich = new InMemoryRandomAccessStream();
			source.Document.SaveToStream(TextGetOptions.UseLf, plain);
			source.Document.SaveToStream(TextGetOptions.FormatRtf, rich);

			plainTarget.Document.LoadFromStream(TextSetOptions.None, plain);
			richTarget.Document.LoadFromStream(TextSetOptions.FormatRtf, rich);
			GetTextWithoutFinalEop(plainTarget.Document, out var plainText);
			GetTextWithoutFinalEop(richTarget.Document, out var richText);
			Assert.AreEqual("one\rtwö", plainText);
			Assert.AreEqual("one\rtwö", richText);
			Assert.AreEqual(FormatEffect.Off, plainTarget.Document.GetRange(0, 3).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, richTarget.Document.GetRange(0, 3).CharacterFormat.Bold);
		}

		[TestMethod]
		public async Task When_AllowFinalEop_Appends_Converted_Paragraph_Mark()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			SUT.Document.SetText(TextSetOptions.None, "text");
			SUT.Document.GetText(TextGetOptions.AllowFinalEop, out var raw);
			SUT.Document.GetText(TextGetOptions.AllowFinalEop | TextGetOptions.UseLf, out var lf);
			SUT.Document.GetText(TextGetOptions.AllowFinalEop | TextGetOptions.UseCrlf, out var crlf);
			Assert.AreEqual("text\r", raw);
			Assert.AreEqual("text\n", lf);
			Assert.AreEqual("text\r\n", crlf);
		}

		[TestMethod]
		// Android and UIKit clipboard backends do not expose custom RTF formats.
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaUIKit)]
		public async Task When_ClipboardCopyFormat_Controls_Rtf_Payload()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "copy");
				SUT.Document.Selection.SetRange(0, 4);

				SUT.ClipboardCopyFormat = RichEditClipboardFormat.AllFormats;
				SUT.Document.Selection.Copy();
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Rtf));

				SUT.ClipboardCopyFormat = RichEditClipboardFormat.PlainText;
				SUT.Document.Selection.Copy();
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(Clipboard.GetContent().Contains(StandardDataFormats.Rtf));
				Assert.IsTrue(Clipboard.GetContent().Contains(StandardDataFormats.Text));
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_InsertImage_Replaces_Range_As_One_Object_With_AlternateText()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			SUT.Document.SetText(TextSetOptions.None, "abcd");

			var range = SUT.Document.GetRange(1, 3);
			range.InsertImage(20, 10, 7, VerticalCharacterAlignment.Baseline, "logo", CreateImageStream(Microsoft.UI.Colors.Red));

			GetTextWithoutFinalEop(SUT.Document, out var raw);
			GetTextWithoutFinalEop(SUT.Document, TextGetOptions.UseObjectText, out var objectText);
			Assert.AreEqual("a\ufffcd", raw);
			Assert.AreEqual("alogod", objectText);
			Assert.AreEqual(1, range.StartPosition);
			Assert.AreEqual(2, range.EndPosition);
			Assert.AreEqual(4, SUT.Document.GetRange(0, 0).StoryLength);

			SUT.Document.Undo();
			GetTextWithoutFinalEop(SUT.Document, out var restored);
			Assert.AreEqual("abcd", restored);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_InsertImage_Renders_With_Requested_Layout_Size()
		{
			var SUT = new RichEditBox
			{
				Width = 200,
				Height = 80,
				Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
			};
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var encoded = Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(2, 2, Microsoft.UI.Colors.Red);
				using var stream = new MemoryStream(encoded).AsRandomAccessStream();

				SUT.Document.SetText(TextSetOptions.None, "AB");
				SUT.Document.GetRange(1, 1).InsertImage(40, 20, 15, VerticalCharacterAlignment.Baseline, "red", stream);
				await WindowHelper.WaitForIdle();

				SUT.Document.GetRange(1, 2).GetRect(PointOptions.ClientCoordinates, out var imageRect, out _);
				Assert.AreEqual(40, imageRect.Width, 1, "The image object should use its requested DIP advance.");
				Assert.IsTrue(imageRect.Height >= 20, $"The image should expand its visual line, whose height was {imageRect.Height}.");

				var screenshot = await UITestHelper.ScreenShot(SUT);
				var redBounds = ImageAssert.GetColorBounds(screenshot, Microsoft.UI.Colors.Red, tolerance: 10);
				Assert.IsTrue(redBounds is { Width: > 20, Height: > 10 }, $"The inserted image should render red pixels, bounds were {redBounds}.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Image_FormattedText_And_RtfStream_Preserve_ObjectText()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			var streamTarget = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(source);
			panel.Children.Add(target);
			panel.Children.Add(streamTarget);
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			source.Document.SetText(TextSetOptions.None, "x");
			source.Document.GetRange(1, 1).InsertImage(8, 9, 6, VerticalCharacterAlignment.Bottom, "picture", CreateImageStream(Microsoft.UI.Colors.Green));
			target.Document.GetRange(0, 0).FormattedText = source.Document.GetRange(0, 2);
			GetTextWithoutFinalEop(target.Document, TextGetOptions.UseObjectText, out var formattedText);
			Assert.AreEqual("xpicture", formattedText);

			var stream = new InMemoryRandomAccessStream();
			source.Document.SaveToStream(TextGetOptions.FormatRtf, stream);
			streamTarget.Document.LoadFromStream(TextSetOptions.FormatRtf, stream);
			GetTextWithoutFinalEop(streamTarget.Document, TextGetOptions.UseObjectText, out var streamText);
			Assert.AreEqual("xpicture", streamText);
			GetTextWithoutFinalEop(streamTarget.Document, out var raw);
			Assert.AreEqual("x\ufffc", raw);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_InsertImage_Validates_And_Counts_Against_MaxLength()
		{
			var SUT = new RichEditBox { MaxLength = 1 };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var bytes = CreateImageStream(Microsoft.UI.Colors.Blue);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
				SUT.Document.GetRange(0, 0).InsertImage(-1, 1, 0, VerticalCharacterAlignment.Top, "bad", bytes));

			bytes.Seek(0);
			SUT.Document.GetRange(0, 0).InsertImage(1, 1, 0, VerticalCharacterAlignment.Top, "one", bytes);
			bytes.Seek(0);
			SUT.Document.GetRange(1, 1).InsertImage(1, 1, 0, VerticalCharacterAlignment.Top, "two", bytes);
			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("\ufffc", text);
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
			GetTextWithoutFinalEop(SUT.Document, out var cutText);
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
			GetTextWithoutFinalEop(SUT.Document, out var rangeCutText);
			Assert.AreEqual("def", rangeCutText);
		}

		[TestMethod]
		public async Task When_Document_SetText_CheckTextLimit_Clamps_To_MaxLength()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.MaxLength = 2;
			SUT.Document.SetText(TextSetOptions.CheckTextLimit, "hello world");

			GetTextWithoutFinalEop(SUT.Document, out var clampedText);
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

			GetTextWithoutFinalEop(SUT.Document, out var charText);
			Assert.AreEqual("cut", charText);
		}

		[TestMethod]
		public async Task When_Default_Text_Command_Flyouts_Are_Available()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.ContextFlyout);
				Assert.IsInstanceOfType<TextCommandBarFlyout>(SUT.SelectionFlyout);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_ProofingMenuFlyout_Is_Stable_MenuFlyout()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);

				var first = SUT.ProofingMenuFlyout;
				var second = SUT.ProofingMenuFlyout;
				Assert.IsInstanceOfType<MenuFlyout>(first);
				Assert.AreSame(first, second);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaTvOS)]
		public void When_Unavailable_System_Clipboard_Reports_No_Paste_Content()
		{
			var editor = new RichEditBox();
			editor.Document.SetText(TextSetOptions.None, "text");

			Assert.IsFalse(editor.Document.CanPaste());
			Assert.IsFalse(editor.Document.Selection.CanPaste(0));
			Assert.IsFalse(editor.Document.GetRange(0, 4).CanPaste(0));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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

			GetTextWithoutFinalEop(SUT.Document, out var text);
			Assert.AreEqual("world", text);
			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(0, range.EndPosition, "Cut should collapse the range to its start.");

			var clipboardText = await Clipboard.GetContent().GetTextAsync();
			Assert.AreEqual("Hello ", clipboardText);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaTvOS)]
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
				GetTextWithoutFinalEop(SUT.Document, out var t);
				return t == "AXYB";
			});

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition, "Paste should collapse the range past the inserted text.");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaTvOS)]
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
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_Rich_Clipboard_Content_Can_Be_Cleared()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "rich");
				SUT.Document.GetRange(0, 4).CharacterFormat.Bold = FormatEffect.On;
				SUT.Document.Selection.SetRange(0, 4);

				SUT.Document.Selection.Copy();
				Clipboard.Clear();
				await WindowHelper.WaitForIdle();
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
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

		private static IRandomAccessStream CreateImageStream(Windows.UI.Color color)
			=> new MemoryStream(Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(2, 2, color)).AsRandomAccessStream();
	}
}
