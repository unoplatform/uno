using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Disabled on iOS (xamarin.uitest 3.2 or iOS 15) https://github.com/unoplatform/uno/issues/8012
		public void Keyboard_Textbox_InsideScrollViewer_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_InsideScrollerViewer_Automated");

			_app.WaitForElement(_app.Marked("MultilineTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var filledTextBox = _app.Marked("FilledTextBox");
			var placeholderTextTextBox = _app.Marked("PlaceholderTextTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");
			var multilineTextBox = _app.Marked("MultilineTextBox");
			var numberTextBox = _app.Marked("NumberTextBox");
			var unfocusButton = _app.Marked("UnfocusButton");

			// Assert initial state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("Text", filledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, placeholderTextTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, multilineTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, numberTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.FastTap(normalTextBox);
				_app.Wait(1);
				TakeScreenshot("0 - Focus on normalTextBox ", ignoreInSnapshotCompare: true);

				// Removing focus on normalTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("0 - Remove Focus on normalTextBox", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Keyboard predicted text can change*/);
			}

			{
				// Setting focus on normalTextBox
				_app.FastTap(filledTextBox);
				_app.Wait(1);
				TakeScreenshot("1 - Focus on filledTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on normalTextBox
				_app.FastTap(unfocusButton);
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("1 - Remove Focus on filledTextBox", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Keyboard predicted text can change*/);
			}

			{
				// Setting focus on placeholderTextTextBox
				_app.FastTap(placeholderTextTextBox);
				_app.Wait(1);
				TakeScreenshot("2 - Focus on placeholderTextTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on placeholderTextTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("2 - Remove Focus on placeholderTextTextBox");
			}

			{
				// Setting focus on disabledTextBox
				_app.FastTap(disabledTextBox);
				_app.Wait(1);
				TakeScreenshot("3 - Focus on disabledTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on disabledTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("3 - Remove Focus on disabledTextBox");
			}

			{
				// Setting focus on multilineTextBox
				_app.FastTap(multilineTextBox);
				_app.Wait(1);
				TakeScreenshot("4 - Focus on multilineTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on multilineTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("4 - Remove Focus on multilineTextBox");
			}

			{
				// Setting focus on numberTextBox
				_app.FastTap(numberTextBox);
				_app.Wait(1);
				TakeScreenshot("5 - Focus on numberTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on numberTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("5 - Remove Focus on numberTextBox");
			}
		}

		[Test]
		[AutoRetry]
		public void Keyboard_Textbox_NoScrollViewer_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_NoScrollViewer_Automated");

			_app.WaitForElement(_app.Marked("MultilineTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var filledTextBox = _app.Marked("FilledTextBox");
			var placeholderTextTextBox = _app.Marked("PlaceholderTextTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");
			var multilineTextBox = _app.Marked("MultilineTextBox");
			var numberTextBox = _app.Marked("NumberTextBox");
			var unfocusButton = _app.Marked("UnfocusButton");

			// Assert initial state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("Text", filledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, placeholderTextTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, multilineTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, numberTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.FastTap(normalTextBox);
				_app.Wait(1);
				TakeScreenshot("0 - Focus on normalTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on normalTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("0 - Remove Focus on normalTextBox", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Keyboard predicted text can change*/);
			}

			{
				// Setting focus on normalTextBox
				_app.FastTap(filledTextBox);
				_app.Wait(1);
				TakeScreenshot("1 - Focus on filledTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on normalTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("1 - Remove Focus on filledTextBox", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Keyboard predicted text can change*/);
			}

			{
				// Setting focus on placeholderTextTextBox
				_app.FastTap(placeholderTextTextBox);
				_app.Wait(1);
				TakeScreenshot("2 - Focus on placeholderTextTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on placeholderTextTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("2 - Remove Focus on placeholderTextTextBox");
			}

			{
				// Setting focus on disabledTextBox
				_app.FastTap(disabledTextBox);
				_app.Wait(1);
				TakeScreenshot("3 - Focus on disabledTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on disabledTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("3 - Remove Focus on disabledTextBox");
			}

			{
				// Setting focus on multilineTextBox
				_app.FastTap(multilineTextBox);
				_app.Wait(1);
				TakeScreenshot("4 - Focus on multilineTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on multilineTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("4 - Remove Focus on multilineTextBox");
			}

			{
				// Setting focus on numberTextBox
				_app.FastTap(numberTextBox);
				_app.Wait(1);
				TakeScreenshot("5 - Focus on numberTextBox", ignoreInSnapshotCompare: true);

				// Removing focus on numberTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
				TakeScreenshot("5 - Remove Focus on numberTextBox");
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled on iOS: https://github.com/unoplatform/uno/issues/1955
		public void Keyboard_Textbox_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_InsideScrollerViewer_Automated");

			_app.WaitForElement(_app.Marked("DisabledTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");
			var unfocusButton = _app.Marked("UnfocusButton");

			// Assert initial state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.FastTap(normalTextBox);
				_app.Wait(1);

				// Writing in normalTextBox 
				_app.EnterText("Test 1");
				Assert.AreEqual("Test 1", normalTextBox.GetDependencyPropertyValue("Text")?.ToString());

				// Removing focus on normalTextBox
				_app.FastTap(unfocusButton);
				_app.Wait(1);
			}

			{
				// Setting focus on disabledTextBox
				_app.FastTap(disabledTextBox);
				_app.Wait(1);

				// Writing in disabledTextBox 
				_app.EnterText("Test 2");
				Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());

				// Removing focus on disabledTextBox
				_app.FastTap(unfocusButton);
			}
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextChanged()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_TextChanged");

			var appendOutput = _app.Marked("AppendTextBlock");

			_app.WaitForElement(appendOutput);

			Assert.AreEqual("", GetText(appendOutput));

			const string AppendedText = "BondStreet";
			var appendInput = TypeInto("AppendTextBox", "Bond", AppendedText);

			Assert.AreEqual(AppendedText, GetText(appendInput));
			Assert.AreEqual(AppendedText, GetText(appendOutput));

			var capOutput = _app.Marked("CapitalizeTextBlock");

			Assert.AreEqual("", GetText(capOutput));

			const string CapitalizedText = "RABBIT";
			var capInput = TypeInto("CapitalizeTextBox", "rabbit", CapitalizedText);

			Assert.AreEqual(CapitalizedText, GetText(capInput));
			Assert.AreEqual(CapitalizedText, GetText(capOutput));
			;
		}

		[Test]
		[AutoRetry]
		public void TextBox_PageLoadedTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_TextChanged");

			var appendOutput = _app.Marked("AppendTextBlock");

			_app.WaitForElement(appendOutput);
		}

		[Test]
		[AutoRetry]
		public void TextBox_MaxLength()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_MaxLength");

			const string Entered = "123456789";
			const string Final = "12345";

			var textBox = TypeInto("MaxLengthTextBox", Entered, Final);

			Assert.AreEqual(Final, GetText(textBox));

			_app.ClearText();

			Assert.AreEqual("", GetText(textBox));
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextChanging_Capitalize()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_TextChanging");

			var textBlock = _app.Marked("CapitalizePreviousTextBlock");

			const string Entered = "This patience is a virtue";
			const string Final = "THIS PATIENCE IS A VIRTue";

			var textBox = TypeInto("CapitalizePreviousTextBox", Entered, Final);

			Assert.AreEqual(Final, GetText(textBox));
			Assert.AreEqual(Final, GetText(textBlock));
		}

		[Test]
		[AutoRetry]
		public void TextBox_TextChanging_Limit()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_TextChanging");

			var textBlock = _app.Marked("LimitLengthTextBlock");

			const string Entered = "abcdefghijklmnopqr";
			const string Final = "defghijklmnopqr";

			var textBox = TypeInto("LimitLengthTextBox", Entered, Final);

			Assert.AreEqual(Final, GetText(textBox));
			Assert.AreEqual(Final, GetText(textBlock));

			_app.ClearText();

			Assert.AreEqual("", GetText(textBox));
			Assert.AreEqual("", GetText(textBlock));

			const string Entered2 = "Any way the wind blows";
			const string Final2 = " the wind blows";

			TypeInto("LimitLengthTextBox", Entered2, Final2);

			Assert.AreEqual(Final2, GetText(textBox));
			Assert.AreEqual(Final2, GetText(textBlock));
		}

		[Test]
		[AutoRetry]
		public void TextBox_BeforeTextChanging()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_BeforeTextChanging");

			var textBlock = _app.Marked("BeforeTextBlock");

			const string Entered = "reassesses";
			const string Final = "eee";

			var textBox = TypeInto("BeforeTextBox", Entered, Final);

			Assert.AreEqual(Final, GetText(textBox));
			Assert.AreEqual(Final, GetText(textBlock));

			_app.ClearText();

			Assert.AreEqual("", GetText(textBox));
			Assert.AreEqual("", GetText(textBlock));

			const string Entered2 = "See the eels";
			const string Final2 = "eeeee";

			TypeInto("BeforeTextBox", Entered2, Final2);

			Assert.AreEqual(Final2, GetText(textBox));
			Assert.AreEqual(Final2, GetText(textBlock));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android is disabled https://github.com/unoplatform/uno/issues/1630
		public void TextBox_Disable()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_Disabled");

			const string Entered = "Catfish";
			const string Final = "Cat";

			var textBox = TypeInto("DisableOnf", Entered, Final);

			var isEnabled = textBox.GetDependencyPropertyValue<bool>("IsEnabled");
			Assert.False(isEnabled);
			Assert.AreEqual(Final, GetText(textBox));
		}

		private QueryEx TypeInto(string textBoxName, string inputText, string expectedText)
		{
			var tb = _app.Marked(textBoxName);
			_app.WaitForElement(tb);
			_app.FastTap(tb);
			_app.EnterText(tb, inputText);
			_app.WaitFor(() => expectedText == GetText(tb));
			return tb;
		}

		private string GetText(QueryEx textBlock) => textBlock.GetDependencyPropertyValue<string>("Text");

		[Test]
		[AutoRetry]
		public void Keyboard_DismissTesting()
		{
			Run(("Uno.UI.Samples.Content.UITests.ButtonTestsControl.AppBar_KeyBoard"));
			var appCommandBar = _app.Marked("AppCommandBar");
			var singleTextBox = _app.Marked("SingleTextBox");

			_app.WaitForElement(singleTextBox);

			// Inital Screenshot
			using var initial = TakeScreenshot("initial", ignoreInSnapshotCompare: true);

			// Focus TextBox to reveal keyboard
			singleTextBox.FastTap();
			_app.Wait(2);

			// Tap on [DONE] AppBarButton
			var doneBtn = appCommandBar.Descendant().WithExactText("Done"); // yes, normal case Done
			doneBtn.Tap();
			_app.Wait(3);

			// Final Screenshot
			using var final = TakeScreenshot("final", ignoreInSnapshotCompare: true);

			// We only validate that the bottom of the screen is the same (so the keyboard is no longer visible).
			// This is to avoid content offset if the status bar was opened by the keyboard or the message box.
			ImageAssert.AreEqual(initial, final, new Rectangle(0, -100, int.MaxValue, 100));
		}
	}
}
