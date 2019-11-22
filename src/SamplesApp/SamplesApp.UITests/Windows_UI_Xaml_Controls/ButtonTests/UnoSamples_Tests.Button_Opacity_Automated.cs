using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests
{
	[TestFixture]
	public partial class Button_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled on iOS: https://github.com/unoplatform/uno/issues/1955
		public void Button_IsOpacity_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.ButtonTestsControl.Button_Opacity_Automated");

			var buttonToIncrementNumber = _app.Marked("ButtonToChangeOpacity");
			var totalClicks = _app.Marked("TotalClicks");
			var valueOfOpacity = _app.Marked("ValueOfOpacity");
			var applyOpacityButton = _app.Marked("ApplyOpacityButton");

			// Assert initial state 
			Assert.AreEqual("0", totalClicks.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("1", valueOfOpacity.GetDependencyPropertyValue("Text")?.ToString());

			// When user click on Button ClickingButtonToChangeOpacity for the first time
			buttonToIncrementNumber.Tap();
			
			// Assert after clicking once while clickingButton enabled
			Assert.AreEqual("1", totalClicks.GetDependencyPropertyValue("Text")?.ToString());

			valueOfOpacity.Tap();
			_app.ClearText(valueOfOpacity);
			_app.EnterText("0.5");

			applyOpacityButton.Tap();

			// When user click on Button ClickingButtonToChangeOpacity for the seconde time
			buttonToIncrementNumber.Tap();

			// Assert after clicking once while clickingButton add
			Assert.AreEqual("2", totalClicks.GetDependencyPropertyValue("Text")?.ToString());

			valueOfOpacity.Tap();
			_app.ClearText(valueOfOpacity);
			_app.EnterText("0");

			applyOpacityButton.Tap();

			// When user click on Button ClickingButtonToChangeOpacity for the third time
			buttonToIncrementNumber.Tap();

			// Assert after clicking once while clickingButton add again
			Assert.AreEqual("3", totalClicks.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
