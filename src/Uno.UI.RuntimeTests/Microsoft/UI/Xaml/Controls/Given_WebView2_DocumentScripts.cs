using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaIOS)]
public class Given_WebView2_DocumentScripts
{
	private const string HostName = "uno-wv2-scripting.example";
	private const string Origin = "https://" + HostName + "/";

	// The Win32 WebView2 constructor blocks the UI thread while the browser process starts; cold starts on CI are slow.
	private const int TimeoutMS = 10_000;

	private string _contentFolder;

	[TestInitialize]
	public void Initialize()
	{
		_contentFolder = Path.Combine(Path.GetTempPath(), "UnoWebView2DocumentScripts", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_contentFolder);
	}

	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;

		try
		{
			Directory.Delete(_contentFolder, recursive: true);
		}
		catch (IOException)
		{
			// The browser process may still hold the file; the temp folder is cleaned by the OS eventually.
		}
	}

	[TestMethod]
	public async Task When_Document_Created_Script_Runs_Before_Page_Scripts()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.unoDocumentCreated = 'document-created';");

		Navigate(webView, "order.html", """
			<html><body><script>
				chrome.webview.postMessage(window.unoDocumentCreated || 'missing');
			</script></body></html>
			""");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("document-created", messages[0]);
	}

	[TestMethod]
	public async Task When_Document_Created_Script_Removed_It_No_Longer_Runs()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		var id = await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.unoDocumentCreated = 'document-created';");
		Assert.IsFalse(string.IsNullOrEmpty(id));
		webView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(id);

		Navigate(webView, "removed.html", """
			<html><body><script>
				chrome.webview.postMessage(window.unoDocumentCreated || 'missing');
			</script></body></html>
			""");

		await TestServices.WindowHelper.WaitFor(() => messages.Count > 0, TimeoutMS);
		Assert.AreEqual("missing", messages[0]);
	}

	[TestMethod]
	public async Task When_Navigating_DOMContentLoaded_Precedes_NavigationCompleted()
	{
		var webView = await CreateWebViewAsync();
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

		Navigate(webView, "lifecycle.html", "<html><body>lifecycle</body></html>");

		await TestServices.WindowHelper.WaitFor(() => events.Contains("NavigationCompleted"), TimeoutMS);
		CollectionAssert.AreEqual(new[] { "DOMContentLoaded", "NavigationCompleted" }, events);
		Assert.AreEqual(navigationCompletedId, domContentLoadedId);
	}

	[TestMethod]
	public async Task When_Page_Posts_Message_Source_Is_The_Page()
	{
		var webView = await CreateWebViewAsync();
		string source = null;
		webView.CoreWebView2.WebMessageReceived += (s, e) => source = e.Source;

		Navigate(webView, "source.html", """
			<html><body><script>
				chrome.webview.postMessage('hello');
			</script></body></html>
			""");

		await TestServices.WindowHelper.WaitFor(() => source is not null, TimeoutMS);
		Assert.IsTrue(source.StartsWith(Origin, StringComparison.OrdinalIgnoreCase), $"Unexpected source '{source}'.");
	}

	[TestMethod]
	public async Task When_PostWebMessageAsString_Page_Listener_Receives_It()
	{
		var webView = await CreateWebViewAsync();
		var messages = CollectStringMessages(webView);

		Navigate(webView, "echo.html", """
			<html><body><script>
				chrome.webview.addEventListener('message', function (e) {
					chrome.webview.postMessage('echo:' + e.data);
				});
				chrome.webview.postMessage('ready');
			</script></body></html>
			""");

		await TestServices.WindowHelper.WaitFor(() => messages.Contains("ready"), TimeoutMS);

		webView.CoreWebView2.PostWebMessageAsString("ping");

		await TestServices.WindowHelper.WaitFor(() => messages.Contains("echo:ping"), TimeoutMS);
	}

	private async Task<WebView2> CreateWebViewAsync()
	{
		var webView = new WebView2
		{
			Width = 200,
			Height = 200,
		};

		TestServices.WindowHelper.WindowContent = webView;
		await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: TimeoutMS);
		await webView.EnsureCoreWebView2Async();

		webView.CoreWebView2.SetVirtualHostNameToFolderMapping(HostName, _contentFolder, CoreWebView2HostResourceAccessKind.Allow);

		return webView;
	}

	private void Navigate(WebView2 webView, string fileName, string html)
	{
		File.WriteAllText(Path.Combine(_contentFolder, fileName), html);
		webView.CoreWebView2.Navigate(Origin + fileName);
	}

	private static List<string> CollectStringMessages(WebView2 webView)
	{
		var messages = new List<string>();
		webView.CoreWebView2.WebMessageReceived += (s, e) => messages.Add(e.TryGetWebMessageAsString());
		return messages;
	}
}
