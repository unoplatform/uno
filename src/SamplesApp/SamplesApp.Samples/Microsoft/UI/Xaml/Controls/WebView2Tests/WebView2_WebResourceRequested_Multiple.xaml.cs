using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebViewTests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, Description = "Manual test for WebResourceRequested with multiple requests. Click 'Add wildcard filter' then 'Navigate test page' to see all resource requests logged.")]
	public sealed partial class WebView2_WebResourceRequested_Multiple : Page
	{
		private int _requestCount = 0;
		private readonly List<string> _requestLog = new List<string>();

		public WebView2_WebResourceRequested_Multiple()
		{
			this.InitializeComponent();

			AddFilterButton.Click += (s, e) =>
			{
				try
				{
					WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
					UpdateLog("Filter added: * (All resources)");
				}
				catch (Exception ex)
				{
					UpdateLog($"Add filter failed: {ex.Message}");
				}
			};

			WebView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;

			NavigateButton.Click += (s, e) =>
			{
				_requestCount = 0;
				_requestLog.Clear();
				UpdateLog("Navigating to test page...");
				// Using a page that loads multiple resources (HTML, CSS, images, etc.)
				WebView.CoreWebView2.Navigate("https://example.com");
			};

			ClearButton.Click += (s, e) =>
			{
				_requestCount = 0;
				_requestLog.Clear();
				UpdateCountAndLog();
			};
		}

		private void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs e)
		{
			try
			{
				_requestCount++;

				var uri = new Uri(e.Request.Uri);
				var resourceType = GetResourceType(uri);
				var method = e.Request.Method;

				var logEntry = $"[{_requestCount}] {method} {resourceType}: {uri.PathAndQuery}";
				_requestLog.Add(logEntry);

				Console.WriteLine($"[WebResourceRequested] {logEntry}");
				Console.WriteLine($"  Headers Count: {GetHeaderCount(e.Request.Headers)}");

				e.Request.Headers.SetHeader("X-Test-Request-Number", _requestCount.ToString());

				_ = DispatcherQueue.TryEnqueue(() => UpdateCountAndLog());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[WebResourceRequested] EXCEPTION: {ex}");
				_ = DispatcherQueue.TryEnqueue(() => UpdateLog($"Error in request handler: {ex.Message}"));
			}
		}

		private string GetResourceType(Uri uri)
		{
			var extension = System.IO.Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();

			// Infer resource type from file extension. If unknown, treat as Document for root or HTML.
			return extension switch
			{
				".css" => "Stylesheet",
				".js" => "Script",
				".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" => "Image",
				".mp4" or ".mp3" or ".wav" => "Media",
				".woff" or ".woff2" or ".ttf" or ".otf" => "Font",
				".xml" or ".json" => "XHR/Fetch",
				".html" or ".htm" => "Document",
				"" when (uri.AbsolutePath == "/" || string.IsNullOrEmpty(extension)) => "Document",
				_ => "Other"
			};
		}

		private int GetHeaderCount(CoreWebView2HttpRequestHeaders headers)
		{
			return headers.Count();
		}

		private void UpdateCountAndLog()
		{
			CountText.Text = $"Total requests: {_requestCount}";

			var displayLog = _requestLog.Skip(Math.Max(0, _requestLog.Count - 20)).ToList();
			var sb = new StringBuilder();

			if (_requestLog.Count > 20)
			{
				sb.AppendLine($"... showing last 20 of {_requestLog.Count} total requests ...\n");
			}

			foreach (var entry in displayLog)
			{
				sb.AppendLine(entry);
			}

			LogText.Text = sb.ToString();
		}

		private void UpdateLog(string message)
		{
			_requestLog.Add(message);
			UpdateCountAndLog();
		}
	}
}
