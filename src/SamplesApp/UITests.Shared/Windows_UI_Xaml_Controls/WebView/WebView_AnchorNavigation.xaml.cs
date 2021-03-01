using System;
using System.Threading;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[SampleControlInfo("WebView", "WebView_AnchorNavigation", description:"This sample tests that anchor navigation raises the proper events. The 2 uris received from the NavigationStarting and NavigationCompleted must update whether you tap the NavigateToAnchor button or tap on anchors from the web content.")]
	public sealed partial class WebView_AnchorNavigation : UserControl
	{
		//Windows.UI.Xaml.Controls.WebView webView;

		public WebView_AnchorNavigation()
		{
			InitializeComponent();

#if HAS_UNO
			webView.Navigate(new Uri("https://tools.ietf.org/html/rfc6749"));
			webView.NavigationStarting += WebView_NavigationStarting;
			webView.NavigationCompleted += WebView_NavigationCompleted;
#endif
		}

		private void WebView_NavigationStarting(Windows.UI.Xaml.Controls.WebView sender, WebViewNavigationStartingEventArgs args)
		{
			NavigationStartingTextBlock.Text = args.Uri.AbsoluteUri;
		}

		private void WebView_NavigationCompleted(Windows.UI.Xaml.Controls.WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			NavigationCompletedTextBlock.Text = args.Uri.AbsoluteUri;
		}

		private void ButtonClicked()
		{
			webView.Navigate(new Uri("https://tools.ietf.org/html/rfc6749#section-1"));
		}

		private void OnClickAnchor()
		{
			_ = webView.InvokeScriptAsync("document.getElementById(\"page-4\").click()", null);
		}
	}
}
