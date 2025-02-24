using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample(Description = "This sample tests that anchor navigation raises the proper events. The 2 uris received from the NavigationStarting and NavigationCompleted must update whether you tap the NavigateToAnchor button or tap on anchors from the web content.")]
	public sealed partial class WebView_AnchorNavigation : UserControl
	{
		public WebView_AnchorNavigation()
		{
			InitializeComponent();

			webView.Navigate(new Uri("https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html"));
			webView.NavigationStarting += WebView_NavigationStarting;
			webView.NavigationCompleted += WebView_NavigationCompleted;
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
			webView.Navigate(new Uri("https://uno-assets.platform.uno/tests/docs/WebView_NavigateToAnchor.html#section-1"));
		}

		private void OnClickAnchor()
		{
			_ = webView.InvokeScriptAsync("document.querySelector(\"a[href =\\\"#page-4\\\"]\").click()", null);
		}
	}
}
