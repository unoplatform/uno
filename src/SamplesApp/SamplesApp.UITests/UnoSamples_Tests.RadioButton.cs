using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Xamarin.UITest;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void RadioButton_IsEnabled_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests.RadioButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyRadioButtonDisabler"));

			var myRadioButton_1 = _app.Marked("MyRadioButton_1");
			var myRadioButton_2 = _app.Marked("MyRadioButton_2");
			var currentRadioButton = _app.Marked("CurrentRadioButton");
			var myRadioButtonDisabler = _app.Marked("MyRadioButtonDisabler");

			// Assert initial state 
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_1.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());


			myRadioButtonDisabler.Tap();
			myRadioButton_2.Tap();

			// Assert after clicking once while radio buttons are disabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButtonDisabler.Tap();
			myRadioButton_2.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void RadioButton_DoubleTap_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests.RadioButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyRadioButtonDisabler"));

			var myRadioButton_1 = _app.Marked("MyRadioButton_1");
			var myRadioButton_2 = _app.Marked("MyRadioButton_2");
			var currentRadioButton = _app.Marked("CurrentRadioButton");
			var myRadioButtonDisabler = _app.Marked("MyRadioButtonDisabler");

			// Assert initial state 
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_1.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
			myRadioButton_1.Tap();

			// Assert after clicking twice while radio buttons are enabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_2.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_2.Tap();

			// Assert after clicking twice while radio buttons are enabled
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void RadioButton_StatePreservation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests.RadioButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyRadioButtonDisabler"));

			var myRadioButton_1 = _app.Marked("MyRadioButton_1");
			var myRadioButton_2 = _app.Marked("MyRadioButton_2");
			var currentRadioButton = _app.Marked("CurrentRadioButton");
			var myRadioButtonDisabler = _app.Marked("MyRadioButtonDisabler");

			// Assert initial state 
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_1.Tap();

			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButtonDisabler.Tap();
			// Assert after clicking once while radio buttons are disabled
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButtonDisabler.Tap();

			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButton_2.Tap();
			// Assert after clicking once while radio buttons are enabled
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			myRadioButtonDisabler.Tap();
			// Assert after clicking once while radio buttons are disabled
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
		}


	}
}
