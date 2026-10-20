#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_ScriptBridgeWebViewCapabilities
{
	private const string Channel = "window.webkit.messageHandlers.unoWebView.postMessage";

	[TestMethod]
	public void When_Bridge_Created_The_Channel_Is_Called_As_A_Method()
	{
		var script = WebViewMessageBridge.CreateScript(Channel, reportLifecycle: true);

		StringAssert.Contains(script, Channel + "(m)");
		StringAssert.Contains(script, "__uno");
		StringAssert.Contains(script, "\"@@uno-webview/lifecycle/\"");
		Assert.IsFalse(script.Contains("__UNO_", StringComparison.Ordinal));
		Assert.ThrowsExactly<ArgumentException>(() => WebViewMessageBridge.CreateScript(" ", true));
	}

	[TestMethod]
	[DataRow("plain", "\"plain\"")]
	[DataRow("quote\" back\\slash", "\"quote\\\" back\\\\slash\"")]
	[DataRow("line\nbreak\r\ttab\u0001", "\"line\\nbreak\\r\\ttab\\u0001\"")]
	[DataRow("a\u2028b\u2029c", "\"a\\u2028b\\u2029c\"")]
	[DataRow("</script><!--'&", "\"\\u003C/script\\u003E\\u003C!--\\u0027\\u0026\"")]
	[DataRow("\uD83D\uDE00", "\"\\uD83D\\uDE00\"")]
	[DataRow("\u00E9\u007F", "\"\\u00E9\\u007F\"")]
	public void When_String_Literal_Encoded(string value, string expected)
		=> Assert.AreEqual(expected, WebViewMessageBridge.ToJavaScriptStringLiteral(value));

	[TestMethod]
	public void When_String_Literal_Encoded_Lone_Surrogates_Are_Kept()
	{
		// Not a DataRow: attribute strings are stored as UTF-8, which cannot hold a lone surrogate.
		Assert.AreEqual("\"lone\\uD800 \\uDC00\"", WebViewMessageBridge.ToJavaScriptStringLiteral("lone\uD800 \uDC00"));
	}

	[TestMethod]
	[DataRow("hello")]
	[DataRow("a\u2028b</script>\"\\\uD83D\uDE00\u0000")]
	public void When_String_Literal_Encoded_It_Is_Json_That_Round_Trips(string value)
		=> Assert.AreEqual(value, JsonSerializer.Deserialize<string>(WebViewMessageBridge.ToJavaScriptStringLiteral(value)));

	[TestMethod]
	[DataRow("@@uno-webview/lifecycle/DOMContentLoaded", true, (int)WebViewBridgeLifecycleEvent.DOMContentLoaded)]
	[DataRow("@@uno-webview/lifecycle/SomethingNew", true, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow("@@uno-webview/lifecycle/", true, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow("\"@@uno-webview/lifecycle/DOMContentLoaded\"", false, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow("@@UNO-WEBVIEW/lifecycle/DOMContentLoaded", false, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow("{\"type\":\"DOMContentLoaded\"}", false, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow("", false, (int)WebViewBridgeLifecycleEvent.Unknown)]
	[DataRow(null, false, (int)WebViewBridgeLifecycleEvent.Unknown)]
	public void When_Parsing_Lifecycle(string? message, bool expected, int expectedKind)
	{
		Assert.AreEqual(expected, WebViewMessageBridge.TryParseLifecycle(message, out var kind));
		Assert.AreEqual((WebViewBridgeLifecycleEvent)expectedKind, kind);
	}

	[TestMethod]
	public async Task When_DocumentStart_Scripts_Are_Ordered_With_Ids_And_Bridge_First()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart);

		bridge.EnsureDocumentStartScriptsInstalled();
		bridge.EnsureDocumentStartScriptsInstalled();
		var first = await bridge.AddScriptToExecuteOnDocumentCreatedAsync("a()", CancellationToken.None);
		var second = await bridge.AddScriptToExecuteOnDocumentCreatedAsync("b()", CancellationToken.None);

		Assert.AreEqual("1", first);
		Assert.AreEqual("2", second);
		Assert.AreEqual(3, bridge.Applied.Count);
		CollectionAssert.AreEqual(new[] { bridge.BridgeScript }, bridge.Applied[0]);
		CollectionAssert.AreEqual(new[] { bridge.BridgeScript, "a()", "b()" }, bridge.Applied[^1]);

		bridge.RemoveScriptToExecuteOnDocumentCreated(first);
		bridge.RemoveScriptToExecuteOnDocumentCreated("unknown");

		Assert.AreEqual(4, bridge.Applied.Count);
		CollectionAssert.AreEqual(new[] { bridge.BridgeScript, "b()" }, bridge.Applied[^1]);
		Assert.AreEqual("3", await bridge.AddScriptToExecuteOnDocumentCreatedAsync("c()", CancellationToken.None));
	}

	[TestMethod]
	public void When_DocumentStart_Without_Override_It_Throws()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart, overrideApply: false);

		Assert.ThrowsExactly<InvalidOperationException>(() => bridge.EnsureDocumentStartScriptsInstalled());
	}

	[TestMethod]
	public async Task When_Script_Queued_Before_Native_It_Is_Installed_With_The_Bridge_Before_First_Navigation()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeBridgeNativeWebView();

		var operation = core.AddScriptToExecuteOnDocumentCreatedAsync("window.first = 1;");
		core.NavigateToString("<html></html>");
		core.SetNativeWebView(native);

		Assert.AreEqual("1", await operation.AsTask());
		CollectionAssert.AreEqual(new[] { "apply:2", "navigate-html" }, native.Calls);
		CollectionAssert.AreEqual(new[] { native.BridgeScript, "window.first = 1;" }, native.Applied[^1]);
	}

	[TestMethod]
	public void When_PostWebMessageAsString_The_Payload_Is_Escaped()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart);

		bridge.PostWebMessageAsString("a\u2028b</script>\"\\\uD83D\uDE00");

		var script = bridge.Evaluated.Single();
		Assert.AreEqual(WebViewMessageBridge.CreateDispatchScript(false, "a\u2028b</script>\"\\\uD83D\uDE00"), script);
		StringAssert.Contains(script, "u.dispatch(false, \"a\\u2028b\\u003C/script\\u003E\\\"\\\\\\uD83D\\uDE00\")");
		Assert.IsFalse(script.Contains('\u2028'));
		Assert.IsFalse(script.Contains("</script", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public void When_PostWebMessageAsJson_It_Is_Passed_As_A_String_To_Parse()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart);

		bridge.PostWebMessageAsJson("{\"a\":[1,\"</script>\"]}");

		StringAssert.Contains(bridge.Evaluated.Single(), "u.dispatch(true, \"{\\\"a\\\":[1,\\\"\\u003C/script\\u003E\\\"]}\")");
	}

	[TestMethod]
	public void When_PostWebMessageAsJson_Is_Invalid_It_Throws_ArgumentException()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart);

		Assert.ThrowsExactly<ArgumentException>(() => bridge.PostWebMessageAsJson("{not json"));
		Assert.ThrowsExactly<ArgumentException>(() => bridge.PostWebMessageAsJson("1); alert(1"));
		Assert.AreEqual(0, bridge.Evaluated.Count);
	}

	[TestMethod]
	public void When_Evaluation_Fails_PostWebMessage_Does_Not_Throw()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart) { FailEvaluation = true };

		bridge.PostWebMessageAsString("x");
	}

	[TestMethod]
	public void When_Lifecycle_Message_It_Is_Consumed_And_Raises_DOMContentLoaded()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.DocumentStart);
		var count = 0;
		bridge.DOMContentLoaded += (s, e) => count++;

		Assert.IsTrue(bridge.TryHandleBridgeMessage("@@uno-webview/lifecycle/DOMContentLoaded"));
		Assert.IsTrue(bridge.TryHandleBridgeMessage("@@uno-webview/lifecycle/Future"));
		Assert.IsFalse(bridge.TryHandleBridgeMessage("\"hello\""));

		Assert.AreEqual(1, count);
	}

	[TestMethod]
	public void When_Lifecycle_Message_CoreWebView2_Raises_DOMContentLoaded_With_Navigation_Id()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeBridgeNativeWebView();
		native.Attach(core);
		core.SetNativeWebView(native);
		ulong startingId = 0;
		var loaded = new List<ulong>();
		var messages = new List<string>();
		core.NavigationStarting += (s, e) => startingId = e.NavigationId;
		core.DOMContentLoaded += (s, e) => loaded.Add(e.NavigationId);
		core.WebMessageReceived += (s, e) => messages.Add(e.WebMessageAsJson);

		core.RaiseNavigationStarting(new Uri("https://example.com/"), out _);
		native.ReceiveFromPage("@@uno-webview/lifecycle/DOMContentLoaded");
		native.ReceiveFromPage("\"hello\"");

		CollectionAssert.AreEqual(new[] { startingId }, loaded);
		CollectionAssert.AreEqual(new[] { "\"hello\"" }, messages);
	}

	[TestMethod]
	public async Task When_Late_Scripts_Run_In_Order_Then_DOMContentLoaded()
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.Late) { FailOn = "bad()" };
		var events = new List<string>();
		bridge.DOMContentLoaded += (s, e) => events.Add("loaded:" + bridge.Evaluated.Count);

		bridge.EnsureDocumentStartScriptsInstalled();
		await bridge.AddScriptToExecuteOnDocumentCreatedAsync("bad()", CancellationToken.None);
		await bridge.AddScriptToExecuteOnDocumentCreatedAsync("good()", CancellationToken.None);
		await bridge.OnDocumentReadyAsync(CancellationToken.None);

		Assert.AreEqual(0, bridge.Applied.Count);
		CollectionAssert.AreEqual(new[] { bridge.BridgeScript, "bad()", "good()" }, bridge.Evaluated);
		CollectionAssert.AreEqual(new[] { "loaded:3" }, events);
		StringAssert.Contains(bridge.BridgeScript, "if (false &&");
	}

	[TestMethod]
	[DataRow("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body></body></html>", "<!DOCTYPE html><html><head>#<meta charset=\"utf-8\"></head><body></body></html>")]
	[DataRow("<HTML lang=en><HEAD data-x=\"1\"><title>t</title></HEAD></HTML>", "<HTML lang=en><HEAD data-x=\"1\">#<title>t</title></HEAD></HTML>")]
	[DataRow("<html><header>h</header></html>", "<html>#<header>h</header></html>")]
	[DataRow("\uFEFF <!doctype html><p>x", "\uFEFF <!doctype html>#<p>x")]
	[DataRow("<p>fragment</p>", "#<p>fragment</p>")]
	[DataRow("<html><script>first()</script><head></head></html>", "<html>#<script>first()</script><head></head></html>")]
	[DataRow("<!doctype html><SCRIPT src=\"a.js\"></SCRIPT><p>x", "<!doctype html>#<SCRIPT src=\"a.js\"></SCRIPT><p>x")]
	[DataRow("<html><head><script>x()</script></head></html>", "<html><head>#<script>x()</script></head></html>")]
	[DataRow("<html><scripts></scripts><head></head></html>", "<html><scripts></scripts><head>#</head></html>")]
	[DataRow("", "#")]
	public async Task When_BeforeParse_Scripts_Are_Inserted_Ahead_Of_Content(string html, string expected)
	{
		var bridge = new FakeBridge(WebViewScriptInjectionMode.BeforeParse);
		await bridge.AddScriptToExecuteOnDocumentCreatedAsync("var s = '</script><!--';", CancellationToken.None);

		var result = bridge.InjectIntoHtml(html);

		var scripts = "<script>" + ScriptBridgeWebViewCapabilities.EscapeForInlineScript(bridge.BridgeScript) + "</script>"
			+ "<script>var s = '<\\/script><\\!--';</script>";
		Assert.AreEqual(expected.Replace("#", scripts, StringComparison.Ordinal), result);
		Assert.AreEqual(0, bridge.Applied.Count);
	}

	private sealed class FakeOwner : IWebView
	{
		public bool SwitchSourceBeforeNavigating => false;

		public bool IsLoaded => false;

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;
	}

	private class FakeBridge(WebViewScriptInjectionMode mode, bool overrideApply = true) : ScriptBridgeWebViewCapabilities(Channel, mode)
	{
		public List<string> Calls { get; } = new();

		public List<List<string>> Applied { get; } = new();

		public List<string> Evaluated { get; } = new();

		public bool FailEvaluation { get; set; }

		public string? FailOn { get; set; }

		protected override Task<string?> EvaluateScriptAsync(string script, CancellationToken ct)
		{
			Evaluated.Add(script);
			return FailEvaluation || script == FailOn
				? Task.FromException<string?>(new InvalidOperationException("evaluation failed"))
				: Task.FromResult<string?>(null);
		}

		protected override void ApplyDocumentStartScripts(IReadOnlyList<string> scripts)
		{
			if (!overrideApply)
			{
				base.ApplyDocumentStartScripts(scripts);
			}

			Calls.Add("apply:" + scripts.Count);
			Applied.Add(new List<string>(scripts));
		}
	}

	private sealed class FakeBridgeNativeWebView() : FakeBridge(WebViewScriptInjectionMode.DocumentStart), INativeWebView
	{
		private CoreWebView2? _core;

		public string DocumentTitle => "";

		public void ReceiveFromPage(string message)
		{
			if (!TryHandleBridgeMessage(message))
			{
				_core!.RaiseWebMessageReceived(message);
			}
		}

		public void GoBack() { }

		public void GoForward() { }

		public void Stop() { }

		public void Reload() { }

		public void ProcessNavigation(Uri uri) => Calls.Add("navigate-uri");

		public void ProcessNavigation(string html) => Calls.Add("navigate-html");

		public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => Calls.Add("navigate-request");

		public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => EvaluateScriptAsync(script, token);

		public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => Task.FromResult<string?>(null);

		public void SetScrollingEnabled(bool isScrollingEnabled) { }

		public void Attach(CoreWebView2 core) => _core = core;
	}
}
