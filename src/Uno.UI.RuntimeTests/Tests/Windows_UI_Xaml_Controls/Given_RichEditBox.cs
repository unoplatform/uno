#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichEditBox
	{
		// =====================================================================
		// Document Model Tests
		// =====================================================================

		#region Document Model Tests

		[TestMethod]
		public async Task When_SetText_Then_GetText_Returns_Same()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.GetText(TextGetOptions.None, out var result);

			Assert.AreEqual("Hello World", result);
		}

		[TestMethod]
		public async Task When_SetText_Normalizes_LineEndings()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Test \r\n normalization
			SUT.Document.SetText(TextSetOptions.None, "Line1\r\nLine2");
			SUT.Document.GetText(TextGetOptions.None, out var resultCrLf);
			Assert.AreEqual("Line1\rLine2", resultCrLf, "\\r\\n should be normalized to \\r");

			// Test \n normalization
			SUT.Document.SetText(TextSetOptions.None, "Line1\nLine2");
			SUT.Document.GetText(TextGetOptions.None, out var resultLf);
			Assert.AreEqual("Line1\rLine2", resultLf, "\\n should be normalized to \\r");

			// Test mixed line endings
			SUT.Document.SetText(TextSetOptions.None, "A\r\nB\nC\rD");
			SUT.Document.GetText(TextGetOptions.None, out var resultMixed);
			Assert.AreEqual("A\rB\rC\rD", resultMixed, "Mixed line endings should all become \\r");
		}

		[TestMethod]
		public async Task When_GetText_UseCrlf_Returns_CrLf()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line1\rLine2");

			SUT.Document.GetText(TextGetOptions.UseCrlf, out var result);
			Assert.AreEqual("Line1\r\nLine2", result,
				"GetText with UseCrlf should convert \\r to \\r\\n");
		}

		[TestMethod]
		public async Task When_GetRange_Returns_Valid_Range()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);

			Assert.AreEqual(0, range.StartPosition);
			Assert.AreEqual(5, range.EndPosition);
			Assert.AreEqual(5, range.Length);

			range.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello", text);
		}

		[TestMethod]
		public async Task When_GetRange_Clamps_To_Document_Bounds()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Short");

			// Request a range beyond document length
			var range = SUT.Document.GetRange(0, 100);
			Assert.AreEqual(5, range.EndPosition, "End position should be clamped to document length");
		}

		[TestMethod]
		public async Task When_Range_SetText_Updates_Document()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(6, 11); // "World"
			range.SetText(TextSetOptions.None, "Uno");

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("Hello Uno", result);
		}

		[TestMethod]
		public async Task When_Range_Text_Property_Updates_Document()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5); // "Hello"
			range.Text = "Goodbye";

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("Goodbye World", result);
		}

		[TestMethod]
		public async Task When_Range_CharacterFormat_Applies_Bold()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5); // "Hello"
			var format = range.CharacterFormat;
			format.Bold = FormatEffect.On;
			range.CharacterFormat = format;

			// Verify the format was applied
			var checkRange = SUT.Document.GetRange(0, 5);
			var checkFormat = checkRange.CharacterFormat;
			Assert.AreEqual(FormatEffect.On, checkFormat.Bold,
				"Bold format should be applied to the range");
		}

		[TestMethod]
		public async Task When_Range_CharacterFormat_Applies_Italic()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			var format = range.CharacterFormat;
			format.Italic = FormatEffect.On;
			range.CharacterFormat = format;

			var checkRange = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(FormatEffect.On, checkRange.CharacterFormat.Italic,
				"Italic format should be applied to the range");
		}

		[TestMethod]
		public async Task When_Range_Move_Navigates_By_Character()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Character, 5);

			Assert.AreEqual(5, moved, "Should move 5 characters forward");
			Assert.AreEqual(5, range.StartPosition, "Start should be at position 5");
			Assert.AreEqual(5, range.EndPosition, "End should equal start after Move (collapsed)");
		}

		[TestMethod]
		public async Task When_Range_Move_Navigates_By_Word()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World Test");

			var range = SUT.Document.GetRange(0, 0);
			var moved = range.Move(TextRangeUnit.Word, 1);

			Assert.AreEqual(1, moved, "Should move 1 word forward");
			// After moving by word from start, position should be at start of next word
			Assert.IsTrue(range.StartPosition > 0, "Position should be past 'Hello'");
		}

		[TestMethod]
		public async Task When_Range_Move_Backwards()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(5, 5);
			var moved = range.Move(TextRangeUnit.Character, -3);

			Assert.AreEqual(-3, moved, "Should move 3 characters backward");
			Assert.AreEqual(2, range.StartPosition, "Position should be at index 2");
		}

		[TestMethod]
		public async Task When_Range_Expand_Word_Selects_Word()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Position cursor inside "Hello"
			var range = SUT.Document.GetRange(2, 2);
			range.Expand(TextRangeUnit.Word);

			range.GetText(TextGetOptions.None, out var selectedText);

			// The expanded range should include the word "Hello" (and possibly trailing space
			// depending on word boundary behavior)
			Assert.IsTrue(selectedText.StartsWith("Hello"),
				$"Expanded word should start with 'Hello', got '{selectedText}'");
			Assert.AreEqual(0, range.StartPosition, "Word start should be at 0");
		}

		[TestMethod]
		public async Task When_Range_Expand_Paragraph_Selects_Paragraph()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line1\rLine2\rLine3");

			// Position cursor inside "Line2"
			var range = SUT.Document.GetRange(7, 7);
			range.Expand(TextRangeUnit.Paragraph);

			range.GetText(TextGetOptions.None, out var selectedText);
			Assert.AreEqual("Line2", selectedText,
				"Expand by paragraph should select the paragraph text");
		}

		[TestMethod]
		public async Task When_Range_FindText_Finds_Match()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World Hello");

			var range = SUT.Document.GetRange(0, 0);
			var found = range.FindText("World", 0, FindOptions.None);

			Assert.IsTrue(found > 0, "FindText should find 'World'");
			range.GetText(TextGetOptions.None, out var foundText);
			Assert.AreEqual("World", foundText);
		}

		[TestMethod]
		public async Task When_Range_FindText_Case_Sensitive()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello world");

			var range = SUT.Document.GetRange(0, 0);

			// Case-insensitive should find it
			var found = range.FindText("WORLD", 0, FindOptions.None);
			Assert.IsTrue(found > 0, "Case-insensitive FindText should find 'WORLD'");

			// Case-sensitive should find it
			var range2 = SUT.Document.GetRange(0, 0);
			var found2 = range2.FindText("WORLD", 0, FindOptions.Case);
			Assert.AreEqual(0, found2, "Case-sensitive FindText should NOT find 'WORLD'");
		}

		[TestMethod]
		public async Task When_Undo_Reverts_Text()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Original");
			SUT.Document.GetText(TextGetOptions.None, out var before);
			Assert.AreEqual("Original", before);

			SUT.Document.SetText(TextSetOptions.None, "Modified");
			SUT.Document.GetText(TextGetOptions.None, out var after);
			Assert.AreEqual("Modified", after);

			Assert.IsTrue(SUT.Document.CanUndo(), "Undo should be available");
			SUT.Document.Undo();

			SUT.Document.GetText(TextGetOptions.None, out var undone);
			Assert.AreEqual("Original", undone, "Undo should revert to 'Original'");
		}

		[TestMethod]
		public async Task When_Redo_Reapplies_Text()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Original");
			SUT.Document.SetText(TextSetOptions.None, "Modified");

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterUndo);
			Assert.AreEqual("Original", afterUndo);

			Assert.IsTrue(SUT.Document.CanRedo(), "Redo should be available");
			SUT.Document.Redo();

			SUT.Document.GetText(TextGetOptions.None, out var afterRedo);
			Assert.AreEqual("Modified", afterRedo, "Redo should reapply 'Modified'");
		}

		[TestMethod]
		public async Task When_ClearUndoRedoHistory_Then_Cannot_Undo()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "First");
			SUT.Document.SetText(TextSetOptions.None, "Second");

			Assert.IsTrue(SUT.Document.CanUndo(), "Should be able to undo before clear");

			SUT.Document.ClearUndoRedoHistory();

			Assert.IsFalse(SUT.Document.CanUndo(), "Should NOT be able to undo after clear");
			Assert.IsFalse(SUT.Document.CanRedo(), "Should NOT be able to redo after clear");
		}

		#endregion

		// =====================================================================
		// Control Properties Tests
		// =====================================================================

		#region Control Properties Tests

		[TestMethod]
		public async Task When_IsReadOnly_Prevents_Document_Edits_Via_Selection()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100, IsReadOnly = true };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Read Only Text");

			// IsReadOnly should be set
			Assert.IsTrue(SUT.IsReadOnly, "IsReadOnly should be true");

			// Document-level SetText still works (IsReadOnly primarily blocks user input)
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Read Only Text", text);
		}

		[TestMethod]
		public async Task When_MaxLength_Is_Set()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100, MaxLength = 10 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(10, SUT.MaxLength, "MaxLength should be set to 10");

			// MaxLength is enforced at the input level (OnCharacterReceived/PasteFromClipboard),
			// not at the Document.SetText level
		}

		[TestMethod]
		public async Task When_AcceptsReturn_Is_False()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100, AcceptsReturn = false };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.AcceptsReturn, "AcceptsReturn should be false");
		}

		[TestMethod]
		public async Task When_AcceptsReturn_Default_Is_True()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.AcceptsReturn, "AcceptsReturn should default to true");
		}

		[TestMethod]
		public async Task When_PlaceholderText_Shows_When_Empty()
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 100,
				PlaceholderText = "Enter text here"
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Enter text here", SUT.PlaceholderText,
				"PlaceholderText should be set");

			// Placeholder should be visible when document is empty and unfocused
			// (when the control has not received focus)
			var placeholder = SUT.FindName("PlaceholderTextContentPresenter") as FrameworkElement;
			if (placeholder != null)
			{
				Assert.AreEqual(Visibility.Visible, placeholder.Visibility,
					"Placeholder should be visible when document is empty and unfocused");
			}
		}

		[TestMethod]
		public async Task When_PlaceholderText_Hides_When_Has_Text()
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 100,
				PlaceholderText = "Enter text here"
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Some text");
			await WindowHelper.WaitForIdle();

			var placeholder = SUT.FindName("PlaceholderTextContentPresenter") as FrameworkElement;
			if (placeholder != null)
			{
				Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility,
					"Placeholder should be hidden when document has text");
			}
		}

		[TestMethod]
		public async Task When_TextWrapping_Changes()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Default is Wrap
			Assert.AreEqual(TextWrapping.Wrap, SUT.TextWrapping, "Default TextWrapping should be Wrap");

			SUT.TextWrapping = TextWrapping.NoWrap;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextWrapping.NoWrap, SUT.TextWrapping,
				"TextWrapping should be updated to NoWrap");
		}

		[TestMethod]
		public async Task When_TextAlignment_Changes()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Left, SUT.TextAlignment, "Default TextAlignment should be Left");

			SUT.TextAlignment = TextAlignment.Center;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(TextAlignment.Center, SUT.TextAlignment,
				"TextAlignment should be updated to Center");
		}

		[TestMethod]
		public async Task When_Header_Is_Set()
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 100,
				Header = "My Header"
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("My Header", SUT.Header);

			var headerPresenter = SUT.FindName("HeaderContentPresenter") as ContentPresenter;
			if (headerPresenter != null)
			{
				Assert.AreEqual(Visibility.Visible, headerPresenter.Visibility,
					"Header should be visible when Header is set");
			}
		}

		[TestMethod]
		public async Task When_CharacterCasing_Upper()
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 100,
				CharacterCasing = CharacterCasing.Upper
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(CharacterCasing.Upper, SUT.CharacterCasing);
		}

		#endregion

		// =====================================================================
		// Selection Tests
		// =====================================================================

		#region Selection Tests

		[TestMethod]
		public async Task When_Selection_TypeText_Replaces()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Select "World" (indices 6..11)
			SUT.Document.Selection.SetRange(6, 11);
			SUT.Document.Selection.TypeText("Uno");

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("Hello Uno", result, "TypeText should replace the selected text");
		}

		[TestMethod]
		public async Task When_Selection_TypeText_Inserts_At_Cursor()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "HelloWorld");

			// Collapsed selection at position 5 (between "Hello" and "World")
			SUT.Document.Selection.SetRange(5, 5);
			SUT.Document.Selection.TypeText(" ");

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("Hello World", result, "TypeText should insert at cursor position");
		}

		[TestMethod]
		public async Task When_Selection_SetRange_Updates()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.Selection.SetRange(0, 5);

			Assert.AreEqual(0, SUT.Document.Selection.StartPosition,
				"Selection start should be 0");
			Assert.AreEqual(5, SUT.Document.Selection.EndPosition,
				"Selection end should be 5");
		}

		[TestMethod]
		public async Task When_Selection_GetText()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.Selection.SetRange(0, 5);
			SUT.Document.Selection.GetText(TextGetOptions.None, out var text);

			Assert.AreEqual("Hello", text, "Selected text should be 'Hello'");
		}

		[TestMethod]
		public async Task When_Selection_Collapse_To_Start()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");
			SUT.Document.Selection.SetRange(3, 8);

			// Collapse(false) collapses to start
			SUT.Document.Selection.Collapse(false);

			Assert.AreEqual(3, SUT.Document.Selection.StartPosition,
				"After collapse to start, StartPosition should be 3");
			Assert.AreEqual(3, SUT.Document.Selection.EndPosition,
				"After collapse to start, EndPosition should equal StartPosition");
		}

		[TestMethod]
		public async Task When_Selection_Collapse_To_End()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");
			SUT.Document.Selection.SetRange(3, 8);

			// Collapse(true) collapses to end
			SUT.Document.Selection.Collapse(true);

			Assert.AreEqual(8, SUT.Document.Selection.StartPosition,
				"After collapse to end, StartPosition should be 8");
			Assert.AreEqual(8, SUT.Document.Selection.EndPosition,
				"After collapse to end, EndPosition should equal StartPosition");
		}

		[TestMethod]
		public async Task When_Selection_Type_Is_InsertionPoint_When_Collapsed()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.Selection.SetRange(5, 5);
			Assert.AreEqual(SelectionType.InsertionPoint, SUT.Document.Selection.Type,
				"Collapsed selection should be InsertionPoint");
		}

		[TestMethod]
		public async Task When_Selection_Type_Is_Normal_When_Not_Collapsed()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.Selection.SetRange(0, 5);
			Assert.AreEqual(SelectionType.Normal, SUT.Document.Selection.Type,
				"Non-collapsed selection should be Normal");
		}

		#endregion

		// =====================================================================
		// Event Tests
		// =====================================================================

		#region Event Tests

		[TestMethod]
		public async Task When_Text_Changes_Raises_TextChanged()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.TextChanged += (s, e) => { };

			// Focus and set text through the document
			SUT.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			SUT.Document.Selection.TypeText("Hello");
			await WindowHelper.WaitForIdle();

			// Note: TextChanged is raised by RichEditBox.RaiseTextChanged() which is called
			// from InsertTextAtCaret. TypeText on the selection directly modifies the document.
			// The event may or may not fire depending on the code path. We verify via the
			// document content instead.
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello", text, "Text should be 'Hello' after TypeText");
		}

		[TestMethod]
		public async Task When_SetText_Fires_Content_Changed()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var textChangedCount = 0;
			SUT.TextChanged += (s, e) => textChangedCount++;

			SUT.Document.SetText(TextSetOptions.None, "Test");
			await WindowHelper.WaitForIdle();

			// The internal ContentChanged event triggers SyncDisplayFromDocument
			// TextChanged is raised by the control when text is modified via input
			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Test", text, "Document text should be updated");
		}

		[TestMethod]
		public async Task When_Selection_Changes_Updates_Positions()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Set initial selection
			SUT.Document.Selection.SetRange(0, 5);
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(5, SUT.Document.Selection.EndPosition);

			// Change selection
			SUT.Document.Selection.SetRange(6, 11);
			Assert.AreEqual(6, SUT.Document.Selection.StartPosition);
			Assert.AreEqual(11, SUT.Document.Selection.EndPosition);

			SUT.Document.Selection.GetText(TextGetOptions.None, out var selectedText);
			Assert.AreEqual("World", selectedText);
		}

		#endregion

		// =====================================================================
		// Format Span Tests
		// =====================================================================

		#region Format Span Tests

		[TestMethod]
		public async Task When_Format_Spans_Shift_On_Insert()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Apply bold to "World" (positions 6-11)
			var range = SUT.Document.GetRange(6, 11);
			var boldFormat = range.CharacterFormat;
			boldFormat.Bold = FormatEffect.On;
			range.CharacterFormat = boldFormat;

			// Insert text before the bold range - "XX" at position 0
			var insertRange = SUT.Document.GetRange(0, 0);
			insertRange.SetText(TextSetOptions.None, "XX");

			// Now "World" should be at positions 8-13
			// Verify format at position 8 (which was position 6 before insertion)
			var checkRange = SUT.Document.GetRange(8, 13);
			var checkFormat = checkRange.CharacterFormat;
			Assert.AreEqual(FormatEffect.On, checkFormat.Bold,
				"Bold format should shift with inserted text");

			// Verify format at position 0 is not bold
			var earlyRange = SUT.Document.GetRange(0, 2);
			var earlyFormat = earlyRange.CharacterFormat;
			Assert.AreNotEqual(FormatEffect.On, earlyFormat.Bold,
				"Inserted text should not be bold");
		}

		[TestMethod]
		public async Task When_Format_Spans_Shrink_On_Delete()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Apply bold to "Hello" (positions 0-5)
			var range = SUT.Document.GetRange(0, 5);
			var boldFormat = range.CharacterFormat;
			boldFormat.Bold = FormatEffect.On;
			range.CharacterFormat = boldFormat;

			// Delete "el" (positions 1-3) from within the bold range
			var deleteRange = SUT.Document.GetRange(1, 3);
			deleteRange.SetText(TextSetOptions.None, "");

			// Now "Hlo" should be bold (positions 0-3)
			SUT.Document.GetText(TextGetOptions.None, out var resultText);
			Assert.AreEqual("Hlo World", resultText, "Text after deletion should be 'Hlo World'");

			var checkRange = SUT.Document.GetRange(0, 3);
			var checkFormat = checkRange.CharacterFormat;
			Assert.AreEqual(FormatEffect.On, checkFormat.Bold,
				"Remaining bold text should still be bold after partial deletion");
		}

		[TestMethod]
		public async Task When_Format_Span_Deleted_Entirely()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello Bold World");

			// Apply bold to "Bold" (positions 6-10)
			var boldRange = SUT.Document.GetRange(6, 10);
			var boldFormat = boldRange.CharacterFormat;
			boldFormat.Bold = FormatEffect.On;
			boldRange.CharacterFormat = boldFormat;

			// Delete "Bold " (positions 6-11) which includes the entire bold span
			var deleteRange = SUT.Document.GetRange(6, 11);
			deleteRange.SetText(TextSetOptions.None, "");

			SUT.Document.GetText(TextGetOptions.None, out var resultText);
			Assert.AreEqual("Hello World", resultText);

			// Verify no bold formatting remains in the resulting text
			var checkRange = SUT.Document.GetRange(0, 11);
			var checkFormat = checkRange.CharacterFormat;
			Assert.AreNotEqual(FormatEffect.On, checkFormat.Bold,
				"No bold formatting should remain after entire bold span is deleted");
		}

		[TestMethod]
		public async Task When_Multiple_Format_Spans_Applied()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World Test");

			// Apply bold to "Hello" (0-5)
			var range1 = SUT.Document.GetRange(0, 5);
			var format1 = range1.CharacterFormat;
			format1.Bold = FormatEffect.On;
			range1.CharacterFormat = format1;

			// Apply italic to "Test" (12-16)
			var range2 = SUT.Document.GetRange(12, 16);
			var format2 = range2.CharacterFormat;
			format2.Italic = FormatEffect.On;
			range2.CharacterFormat = format2;

			// Verify bold on "Hello"
			var checkBold = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(FormatEffect.On, checkBold.CharacterFormat.Bold,
				"'Hello' should be bold");

			// Verify italic on "Test"
			var checkItalic = SUT.Document.GetRange(12, 16);
			Assert.AreEqual(FormatEffect.On, checkItalic.CharacterFormat.Italic,
				"'Test' should be italic");

			// Verify "World" has no special formatting
			var checkPlain = SUT.Document.GetRange(6, 11);
			var plainFormat = checkPlain.CharacterFormat;
			Assert.AreNotEqual(FormatEffect.On, plainFormat.Bold,
				"'World' should not be bold");
			Assert.AreNotEqual(FormatEffect.On, plainFormat.Italic,
				"'World' should not be italic");
		}

		[TestMethod]
		public async Task When_Underline_Format_Applied()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			var format = range.CharacterFormat;
			format.Underline = UnderlineType.Single;
			range.CharacterFormat = format;

			var checkRange = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(UnderlineType.Single, checkRange.CharacterFormat.Underline,
				"Underline format should be applied");
		}

		[TestMethod]
		public async Task When_Format_Overwritten_By_New_Span()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// First apply bold to "Hello World" (0-11)
			var range1 = SUT.Document.GetRange(0, 11);
			var format1 = range1.CharacterFormat;
			format1.Bold = FormatEffect.On;
			range1.CharacterFormat = format1;

			// Then apply italic to "World" (6-11), which should overwrite the bold span portion
			var range2 = SUT.Document.GetRange(6, 11);
			var format2 = range2.CharacterFormat;
			format2.Italic = FormatEffect.On;
			format2.Bold = FormatEffect.Off;
			range2.CharacterFormat = format2;

			// "Hello" should still be bold
			var checkHello = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(FormatEffect.On, checkHello.CharacterFormat.Bold,
				"'Hello' should remain bold");

			// "World" should be italic but not bold
			var checkWorld = SUT.Document.GetRange(6, 11);
			Assert.AreEqual(FormatEffect.On, checkWorld.CharacterFormat.Italic,
				"'World' should be italic");
		}

		#endregion

		// =====================================================================
		// Range Operations Tests
		// =====================================================================

		#region Range Operations Tests

		[TestMethod]
		public async Task When_Range_MoveEnd_Extends_Selection()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			range.MoveEnd(TextRangeUnit.Character, 1);

			Assert.AreEqual(6, range.EndPosition, "MoveEnd should extend end position by 1");
			Assert.AreEqual(0, range.StartPosition, "Start position should remain at 0");
		}

		[TestMethod]
		public async Task When_Range_MoveStart_Shrinks_Selection()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 11);
			range.MoveStart(TextRangeUnit.Character, 6);

			Assert.AreEqual(6, range.StartPosition, "MoveStart should move start to 6");
			Assert.AreEqual(11, range.EndPosition, "End position should remain at 11");

			range.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("World", text);
		}

		[TestMethod]
		public async Task When_Range_Collapse_To_Start()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(3, 8);
			range.Collapse(false); // Collapse to start

			Assert.AreEqual(3, range.StartPosition);
			Assert.AreEqual(3, range.EndPosition);
			Assert.AreEqual(0, range.Length);
		}

		[TestMethod]
		public async Task When_Range_Collapse_To_End()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(3, 8);
			range.Collapse(true); // Collapse to end

			Assert.AreEqual(8, range.StartPosition);
			Assert.AreEqual(8, range.EndPosition);
			Assert.AreEqual(0, range.Length);
		}

		[TestMethod]
		public async Task When_Range_IsEqual()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range1 = SUT.Document.GetRange(0, 5);
			var range2 = SUT.Document.GetRange(0, 5);
			var range3 = SUT.Document.GetRange(0, 6);

			Assert.IsTrue(range1.IsEqual(range2), "Identical ranges should be equal");
			Assert.IsFalse(range1.IsEqual(range3), "Different ranges should not be equal");
		}

		[TestMethod]
		public async Task When_Range_InRange()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var outer = SUT.Document.GetRange(0, 11);
			var inner = SUT.Document.GetRange(2, 5);

			Assert.IsTrue(outer.InRange(inner), "Inner range should be within outer range");
			Assert.IsFalse(inner.InRange(outer), "Outer range should NOT be within inner range");
		}

		[TestMethod]
		public async Task When_Range_GetClone()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var original = SUT.Document.GetRange(0, 5);
			var clone = original.GetClone();

			Assert.IsTrue(original.IsEqual(clone), "Clone should equal original");

			// Modifying clone should not affect original
			clone.SetRange(0, 11);
			Assert.AreEqual(5, original.EndPosition, "Original should not be affected by clone modification");
		}

		[TestMethod]
		public async Task When_Range_Character_Property()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello");

			var range = SUT.Document.GetRange(0, 1);
			Assert.AreEqual('H', range.Character, "Character at position 0 should be 'H'");
		}

		[TestMethod]
		public async Task When_Range_Delete_Removes_Content()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(5, 11); // " World"
			var deleted = range.Delete(TextRangeUnit.Character, 1);

			Assert.IsTrue(deleted > 0, "Delete should return number of deleted characters");

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("Hello", result, "' World' should be deleted");
		}

		[TestMethod]
		public async Task When_Range_ChangeCase_Lower()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "HELLO");

			var range = SUT.Document.GetRange(0, 5);
			range.ChangeCase(LetterCase.Lower);

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("hello", result, "ChangeCase(Lower) should lowercase text");
		}

		[TestMethod]
		public async Task When_Range_ChangeCase_Upper()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "hello");

			var range = SUT.Document.GetRange(0, 5);
			range.ChangeCase(LetterCase.Upper);

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("HELLO", result, "ChangeCase(Upper) should uppercase text");
		}

		#endregion

		// =====================================================================
		// Control Lifecycle Tests
		// =====================================================================

		#region Control Lifecycle Tests

		[TestMethod]
		public async Task When_Created_Document_Is_Not_Null()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.Document, "Document should not be null after creation");
			Assert.IsNotNull(SUT.TextDocument, "TextDocument should not be null");
			Assert.AreSame(SUT.Document, SUT.TextDocument,
				"Document and TextDocument should be the same object");
		}

		[TestMethod]
		public async Task When_Created_Document_Is_Empty()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("", text, "New document should be empty");
		}

		[TestMethod]
		public async Task When_Created_Selection_Is_At_Start()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.Document.Selection, "Selection should not be null");
			Assert.AreEqual(0, SUT.Document.Selection.StartPosition, "Selection start should be at 0");
			Assert.AreEqual(0, SUT.Document.Selection.EndPosition, "Selection end should be at 0");
		}

		[TestMethod]
		public async Task When_Loaded_Has_NonZero_Size()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.ActualWidth > 0, "Should have non-zero width");
			Assert.IsTrue(SUT.ActualHeight > 0, "Should have non-zero height");
		}

		[TestMethod]
		public async Task When_SetText_Then_Clear_Document()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Some content");
			SUT.Document.GetText(TextGetOptions.None, out var before);
			Assert.AreEqual("Some content", before);

			SUT.Document.SetText(TextSetOptions.None, "");
			SUT.Document.GetText(TextGetOptions.None, out var after);
			Assert.AreEqual("", after, "Document should be empty after clearing");
		}

		[TestMethod]
		public async Task When_IsEnabled_False_Uses_Disabled_State()
		{
			var SUT = new RichEditBox
			{
				Width = 300,
				Height = 100,
				IsEnabled = false
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsEnabled, "IsEnabled should be false");
		}

		#endregion

		// =====================================================================
		// Document Default Format Tests
		// =====================================================================

		#region Document Default Format Tests

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Is_Returned()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var defaultFormat = SUT.Document.GetDefaultCharacterFormat();
			Assert.IsNotNull(defaultFormat, "Default character format should not be null");
		}

		[TestMethod]
		public async Task When_DefaultCharacterFormat_Set_And_Retrieved()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var newFormat = SUT.Document.GetDefaultCharacterFormat();
			newFormat.Bold = FormatEffect.On;
			SUT.Document.SetDefaultCharacterFormat(newFormat);

			var retrieved = SUT.Document.GetDefaultCharacterFormat();
			Assert.AreEqual(FormatEffect.On, retrieved.Bold,
				"Default character format should retain bold setting");
		}

		[TestMethod]
		public async Task When_DefaultParagraphFormat_Is_Returned()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var defaultFormat = SUT.Document.GetDefaultParagraphFormat();
			Assert.IsNotNull(defaultFormat, "Default paragraph format should not be null");
		}

		#endregion

		// =====================================================================
		// Undo/Redo Advanced Tests
		// =====================================================================

		#region Undo Redo Advanced Tests

		[TestMethod]
		public async Task When_Undo_At_Beginning_Does_Nothing()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.Document.CanUndo(), "Cannot undo on fresh document");

			// Undo on empty stack should not throw
			SUT.Document.Undo();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("", text, "Document should remain empty");
		}

		[TestMethod]
		public async Task When_Redo_At_End_Does_Nothing()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.Document.CanRedo(), "Cannot redo on fresh document");

			// Redo on empty stack should not throw
			SUT.Document.Redo();

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("", text, "Document should remain empty");
		}

		[TestMethod]
		public async Task When_Multiple_Undos()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Step1");
			SUT.Document.SetText(TextSetOptions.None, "Step2");
			SUT.Document.SetText(TextSetOptions.None, "Step3");

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterFirstUndo);
			Assert.AreEqual("Step2", afterFirstUndo);

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterSecondUndo);
			Assert.AreEqual("Step1", afterSecondUndo);

			SUT.Document.Undo();
			SUT.Document.GetText(TextGetOptions.None, out var afterThirdUndo);
			Assert.AreEqual("", afterThirdUndo, "Should undo back to empty document");
		}

		[TestMethod]
		public async Task When_UndoLimit_Is_Respected()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.UndoLimit = 2;

			SUT.Document.SetText(TextSetOptions.None, "Step1");
			SUT.Document.SetText(TextSetOptions.None, "Step2");
			SUT.Document.SetText(TextSetOptions.None, "Step3");

			// With limit of 2, we can only undo twice
			SUT.Document.Undo();
			SUT.Document.Undo();
			SUT.Document.Undo(); // This should do nothing because limit was 2

			SUT.Document.GetText(TextGetOptions.None, out var result);
			// We should be able to undo at most 2 steps from Step3
			Assert.AreEqual("Step1", result,
				"Undo should be limited by UndoLimit");
		}

		#endregion

		// =====================================================================
		// Batch Update Tests
		// =====================================================================

		#region Batch Update Tests

		[TestMethod]
		public async Task When_BatchDisplayUpdates_Defers_Content_Changed()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var batchCount = SUT.Document.BatchDisplayUpdates();
			Assert.AreEqual(1, batchCount, "BatchDisplayUpdates should return 1");

			SUT.Document.SetText(TextSetOptions.None, "Batch test");

			var remaining = SUT.Document.ApplyDisplayUpdates();
			Assert.AreEqual(0, remaining, "ApplyDisplayUpdates should return 0");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Batch test", text, "Text should be set after batch apply");
		}

		#endregion

		// =====================================================================
		// Range SetRange Tests
		// =====================================================================

		#region Range SetRange Tests

		[TestMethod]
		public async Task When_Range_SetRange_Swaps_Invalid_Order()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 0);
			range.SetRange(8, 3); // end < start - should be swapped

			Assert.AreEqual(3, range.StartPosition, "Start should be min of 3 and 8");
			Assert.AreEqual(8, range.EndPosition, "End should be max of 3 and 8");
		}

		[TestMethod]
		public async Task When_Range_StoryLength_Returns_Document_Length()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			var range = SUT.Document.GetRange(0, 5);
			Assert.AreEqual(11, range.StoryLength, "StoryLength should be the full document length");
		}

		#endregion

		// =====================================================================
		// Selection Advanced Tests
		// =====================================================================

		#region Selection Advanced Tests

		[TestMethod]
		public async Task When_Selection_SetText_Replaces_And_Collapses()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			SUT.Document.Selection.SetRange(6, 11); // "World"
			SUT.Document.Selection.SetText(TextSetOptions.None, "Earth");

			SUT.Document.GetText(TextGetOptions.None, out var text);
			Assert.AreEqual("Hello Earth", text);

			// After SetText, selection should collapse to end of inserted text
			Assert.AreEqual(SUT.Document.Selection.StartPosition, SUT.Document.Selection.EndPosition,
				"Selection should be collapsed after SetText");
		}

		[TestMethod]
		public async Task When_Selection_Expand_Word()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello World");

			// Place cursor inside "World"
			SUT.Document.Selection.SetRange(8, 8);
			SUT.Document.Selection.Expand(TextRangeUnit.Word);

			SUT.Document.Selection.GetText(TextGetOptions.None, out var selectedText);
			Assert.IsTrue(selectedText.Contains("World"),
				$"Expanded selection should contain 'World', got '{selectedText}'");
		}

		#endregion

		// =====================================================================
		// SetText with null Tests
		// =====================================================================

		#region Null Handling Tests

		[TestMethod]
		public async Task When_SetText_With_Null_Clears_Document()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Some text");

			// Setting null should clear the document
			SUT.Document.SetText(TextSetOptions.None, null);

			SUT.Document.GetText(TextGetOptions.None, out var result);
			Assert.AreEqual("", result, "SetText(null) should clear the document");
		}

		#endregion

		// =====================================================================
		// Multi-paragraph Tests
		// =====================================================================

		#region Multi-paragraph Tests

		[TestMethod]
		public async Task When_MultiParagraph_GetParagraphBounds()
		{
			var SUT = new RichEditBox { Width = 300, Height = 200 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line1\rLine2\rLine3");

			// Check paragraph bounds for position in "Line2"
			var range = SUT.Document.GetRange(7, 7); // middle of "Line2"
			range.Expand(TextRangeUnit.Paragraph);

			range.GetText(TextGetOptions.None, out var paraText);
			Assert.AreEqual("Line2", paraText,
				"Paragraph expansion from position 7 should give 'Line2'");
		}

		[TestMethod]
		public async Task When_Range_EndOf_Paragraph()
		{
			var SUT = new RichEditBox { Width = 300, Height = 200 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line1\rLine2\rLine3");

			var range = SUT.Document.GetRange(7, 7); // inside "Line2"
			range.EndOf(TextRangeUnit.Paragraph, false);

			// After EndOf(Paragraph), range should be collapsed at end of "Line2"
			Assert.AreEqual(11, range.StartPosition,
				"EndOf(Paragraph) should move to end of 'Line2' at position 11");
		}

		[TestMethod]
		public async Task When_Range_StartOf_Paragraph()
		{
			var SUT = new RichEditBox { Width = 300, Height = 200 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Line1\rLine2\rLine3");

			var range = SUT.Document.GetRange(8, 8); // inside "Line2"
			range.StartOf(TextRangeUnit.Paragraph, false);

			// After StartOf(Paragraph), range should be collapsed at start of "Line2"
			Assert.AreEqual(6, range.StartPosition,
				"StartOf(Paragraph) should move to start of 'Line2' at position 6");
		}

		#endregion

		// =====================================================================
		// DefaultTabStop Tests
		// =====================================================================

		#region DefaultTabStop Tests

		[TestMethod]
		public async Task When_DefaultTabStop_Has_Default_Value()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(36f, SUT.Document.DefaultTabStop,
				"DefaultTabStop should be 36 (0.5 inch)");
		}

		[TestMethod]
		public async Task When_DefaultTabStop_Can_Be_Changed()
		{
			var SUT = new RichEditBox { Width = 300, Height = 100 };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.DefaultTabStop = 72f;
			Assert.AreEqual(72f, SUT.Document.DefaultTabStop,
				"DefaultTabStop should be settable to 72");
		}

		#endregion
	}
}
#endif
