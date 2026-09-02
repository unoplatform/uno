using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Tests that PostMessage continues to work after removing and re-adding WebView2 to the visual tree. " +
		"Steps: 1) Click 'Send PostMessage from JS' and verify a message appears in the log. " +
		"2) Click 'Remove WebView2'. 3) Click 'Add WebView2'. " +
		"4) Click 'Send PostMessage from JS' again and verify the message still appears.")]
	public sealed partial class WebView2_PostMessage_RemoveAdd : Page
	{
		private int _messageCount;

		private const string HtmlContent = @"
<!DOCTYPE html>
<html>
<body>
	<h3>WebView2 PostMessage Test</h3>
	<p id='status'>Ready</p>
	<script>
		function sendMessage() {
			try {
				var count = ++window.messageCount || (window.messageCount = 1);
				var msg = 'Hello from WebView2 #' + count;

				if (window.hasOwnProperty('chrome') && typeof chrome.webview !== 'undefined') {
					// Windows
					chrome.webview.postMessage(msg);
				} else if (window.hasOwnProperty('unoWebView')) {
					// Android
					unoWebView.postMessage(JSON.stringify(msg));
				} else if (window.hasOwnProperty('webkit') && typeof webkit.messageHandlers !== 'undefined') {
					// iOS and macOS
					webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(msg));
				} else {
					document.getElementById('status').innerText = 'Error: no postMessage handler available';
					return;
				}
				document.getElementById('status').innerText = 'Message sent: ' + count;
			} catch (ex) {
				document.getElementById('status').innerText = 'Error: ' + ex;
			}
		}
	</script>
</body>
</html>";

		public WebView2_PostMessage_RemoveAdd()
		{
			this.InitializeComponent();
			Loaded += async (s, e) => await SetupWebView();
		}

		private async Task SetupWebView()
		{
			await TestWebView.EnsureCoreWebView2Async();
			TestWebView.NavigationCompleted += OnNavigationCompleted;
			TestWebView.WebMessageReceived += OnWebMessageReceived;
			TestWebView.NavigateToString(HtmlContent);
		}

		private void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			LogMessage($"[Navigation completed, success={args.IsSuccess}]");
		}

		private void OnWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			var message = args.TryGetWebMessageAsString();
			LogMessage($"#{++_messageCount}: {message}");
		}

		private void OnRemoveClick(object sender, RoutedEventArgs e)
		{
			WebViewContainer.Child = null;
			RemoveButton.IsEnabled = false;
			AddButton.IsEnabled = true;
			LogMessage("[WebView2 removed from visual tree]");
		}

		private void OnAddClick(object sender, RoutedEventArgs e)
		{
			WebViewContainer.Child = TestWebView;
			RemoveButton.IsEnabled = true;
			AddButton.IsEnabled = false;
			LogMessage("[WebView2 re-added to visual tree]");
		}

		private async void OnSendMessageClick(object sender, RoutedEventArgs e)
		{
			try
			{
				await TestWebView.ExecuteScriptAsync("sendMessage()");
			}
			catch (Exception ex)
			{
				LogMessage($"[Error: {ex.Message}]");
			}
		}

		private void LogMessage(string message)
		{
			MessageLog.Text += message + "\n";
		}
	}
}
