using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Sends host-to-page messages via PostWebMessageAsString and PostWebMessageAsJson; the page logs them.")]
	public sealed partial class WebView2_PostWebMessage : Page
	{
		private const string HostedHtml = """
			<!doctype html>
			<html>
			<head><meta charset='utf-8'><title>PostWebMessage demo</title></head>
			<body style='font-family: -apple-system, system-ui, sans-serif; padding: 12px;'>
				<h2>Host -> Page log</h2>
				<ul id='log' style='font-family: monospace; white-space: pre;'></ul>
				<script>
					(function () {
						var log = document.getElementById('log');
						function append(line) {
							var li = document.createElement('li');
							li.textContent = line;
							log.appendChild(li);
						}
						append('Page loaded at ' + new Date().toISOString());
						if (window.chrome && window.chrome.webview && window.chrome.webview.addEventListener) {
							window.chrome.webview.addEventListener('message', function (e) {
								try {
									append('Received (typeof=' + (typeof e.data) + '): ' + JSON.stringify(e.data));
								} catch (err) {
									append('Received (toString): ' + e.data);
								}
							});
							append('Listener registered.');
						} else {
							append('chrome.webview.addEventListener is not available.');
						}
					})();
				</script>
			</body>
			</html>
			""";

		public WebView2_PostWebMessage()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await WebView.EnsureCoreWebView2Async();
			WebView.NavigateToString(HostedHtml);
		}

		private void OnPostStringClick(object sender, RoutedEventArgs e)
		{
			WebView.CoreWebView2?.PostWebMessageAsString(MessageInput.Text);
		}

		private void OnPostJsonClick(object sender, RoutedEventArgs e)
		{
			WebView.CoreWebView2?.PostWebMessageAsJson("{\"n\":42,\"text\":\"json payload\"}");
		}
	}
}
