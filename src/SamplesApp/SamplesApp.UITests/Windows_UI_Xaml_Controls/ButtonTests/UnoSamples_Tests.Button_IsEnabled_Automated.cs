using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Xamarin.UITest;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests
{
	[TestFixture]
	partial class Button_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Button_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.Button_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("ToggleClickingButtonIsEnable"));

			var clickingButton = _app.Marked("ClickingButton");
			var totalClicksText = _app.Marked("TotalClicks");
			var toggleClickingButtonIsEnable = _app.Marked("ToggleClickingButtonIsEnable");

			// Assert initial state
			Assert.AreEqual("0", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());

			clickingButton.Tap();

			// Assert after clicking once while clickingButton enabled
			Assert.AreEqual("1", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());

			toggleClickingButtonIsEnable.Tap();
			clickingButton.Tap();

			// Assert after clicking once while clickingButton disabled
			Assert.AreEqual("1", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void CheckBox_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.CheckBox_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyCheckBoxDisabler"));

			var myCheckBox = _app.Marked("MyCheckBox");
			var checkBoxIsCheckedState = _app.Marked("CheckBoxIsCheckedState");
			var myCheckBoxDisabler = _app.Marked("MyCheckBoxDisabler");

			// Assert initial state
			Assert.AreEqual("False", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBox.Tap();

			// Assert after clicking once while myCheckBox enabled
			Assert.AreEqual("True", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBoxDisabler.Tap();
			myCheckBox.Tap();

			// Assert after clicking once while myCheckBox disabled
			Assert.AreEqual("True", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBoxDisabler.Tap();
			myCheckBox.Tap();

			// Assert after clicking once while myCheckBox enabled
			Assert.AreEqual("False", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void ToggleButton_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.ToggleButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyToggleButtonDisabler"));

			var myToggleButton = _app.Marked("MyToggleButton");
			var toggleButtonIsCheckedState = _app.Marked("ToggleButtonIsCheckedState");
			var myToggleButtonDisabler = _app.Marked("MyToggleButtonDisabler");

			// Assert initial state
			Assert.AreEqual("False", toggleButtonIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myToggleButton.Tap();

			// Assert after clicking once while myToggleButton enabled
			Assert.AreEqual("True", toggleButtonIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myToggleButtonDisabler.Tap();
			myToggleButton.Tap();

			// Assert after clicking once while myToggleButton disabled
			Assert.AreEqual("True", toggleButtonIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myToggleButtonDisabler.Tap();
			myToggleButton.Tap();

			// Assert after clicking once while myToggleButton enabled
			Assert.AreEqual("False", toggleButtonIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void ToggleSwitch_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.ToggleSwitch_IsEnable_Automated");

			var myToggleSwitch_1 = _app.Marked("MyToggleSwitch_1");
			var myToggleSwitch_2 = _app.Marked("MyToggleSwitch_2");
			var toggleSwitch_1_IsOn = _app.Marked("ToggleSwitch_1_IsOn");
			var toggleSwitch_2_IsOn = _app.Marked("ToggleSwitch_2_IsOn");

			// Assert initial state
			Assert.AreEqual("ToggleSwitch_1 is OFF", toggleSwitch_1_IsOn.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("ToggleSwitch_2 is OFF", toggleSwitch_2_IsOn.GetDependencyPropertyValue("Text")?.ToString());

			myToggleSwitch_1.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("ToggleSwitch_1 is ON", toggleSwitch_1_IsOn.GetDependencyPropertyValue("Text")?.ToString());

			myToggleSwitch_2.Tap();

			// Assert after clicking once while radio buttons are disabled
			Assert.AreEqual("ToggleSwitch_2 is OFF", toggleSwitch_2_IsOn.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void HyperlinkButton_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.HyperlinkButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("ToggleClickingButtonIsEnable"));

			var clickingButton = _app.Marked("ClickingButton");
			var totalClicksText = _app.Marked("TotalClicks");
			var toggleClickingButtonIsEnable = _app.Marked("ToggleClickingButtonIsEnable");

			// Assert initial state
			Assert.AreEqual("0", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());

			clickingButton.Tap();

			// Assert after clicking once while clickingButton enabled
			Assert.AreEqual("1", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());

			toggleClickingButtonIsEnable.Tap();
			clickingButton.Tap();

			// Assert after clicking once while clickingButton disabled
			Assert.AreEqual("1", totalClicksText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void CheckBox_IsEnabled_StatePreservation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.CheckBox_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyCheckBoxDisabler"));

			var myCheckBox = _app.Marked("MyCheckBox");
			var checkBoxIsCheckedState = _app.Marked("CheckBoxIsCheckedState");
			var myCheckBoxDisabler = _app.Marked("MyCheckBoxDisabler");

			//disabled state preservation
			// Assert initial state
			Assert.AreEqual("False", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBoxDisabler.Tap();

			// Assert after disabling the check box
			Assert.AreEqual("False", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			//Enable state preservation

			myCheckBoxDisabler.Tap();

			// Assert initial state
			Assert.AreEqual("False", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBox.Tap();

			// Assert after clicking once while myCheckBox enabled
			Assert.AreEqual("True", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());

			myCheckBoxDisabler.Tap();

			// Assert after disabling the check box
			Assert.AreEqual("True", checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Uno.UITest.Helpers.Queries.Platform.iOS, Uno.UITest.Helpers.Queries.Platform.Browser)] // Android is disabled https://github.com/unoplatform/uno/issues/1632

		public void CheckBox_DoubleTapValidation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.CheckBox_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyCheckBoxDisabler"));

			var myCheckBox = _app.Marked("MyCheckBox");
			var checkBoxIsCheckedState = _app.Marked("CheckBoxIsCheckedState");
			var myCheckBoxDisabler = _app.Marked("MyCheckBoxDisabler");

			void Check(bool expected, string msg)
			{
				for (var i = 0; i < 5; i++)
				{
					_app.Wait(0.25f * i);

					var s = checkBoxIsCheckedState.GetDependencyPropertyValue("Text")?.ToString();
					if (!bool.TryParse(s, out var v))
					{
						if (i < 4)
						{
							continue;
						}

						throw new InvalidOperationException($"\"{s}\" is not a valid bool value for test {msg}.");
					}

					if (v != expected && i < 4)
					{
						continue;
					}

					v.Should().Be(expected, msg);
				}
			}

			// Assert initial state
			Check(false, "initial value: unchecked");

			myCheckBox.DoubleTap();

			Check(false, "Assert after fist double click while myCheckBox is not checked");

			myCheckBox.Tap();
			Check(true, "Assert after single tap myCheckBox should be checked");

			myCheckBox.DoubleTap();

			// Assert after double click while myCheckBox is enabled
			Check(true, "Assert after another double click while myCheckBox should stay checked");
		}
	}
}
