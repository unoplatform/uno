#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[RunsOnUIThread]
[TestClass]
public class Given_WebView2_ReviewContracts
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Cookie_Scope_Survives_Add_Read_Update_And_Delete_With_The_Original_Identity(bool domainCookie)
	{
		var webView = await CreateWebViewAsync();
		var manager = webView.CoreWebView2!.CookieManager;
		var name = $"uno_scope_{Guid.NewGuid():N}";
		var domain = domainCookie ? ".login.example.com" : "login.example.com";
		var cookie = manager.CreateCookie(name, "initial", domain, "/scope");
		cookie.IsSecure = true;
		cookie.IsHttpOnly = true;
		const string origin = "https://login.example.com/scope/page";
		const string subdomain = "https://child.login.example.com/scope/page";
		try
		{
			manager.AddOrUpdateCookie(cookie);
			var stored = await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies is [{ Value: "initial" }]);
			Assert.AreEqual(cookie.Domain, stored[0].Domain);
			Assert.IsTrue(stored[0].IsHttpOnly);
			Assert.AreEqual(domainCookie, (await manager.GetCookiesAsync(subdomain)).Any(item => item.Name == name));
			Assert.IsFalse((await manager.GetCookiesAsync("https://otherlogin.example.com/scope/page")).Any(item => item.Name == name));
			Assert.IsFalse((await manager.GetCookiesAsync("https://example.com/scope/page")).Any(item => item.Name == name));

			cookie.Value = "updated";
			manager.AddOrUpdateCookie(cookie);
			stored = await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies is [{ Value: "updated" }]);
			Assert.AreEqual(cookie.Domain, stored[0].Domain);
			Assert.AreEqual(domainCookie, (await manager.GetCookiesAsync(subdomain)).Any(item => item.Name == name));

			manager.DeleteCookie(cookie);
			await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 0);
			Assert.IsFalse((await manager.GetCookiesAsync(subdomain)).Any(item => item.Name == name));
		}
		finally
		{
			try
			{
				await DeleteScopeCookiesAsync(manager, name);
			}
			finally
			{
				Close(webView);
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Host_And_Domain_Cookies_Share_A_Name_Deletion_Keeps_Their_Identities_Distinct()
	{
		var webView = await CreateWebViewAsync();
		var manager = webView.CoreWebView2!.CookieManager;
		var name = $"uno_scope_{Guid.NewGuid():N}";
		var hostOnly = manager.CreateCookie(name, "host", "login.example.com", "/scope");
		var domain = manager.CreateCookie(name, "domain", ".login.example.com", "/scope");
		hostOnly.IsSecure = domain.IsSecure = true;
		const string origin = "https://login.example.com/scope/page";
		const string subdomain = "https://child.login.example.com/scope/page";
		try
		{
			manager.AddOrUpdateCookie(hostOnly);
			manager.AddOrUpdateCookie(domain);
			await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 2);

			manager.DeleteCookiesWithDomainAndPath(name, domain.Domain, domain.Path);
			var remaining = await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 1);
			Assert.AreEqual(hostOnly.Domain, remaining[0].Domain);
			Assert.AreEqual(hostOnly.Value, remaining[0].Value);
			Assert.IsFalse((await manager.GetCookiesAsync(subdomain)).Any(item => item.Name == name));

			manager.AddOrUpdateCookie(domain);
			await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 2);
			manager.DeleteCookie(hostOnly);
			remaining = await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 1);
			Assert.AreEqual(domain.Domain, remaining[0].Domain);
			Assert.AreEqual(domain.Value, remaining[0].Value);

			manager.DeleteCookie(domain);
			await WaitForScopeCookiesAsync(manager, origin, name, cookies => cookies.Length == 0);
		}
		finally
		{
			try
			{
				await DeleteScopeCookiesAsync(manager, name);
			}
			finally
			{
				Close(webView);
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_ContentLoading_Posts_Initialization_Messages_The_Bridge_Is_Ready()
	{
		var webView = await CreateWebViewAsync();
		try
		{
			var core = webView.CoreWebView2!;
			var posted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var messages = new List<string>();
			core.ContentLoading += (_, _) =>
			{
				try
				{
					core.PostWebMessageAsString("initialization");
					core.PostWebMessageAsJson("""{"initialized":true}""");
					posted.TrySetResult();
				}
				catch (Exception error)
				{
					posted.TrySetException(error);
				}
			};
			core.WebMessageReceived += (_, args) => messages.Add(args.WebMessageAsJson);
			webView.NavigateToString("""
				<!doctype html><html><head><script>
				chrome.webview.addEventListener('message', function (event) {
					chrome.webview.postMessage({ data: event.data, source: event.source === chrome.webview });
				});
				</script></head><body>initialization messages</body></html>
				""");

			await posted.Task.WaitAsync(TimeSpan.FromSeconds(10));
			await TestServices.WindowHelper.WaitFor(() => messages.Count == 2, 5_000);
			CollectionAssert.AreEquivalent(new[]
			{
				"""{"data":"initialization","source":true}""",
				"""{"data":{"initialized":true},"source":true}""",
			}, messages);
		}
		finally
		{
			Close(webView);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Document_Events_Are_Raised_At_Their_Actual_Phases()
	{
		var webView = await CreateWebViewAsync();
		try
		{
			var events = new List<string>();
			var core = webView.CoreWebView2!;
			core.ContentLoading += (_, _) => events.Add("content");
			core.DOMContentLoaded += (_, _) => events.Add("dom");
			core.NavigationCompleted += (_, _) => events.Add("completed");
			core.WebMessageReceived += (_, args) => events.Add(args.TryGetWebMessageAsString());

			webView.NavigateToString("""
				<!doctype html><html><head><script>
				chrome.webview.postMessage('inline');
				document.addEventListener('DOMContentLoaded', function () { chrome.webview.postMessage('page-dom'); });
				window.addEventListener('load', function () { chrome.webview.postMessage('page-load'); });
				</script></head><body>document phases</body></html>
				""");
			await TestServices.WindowHelper.WaitFor(() => events.Contains("completed"), 10_000);

			CollectionAssert.AreEqual(new[] { "content", "inline", "dom", "page-dom", "page-load", "completed" }, events);
		}
		finally
		{
			Close(webView);
		}
	}

	[TestMethod]
	[DataRow("about:blank")]
	[DataRow("data:text/html,%3Chtml%3E%3Cbody%3Euri%3C/body%3E%3C/html%3E")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Wasm_Navigates_From_String_To_Uri_Unsupported_Events_And_Messages_Are_Not_Faked(string uri)
	{
		var webView = await CreateWebViewAsync();
		try
		{
			var core = webView.CoreWebView2!;
			var completions = 0;
			var contentEvents = 0;
			core.NavigationCompleted += (_, _) => completions++;
			core.ContentLoading += (_, _) => contentEvents++;
			core.DOMContentLoaded += (_, _) => contentEvents++;
			webView.NavigateToString("<!doctype html><html><body>string</body></html>");
			await TestServices.WindowHelper.WaitFor(() => completions == 1, 10_000);
			Assert.AreEqual(2, contentEvents);

			contentEvents = 0;
			core.Navigate(uri);
			await TestServices.WindowHelper.WaitFor(() => completions == 2, 10_000);

			Assert.AreEqual(0, contentEvents);
			Assert.ThrowsExactly<NotSupportedException>(() => core.PostWebMessageAsString("unsupported"));
			Assert.ThrowsExactly<NotSupportedException>(() => core.PostWebMessageAsJson("{}"));
		}
		finally
		{
			Close(webView);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_A_User_Script_Is_Removed_The_Document_Start_Message_Bridge_Remains()
	{
		var webView = await CreateWebViewAsync();
		try
		{
			var core = webView.CoreWebView2!;
			var id = await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__removedScript = true;");
			core.RemoveScriptToExecuteOnDocumentCreated(id);
			var ready = false;
			var echoed = false;
			core.WebMessageReceived += (_, args) =>
			{
				ready |= args.WebMessageAsJson == "\"ready\"";
				echoed |= args.WebMessageAsJson == "\"echo\"";
			};
			webView.NavigateToString("""
				<!doctype html><html><head><script>
				chrome.webview.addEventListener('message', function (event) {
					if (event.data === 'ping' && event.source === chrome.webview && !window.__removedScript) {
						chrome.webview.postMessage('echo');
					}
				});
				chrome.webview.postMessage('ready');
				</script></head><body>document-start messaging</body></html>
				""");
			await TestServices.WindowHelper.WaitFor(() => ready, 10_000);

			core.PostWebMessageAsString("ping");
			await TestServices.WindowHelper.WaitFor(() => echoed, 5_000);
		}
		finally
		{
			Close(webView);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Android_Document_Start_Contracts_Are_Unsupported_They_Are_Rejected()
	{
		var webView = await CreateWebViewAsync();
		try
		{
			var core = webView.CoreWebView2!;
			Func<Task> addScript = async () => await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__mustRunAtDocumentStart = true;");
			await addScript.Should().ThrowAsync<NotSupportedException>();
			Assert.ThrowsExactly<NotSupportedException>(() => core.RemoveScriptToExecuteOnDocumentCreated("unsupported"));
			Assert.ThrowsExactly<NotSupportedException>(() => core.PostWebMessageAsString("unsupported"));
			Assert.ThrowsExactly<NotSupportedException>(() => core.PostWebMessageAsJson("{}"));

			var received = false;
			core.WebMessageReceived += (_, args) => received |= args.WebMessageAsJson == "\"native-ready\"";
			webView.NavigateToString("<html><body><script>unoWebView.postMessage(JSON.stringify('native-ready'));</script></body></html>");
			await TestServices.WindowHelper.WaitFor(() => received, 10_000);
		}
		finally
		{
			Close(webView);
		}
	}

	[TestMethod]
	[DataRow(CoreWebView2CookieSameSiteKind.Strict)]
	[DataRow(CoreWebView2CookieSameSiteKind.Lax)]
	[DataRow(CoreWebView2CookieSameSiteKind.None)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_IOS_Cookies_Are_Stored_Their_Security_Attributes_Are_Not_Weakened(CoreWebView2CookieSameSiteKind sameSite)
	{
		var webView = await CreateWebViewAsync();
		var manager = webView.CoreWebView2!.CookieManager;
		var cookie = manager.CreateCookie($"uno_{Guid.NewGuid():N}", "value", "example.com", "/scope");
		cookie.IsHttpOnly = true;
		cookie.IsSecure = true;
		cookie.SameSite = sameSite;
		try
		{
			var rejected = false;
			try
			{
				manager.AddOrUpdateCookie(cookie);
			}
			catch (NotSupportedException)
			{
				rejected = true;
			}

			if (rejected)
			{
				Assert.IsFalse((await manager.GetCookiesAsync(string.Empty)).Any(item => item.Name == cookie.Name),
					"An unsupported policy must not leave a weaker cookie in the store.");
			}
			else
			{
				CoreWebView2Cookie? actual = null;
				for (var attempt = 0; attempt < 100 && actual is null; attempt++)
				{
					actual = (await manager.GetCookiesAsync("https://example.com/scope/page")).SingleOrDefault(item => item.Name == cookie.Name);
					if (actual is null)
					{
						await Task.Delay(50);
					}
				}
				Assert.IsNotNull(actual);
				Assert.IsTrue(actual.IsHttpOnly);
				Assert.IsTrue(actual.IsSecure);
				Assert.AreEqual(sameSite, actual.SameSite);
				Assert.IsFalse((await manager.GetCookiesAsync("http://example.com/scope/page")).Any(item => item.Name == cookie.Name));
				Assert.IsFalse((await manager.GetCookiesAsync("https://example.com/other")).Any(item => item.Name == cookie.Name));
			}
		}
		finally
		{
			try
			{
				manager.DeleteCookie(cookie);
			}
			finally
			{
				Close(webView);
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include,
		RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaX11
		| RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Closed_The_Native_Presenter_Is_Empty()
	{
		var webView = await CreateWebViewAsync();
		try
		{
#if HAS_UNO
			var presenter = webView.GetNativePresenter();
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.Content);
			webView.Close();
			webView.Close();
			Assert.IsNull(presenter.Content);
			Assert.IsNull(webView.CoreWebView2);
#endif
		}
		finally
		{
			Close(webView);
		}
	}

	private static async Task<CoreWebView2Cookie[]> WaitForScopeCookiesAsync(
		CoreWebView2CookieManager manager, string uri, string name, Func<CoreWebView2Cookie[], bool> predicate)
	{
		for (var attempt = 0; attempt < 100; attempt++)
		{
			var cookies = (await manager.GetCookiesAsync(uri)).Where(cookie => cookie.Name == name).ToArray();
			if (predicate(cookies))
			{
				return cookies;
			}
			await Task.Delay(50);
		}
		throw new AssertFailedException($"Cookie '{name}' did not reach the expected state for '{uri}'.");
	}

	private static async Task DeleteScopeCookiesAsync(CoreWebView2CookieManager manager, string name)
	{
		foreach (var cookie in (await manager.GetCookiesAsync(string.Empty)).Where(cookie => cookie.Name == name))
		{
			manager.DeleteCookie(cookie);
		}
		await WaitForScopeCookiesAsync(manager, string.Empty, name, cookies => cookies.Length == 0);
	}

	private static async Task<WebView2> CreateWebViewAsync()
	{
		var webView = new WebView2 { Width = 320, Height = 240 };
		try
		{
			await UITestHelper.Load(webView);
			await webView.EnsureCoreWebView2Async();
			Assert.IsNotNull(webView.CoreWebView2);
			return webView;
		}
		catch
		{
			Close(webView);
			throw;
		}
	}

	private static void Close(WebView2 webView)
	{
		try
		{
			webView.Close();
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
