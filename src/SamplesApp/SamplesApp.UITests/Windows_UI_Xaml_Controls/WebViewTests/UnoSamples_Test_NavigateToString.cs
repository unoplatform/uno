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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.WebViewTests
{
	[TestFixture]
	public partial class WebView_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void WebView_NavigateToLongString()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.WebView.WebView_NavigateToString2");

			_app.WaitForElement(_app.Marked("startButton"));

			TakeScreenshot("Initial");

			var startButton = _app.Marked("startButton");
			var clickResult = _app.Marked("WebView_NavigateToStringResult");

			// step 1: generate long string
			_app.Tap(startButton);

			_app.WaitForText(clickResult, "string ready");  // timeout here means: add wait

			// step 2: NavigateTo
			_app.Tap(startButton);
			_app.WaitForText(clickResult, "success");   // timeout here means: bug reappear

			TakeScreenshot("AfterSuccess");

		}

	}
}
