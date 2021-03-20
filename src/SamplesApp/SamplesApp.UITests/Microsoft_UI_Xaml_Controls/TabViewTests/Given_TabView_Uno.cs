using System;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	public partial class Given_TabView_Uno : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void SelectionContentTest()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.TabViewTests.TabViewItemsSourceTests");

			Console.WriteLine("Verify content is displayed for initially selected tab.");
			_app.WaitForElement("SelectButton");
			_app.Tap("SelectButton");

			_app.WaitForElement("ContentButton");
			var contentButton = _app.Marked("ContentButton");

			var result = contentButton.GetDependencyPropertyValue<string>("Content");
			Assert.AreEqual("Tab 2", result);
		}
	}
}
