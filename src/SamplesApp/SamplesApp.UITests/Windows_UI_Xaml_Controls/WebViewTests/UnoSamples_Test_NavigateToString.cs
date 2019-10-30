using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.WebView
{
	[TestFixture]
	public partial class WebView_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void WebView_NavigateToLongString()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.WebView.WebView_NavigateToString2");

			var startButton = _app.Marked("startButton");
			var clickResult = _app.Marked("WebView_NavigateToStringResult");

			_app.WaitForElement(clickResult);

			TakeScreenshot("Initial");

			_app.Tap(startButton);
			_app.WaitForDependencyPropertyValue(clickResult, "Text", "success");
			TakeScreenshot("AfterSuccess");


		}

	}
}
