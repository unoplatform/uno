using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.RadioButtonTests
{
	public partial class UnoSamples_RadioButton_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void RadioButtons_Enable_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests.RadioButton_IsEnabled_Automated");

			_app.WaitForElement(_app.Marked("MyRadioButtonDisabler"));
			var currentRadioButton = _app.Marked("CurrentRadioButton");
			var myRadioButton_1 = _app.Marked("MyRadioButton_1");
			var myRadioButton_2 = _app.Marked("MyRadioButton_2");

			// Verify the selection of radio button initially. 
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Click on radio button 1 and verify the selection of radio button
			myRadioButton_1.Tap();
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Click on radio button 2 and verify the selection of radio button
			myRadioButton_2.Tap();
			Assert.AreEqual("Radio Button 2", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Again click on radio button 1 and verify the selection of radio button
			myRadioButton_1.Tap();
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void RadioButtons_Disable_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests.RadioButton_IsEnabled_Automated");

			var myRadioButtonDisabler = _app.Marked("MyRadioButtonDisabler");

			_app.WaitForElement(myRadioButtonDisabler);
			var currentRadioButton = _app.Marked("CurrentRadioButton");
			var myRadioButton_1 = _app.Marked("MyRadioButton_1");
			var myRadioButton_2 = _app.Marked("MyRadioButton_2");

			// Verify the selection of radio button initially.
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Disable radio buttons and click on radio button 1 and verify the selection of radio button
			myRadioButtonDisabler.Tap();
			myRadioButton_1.Tap();
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Click on radio button 2 and verify the selection of radio button
			myRadioButton_2.Tap();
			Assert.AreEqual("None", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Enable radio buttons and click on radio button 1 and verify the selection of radio button
			myRadioButtonDisabler.Tap();
			myRadioButton_1.Tap();
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());

			// Disable radio buttons and click on radio button 2 and verify the selection of radio button
			myRadioButtonDisabler.Tap();
			myRadioButton_2.Tap();
			Assert.AreEqual("Radio Button 1", currentRadioButton.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
