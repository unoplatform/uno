using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests
{
	[TestFixture]
	public partial class TextBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TextBox_NaturalSize_When_Empty_Is_Right_Width()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxes.TextBox_NaturalSize");

			var sut = _app.Marked("textbox_sut").FirstResult().Rect;
			var recth = _app.Marked("recth").FirstResult().Rect;

			sut.Width.Should().Be(recth.Width, "Invalid Width");
		}

		[Test]
		[AutoRetry]
		public void TextBox_NaturalSize_When_Empty_Is_Right_XPos()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxes.TextBox_NaturalSize");

			var sut = _app.Marked("textbox_sut").FirstResult().Rect;
			var recth = _app.Marked("recth").FirstResult().Rect;

			sut.X.Should().Be(recth.X, "Invalid X position");
		}

		[Test]
		[AutoRetry]
		public void TextBox_UpdatedBinding_On_OneWay_Mode()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_Bindings");

			var textboxTwoWay = _app.Marked("textboxTwoWay");
			var textboxOneWay = _app.Marked("textboxOneWay");
			var textboxDefault = _app.Marked("textboxDefault");

			var textblock = _app.Marked("textblock");

			// Initial situation
			textblock.GetDependencyPropertyValue("Text").Should().Be("", "Initial should be empty");

			// Enter text in two-way text
			textboxTwoWay.EnterText("TwoWay Content");
			textboxOneWay.FastTap();

			_app.WaitForText(textblock, "TwoWay Content");

			// Enter text in one-way text
			textboxOneWay.EnterText("OneWay Content");
			textboxDefault.FastTap();

			// Ensure bound valud didn't change
			Thread.Sleep(120);
			_app.WaitForText(textblock, "TwoWay Content");

			// Enter text in one-way text
			textboxDefault.EnterText("Default Content");
			textboxTwoWay.FastTap();

			// Ensure bound valud didn't change
			Thread.Sleep(120);
			_app.WaitForText(textblock, "TwoWay Content");
		}

		[Test]
		[AutoRetry]
		public void TextBox_Foreground()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_Foreground");

			var tb1 = _app.Marked("tb1");
			var tb2 = _app.Marked("tb2");

			tb1.FastTap();
			TakeScreenshot("tb1 focused", ignoreInSnapshotCompare: true);

			tb2.FastTap();
			TakeScreenshot("tb2 focused", ignoreInSnapshotCompare: true);
		}

		[Test]
		[AutoRetry]
		public void TextBox_RoundedCorners()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_RoundedCorners");

			var textBox1 = _app.Marked("textBox1");
			var textBox2 = _app.Marked("textBox2");

			var textBox1Result_Before = _app.Query(textBox1).First();

			textBox1.FastTap();
			textBox1.EnterText("hello 01");

			_app.WaitForText(textBox1, "hello 01");

			textBox2.FastTap();
			textBox2.EnterText("hello 02");

			_app.WaitForText(textBox2, "hello 02");

			var textBox1Result_After = _app.Query(textBox1).First();

			using (new AssertionScope())
			{
				textBox1Result_After.Rect.X.Should().Be(textBox1Result_Before.Rect.X);
				// We doesn't check the Y here because of the status bar causing the test to be unreliable
				textBox1Result_After.Rect.Width.Should().Be(textBox1Result_Before.Rect.Width);
				textBox1Result_After.Rect.Height.Should().Be(textBox1Result_Before.Rect.Height);
			}
		}

		[Test]
		[AutoRetry]
		public void TextBox_DeleteButton()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_DeleteButton_Automated");

			var textBox1 = _app.Marked("textBox1");
			var textBox2 = _app.Marked("textBox2");

			textBox1.FastTap();
			textBox1.EnterText("hello 01");

			_app.WaitForText(textBox1, "hello 01");

			textBox2.FastTap();
			textBox2.EnterText("hello 02");

			_app.WaitForText(textBox2, "hello 02");

			var textBox1Result = _app.Query(textBox1).First();
			var textBox2Result = _app.Query(textBox2).First();

			// Focus the first textbox
			textBox1.FastTap();

			var deleteButton1 = FindDeleteButton(textBox1Result);

			_app.TapCoordinates(deleteButton1.Rect.CenterX, deleteButton1.Rect.CenterY);

			_app.WaitForText(textBox1, "");

			// Focus the first textbox
			textBox2.FastTap();

			var deleteButton2 = FindDeleteButton(textBox2Result);

			_app.TapCoordinates(deleteButton2.Rect.CenterX, deleteButton2.Rect.CenterY);

			_app.WaitForText(textBox2, "");
		}

		private Uno.UITest.IAppResult FindDeleteButton(Uno.UITest.IAppResult source)
		{
			var deleteButtons = _app.Marked("DeleteButton");
			var appResult = _app.Query(deleteButtons).ToArray();
			var deleteButton = appResult
				.First(r =>
					r.Rect.CenterX > source.Rect.X
					&& r.Rect.CenterX < source.Rect.Right
					&& r.Rect.CenterY > source.Rect.Y
					&& r.Rect.CenterY < source.Rect.Bottom
				);
			return deleteButton;
		}

		[Test]
		[AutoRetry]
		public async Task TextBox_Readonly()
		{
			Run("Uno.UI.Samples.UITests.TextBoxControl.TextBox_IsReadOnly");

			var tglReadonly = _app.Marked("tglReadonly");
			var txt = _app.Marked("txt");

			const string initialText = "This text should initially be READONLY and ENABLED...";
			_app.WaitForText(txt, initialText);

			// Don't use EnterText(txt, "") as it waits for the keyboard that will not arrive (on iOS)
			_app.Tap(txt);
			try
			{
				_app.EnterText("ERROR1");
			}
			catch (Exception e)
			{
				// Ignore the exception for now.
				Console.WriteLine(e);
			}

			_app.WaitForText(txt, initialText);

			tglReadonly.FastTap();
			_app.EnterText(txt, "Hello!");
			await Task.Delay(100);
			_app.WaitForText(txt, initialText + "Hello!");

			tglReadonly.FastTap();
			_app.FastTap(txt);

			try
			{
				_app.EnterText("ERROR2");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			await Task.Delay(100);
			_app.WaitForText(txt, initialText + "Hello!");

			var previousText = txt.GetDependencyPropertyValue<string>("Text");

			tglReadonly.Tap();
			_app.EnterText(txt, " Works again!");

			var newText = "";

			_app.WaitFor(() => (newText = txt.GetDependencyPropertyValue<string>("Text")) != previousText);

			Assert.IsTrue(newText.Contains("Works again!"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser, Platform.iOS)] // Disabled on Android due to pixel color approximation, will be restored in next PR
		public void PasswordBox_RevealInScrollViewer()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.PasswordBox_Reveal_Scroll");

			var passwordBox = _app.Marked("MyPasswordBox");
			var passwordBoxRect = _app.GetRect(passwordBox);
			_app.Wait(TimeSpan.FromMilliseconds(500)); // Make sure to show the status bar
			using var initial = TakeScreenshot("initial", ignoreInSnapshotCompare: true);

			// Focus the PasswordBox
			passwordBox.Tap();

			// Press the reveal button, and move up (so the ScrollViewer will kick in and cancel the pointer), then release
			_app.DragCoordinates(passwordBoxRect.X + 10, passwordBoxRect.Right - 10, passwordBoxRect.X - 100, passwordBoxRect.Right - 10);

			using var result = TakeScreenshot("result", ignoreInSnapshotCompare: true);

			ImageAssert.AreEqual(initial, result, new Rectangle(
				(int)passwordBoxRect.X + 8, // +8 : ignore borders (as we are still focused)
				(int)passwordBoxRect.Y + 8,
				100, // Ignore the reveal button on right (as we are still focused)
				(int)passwordBoxRect.Height - 16));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_Formatting_FlickerText()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_Formatting_Flicker");

			var textbox = _app.Marked("SomeTextBox");
			var scoreboard = _app.Marked("Scoreboard");
			_app.WaitForElement(textbox);

			_app.EnterText(textbox, "a");
			_app.DismissKeyboard();

			var text1 = textbox.GetDependencyPropertyValue<string>("Text");

			text1.Should().StartWith("modified ", because: "custom IInputFilter should've hijacked the input");

			_app.EnterText(textbox, "a");
			_app.DismissKeyboard();

			var text2 = textbox.GetDependencyPropertyValue<string>("Text");
			var text3 = scoreboard.GetDependencyPropertyValue<string>("Text");

			using (new AssertionScope())
			{
				text2.Should().Be(text1, because: "Text content should not change at max length.");
				text3.Should().Be("TextChanged: 1");
			}
		}

		[Test]
		[AutoRetry]
		public void TextBox_No_Text_Entered()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxControl.TextBox_Binding_Null");

			const string MappedText = "MappedText";
			const string TextBox = "TargetTextBox";
			const string DummyButton = "DummyButton";

			string GetMappedText() => _app.GetText(MappedText);

			void DefocusTextBox()
			{
				_app.FastTap(DummyButton);

				_app.WaitForFocus(DummyButton);
			}

			Assert.AreEqual("initial", GetMappedText());

			_app.FastTap(TextBox);

			_app.WaitForFocus(TextBox);

			DefocusTextBox();

			Assert.AreEqual("initial", GetMappedText()); //Binding not pushed on losing focus

			_app.FastTap(TextBox);

			_app.EnterText("fleep");

			DefocusTextBox();

			Assert.AreEqual("fleep", GetMappedText());

			_app.FastTap("ResetButton");

			_app.WaitForText(MappedText, "reset");

			_app.FastTap(TextBox);

			_app.WaitForFocus(TextBox);

			DefocusTextBox();

			Assert.AreEqual("reset", GetMappedText());
		}

		[Test]
		[AutoRetry]
		public void TextBox_BeforeTextChanging_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_BeforeTextChanging");

			var beforeTextBox = _app.Marked("BeforeTextBox");

			// Enter text and verify that only e is permittable in text box
			Assert.AreEqual("", beforeTextBox.GetDependencyPropertyValue("Text")?.ToString());
			beforeTextBox.EnterText("Enter text and verify that only e is permittable");
			Assert.AreEqual("eeeeee", beforeTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextAlignment_Left_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextAlignment");

			var leftAlignedTextBox = _app.Marked("LeftAlignedTextBox");

			// Assert initial text alignment, change text and assert final text alignment
			ChangeTextAndAssertBeforeAfter(leftAlignedTextBox, "Left", "LeftAlignedText", "Left");
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextAlignment_Center_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextAlignment");

			var centerAlignedTextBox = _app.Marked("CenterAlignedTextBox");

			// Assert initial text alignment, change text and assert final text alignment
			ChangeTextAndAssertBeforeAfter(centerAlignedTextBox, "Center", "CenterAlignedText", "Center");
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextAlignment_Right_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextAlignment");

			var rightAlignedTextBox = _app.Marked("RightAlignedTextBox");

			// Assert initial text alignment, change text and assert final text alignment
			ChangeTextAndAssertBeforeAfter(rightAlignedTextBox, "Right", "RightAlignedText", "Right");
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextAlignment_Justify_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextAlignment");

			var justifyAlignedTextBox = _app.Marked("JustifyAlignedTextBox");

			// Assert initial text alignment, change text and assert final text alignment
			ChangeTextAndAssertBeforeAfter(justifyAlignedTextBox, "Justify", "JustifyAlignedText", "Justify");
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextAlignment_DetectFromContent_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextAlignment");

			var detectFromContentAlignedTextBox = _app.Marked("DetectFromContentAlignedTextBox");

			// Assert initial text alignment, change text and assert final text alignment
			ChangeTextAndAssertBeforeAfter(detectFromContentAlignedTextBox, "DetectFromContent", "DetectFromContentAlignedText", "DetectFromContent");
		}

		private void ChangeTextAndAssertBeforeAfter(QueryEx textbox, string initialTextAlignment, string finalText, string finalTextAlignment)
		{
			// Focus textbox
			textbox.Tap();

			// Assert initial state
			Assert.AreEqual(initialTextAlignment, textbox.GetDependencyPropertyValue("TextAlignment")?.ToString());

			// Update text content
			_app.ClearText();
			_app.EnterText(finalText);

			// Assert final state
			Assert.AreEqual(finalTextAlignment, textbox.GetDependencyPropertyValue("TextAlignment")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextProperty_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_TextProperty");

			var textBox1 = _app.Marked("TextBox1");
			var textBox2 = _app.Marked("TextBox2");
			var textChangedTextBlock = _app.Marked("TextChangedTextBlock");
			var lostFocusTextBlock = _app.Marked("LostFocusTextBlock");

			// Initial verification of text
			Assert.AreEqual("", textChangedTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("", lostFocusTextBlock.GetDependencyPropertyValue("Text")?.ToString());

			// Change text and verify text of text blocks
			textBox1.Tap();
			textBox1.ClearText();
			textBox1.EnterText("Testing text property");
			Assert.AreEqual("Testing text property", textChangedTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("", lostFocusTextBlock.GetDependencyPropertyValue("Text")?.ToString());

			// change focus and assert
			textBox2.Tap();
			Assert.AreEqual("Testing text property", textChangedTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("Testing text property", lostFocusTextBlock.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void Focus_Programmatic()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_Focus_Programmatic");

			_app.WaitForElement("SetFocus");

			_app.FastTap("SetFocus");

			_app.WaitForText("StatusText", "Got focus");

			_app.EnterText("orangutan");

			_app.WaitForText("TargetTextBox", "orangutan");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_Readonly_ShouldNotBringUpKeyboard()
		{
			Run("Uno.UI.Samples.UITests.TextBoxControl.TextBox_IsReadOnly");

			var target = _app.Marked("txt");
			var readonlyToggle = _app.Marked("tglReadonly"); // default: checked

			// initial state
			_app.WaitForElement(target);
			using var screenshot1 = TakeScreenshot("textbox readonly", ignoreInSnapshotCompare: true);

			// remove readonly and focus textbox
			readonlyToggle.Tap(); // now: unchecked
			target.Tap();
			_app.Wait(seconds: 1); // allow keyboard to fully open
			using var screenshot2 = TakeScreenshot("textbox focused with keyboard", ignoreInSnapshotCompare: true);

			// reapply readonly and try focus textbox
			readonlyToggle.Tap(); // now: checked
			target.Tap();
			_app.Wait(seconds: 1); // allow keyboard to fully open (if ever possible)
			_app.WaitForElement(target);
			using var screenshot3 = TakeScreenshot("textbox readonly again", ignoreInSnapshotCompare: true);

			// the bottom half should only contains the keyboard and some blank space,
			// but not the textbox nor the toggle buttons that needs to be excluded.
			var screen = _app.GetScreenDimensions();
			var bottomHalfRect = new Rectangle(
				0, (int)screen.Height / 2,
				(int)screen.Width, (int)screen.Height / 2
			);

			ImageAssert.AreNotEqual(screenshot1, screenshot2, bottomHalfRect); // no keyboard != keyboard
			ImageAssert.AreEqual(screenshot1, screenshot3, bottomHalfRect); // no keyboard == no keyboard
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_IsReadOnly_AcceptsReturn_Test()
		{
			/* test disabled for ios and wasm, due to #
			 *-ios: when setting AcceptsReturn to false, the TextBox doesn't resize appropriately
			 * -wasm: AcceptsReturn is not implemented or does nothing */

			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_IsReadOnly_AcceptsReturn");
			// for context, IsReadOnly=True used to break AcceptsReturn=True on android

			var target = _app.Marked("TargetTextBox");
			var readonlyCheckBox = _app.Marked("IsReadOnlyCheckBox"); // default: checked
			var multilineCheckBox = _app.Marked("AcceptsReturnCheckBox"); // default: checked

			// initial state
			_app.WaitForElement(target);
			var multilineReadonlyTextRect = target.FirstResult().Rect;

			// remove readonly
			readonlyCheckBox.Tap(); // now: unchecked
			var multilineTextRect = target.FirstResult().Rect;

			// remove multiline
			multilineCheckBox.Tap(); // now: unchecked
			var normalTextRect = target.FirstResult().Rect;

			multilineTextRect.Height.Should().Be(multilineReadonlyTextRect.Height, because: "toggling IsReadOnly should not affect AcceptsReturn=True(multiline) TextBox.Height");
			normalTextRect.Height.Should().NotBe(multilineTextRect.Height, because: "toggling AcceptsReturn should not affect TextBox.Height");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_CharacterCasingNormal_ShouldAcceptAllCasing_Test()
		{
			const string text = "Uno Platform";

			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_CharacterCasing");

			var normalCasingTextBox = _app.Marked("NormalCasingTextBox");

			normalCasingTextBox.Tap();
			normalCasingTextBox.ClearText();
			normalCasingTextBox.EnterText(text);

			Assert.AreEqual(text, normalCasingTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_CharacterCasingDefault_ShouldAcceptAllCasing_Test()
		{
			const string text = "Uno Platform";

			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_CharacterCasing");

			var defaultCasingTextBox = _app.Marked("DefaultCasingTextBox");

			defaultCasingTextBox.Tap();
			defaultCasingTextBox.ClearText();
			defaultCasingTextBox.EnterText(text);

			Assert.AreEqual(text, defaultCasingTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_CharacterCasingLower_ShouldBeAllLower_Test()
		{
			const string text = "Uno Platform";

			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_CharacterCasing");

			var lowerCasingTextBox = _app.Marked("LowerCasingTextBox");

			lowerCasingTextBox.Tap();
			lowerCasingTextBox.ClearText();
			lowerCasingTextBox.EnterText(text);

			Assert.AreEqual(text.ToLowerInvariant(), lowerCasingTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void TextBox_CharacterCasingUpper_ShouldBeAllUpper_Test()
		{
			const string text = "Uno Platform";

			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_CharacterCasing");

			var upperCasingTextBox = _app.Marked("UpperCasingTextBox");

			upperCasingTextBox.Tap();
			upperCasingTextBox.ClearText();
			upperCasingTextBox.EnterText(text);

			Assert.AreEqual(text.ToUpperInvariant(), upperCasingTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void TextBox_AutoGrow_Vertically_Test()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Multiline_AutoHeight");

			_app.FastTap("btnSingle");
			_app.WaitForElement("Test");
			var height1 = _app.GetLogicalRect("Test").Height;

			_app.FastTap("btnDouble");
			_app.WaitForElement("Test");
			var height2 = _app.GetLogicalRect("Test").Height;

			using var _ = new AssertionScope();
			height2.Should().BeGreaterThan(height1);


			_app.FastTap("btnSingle");
			_app.WaitForElement("Test");
			var height3 = _app.GetLogicalRect("Test").Height;

			height3.Should().Be(height1);
		}

		[Test]
		[AutoRetry]
		public void TextBox_AutoGrow_Vertically_Wrapping_Test()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Multiline_AutoHeight");

			_app.Marked("Test").SetDependencyPropertyValue("MaxWidth", "200");
			_app.Marked("Test").SetDependencyPropertyValue("TextWrapping", "Wrap");
			_app.Marked("Test").SetDependencyPropertyValue("Text", "Short");
			_app.WaitForElement("Test");
			var height1 = _app.GetLogicalRect("Test").Height;

			_app.EnterText("Test", "This is a significantly longer text. It should wraps.");
			_app.WaitForElement("Test");
			var height2 = _app.GetLogicalRect("Test").Height;

			using var _ = new AssertionScope();
			height2.Should().BeGreaterThan(height1);


			_app.Marked("Test").SetDependencyPropertyValue("Text", "Short");
			var height3 = _app.GetLogicalRect("Test").Height;

			height3.Should().Be(height1);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void TextBox_AutoGrow_Vertically_NoWrapping_Test()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Multiline_AutoHeight");

			_app.Marked("Test").SetDependencyPropertyValue("MaxWidth", "200");
			_app.Marked("Test").SetDependencyPropertyValue("Text", "Short");
			_app.WaitForElement("Test");
			var height1 = _app.GetLogicalRect("Test").Height;

			_app.EnterText("Test", "This is a significantly longer text. Since there's no wrapping, it should remains on a single line.");
			_app.WaitForElement("Test");
			var height2 = _app.GetLogicalRect("Test").Height;

			height2.Should().Be(height1);
		}

		[Test]
		[AutoRetry]
		public void TextBox_AutoGrow_Horizontally_Test()
		{
			using var _ = new AssertionScope();

			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Multiline_AutoHeight");

			_app.Marked("Test").SetDependencyPropertyValue("MinWidth", "120");
			_app.Marked("Test").SetDependencyPropertyValue("HorizontalAlignment", "Left");
			_app.Marked("Test").SetDependencyPropertyValue("Text", "Short");

			_app.WaitForElement("Test");
			var width1 = _app.GetLogicalRect("Test").Width;
			var height1 = _app.GetLogicalRect("Test").Height;

			width1.Should().Be(120f);


			_app.EnterText("Test", "This is a significantly larger text. Since there's no wrapping, it should remains on a single line.");
			_app.WaitForElement("Test");
			var width2 = _app.GetLogicalRect("Test").Width;
			var height2 = _app.GetLogicalRect("Test").Height;

			width2.Should().BeGreaterThan(width1);
			height2.Should().Be(height1);

			_app.Marked("Test").SetDependencyPropertyValue("Text", "Short");
			_app.WaitForElement("Test");
			var width3 = _app.GetLogicalRect("Test").Width;
			var height3 = _app.GetLogicalRect("Test").Height;

			width1.Should().Be(width1);
			height3.Should().Be(height1);
		}

		[Test]
		[AutoRetry]
		public void PasswordBox_AutoGrow_Horizontally_Test()
		{
			using var _ = new AssertionScope();

			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.PasswordBox_Simple");

			_app.Marked("autoGrow").SetDependencyPropertyValue("MinWidth", "120");
			_app.Marked("autoGrow").SetDependencyPropertyValue("HorizontalAlignment", "Left");
			_app.Marked("autoGrow").SetDependencyPropertyValue("Password", "");

			_app.WaitForElement("autoGrow");
			var width1 = _app.GetLogicalRect("autoGrow").Width;
			var height1 = _app.GetLogicalRect("autoGrow").Height;

			width1.Should().Be(120f);


			_app.EnterText("autoGrow", "This is a long password.");
			_app.WaitForElement("autoGrow");
			var width2 = _app.GetLogicalRect("autoGrow").Width;
			var height2 = _app.GetLogicalRect("autoGrow").Height;

			width2.Should().BeGreaterThan(width1);
			height2.Should().Be(height1);

			_app.Marked("autoGrow").SetDependencyPropertyValue("Password", "short");
			_app.WaitForElement("autoGrow");
			var width3 = _app.GetLogicalRect("autoGrow").Width;
			var height3 = _app.GetLogicalRect("autoGrow").Height;

			width3.Should().Be(width1);
			height3.Should().Be(height1);
		}

		[Test]
		[AutoRetry]
		public void TextBox_Foreground_Color_Changing()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_Foreground_Changing");

			_app.WaitForElement("DummyButton");
			_app.FastTap("DummyButton"); // Ensure pointer is out of the way and mouse-over states aren't accidentally active

			const string SingleLine = "SingleLineTextBox";
			const string Multiline = "MultilineTextBox";

			using var initial = TakeScreenshot("Initial");

			PointF GetPixelPosition(string textBoxName)
			{
				var rect = _app.GetPhysicalRect(textBoxName);
				var firstPosition = GetPixelPositionWithColor(initial, rect, Color.Tomato);
				return new PointF(firstPosition.X + 5, firstPosition.Y + 5);
			}
			var singleLinePosition = GetPixelPosition(SingleLine);
			var multilinePosition = GetPixelPosition(Multiline);

			_app.FastTap("ChangeForegroundButton");
			_app.WaitForText("StatusTextBlock", "Changed");

			using var after = TakeScreenshot("Foreground Color changed");

			ImageAssert.HasColorAt(after, singleLinePosition.X, singleLinePosition.Y, Color.Blue);
			ImageAssert.HasColorAt(after, multilinePosition.X, multilinePosition.Y, Color.Blue);
		}

		private static PointF GetPixelPositionWithColor(ScreenshotInfo screenshotInfo, IAppRect boundingRect, Color expectedColor)
		{
			var bitmap = screenshotInfo.GetBitmap();
			for (var x = boundingRect.X; x < boundingRect.Right; x++)
			{
				for (var y = boundingRect.Y; y < boundingRect.Bottom; y++)
				{
					var pixel = bitmap.GetPixel((int)x, (int)y);
					if (pixel.ToArgb() == expectedColor.ToArgb())
					{
						return new PointF(x, y);
					}
				}
			}

			throw new InvalidOperationException($"Color {expectedColor} was not found.");
		}
	}
}
