using Uno.UI.Samples.Controls;
using System;
using Microsoft.UI.Xaml;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Microsoft.UI.Xaml.Controls;
using Windows.Web.Http;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", "WebView2_WithHeaders", typeof(WebView2ViewModel))]
	public sealed partial class WebView2_WithHeaders : UserControl
	{
#if HAS_UNO
		public WebView2_WithHeaders()
		{
			InitializeComponent();
			MyButton.Click += MyButton_OnClick;
			MyWebView2.NavigationCompleted += async (s, e) =>
			{
				if (MyWebView2.Source.Scheme == "about")
				{
					MyTextBlock.Text = "Click button to send request with custom headers...";
				}
				else
				{
					var html = await MyWebView2.CoreWebView2.ExecuteScriptAsync("document.body.outerText");
					if (html.Contains("\\\"Hello\\\": \\\"TESTTEST, TEST2\\\"") && html.Contains("\\\"Hello2\\\": \\\"TEST111\\\""))
					{
						MyTextBlock.Text = "Success! Found both HELLO and HELLO2 headers with their respective values";
					}
					else
					{
						MyTextBlock.Text = "Failed! Did not find expected HELLO and HELLO2 or they values. See HTML output...";
					}
				}
			};
		}

		private void MyButton_OnClick(object sender, RoutedEventArgs e)
		{
			var url = "https://httpbin.org/headers";
			var request = new HttpRequestMessage
			{
				RequestUri = new Uri(url),
				Method = HttpMethod.Get
			};
			request.Headers.Add("HELLO", "TESTTEST, TEST2");
			request.Headers.Add("HELLO2", "TEST111");
			MyWebView2.CoreWebView2.NavigateWithHttpRequestMessage(request);
		}
#endif
	}
}
