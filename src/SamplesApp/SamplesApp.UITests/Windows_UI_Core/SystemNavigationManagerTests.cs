using System;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Core
{
	[TestFixture]
	public partial class SystemNavigationManagerTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)]
		public void When_Hardware_Back_Button_Pressed()
		{
			Run("UITests.Windows_UI_Core.SystemNavigationManagerTests.HardwareBackButton");

			var handleCheckBox = _app.Marked("HandleCheckBox");
			var outputTextBlock = _app.Marked("OutputTextBlock");

			_app.WaitForElement(handleCheckBox);
			_app.WaitForElement(outputTextBlock);

			// Ensure Handle is checked
			_app.FastTap(handleCheckBox);

			Assert.IsTrue(string.IsNullOrEmpty(outputTextBlock.GetText()));

			_app.Back();

			Assert.IsTrue(outputTextBlock.GetText().StartsWith("Back requested", StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
