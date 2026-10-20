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
/// What an unmodified BlazorWebView needs from a host: pages served for https://0.0.0.1/ through WebResourceRequested
/// (answered after an await), document-created scripts, DOMContentLoaded and chrome.webview messaging.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_WebView2_VirtualHostResponses
{
	private const string Origin = "https://0.0.0.1/";
	private const int TimeoutMS = 10_000;

	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

	[TestMethod]
	public async Task When_Answered_After_Await_Page_Renders_With_Subresources()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);
		var server = Serve(webView, new()
		{
			["/index.html"] = Html("""
				<!DOCTYPE html>
				<html><head><link rel="stylesheet" href="site.css"></head>
				<body><div id="box">box</div><script src="app.js"></script></body></html>
				"""),
			["/site.css"] = new(200, "OK", "Content-Type: text/css", "#box { color: rgb(255, 0, 0); }"),
			["/app.js"] = new(200, "OK", "Content-Type: text/javascript", """
				window.addEventListener('load', function () {
					chrome.webview.postMessage(getComputedStyle(document.getElementById('box')).color);
				});
				"""),
		});

		webView.CoreWebView2.Navigate(Origin + "index.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("rgb(255, 0, 0)", messages[0]);
		CollectionAssert.IsSubsetOf(new[] { "/index.html", "/site.css", "/app.js" }, server.Requested);
	}

	[TestMethod]
	public async Task When_Responses_Carry_Status_And_Headers_The_Page_Sees_Them()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);
		Serve(webView, new()
		{
			["/status.html"] = Html("""
				<html><body><script>
					Promise.all([fetch('/data.json'), fetch('/missing.json')]).then(function (r) {
						chrome.webview.postMessage(r[0].status + ':' + r[0].headers.get('X-Uno-Test') + '|' + r[1].status + ':' + r[1].statusText);
					}, function (e) { chrome.webview.postMessage('error:' + e); });
				</script></body></html>
				"""),
			["/data.json"] = new(201, "Created", "Content-Type: application/json\r\nX-Uno-Test: 42", "{}"),
		});

		webView.CoreWebView2.Navigate(Origin + "status.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("201:42|404:Not Found", messages[0]);
	}

	[TestMethod]
	public async Task When_Document_Created_Script_Runs_Before_Inline_Script()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);
		Serve(webView, new()
		{
			["/order.html"] = Html("""
				<html><head><script>
					chrome.webview.postMessage(window.unoDocumentCreated || 'missing');
				</script></head></html>
				"""),
		});

		await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.unoDocumentCreated = 'document-created';");
		webView.CoreWebView2.Navigate(Origin + "order.html");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("document-created", messages[0]);
	}

	[TestMethod]
	public async Task When_Navigating_DOMContentLoaded_Precedes_NavigationCompleted()
	{
		var webView = await CreateWebViewAsync();
		Serve(webView, new() { ["/lifecycle.html"] = Html("<html><body>lifecycle</body></html>") });

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

		webView.CoreWebView2.Navigate(Origin + "lifecycle.html");

		await TestServices.WindowHelper.WaitFor(() => events.Contains("NavigationCompleted"), TimeoutMS);
		CollectionAssert.AreEqual(new[] { "DOMContentLoaded", "NavigationCompleted" }, events);
		Assert.AreEqual(navigationCompletedId, domContentLoadedId);
	}

	[TestMethod]
	public async Task When_Page_Posts_Message_Source_Is_The_Virtual_Host()
	{
		var webView = await CreateWebViewAsync();
		string source = null;
		string json = null;
		webView.CoreWebView2.WebMessageReceived += (s, e) =>
		{
			source = e.Source;
			json = e.WebMessageAsJson;
		};
		Serve(webView, new()
		{
			["/source.html"] = Html("<html><body><script>chrome.webview.postMessage({ a: [1, 'b'] });</script></body></html>"),
		});

		webView.CoreWebView2.Navigate(Origin + "source.html");

		await TestServices.WindowHelper.WaitFor(() => source is not null, TimeoutMS);
		Assert.IsTrue(source.StartsWith(Origin, StringComparison.OrdinalIgnoreCase), $"Unexpected source '{source}'.");
		Assert.AreEqual("{\"a\":[1,\"b\"]}", json);
	}

	[TestMethod]
	public async Task When_Host_Posts_Messages_Page_Listener_Receives_Them()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);
		Serve(webView, new()
		{
			["/echo.html"] = Html("""
				<html><body><script>
					chrome.webview.addEventListener('message', function (e) {
						chrome.webview.postMessage('echo:' + (typeof e.data === 'string' ? e.data : JSON.stringify(e.data)));
					});
					chrome.webview.postMessage('ready');
				</script></body></html>
				"""),
		});

		webView.CoreWebView2.Navigate(Origin + "echo.html");
		await TestServices.WindowHelper.WaitFor(() => messages.Contains("ready"), TimeoutMS);

		webView.CoreWebView2.PostWebMessageAsString("ping");
		await TestServices.WindowHelper.WaitFor(() => messages.Contains("echo:ping"), TimeoutMS);

		webView.CoreWebView2.PostWebMessageAsJson("{\"n\":1}");
		await TestServices.WindowHelper.WaitFor(() => messages.Contains("echo:{\"n\":1}"), TimeoutMS);
	}

	[TestMethod]
	public async Task When_No_Handler_Answers_The_Request_Loads_Normally()
	{
		var webView = await CreateWebViewAsync();
		var requested = new List<string>();
		string message = null;

		webView.CoreWebView2.SetVirtualHostNameToFolderMapping("UnoNativeAssets", "WebContent", CoreWebView2HostResourceAccessKind.Allow);
		webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
		webView.CoreWebView2.WebResourceRequested += (s, e) => requested.Add(e.Request.Uri);
		webView.CoreWebView2.WebMessageReceived += (s, e) => message = e.TryGetWebMessageAsString();

		webView.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");

		await TestServices.WindowHelper.WaitFor(() => message is not null, TimeoutMS);
		Assert.AreEqual("rgb(255, 0, 0)", message);
		Assert.IsTrue(requested.Exists(uri => uri.EndsWith("css/site.css", StringComparison.Ordinal)), string.Join(", ", requested));
	}

	private static async Task<WebView2> CreateWebViewAsync()
	{
		var webView = new WebView2
		{
			Width = 200,
			Height = 200,
		};

		TestServices.WindowHelper.WindowContent = webView;
		await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: TimeoutMS);
		await webView.EnsureCoreWebView2Async();

		return webView;
	}

	private static Resource Html(string body) => new(200, "OK", "Content-Type: text/html; charset=utf-8", body);

	/// <summary>
	/// Serves <paramref name="resources"/> for the virtual host, answering only after an await (as BlazorWebView
	/// does), and 404 for anything else under it.
	/// </summary>
	private static VirtualHost Serve(WebView2 webView, Dictionary<string, Resource> resources)
	{
		var host = new VirtualHost();
		var core = webView.CoreWebView2;

		core.AddWebResourceRequestedFilter(Origin + "*", CoreWebView2WebResourceContext.All);
		core.WebResourceRequested += async (s, e) =>
		{
			var path = new Uri(e.Request.Uri).AbsolutePath;
			host.Requested.Add(path);

			var deferral = e.GetDeferral();
			try
			{
				await Task.Delay(20);

				var resource = resources.TryGetValue(path, out var found)
					? found
					: new Resource(404, "Not Found", "Content-Type: text/plain", "missing");

				var stream = new MemoryStream(Encoding.UTF8.GetBytes(resource.Body)).AsRandomAccessStream();
				e.Response = core.Environment.CreateWebResourceResponse(stream, resource.Status, resource.Reason, resource.Headers);
			}
			finally
			{
				deferral.Complete();
			}
		};

		return host;
	}

	private static List<string> CollectStringMessages(WebView2 webView)
	{
		var messages = new List<string>();
		webView.CoreWebView2.WebMessageReceived += (s, e) => messages.Add(e.TryGetWebMessageAsString());
		return messages;
	}

	private sealed record Resource(int Status, string Reason, string Headers, string Body);

	private sealed class VirtualHost
	{
		public List<string> Requested { get; } = new();
	}
}
