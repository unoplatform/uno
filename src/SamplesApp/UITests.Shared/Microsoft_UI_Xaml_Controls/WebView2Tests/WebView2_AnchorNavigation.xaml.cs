using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Description = "This sample tests that anchor navigation raises the proper events. The 2 uris received from the NavigationStarting and NavigationCompleted must update whether you tap the NavigateToAnchor button or tap on anchors from the web content.")]
	public sealed partial class WebView2_AnchorNavigation : UserControl
	{
		public WebView2_AnchorNavigation()
		{
			InitializeComponent();

#if HAS_UNO
			//TODO:MZ:
			//webView.Navigate(new Uri("https://uno-assets.platform.uno/tests/docs/WebView2_NavigateToAnchor.html"));
			//webView.NavigationStarting += WebView2_NavigationStarting;
			//webView.NavigationCompleted += WebView2_NavigationCompleted;
#endif
		}

		private void WebView2_NavigationStarting(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
		{
			NavigationStartingTextBlock.Text = args.Uri.ToString();
		}

		private void WebView2_NavigationCompleted(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
		{
			//TODO: MZ: How to get the URI here?
			//NavigationCompletedTextBlock.Text = sender.CoreWebView2.Ur;
		}

		private async void ButtonClicked()
		{
			await webView.EnsureCoreWebView2Async();
			webView.CoreWebView2.Navigate("https://uno-assets.platform.uno/tests/docs/WebView2_NavigateToAnchor.html#section-1");
		}

		private void OnClickAnchor()
		{
			_ = webView.ExecuteScriptAsync("document.querySelector(\"a[href =\\\"#page-4\\\"]\").click()");
		}
	}
}
