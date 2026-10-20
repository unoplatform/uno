#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_CoreWebView2_Scripting
{
	[TestMethod]
	public async Task When_Script_Added_Before_Native_It_Is_Registered_Before_First_Navigation()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeScriptingNativeWebView();

		var operation = core.AddScriptToExecuteOnDocumentCreatedAsync("window.first = 1;");
		core.NavigateToString("<html></html>");

		Assert.AreEqual(AsyncStatus.Started, operation.Status);

		core.SetNativeWebView(native);

		Assert.AreEqual("window.first = 1;", native.Scripts["script-1"]);
		CollectionAssert.AreEqual(new[] { "add:window.first = 1;", "navigate-html" }, native.Calls);
		Assert.AreEqual("script-1", await operation.AsTask());
	}

	[TestMethod]
	public async Task When_Script_Added_After_Native_Id_Round_Trips()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeScriptingNativeWebView();
		core.SetNativeWebView(native);

		var id = await core.AddScriptToExecuteOnDocumentCreatedAsync("window.a = 1;").AsTask();
		var otherId = await core.AddScriptToExecuteOnDocumentCreatedAsync("window.b = 2;").AsTask();

		Assert.AreNotEqual(id, otherId);

		core.RemoveScriptToExecuteOnDocumentCreated(id);

		CollectionAssert.AreEquivalent(new[] { otherId }, new List<string>(native.Scripts.Keys));
	}

	[TestMethod]
	public async Task When_Pending_Script_Is_Cancelled_It_Is_Not_Forwarded()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeScriptingNativeWebView();

		var operation = core.AddScriptToExecuteOnDocumentCreatedAsync("window.cancelled = 1;");
		var task = operation.AsTask();
		operation.Cancel();

		core.SetNativeWebView(native);

		await Assert.ThrowsAsync<OperationCanceledException>(() => task);
		Assert.AreEqual(0, native.Scripts.Count);
	}

	[TestMethod]
	public void When_Capability_Missing_AddScript_Throws_NotImplemented()
	{
		var core = new CoreWebView2(new FakeOwner());
		core.SetNativeWebView(new FakeNativeWebView());

		Assert.ThrowsExactly<NotImplementedException>(() => core.AddScriptToExecuteOnDocumentCreatedAsync("1"));
	}

	[TestMethod]
	public async Task When_Capability_Missing_Pending_Script_Faults_NotImplemented()
	{
		var core = new CoreWebView2(new FakeOwner());

		var task = core.AddScriptToExecuteOnDocumentCreatedAsync("1").AsTask();
		core.SetNativeWebView(new FakeNativeWebView());

		await Assert.ThrowsExactlyAsync<NotImplementedException>(() => task);
	}

	[TestMethod]
	public void When_PostWebMessage_It_Is_Forwarded()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeScriptingNativeWebView();
		core.SetNativeWebView(native);

		core.PostWebMessageAsString("hello");
		core.PostWebMessageAsJson("{\"a\":1}");

		CollectionAssert.AreEqual(new[] { "string:hello", "json:{\"a\":1}" }, native.Calls);
	}

	[TestMethod]
	public void When_Capability_Missing_Members_Report_NotImplemented()
	{
		var core = new CoreWebView2(new FakeOwner());
		core.SetNativeWebView(new FakeNativeWebView());

		var original = ApiInformation.IsFailWhenNotImplemented;
		ApiInformation.IsFailWhenNotImplemented = true;
		try
		{
			Assert.ThrowsExactly<NotImplementedException>(() => core.PostWebMessageAsString("hello"));
			Assert.ThrowsExactly<NotImplementedException>(() => core.PostWebMessageAsJson("{}"));
			Assert.ThrowsExactly<NotImplementedException>(() => core.RemoveScriptToExecuteOnDocumentCreated("id"));
			Assert.ThrowsExactly<NotImplementedException>(() => core.DOMContentLoaded += (s, e) => { });
		}
		finally
		{
			ApiInformation.IsFailWhenNotImplemented = original;
		}
	}

	[TestMethod]
	public void When_DOMContentLoaded_It_Carries_The_Navigation_Id()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeScriptingNativeWebView();
		core.SetNativeWebView(native);

		ulong startingId = 0;
		var loadedIds = new List<ulong>();
		core.NavigationStarting += (s, e) => startingId = e.NavigationId;
		core.DOMContentLoaded += (s, e) => loadedIds.Add(e.NavigationId);

		core.RaiseNavigationStarting(new Uri("https://example.com/"), out _);
		native.RaiseDOMContentLoaded();

		CollectionAssert.AreEqual(new[] { startingId }, loadedIds);
		Assert.AreNotEqual(0UL, startingId);
	}

	[TestMethod]
	public void When_Native_Replaced_DOMContentLoaded_Follows_The_New_Native()
	{
		var core = new CoreWebView2(new FakeOwner());
		var first = new FakeScriptingNativeWebView();
		var second = new FakeScriptingNativeWebView();
		var count = 0;
		core.DOMContentLoaded += (s, e) => count++;

		core.SetNativeWebView(first);
		core.SetNativeWebView(second);
		first.RaiseDOMContentLoaded();
		second.RaiseDOMContentLoaded();

		Assert.AreEqual(1, count);
	}

	[TestMethod]
	public void When_WebMessage_Has_Source()
	{
		var core = new CoreWebView2(new FakeOwner());
		CoreWebView2WebMessageReceivedEventArgs? received = null;
		core.WebMessageReceived += (s, e) => received = e;

		core.RaiseWebMessageReceived("\"hi\"", "https://0.0.0.1/index.html");

		Assert.IsNotNull(received);
		Assert.AreEqual("https://0.0.0.1/index.html", received.Source);
		Assert.AreEqual("hi", received.TryGetWebMessageAsString());
	}

	[TestMethod]
	public void When_WebMessage_Source_Not_Reported_It_Throws_NotImplemented()
	{
		var core = new CoreWebView2(new FakeOwner());
		CoreWebView2WebMessageReceivedEventArgs? received = null;
		core.WebMessageReceived += (s, e) => received = e;

		core.RaiseWebMessageReceived("\"hi\"");

		Assert.IsNotNull(received);
		Assert.ThrowsExactly<NotImplementedException>(() => _ = received.Source);
	}

	private sealed class FakeOwner : IWebView
	{
		public bool SwitchSourceBeforeNavigating => false;

		public bool IsLoaded => false;

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;
	}

	private class FakeNativeWebView : INativeWebView
	{
		public List<string> Calls { get; } = new();

		public string DocumentTitle => "";

		public void GoBack() { }

		public void GoForward() { }

		public void Stop() { }

		public void Reload() { }

		public void ProcessNavigation(Uri uri) => Calls.Add("navigate-uri");

		public void ProcessNavigation(string html) => Calls.Add("navigate-html");

		public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => Calls.Add("navigate-request");

		public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => Task.FromResult<string?>(null);

		public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => Task.FromResult<string?>(null);

		public void SetScrollingEnabled(bool isScrollingEnabled) { }
	}

	private sealed class FakeScriptingNativeWebView : FakeNativeWebView, ISupportsDocumentCreatedScripts, ISupportsPostWebMessage, ISupportsDOMContentLoaded
	{
		private int _nextId;

		public Dictionary<string, string> Scripts { get; } = new();

		public event EventHandler? DOMContentLoaded;

		public void RaiseDOMContentLoaded() => DOMContentLoaded?.Invoke(this, EventArgs.Empty);

		public Task<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		{
			Calls.Add("add:" + javaScript);
			var id = "script-" + (++_nextId);
			Scripts[id] = javaScript;
			return Task.FromResult(id);
		}

		public void RemoveScriptToExecuteOnDocumentCreated(string id) => Scripts.Remove(id);

		public void PostWebMessageAsJson(string webMessageAsJson) => Calls.Add("json:" + webMessageAsJson);

		public void PostWebMessageAsString(string webMessageAsString) => Calls.Add("string:" + webMessageAsString);
	}
}
