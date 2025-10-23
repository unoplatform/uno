using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests
{
	[TestFixture]
	public partial class TextBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TextBox_Content_DoesNotInheritParentTextBlockStyle()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_ImplicitParentTextBlockStyle");

			var sut = _app.Marked("textbox").FirstResult().Rect;

			sut.Height.Should().BeLessThan(100);
		}

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

			// Select the inner content to avoid the browser tapping the header
			// and incorrectly focus the inner input control but not the TextBox.
			var textBoxInner1 = AppInitializer.GetLocalPlatform() == Platform.Browser
				? textBox1.Descendant("ScrollContentPresenter")
				: textBox1;

			var textBox2 = _app.Marked("textBox2");
			var textBoxInner2 = AppInitializer.GetLocalPlatform() == Platform.Browser
				? textBox2.Descendant("ScrollContentPresenter")
				: textBox2;

			textBoxInner1.FastTap();
			textBoxInner1.EnterText("hello 01");

			_app.WaitForText(textBox1, "hello 01");

			textBoxInner2.FastTap();
			textBoxInner2.EnterText("hello 02");

			_app.WaitForText(textBox2, "hello 02");

			var textBox1Result = _app.Query(textBox1).First();
			var textBox2Result = _app.Query(textBox2).First();

			// Focus the firs	t textbox
			textBoxInner1.FastTap();

			var deleteButton1 = FindDeleteButton(textBox1Result);

			_app.TapCoordinates(deleteButton1.Rect.CenterX, deleteButton1.Rect.CenterY);

			_app.WaitForText(textBox1, "");

			// Focus the first textbox
			textBoxInner2.FastTap();

			var deleteButton2 = FindDeleteButton(textBox2Result);

			_app.TapCoordinates(deleteButton2.Rect.CenterX, deleteButton2.Rect.CenterY);

			_app.WaitForText(textBox2, "");
		}

		[Test]
		[AutoRetry]
		public void TextBox_DeleteButton_Depends_On_Width()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.Width_Affects_Delete_Button");

			var initiallyLarge = _app.Marked("initiallyLarge");
			var initiallySmall = _app.Marked("initiallySmall");

			initiallyLarge.FastTap();
			Assert.NotNull(FindDeleteButton(_app.Query(initiallyLarge).Single()));

			initiallySmall.FastTap();
			Assert.Null(FindDeleteButton(_app.Query(initiallySmall).Single()));

			_app.Marked("makeSmallerBtn").FastTap();
			initiallyLarge.FastTap();
			Assert.Null(FindDeleteButton(_app.Query(initiallyLarge).Single()));

			_app.Marked("makeBiggerBtn").FastTap();
			initiallySmall.FastTap();
			Assert.NotNull(FindDeleteButton(_app.Query(initiallySmall).Single()));
		}

		private Uno.UITest.IAppResult FindDeleteButton(Uno.UITest.IAppResult source)
		{
			var deleteButtons = _app.Marked("DeleteButton");
			var appResult = _app.Query(deleteButtons).ToArray();
			var deleteButton = appResult
				.FirstOrDefault(r =>
					r.Rect.CenterX > source.Rect.X
					&& r.Rect.CenterX < source.Rect.Right
					&& r.Rect.CenterY > source.Rect.Y
					&& r.Rect.CenterY < source.Rect.Bottom
				);
			return deleteButton;
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled on iOS as flaky #9080
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

			tglReadonly.FastTap();
			_app.EnterText(txt, " Works again!");

			var newText = "";

			_app.WaitFor(() => (newText = txt.GetDependencyPropertyValue<string>("Text")) != previousText);

			Assert.IsTrue(newText.Contains("Works again!"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Disabled on Android due to pixel color approximation, will be restored in next PR, flaky on iOS #9080
		public void PasswordBox_RevealInScrollViewer()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.PasswordBox_Reveal_Scroll");

			var passwordBox = _app.Marked("MyPasswordBox");
			var passwordBoxRect = _app.GetPhysicalRect(passwordBox);
			_app.Wait(TimeSpan.FromMilliseconds(500)); // Make sure to show the status bar
			using var initial = TakeScreenshot("initial", ignoreInSnapshotCompare: true);

			// Focus the PasswordBox
			passwordBox.FastTap();

			// Press the reveal button, and move up (so the ScrollViewer will kick in and cancel the pointer), then release
			_app.DragCoordinates(passwordBoxRect.X + 10, passwordBoxRect.Right - 10, passwordBoxRect.X - 100, passwordBoxRect.Right - 10);

			// Scrolling may happen differed. If refactoring this test as a runtime tests
			// Use layoutslot information to ensure visibility.
			Thread.Sleep(500);

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
				text3.Should().Be("TextChanged: 2");
			}
		}

		[Test]
		[AutoRetry]
		[Timeout(400000)] // Increased iOS timeout for Xamarin.UITest 3.2
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

			_app.EnterText("fleet");

			DefocusTextBox();

			Assert.AreEqual("fleet", GetMappedText());

			_app.FastTap("ResetButton");

			_app.WaitForText(MappedText, "reset");

			_app.FastTap(TextBox);

			_app.WaitForFocus(TextBox);

			DefocusTextBox();

			Assert.AreEqual("reset", GetMappedText());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Flaky on iOS native https://github.com/unoplatform/uno/issues/9080
		public void TextBox_BeforeTextChanging_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_BeforeTextChanging");

			var beforeTextBox = _app.Marked("BeforeTextBox");

			// Enter text and verify that only e is permittable in text box
			_app.WaitForText(beforeTextBox, "");
			beforeTextBox.EnterText("Enter text and verify that only e is permittable");
			_app.WaitForText(beforeTextBox, "eeeeee");
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
			textbox.FastTap();

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

			// Select the inner content to avoid the browser tapping the header
			// and incorrectly focus the inner input control but not the TextBox.
			var textBoxInner1 = AppInitializer.GetLocalPlatform() == Platform.Browser
				? textBox1.Descendant("ScrollContentPresenter")
				: textBox1;

			var textBox2 = _app.Marked("TextBox2");

			var textBoxInner2 = AppInitializer.GetLocalPlatform() == Platform.Browser
				? textBox2.Descendant("ScrollContentPresenter")
				: textBox2;

			var textChangedTextBlock = _app.Marked("TextChangedTextBlock");
			var lostFocusTextBlock = _app.Marked("LostFocusTextBlock");

			// Initial verification of text
			Assert.AreEqual("", textChangedTextBlock.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("", lostFocusTextBlock.GetDependencyPropertyValue("Text")?.ToString());

			// Change text and verify text of text blocks
			textBoxInner1.FastTap();
			textBoxInner1.ClearText();
			textBoxInner1.EnterText("Testing text property");
			_app.WaitForText(textChangedTextBlock, "Testing text property");
			_app.WaitForText(lostFocusTextBlock, "");

			// change focus and assert
			textBoxInner2.FastTap();
			_app.WaitForText(textChangedTextBlock, "Testing text property");
			_app.WaitForText(lostFocusTextBlock, "Testing text property");
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
		[Ignore("Android 31 or later does not bring the keyboard up https://github.com/unoplatform/uno/issues/9080")]
		public void TextBox_Readonly_ShouldNotBringUpKeyboard()
		{
			Run("Uno.UI.Samples.UITests.TextBoxControl.TextBox_IsReadOnly");

			var target = _app.Marked("txt");
			var readonlyToggle = _app.Marked("tglReadonly"); // default: checked

			// initial state
			_app.WaitForElement(target);
			using var screenshot1 = TakeScreenshot("textbox readonly", ignoreInSnapshotCompare: true);

			// remove readonly and focus textbox
			readonlyToggle.FastTap(); // now: unchecked
			target.FastTap();
			_app.Wait(seconds: 1); // allow keyboard to fully open
			using var screenshot2 = TakeScreenshot("textbox focused with keyboard", ignoreInSnapshotCompare: true);

			// reapply readonly and try focus textbox
			readonlyToggle.FastTap(); // now: checked
			target.FastTap();
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
		public void TextBox_IsReadOnly_AcceptsReturn_Test()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_IsReadOnly_AcceptsReturn");
			// for context, IsReadOnly=True used to break AcceptsReturn=True on android

			var target = _app.Marked("TargetTextBox");
			var readonlyCheckBox = _app.Marked("IsReadOnlyCheckBox"); // default: checked
			var multilineCheckBox = _app.Marked("AcceptsReturnCheckBox"); // default: checked

			// initial state
			_app.WaitForElement(target);
			target.SetDependencyPropertyValue("TextWrapping", "Wrap");
			var multilineReadonlyTextRect = target.FirstResult().Rect.ToRectangle();

			// remove readonly
			readonlyCheckBox.FastTap(); // now: unchecked
			var multilineTextRect = target.FirstResult().Rect.ToRectangle();

			// remove multiline
			multilineCheckBox.FastTap(); // now: unchecked
			var normalTextRect = target.FirstResult().Rect.ToRectangle();

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

			normalCasingTextBox.FastTap();
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

			defaultCasingTextBox.FastTap();
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

			lowerCasingTextBox.FastTap();
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

			upperCasingTextBox.FastTap();
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
			_app.Marked("Test").SetDependencyPropertyValue("TextWrapping", "Wrap");
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

		[Test]
		[AutoRetry]
		public void InputScope_Should_Not_Validate_Input()
		{
			// InputScope doesn't prevent any input, it just changes the keyboard shown for touch devices (or when in Tablet Mode in Windows).

			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_InputScope_CurrencyAmount");

			_app.EnterText(marked: "CurrencyTextBox", text: "123,,321abc123");

			_app.WaitForDependencyPropertyValue("CurrencyTextBox", "Text", "123,,321abc123");
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

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void TextBox_WithPadding_Focus()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_WithPadding_Focus");

			const string Text = "asdqwe";

			foreach (var marked in new[] { "textBox1", "textBox2" })
			{
				// tap near the edge (area pushed/occupied by TextBox.Padding)
				var rect = _app.GetPhysicalRect(marked);
				_app.TapCoordinates(rect.Right - 5, rect.Bottom - 5);
				_app.Wait(seconds: 1);

				_app.EnterText(Text);
				_app.WaitForText(marked, Text, timeout: TimeSpan.FromSeconds(20));
			}
		}

		[Test]
		[AutoRetry]
		public void TextBox_With_Description()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_Description", skipInitialScreenshot: true);
			var textBoxRect = ToPhysicalRect(_app.WaitForElement("DescriptionTextBox")[0].Rect);
			using var screenshot = TakeScreenshot("TextBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(screenshot, textBoxRect.X + textBoxRect.Width / 2, textBoxRect.Y + textBoxRect.Height - 50, Color.Red);
		}

		[Test]
		[AutoRetry]
		public void PasswordBox_With_Description()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.PasswordBox_Description", skipInitialScreenshot: true);
			var passwordBoxRect = ToPhysicalRect(_app.WaitForElement("DescriptionPasswordBox")[0].Rect);
			using var screenshot = TakeScreenshot("PasswordBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(screenshot, passwordBoxRect.X + passwordBoxRect.Width / 2, passwordBoxRect.Y + passwordBoxRect.Height - 50, Color.Red);
		}

		[Test]
		[AutoRetry]
		public void TextBox_Visibility()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_Visibility", skipInitialScreenshot: true);
			var textBoxRect = ToPhysicalRect(_app.WaitForElement("MyTextBox")[0].Rect);
			var buttonRect = ToPhysicalRect(_app.WaitForElement("ShowHideButton")[0].Rect);

			_app.FastTap("ShowHideButton");

			if (AppInitializer.GetLocalPlatform() == Platform.Browser)
			{
				var textBoxRect2 = ToPhysicalRect(_app.WaitForElement("MyTextBox")[0].Rect);

				// Assert that after clicking, MyTextBox goes out of screen (hidden).
				Assert.AreEqual(-100000 + textBoxRect.X, textBoxRect2.X);
				Assert.AreEqual(-100000 + textBoxRect.Y, textBoxRect2.Y);
			}
			else
			{
				_app.WaitForNoElement("MyTextBox");
			}

			var buttonRect2 = ToPhysicalRect(_app.WaitForElement("ShowHideButton")[0].Rect);

			// Assert that after clicking, ShowHideButton moves up (because MyTextBox is collapsed)
			Assert.Less(buttonRect2.Y, buttonRect.Y);
			Assert.AreEqual(textBoxRect.Height, buttonRect.Y - buttonRect2.Y);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Too flaky on Wasm for some reason
		public void TextBox_VerticalAlignment()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TextBox.TextBox_VerticalAlignment", skipInitialScreenshot: true);
			var textBoxRect = ToPhysicalRect(_app.WaitForElement("MyTextBox")[0].Rect).ToRectangle();

			using var screenshot = TakeScreenshot("TextBox_VerticalAlignment");

			var rectNotContainingRed = new Rectangle(textBoxRect.X, textBoxRect.Y + 30, textBoxRect.Width, textBoxRect.Height - 30);
			ImageAssert.DoesNotHaveColorInRectangle(screenshot, rectNotContainingRed, Color.Red, tolerance: 20);

			var rectContainingRed = new Rectangle(textBoxRect.X, textBoxRect.Y, textBoxRect.Width, 30);
			ImageAssert.HasColorInRectangle(screenshot, rectContainingRed, Color.Red, tolerance: 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TextBox_Selection_IsReadOnly()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_Selection");

			var readonlyTextBoxRect = _app.WaitForElement("MyReadOnlyTextBox").Single().Rect;
			var readonlyTextBox = _app.Marked("MyReadOnlyTextBox");

			var centerPointReadOnlyX = (int)readonlyTextBoxRect.CenterX;
			var centerPointReadOnlyY = (int)readonlyTextBoxRect.CenterY;

			// Initial verification			
			Assert.IsTrue(readonlyTextBox.GetDependencyPropertyValue<bool>("IsReadOnly"));
			Assert.IsNull(readonlyTextBox.GetDependencyPropertyValue("SelectedText")?.ToString());

			// Attempt selection
			_app.DoubleTapCoordinates(centerPointReadOnlyX, centerPointReadOnlyY);

			// Final verification of SelectedText
			Assert.IsNotEmpty(readonlyTextBox.GetDependencyPropertyValue("SelectedText")?.ToString());
		}
	}
}
