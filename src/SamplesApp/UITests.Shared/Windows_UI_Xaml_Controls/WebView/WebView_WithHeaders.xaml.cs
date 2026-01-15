using Uno.UI.Samples.Controls;
using System;
using Microsoft.UI.Xaml;
using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Microsoft.UI.Xaml.Controls;
using Windows.Web.Http;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample("WebView", Name = "WebView_WithHeaders", ViewModelType = typeof(WebViewViewModel))]
	public sealed partial class WebView_WithHeaders : UserControl
	{
#if HAS_UNO
		public WebView_WithHeaders()
		{
			InitializeComponent();
			MyButton.Click += MyButton_OnClick;
			MyWebView2.NavigationCompleted += async (s, e) =>
			{
				if (MyWebView2.Source is null)
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
			var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
			request.Headers.Add("HELLO", "TESTTEST, TEST2");
			request.Headers.Add("HELLO2", "TEST111");
			MyWebView2.NavigateWithHttpRequestMessage(request);
		}
#endif
	}
}
