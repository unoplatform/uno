using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

/// <summary>
/// A page served for <c>https://0.0.0.1/</c> through <c>WebResourceRequested</c>, the way Blazor's WebView serves its
/// app, with <c>chrome.webview</c> scripting and messaging.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaIOS)]
public class Given_WebView2_VirtualHost
{
	private const string Origin = "https://0.0.0.1/";
	private const int TimeoutMS = 10_000;

	// 1x1 transparent PNG.
	private const string PixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

	[TestMethod]
	public async Task When_Page_Served_With_Async_Deferral_Subresources_Load()
	{
		var (webView, host) = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		host.Add("index.html", "text/html", """
			<!DOCTYPE html>
			<html><head>
				<link rel="stylesheet" href="site.css">
				<script src="app.js"></script>
			</head><body>
				<img id="img" src="pixel.png">
				<script>
					window.addEventListener('load', function () {
						chrome.webview.postMessage([
							window.unoAppJs || 'missing',
							getComputedStyle(document.body).marginLeft,
							document.getElementById('img').naturalWidth
						].join('|'));
					});
				</script>
			</body></html>
			""");
		host.Add("app.js", "text/javascript", "window.unoAppJs = 'loaded';");
		host.Add("site.css", "text/css", "body { margin-left: 7px; }");
		host.Add("pixel.png", "image/png", Convert.FromBase64String(PixelPng));

		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("loaded|7px|1", messages[0]);

		CollectionAssert.IsSubsetOf(
			new[] { Origin + "index.html", Origin + "app.js", Origin + "site.css", Origin + "pixel.png" },
			host.RequestedUris);
		Assert.AreEqual(CoreWebView2WebResourceContext.Document, host.Contexts[Origin + "index.html"]);
		Assert.AreNotEqual(CoreWebView2WebResourceContext.Document, host.Contexts[Origin + "app.js"]);
		Assert.AreEqual(Origin + "index.html", webView.CoreWebView2.Source);
	}

	[TestMethod]
	public async Task When_Response_Status_And_Headers_Reach_The_Page()
	{
		var (webView, host) = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		host.Add("index.html", "text/html", """
			<html><body><script>
				(async function () {
					var data = await fetch('data.json');
					var gone = await fetch('gone.txt');
					var missing = await fetch('missing.txt');
					chrome.webview.postMessage([
						data.status,
						data.headers.get('X-Uno-Test'),
						data.headers.get('Content-Type'),
						await data.text(),
						gone.status,
						await gone.text(),
						missing.status
					].join('|'));
				})();
			</script></body></html>
			""");
		host.Add("data.json", 201, "Created", "Content-Type: application/json\r\nX-Uno-Test: hello", Encoding.UTF8.GetBytes("{\"a\":1}"));
		host.Add("gone.txt", 404, "Not Found", "Content-Type: text/plain", Encoding.UTF8.GetBytes("gone"));

		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("201|hello|application/json|{\"a\":1}|404|gone|404", messages[0]);
	}

	[TestMethod]
	public async Task When_Document_Created_Script_Runs_Before_Inline_Script()
	{
		var (webView, host) = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.unoDocumentCreated = 'document-created';");

		host.Add("index.html", "text/html", """
			<html><head><script>
				chrome.webview.postMessage(window.unoDocumentCreated || 'missing');
			</script></head><body></body></html>
			""");

		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("document-created", messages[0]);
	}

	[TestMethod]
	public async Task When_Navigating_DOMContentLoaded_Precedes_NavigationCompleted()
	{
		var (webView, host) = await CreateWebViewAsync();
		var events = new List<string>();
		ulong domContentLoadedId = 0;
		ulong navigationCompletedId = 0;

		webView.CoreWebView2.DOMContentLoaded += (s, e) =>
		{
			events.Add("DOMContentLoaded");
			domContentLoadedId = e.NavigationId;
		};
		webView.CoreWebView2.NavigationCompleted += (s, e) =>
		{
			events.Add("NavigationCompleted");
			navigationCompletedId = e.NavigationId;
		};

		host.Add("index.html", "text/html", "<html><body>lifecycle</body></html>");
		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => events.Contains("NavigationCompleted"), TimeoutMS);
		CollectionAssert.AreEqual(new[] { "DOMContentLoaded", "NavigationCompleted" }, events);
		Assert.AreEqual(navigationCompletedId, domContentLoadedId);
	}

	[TestMethod]
	public async Task When_Page_Posts_Message_Source_Is_The_Virtual_Host()
	{
		var (webView, host) = await CreateWebViewAsync();
		string source = null;
		string message = null;
		webView.CoreWebView2.WebMessageReceived += (s, e) =>
		{
			source = e.Source;
			message = e.TryGetWebMessageAsString();
		};

		host.Add("index.html", "text/html", """
			<html><body><script>
				chrome.webview.postMessage('hello');
			</script></body></html>
			""");
		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => source is not null, TimeoutMS);
		Assert.AreEqual("hello", message);
		Assert.IsTrue(source.StartsWith(Origin, StringComparison.OrdinalIgnoreCase), $"Unexpected source '{source}'.");
	}

	[TestMethod]
	public async Task When_PostWebMessage_Page_Listener_Receives_It()
	{
		var (webView, host) = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		host.Add("index.html", "text/html", """
			<html><body><script>
				chrome.webview.addEventListener('message', function (e) {
					chrome.webview.postMessage('echo:' + (typeof e.data === 'string' ? e.data : JSON.stringify(e.data)));
				});
				chrome.webview.postMessage('ready');
			</script></body></html>
			""");
		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Contains("ready"), TimeoutMS);

		webView.CoreWebView2.PostWebMessageAsString("ping");
		await TestServices.WindowHelper.WaitFor(() => messages.Contains("echo:ping"), TimeoutMS);

		webView.CoreWebView2.PostWebMessageAsJson("{\"n\":42}");
		await TestServices.WindowHelper.WaitFor(() => messages.Contains("echo:{\"n\":42}"), TimeoutMS);
	}

	[TestMethod]
	public async Task When_Navigation_Stops_A_Pending_Deferral_Late_Response_Is_Ignored()
	{
		var (webView, host) = await CreateWebViewAsync();
		var slowRequested = false;
		var releaseSlow = new TaskCompletionSource();
		var completedSources = new List<string>();
		webView.CoreWebView2.NavigationCompleted += (s, e) => completedSources.Add(webView.CoreWebView2.Source);

		host.AddAsync("slow.html", async () =>
		{
			slowRequested = true;
			await releaseSlow.Task;
			return ("text/html", Encoding.UTF8.GetBytes("<html><body>slow</body></html>"));
		});
		host.Add("fast.html", "text/html", "<html><body>fast</body></html>");

		webView.CoreWebView2.Navigate(Origin + "slow.html");
		await TestServices.WindowHelper.WaitFor(() => slowRequested, TimeoutMS);

		webView.CoreWebView2.Navigate(Origin + "fast.html");
		await TestServices.WindowHelper.WaitFor(() => completedSources.Contains(Origin + "fast.html"), TimeoutMS);

		// The slow task was stopped by WebKit; answering it now must neither crash nor replace the page.
		releaseSlow.SetResult();
		await Task.Delay(500);

		Assert.AreEqual(Origin + "fast.html", webView.CoreWebView2.Source);
		Assert.AreEqual("\"fast\"", await webView.CoreWebView2.ExecuteScriptAsync("document.body.textContent"));
	}

	private static async Task<(WebView2 WebView, VirtualHost Host)> CreateWebViewAsync()
	{
		var webView = new WebView2
		{
			Width = 200,
			Height = 200,
		};

		TestServices.WindowHelper.WindowContent = webView;
		await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: TimeoutMS);

		// Like MAUI's BlazorWebView: a detached environment creates the responses.
		var environment = await CoreWebView2Environment.CreateAsync();
		await webView.EnsureCoreWebView2Async(environment);

		var host = new VirtualHost(environment);
		webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
		webView.CoreWebView2.WebResourceRequested += host.OnWebResourceRequested;

		return (webView, host);
	}

	private static List<string> CollectStringMessages(WebView2 webView)
	{
		var messages = new List<string>();
		webView.CoreWebView2.WebMessageReceived += (s, e) => messages.Add(e.TryGetWebMessageAsString());
		return messages;
	}

	/// <summary>
	/// Serves registered paths of <see cref="Origin"/>, always after an await so the deferral is real.
	/// Unknown paths get no response.
	/// </summary>
	private sealed class VirtualHost(CoreWebView2Environment environment)
	{
		private readonly Dictionary<string, Func<Task<CoreWebView2WebResourceResponse>>> _routes = new();

		public List<string> RequestedUris { get; } = new();

		public Dictionary<string, CoreWebView2WebResourceContext> Contexts { get; } = new();

		public void Add(string path, string contentType, string body)
			=> Add(path, contentType, Encoding.UTF8.GetBytes(body));

		public void Add(string path, string contentType, byte[] body)
			=> Add(path, 200, "OK", "Content-Type: " + contentType, body);

		public void Add(string path, int statusCode, string reasonPhrase, string headers, byte[] body)
			=> _routes[path] = () => Task.FromResult(environment.CreateWebResourceResponse(new MemoryStream(body).AsRandomAccessStream(), statusCode, reasonPhrase, headers));

		public void AddAsync(string path, Func<Task<(string ContentType, byte[] Body)>> respond)
			=> _routes[path] = async () =>
			{
				var (contentType, body) = await respond();
				return environment.CreateWebResourceResponse(new MemoryStream(body).AsRandomAccessStream(), 200, "OK", "Content-Type: " + contentType);
			};

		public async void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs e)
		{
			var uri = e.Request.Uri;
			if (!uri.StartsWith(Origin, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			RequestedUris.Add(uri);
			Contexts[uri] = e.ResourceContext;

			var deferral = e.GetDeferral();
			try
			{
				await Task.Delay(10);

				var path = new Uri(uri).AbsolutePath.TrimStart('/');
				if (_routes.TryGetValue(path, out var route))
				{
					e.Response = await route();
				}
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
