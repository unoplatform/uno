using Uno.UI.Samples.Controls;
using System;
using Windows.UI.Xaml;
using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[SampleControlInfo("WebView", "WebView_WithHeaders", typeof(WebViewViewModel))]
	public sealed partial class WebView_WithHeaders : UserControl
	{
#if HAS_UNO
		public WebView_WithHeaders()
		{
			InitializeComponent();
			MyButton.Click += MyButton_OnClick;
		}

		private void MyButton_OnClick(object sender, RoutedEventArgs e)
		{
			var url = "http://requestb.in/rw35narw";
			MyTextBlock.Text = $"Inspect data at {url}?inspect";
			var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
			request.Headers.Add("HELLO", "TESTTEST, TEST2");
			request.Headers.Add("HELLO2", "TEST111");
			MyWebView2.NavigateWithHttpRequestMessage(request);
		}
#endif
	}
}
