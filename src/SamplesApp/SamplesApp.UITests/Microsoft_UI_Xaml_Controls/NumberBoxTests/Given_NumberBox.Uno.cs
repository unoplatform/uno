using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public partial class NumberBoxTests
    {
		[Test]
		[AutoRetry]
		public void NumberBox_Header()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBox_Header", skipInitialScreenshot: true);

			var headerContentTextBlock = _app.Marked("NumberBoxHeaderContent");
			_app.WaitForElement(headerContentTextBlock);

			Assert.AreEqual("This is a NumberBox Header", headerContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}
	}
}
