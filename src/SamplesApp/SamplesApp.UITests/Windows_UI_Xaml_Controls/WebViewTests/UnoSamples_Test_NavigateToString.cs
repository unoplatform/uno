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
		public void WebView_NavigateToLongString()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.WebView.WebView_NavigateToString2");

			_app.WaitForElement(_app.Marked("startButton"));

			TakeScreenshot("Initial");

			var startButton = _app.Marked("startButton");
			var clickResult = _app.Marked("WebView_NavigateToStringResult");

			_app.Tap(startButton);

			//for(int i=10; i<100; i+=10)
			//{
			//	_app.WaitForText(clickResult, i.ToString());
			//}
			_app.Wait(TimeSpan.FromSeconds(300)); // Android: 20 is sufficient
			_app.WaitForText(clickResult, "string is generated");
			_app.WaitForText(clickResult, "success");
			TakeScreenshot("AfterSuccess");


		}

	}
}
