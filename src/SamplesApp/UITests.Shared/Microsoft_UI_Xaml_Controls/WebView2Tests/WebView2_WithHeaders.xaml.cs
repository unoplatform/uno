using Uno.UI.Samples.Controls;
using System;
using Windows.UI.Xaml;
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[SampleControlInfo("WebView", "WebView2_WithHeaders", typeof(WebView2ViewModel))]
	public sealed partial class WebView2_WithHeaders : UserControl
	{
#if HAS_UNO
		public WebView2_WithHeaders()
		{
			InitializeComponent();
			MyButton.Click += MyButton_OnClick;
		}

		private void MyButton_OnClick(object sender, RoutedEventArgs e)
		{
			var url = "http://requestb.in/rw35narw";
			MyTextBlock.Text = $"Inspect data at {url}?inspect";
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
