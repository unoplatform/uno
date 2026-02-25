using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebViewTests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, Description = "Manual test for WebResourceRequested. Click 'Add wildcard filter' then 'Navigate test page'.")]
	public sealed partial class WebView2_WebResourceRequested : Page
	{
		public WebView2_WebResourceRequested()
		{
			this.InitializeComponent();

			AddFilterButton.Click += (s, e) =>
			{
				try
				{
					WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
					StatusText.Text = "Filter added";
				}
				catch (Exception ex)
				{
					StatusText.Text = "Add filter failed: " + ex.Message;
				}
			};

			WebView.CoreWebView2.WebResourceRequested += (s, e) =>
			{
				try
				{
					Console.WriteLine($"[Test] Event handler called. e type: {e.GetType().Name}");
					Console.WriteLine($"[Test] e.Request type: {e.Request.GetType().Name}");
					Console.WriteLine($"[Test] e.Request.Headers type: {e.Request.Headers.GetType().Name}");

					// Inject an Authorization header so you can verify it on an echo endpoint
					e.Request.Headers.SetHeader("Authorization", "Session TEST_TOKEN");
					var authHeader = e.Request.Headers.GetHeader("Authorization");
					_ = DispatcherQueue.TryEnqueue(() => StatusText.Text = $"WebResourceRequested: {e.Request.Uri}\nAuth header: {authHeader}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[Test] EXCEPTION: {ex}");
					_ = DispatcherQueue.TryEnqueue(() => StatusText.Text = "WebResourceRequested handler error: " + ex.Message);
				}
			};

			NavigateButton.Click += (s, e) =>
			{
				WebView.CoreWebView2.Navigate("https://httpbin.org/headers");
			};
		}
	}
}
