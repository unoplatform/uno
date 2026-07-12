#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[RunsOnUIThread]
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaWasm)]
public class Given_WebView2_Improvements
{
	private static readonly string MessageBridgeHtml = """
		<!doctype html>
		<html>
		<head><meta charset="utf-8"><title>WebView2 improvements</title></head>
		<body>
		<script>
			chrome.webview.addEventListener('message', function (event) {
				chrome.webview.postMessage({ kind: 'host', data: event.data });
			});
			chrome.webview.postMessage({ kind: 'ready' });
		</script>
		</body>
		</html>
		""";

	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

	[TestMethod]
	public async Task When_Factories_And_Cookie_Defaults_Are_Used()
	{
		var options = new CoreWebView2EnvironmentOptions();
		var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);
		var controllerOptions = environment.CreateCoreWebView2ControllerOptions();
		var printSettings = environment.CreatePrintSettings();
		var webView = new WebView2();
		var cookie = webView.CoreWebView2.CookieManager.CreateCookie("uno", "value", "example.com", "/");

		Assert.IsNotNull(controllerOptions);
		Assert.AreEqual(1, printSettings.Copies);
		Assert.AreEqual(1d, printSettings.ScaleFactor);
		Assert.AreEqual(1d / 2.54d, printSettings.MarginTop);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => printSettings.ScaleFactor = 3d);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => printSettings.Copies = 0);
		Assert.AreEqual(-1d, cookie.Expires);
		Assert.IsTrue(cookie.IsSession);
		Assert.AreEqual(CoreWebView2CookieSameSiteKind.Lax, cookie.SameSite);

		cookie.Expires = 0d;
		Assert.IsFalse(cookie.IsSession);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => cookie.Expires = -2d);
	}

	[TestMethod]
	public async Task When_NavigateToString_Lifecycle_And_Messaging_Are_Ordered()
	{
		var webView = await CreateWebViewAsync();
		var core = webView.CoreWebView2;
		var lifecycle = new List<string>();
		ulong? navigationId = null;
		var navigationCompleted = false;
		string? webMessage = null;
		var testNavigationRequested = false;

		core.NavigationStarting += (_, args) =>
		{
			if (testNavigationRequested && navigationId is null)
			{
				navigationId = args.NavigationId;
				lifecycle.Add("NavigationStarting");
			}
		};
		core.ContentLoading += (_, args) =>
		{
			if (args.NavigationId == navigationId)
			{
				lifecycle.Add("ContentLoading");
			}
		};
		core.DOMContentLoaded += (_, args) =>
		{
			if (args.NavigationId == navigationId)
			{
				lifecycle.Add("DOMContentLoaded");
			}
		};
		core.NavigationCompleted += (_, args) =>
		{
			if (args.NavigationId == navigationId)
			{
				lifecycle.Add("NavigationCompleted");
				navigationCompleted = true;
			}
		};
		core.WebMessageReceived += (_, args) => webMessage = args.WebMessageAsJson;

		testNavigationRequested = true;
		webView.NavigateToString(MessageBridgeHtml);
		await TestServices.WindowHelper.WaitFor(() => navigationCompleted, 10_000);
		await TestServices.WindowHelper.WaitFor(() => webMessage?.Contains("\"kind\":\"ready\"", StringComparison.Ordinal) == true, 5_000);

		CollectionAssert.AreEqual(
			new[] { "NavigationStarting", "ContentLoading", "DOMContentLoaded", "NavigationCompleted" },
			lifecycle);

		webMessage = null;
		core.PostWebMessageAsString("hello");
		await TestServices.WindowHelper.WaitFor(
			() => webMessage?.Contains("\"kind\":\"host\"", StringComparison.Ordinal) == true
				&& webMessage.Contains("\"data\":\"hello\"", StringComparison.Ordinal),
			5_000);

		webMessage = null;
		core.PostWebMessageAsJson("{\"answer\":42}");
		await TestServices.WindowHelper.WaitFor(
			() => webMessage?.Contains("\"answer\":42", StringComparison.Ordinal) == true,
			5_000);

		Assert.ThrowsExactly<ArgumentException>(() => core.PostWebMessageAsJson("not-json"));
		core.Settings.IsWebMessageEnabled = false;
		Assert.ThrowsExactly<UnauthorizedAccessException>(() => core.PostWebMessageAsString("blocked"));
	}

	[TestMethod]
	public async Task When_CoreWebView2_Is_Initialized_The_Event_Reflects_Native_Completion()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		var initializedCount = 0;
		CoreWebView2InitializedEventArgs? initializedArgs = null;
		webView.CoreWebView2Initialized += (_, args) =>
		{
			initializedCount++;
			initializedArgs = args;
		};

		var initialization = webView.EnsureCoreWebView2Async();
		Assert.AreEqual(0, initializedCount);

		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await initialization;

		Assert.AreEqual(1, initializedCount);
		Assert.IsNotNull(initializedArgs);
		Assert.IsNull(initializedArgs.Exception);
		await webView.EnsureCoreWebView2Async();
		Assert.AreEqual(1, initializedCount);
	}

	[TestMethod]
	public async Task When_Blank_WebView2_Is_Loaded_Initialization_Is_Not_Implicit()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		var initializedCount = 0;
		webView.CoreWebView2Initialized += (_, _) => initializedCount++;
		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;

		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0, initializedCount);
		Assert.ThrowsExactly<InvalidOperationException>(() => webView.ExecuteScriptAsync("1 + 1"));
		await webView.EnsureCoreWebView2Async();
		Assert.AreEqual(1, initializedCount);
	}

	[TestMethod]
	public async Task When_Source_Is_Set_Twice_Before_Load_Only_The_Latest_Is_Navigated()
	{
		static Uri CreateDataUri(string content) =>
			new($"data:text/html;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content))}");

		var firstSource = CreateDataUri("<html><body>first</body></html>");
		var secondSource = CreateDataUri("<html><body>second</body></html>");
		var webView = new WebView2 { Width = 320, Height = 240 };
		var initializedCount = 0;
		var navigatedUris = new List<string?>();
		var latestNavigationCompleted = false;
		webView.CoreWebView2Initialized += (_, _) => initializedCount++;
		webView.NavigationStarting += (_, args) => navigatedUris.Add(args.Uri);
		webView.NavigationCompleted += (_, args) =>
			latestNavigationCompleted |= args.Uri == secondSource;

		webView.Source = firstSource;
		webView.Source = secondSource;
		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitFor(() => latestNavigationCompleted, 10_000);

		Assert.AreEqual(1, initializedCount);
		Assert.AreEqual(secondSource, webView.Source);
		Assert.IsFalse(navigatedUris.Contains(firstSource.AbsoluteUri));
		Assert.AreEqual(1, navigatedUris.Count(uri => uri == secondSource.AbsoluteUri));
	}

	[TestMethod]
	public async Task When_Concurrent_Ensure_Uses_The_First_Environment()
	{
		var firstEnvironment = await CoreWebView2Environment.CreateAsync();
		var secondEnvironment = await CoreWebView2Environment.CreateAsync();
		var webView = new WebView2 { Width = 320, Height = 240 };
		var initializedCount = 0;
		webView.CoreWebView2Initialized += (_, _) => initializedCount++;

		var firstInitialization = webView.EnsureCoreWebView2Async(firstEnvironment);
		var secondInitialization = webView.EnsureCoreWebView2Async(secondEnvironment);
		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await firstInitialization;
		await secondInitialization;

		Assert.AreSame(firstEnvironment, webView.CoreWebView2.Environment);
		Assert.AreEqual(1, initializedCount);
	}

	[TestMethod]
	public async Task When_ExecuteScript_Returns_WebView2_Json()
	{
		var webView = await CreateWebViewAsync();
		var completed = false;
		webView.NavigationCompleted += (_, _) => completed = true;
		webView.NavigateToString("<html><body></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completed, 10_000);

		Assert.AreEqual("2", await webView.ExecuteScriptAsync("1 + 1"));
		Assert.AreEqual("\"hello \\\"Uno\\\"\"", await webView.ExecuteScriptAsync("'hello \\\"Uno\\\"'"));
		Assert.AreEqual("{\"answer\":42}", await webView.ExecuteScriptAsync("({ answer: 42 })"));
		Assert.AreEqual("null", await webView.ExecuteScriptAsync("undefined"));
	}

	[TestMethod]
	public async Task When_WebView2_Is_Closed_It_Cannot_Be_Reused()
	{
		var webView = await CreateWebViewAsync();
		var core = webView.CoreWebView2!;
		webView.Close();
		webView.Close();

		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CanGoForward);
		Assert.IsNull(webView.CoreWebView2);
		Assert.ThrowsExactly<ObjectDisposedException>(() => webView.Source = new Uri("https://example.com/"));
		Assert.ThrowsExactly<ObjectDisposedException>(() => core.Navigate("https://example.com/"));
		Assert.ThrowsExactly<ObjectDisposedException>(() => core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All));
		Assert.ThrowsExactly<ObjectDisposedException>(() => core.RemoveWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All));
		Func<Task> ensure = async () => await webView.EnsureCoreWebView2Async();
		await ensure.Should().ThrowAsync<ObjectDisposedException>();
	}

	[TestMethod]
	public async Task When_WebView2_Is_Closed_During_Initialization_Ensure_Faults()
	{
		var webView = new WebView2();
		var initializedCount = 0;
		CoreWebView2InitializedEventArgs? initializedArgs = null;
		webView.CoreWebView2Initialized += (_, args) =>
		{
			initializedCount++;
			initializedArgs = args;
		};

		var initialization = webView.EnsureCoreWebView2Async();
		webView.Close();

		Func<Task> awaitInitialization = async () => await initialization;
		await awaitInitialization.Should().ThrowAsync<ObjectDisposedException>();
		Assert.AreEqual(1, initializedCount);
		Assert.IsInstanceOfType<ObjectDisposedException>(initializedArgs?.Exception);
		Assert.IsNull(webView.CoreWebView2);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_MacOS_InPrivate_Environment_Is_Applied_Before_Initialization()
	{
		var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, new CoreWebView2EnvironmentOptions());
		var controllerOptions = environment.CreateCoreWebView2ControllerOptions();
		controllerOptions.IsInPrivateModeEnabled = true;
		var webView = new WebView2 { Width = 320, Height = 240 };
		var initialization = webView.EnsureCoreWebView2Async(environment, controllerOptions);
		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await initialization;

		Assert.AreSame(environment, webView.CoreWebView2.Environment);
		var otherEnvironment = await CoreWebView2Environment.CreateAsync();
		Func<Task> reinitialize = async () => await webView.EnsureCoreWebView2Async(otherEnvironment);
		await reinitialize.Should().ThrowAsync<ArgumentException>();
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_DocumentCreatedScript_Is_Added_And_Removed()
	{
		var webView = await CreateWebViewAsync();
		var core = webView.CoreWebView2;
		var completionCount = 0;
		core.NavigationCompleted += (_, _) => completionCount++;

		var scriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__unoDocumentStart = 'injected';");
		webView.NavigateToString("<html><body></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completionCount >= 1, 10_000);
		Assert.AreEqual("\"injected\"", await core.ExecuteScriptAsync("window.__unoDocumentStart"));

		core.RemoveScriptToExecuteOnDocumentCreated(scriptId);
		webView.NavigateToString("<html><body></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completionCount >= 2, 10_000);
		Assert.AreEqual("\"undefined\"", await core.ExecuteScriptAsync("typeof window.__unoDocumentStart"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_UserAgent_And_ScriptEnabled_Are_Applied()
	{
		var webView = await CreateWebViewAsync();
		var core = webView.CoreWebView2;
		var completionCount = 0;
		core.NavigationCompleted += (_, _) => completionCount++;
		core.Settings.IsZoomControlEnabled = false;
		Assert.IsFalse(core.Settings.IsZoomControlEnabled);

		core.Settings.UserAgent = "Uno-WebView2-Test";
		webView.NavigateToString("<html><body></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completionCount >= 1, 10_000);
		Assert.AreEqual("\"Uno-WebView2-Test\"", await core.ExecuteScriptAsync("navigator.userAgent"));

		core.Settings.IsScriptEnabled = false;
		webView.NavigateToString("<html><body><script>window.__unoInlineScript = true;</script></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completionCount >= 2, 10_000);
		Assert.AreEqual("\"undefined\"", await core.ExecuteScriptAsync("typeof window.__unoInlineScript"));

		core.Settings.IsScriptEnabled = true;
		webView.NavigateToString("<html><body><script>window.__unoInlineScript = true;</script></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completionCount >= 3, 10_000);
		Assert.AreEqual("\"true\"", await core.ExecuteScriptAsync("String(window.__unoInlineScript)"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Cookie_Is_RoundTripped_And_Deleted()
	{
		var webView = await CreateWebViewAsync();
		var manager = webView.CoreWebView2.CookieManager;
		var name = $"uno_{Guid.NewGuid():N}";
		var cookie = manager.CreateCookie(name, "cookie-value", "example.com", "/scope");
		cookie.IsSecure = true;
		cookie.IsHttpOnly = true;
		cookie.SameSite = CoreWebView2CookieSameSiteKind.Strict;
		cookie.Expires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

		manager.AddOrUpdateCookie(cookie);
		var matching = await manager.GetCookiesAsync("https://example.com/scope/page");
		var actual = matching.Single(item => item.Name == name);

		Assert.AreEqual("cookie-value", actual.Value);
		Assert.IsTrue(actual.IsSecure);
		Assert.IsTrue(actual.IsHttpOnly);
		Assert.IsFalse(actual.IsSession);
		Assert.AreEqual(CoreWebView2CookieSameSiteKind.Strict, actual.SameSite);
		Assert.IsFalse((await manager.GetCookiesAsync("http://example.com/scope/page")).Any(item => item.Name == name));
		Assert.IsFalse((await manager.GetCookiesAsync("https://example.com/other")).Any(item => item.Name == name));

		manager.DeleteCookie(cookie);
		Assert.IsFalse((await manager.GetCookiesAsync("https://example.com/scope/page")).Any(item => item.Name == name));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_PrintToPdfStream_Returns_A_Pdf()
	{
		var webView = await CreateWebViewAsync();
		var completed = false;
		webView.NavigationCompleted += (_, _) => completed = true;
		webView.NavigateToString("<html><body><h1>Uno PDF</h1></body></html>");
		await TestServices.WindowHelper.WaitFor(() => completed, 10_000);

		var customSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
		customSettings.ScaleFactor = 1.5d;
		Func<Task> customPrint = async () => await webView.CoreWebView2.PrintToPdfStreamAsync(customSettings);
		await customPrint.Should().ThrowAsync<NotSupportedException>();

		var unsupportedSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
		unsupportedSettings.Copies = 2;
		Func<Task> unsupportedPrint = async () => await webView.CoreWebView2.PrintToPdfStreamAsync(unsupportedSettings);
		await unsupportedPrint.Should().ThrowAsync<NotSupportedException>();

		using var pdf = await webView.CoreWebView2.PrintToPdfStreamAsync(null);
		using var stream = pdf.AsStreamForRead();
		var signature = new byte[5];
		var bytesRead = await stream.ReadAsync(signature, 0, signature.Length);

		Assert.AreEqual(signature.Length, bytesRead);
		CollectionAssert.AreEqual(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' }, signature);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_MacOS_Fragment_Navigation_Is_Cancelled_No_Completion_Is_Raised()
	{
		var filePath = Path.Combine(Path.GetTempPath(), $"uno-webview2-fragment-{Guid.NewGuid():N}.html");
		await File.WriteAllTextAsync(filePath, "<html><body><div id='target'>target</div></body></html>");

		try
		{
			var webView = await CreateWebViewAsync();
			var initialNavigationCompleted = false;
			webView.NavigationCompleted += (_, _) => initialNavigationCompleted = true;
			webView.CoreWebView2.Navigate(new Uri(filePath).AbsoluteUri);
			await TestServices.WindowHelper.WaitFor(() => initialNavigationCompleted, 10_000);
			var fragmentUrl = $"{webView.Source.AbsoluteUri.Split('#')[0]}#target";

			ulong? cancelledNavigation = null;
			var cancelledNavigationCompleted = false;
			webView.NavigationStarting += (_, args) =>
			{
				if (args.Uri == fragmentUrl)
				{
					cancelledNavigation = args.NavigationId;
					args.Cancel = true;
				}
			};
			webView.NavigationCompleted += (_, args) =>
				cancelledNavigationCompleted |= args.NavigationId == cancelledNavigation;

			webView.CoreWebView2.Navigate(fragmentUrl);
			await TestServices.WindowHelper.WaitFor(() => cancelledNavigation is not null, 5_000);
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(cancelledNavigationCompleted);
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Unsupported_Browser_Capabilities_Are_Explicit()
	{
		var webView = await CreateWebViewAsync();
		var core = webView.CoreWebView2;
		var cookie = core.CookieManager.CreateCookie("uno", "value", "example.com", "/");

		Action addCookie = () => core.CookieManager.AddOrUpdateCookie(cookie);
		addCookie.Should().Throw<NotSupportedException>();

		Func<Task> getCookies = async () => await core.CookieManager.GetCookiesAsync("https://example.com/");
		await getCookies.Should().ThrowAsync<NotSupportedException>();

		Func<Task> addScript = async () => await core.AddScriptToExecuteOnDocumentCreatedAsync("window.test = true;");
		await addScript.Should().ThrowAsync<NotSupportedException>();
		((Action)(() => core.RemoveScriptToExecuteOnDocumentCreated("unsupported"))).Should().Throw<NotSupportedException>();

		Func<Task> printPdf = async () => await core.PrintToPdfStreamAsync(null);
		await printPdf.Should().ThrowAsync<NotSupportedException>();

		((Action)(() => core.Settings.UserAgent = "custom")).Should().Throw<NotSupportedException>();
		((Action)(() => core.Settings.IsScriptEnabled = false)).Should().Throw<NotSupportedException>();
		((Action)(() => core.Settings.IsZoomControlEnabled = false)).Should().Throw<NotSupportedException>();
		Assert.IsNull(core.Settings.UserAgent);
		Assert.IsTrue(core.Settings.IsScriptEnabled);
		Assert.IsTrue(core.Settings.IsZoomControlEnabled);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Custom_Environment_Is_Rejected()
	{
		var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, "custom-profile", null);
		var webView = new WebView2();
		Func<Task> initialize = async () => await webView.EnsureCoreWebView2Async(environment);

		await initialize.Should().ThrowAsync<NotSupportedException>();
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Navigation_Is_Cancelled_No_Completion_Is_Raised()
	{
		var webView = await CreateWebViewAsync();
		ulong? cancelledNavigation = null;
		var completed = false;
		webView.NavigationStarting += (_, args) =>
		{
			if (args.Uri?.StartsWith("data:", StringComparison.OrdinalIgnoreCase) == true)
			{
				cancelledNavigation = args.NavigationId;
				args.Cancel = true;
			}
		};
		webView.NavigationCompleted += (_, args) => completed |= args.NavigationId == cancelledNavigation;

		webView.NavigateToString("<html><body>cancelled</body></html>");
		await TestServices.WindowHelper.WaitFor(() => cancelledNavigation is not null, 5_000);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(completed);
	}

	private static async Task<WebView2> CreateWebViewAsync()
	{
		var webView = new WebView2
		{
			Width = 320,
			Height = 240,
		};
		var border = new Border { Child = webView };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await webView.EnsureCoreWebView2Async();
		await TestServices.WindowHelper.WaitForIdle();
		return webView;
	}
}
