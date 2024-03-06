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
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void WebView_NavigateToLongString()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.WebView.WebView_NavigateToString2");

			_app.WaitForElement(_app.Marked("startButton"));

			TakeScreenshot("Initial");

			var startButton = _app.Marked("startButton");
			var clickResult = _app.Marked("WebView_NavigateToStringResult");

			// step 1: generate long string
			_app.FastTap(startButton);

			_app.WaitForText(clickResult, "string ready");  // timeout here means: add wait

			// step 2: NavigateTo
			_app.FastTap(startButton);
			_app.WaitForText(clickResult, "success");   // timeout here means: bug reappear

			TakeScreenshot("AfterSuccess");

		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void WebView_NavigateToAnchor()
		{
			Run("Uno.UI.Samples.Content.UITests.WebView.WebView_AnchorNavigation");

			_app.WaitForElement(_app.Marked("NavigateToAnchorButton"));

			TakeScreenshot("Initial");

			var navigationCompletedTextBlock = _app.Marked("NavigationCompletedTextBlock");
			var navigateToAnchorButton = _app.Marked("NavigateToAnchorButton");
			var clickAnchorButton = _app.Marked("ClickAnchorButton");

			_app.WaitForText(navigationCompletedTextBlock, "https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html");

			// navigate to anchor
			_app.FastTap(navigateToAnchorButton);
			_app.WaitForText(navigationCompletedTextBlock, "https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html#section-1");

			TakeScreenshot("navigate to anchor");

			// user click in the browser itself
			_app.FastTap(clickAnchorButton);
			_app.WaitForText(navigationCompletedTextBlock, "https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html#page-4");

			TakeScreenshot("click anchor");
		}
	}
}
