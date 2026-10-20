#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_CoreWebView2_VirtualHosts
{
	[TestMethod]
	public void When_Capability_Navigate_Sends_Native_Url_And_Source_Stays_Public()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeCustomSchemeNativeWebView();
		var sources = new List<string>();
		core.SourceChanged += (s, e) => sources.Add(core.Source);

		core.Navigate("https://0.0.0.1/index.html?x=1#top");
		core.SetNativeWebView(native);

		CollectionAssert.AreEqual(new[] { "uno-app://0.0.0.1/index.html?x=1#top" }, native.NavigatedUris);
		Assert.AreSame(core.VirtualHostMap, native.Map);

		core.RaiseNavigationCompleted(new Uri("uno-app://0.0.0.1/index.html?x=1#top"), true, 200, CoreWebView2WebErrorStatus.Unknown);

		Assert.AreEqual("https://0.0.0.1/index.html?x=1#top", core.Source);
		CollectionAssert.AreEqual(new[] { "https://0.0.0.1/index.html?x=1#top" }, sources);
	}

	[TestMethod]
	public void When_Capability_Navigation_Events_Show_Public_Urls()
	{
		var core = new CoreWebView2(new FakeOwner());
		core.SetNativeWebView(new FakeCustomSchemeNativeWebView());
		string? startingUri = null;
		Uri? completedUri = null;
		core.NavigationStarting += (s, e) => startingUri = e.Uri;
		core.NavigationCompleted += (s, e) => completedUri = e.Uri;

		core.RaiseNavigationStarting(new Uri("uno-app://0.0.0.1/page?q=1#f"), out _);
		core.RaiseNavigationCompleted(new Uri("uno-app://0.0.0.1/page?q=1#f"), true, 200, CoreWebView2WebErrorStatus.Unknown);

		Assert.AreEqual("https://0.0.0.1/page?q=1#f", startingUri);
		Assert.AreEqual(new Uri("https://0.0.0.1/page?q=1#f"), completedUri);
	}

	[TestMethod]
	public void When_Capability_WebMessage_Source_Is_Public()
	{
		var core = new CoreWebView2(new FakeOwner());
		core.SetNativeWebView(new FakeCustomSchemeNativeWebView());
		string? source = null;
		core.WebMessageReceived += (s, e) => source = e.Source;

		core.RaiseWebMessageReceived("\"hi\"", "uno-app://0.0.0.1/index.html");

		Assert.AreEqual("https://0.0.0.1/index.html", source);
	}

	[TestMethod]
	public async Task When_Capability_WebResourceRequested_Shows_Public_Uri_And_Forwards_Response()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeCustomSchemeNativeWebView();
		core.SetNativeWebView(native);
		var environment = await CoreWebView2Environment.CreateAsync().AsTask();
		var response = environment.CreateWebResourceResponse(null, 200, "OK", "Content-Type: text/html");
		CoreWebView2WebResourceRequestedEventArgs? received = null;
		core.WebResourceRequested += (s, e) =>
		{
			received = e;
			Assert.AreEqual("https://0.0.0.1/_framework/blazor.webview.js?v=2", e.Request.Uri);
			e.Response = response;
		};

		var nativeArgs = new FakeWebResourceRequestedEventArgs("uno-app://0.0.0.1/_framework/blazor.webview.js?v=2");
		native.RaiseWebResourceRequested(nativeArgs);

		Assert.IsNotNull(received);
		Assert.AreSame(nativeArgs, received.NativeArgs);
		Assert.AreSame(response, received.Response);
		Assert.IsNotNull(nativeArgs.Response);
		Assert.AreEqual(200, nativeArgs.Response.StatusCode);

		received.Request.Uri = "https://0.0.0.1/other";
		Assert.AreEqual("uno-app://0.0.0.1/other", nativeArgs.Request.Uri);
	}

	[TestMethod]
	public void When_Capability_Filters_Are_Translated()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeCustomSchemeNativeWebView();

		core.AddWebResourceRequestedFilter("https://0.0.0.1/*", CoreWebView2WebResourceContext.All);
		core.SetNativeWebView(native);
		core.AddWebResourceRequestedFilter("https://example.com/*", CoreWebView2WebResourceContext.All);
		core.RemoveWebResourceRequestedFilter("https://0.0.0.1/*", CoreWebView2WebResourceContext.All);

		CollectionAssert.AreEqual(
			new[] { "add:uno-app://0.0.0.1/*", "add:https://example.com/*", "remove:uno-app://0.0.0.1/*" },
			native.FilterCalls);
	}

	[TestMethod]
	public void When_Capability_Folder_Mapping_Host_Is_Virtual_And_In_The_Map()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakeCustomSchemeNativeWebView();
		core.SetNativeWebView(native);

		core.SetVirtualHostNameToFolderMapping("AppAssets.Example", "wwwroot", CoreWebView2HostResourceAccessKind.Allow);
		core.Navigate("https://appassets.example/index.html");

		Assert.AreEqual("uno-app://appassets.example/index.html", native.NavigatedUris[^1]);
		Assert.IsTrue(native.Map!.TryGetFolder("appassets.example", out var folder));
		Assert.AreEqual("wwwroot", folder);
		Assert.AreEqual("wwwroot", core.HostToFolderMap["appassets.example"]);

		core.ClearVirtualHostNameToFolderMapping("appassets.example");
		core.Navigate("https://appassets.example/index.html");

		Assert.AreEqual("https://appassets.example/index.html", native.NavigatedUris[^1]);
	}

	[TestMethod]
	public void When_No_Capability_Urls_Are_Unchanged()
	{
		var core = new CoreWebView2(new FakeOwner());
		var native = new FakePlainNativeWebView();
		core.SetNativeWebView(native);
		string? startingUri = null;
		string? messageSource = null;
		CoreWebView2WebResourceRequestedEventArgs? received = null;
		core.NavigationStarting += (s, e) => startingUri = e.Uri;
		core.WebMessageReceived += (s, e) => messageSource = e.Source;
		core.WebResourceRequested += (s, e) => received = e;

		var target = new Uri("https://0.0.0.1/index.html");
		core.Navigate(target.AbsoluteUri);
		core.AddWebResourceRequestedFilter("https://0.0.0.1/*", CoreWebView2WebResourceContext.All);
		core.RaiseNavigationStarting(new Uri("uno-app://0.0.0.1/a"), out _);
		core.RaiseNavigationCompleted(new Uri("uno-app://0.0.0.1/a"), true, 200, CoreWebView2WebErrorStatus.Unknown);
		core.RaiseWebMessageReceived("\"hi\"", "uno-app://0.0.0.1/a");
		var nativeArgs = new FakeWebResourceRequestedEventArgs("uno-app://0.0.0.1/a");
		var args = new CoreWebView2WebResourceRequestedEventArgs(nativeArgs);
		native.RaiseWebResourceRequested(args);

		CollectionAssert.AreEqual(new[] { "https://0.0.0.1/index.html" }, native.NavigatedUris);
		CollectionAssert.AreEqual(new[] { "add:https://0.0.0.1/*" }, native.FilterCalls);
		Assert.AreEqual("uno-app://0.0.0.1/a", startingUri);
		Assert.AreEqual("uno-app://0.0.0.1/a", core.Source);
		Assert.AreEqual("uno-app://0.0.0.1/a", messageSource);
		Assert.AreSame(args, received);
	}

	private sealed class FakeOwner : IWebView
	{
		public bool SwitchSourceBeforeNavigating => false;

		public bool IsLoaded => false;

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;
	}

	private class FakePlainNativeWebView : INativeWebView, ISupportsWebResourceRequested
	{
		public List<string> NavigatedUris { get; } = new();

		public List<string> FilterCalls { get; } = new();

		public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

		public string DocumentTitle => "";

		public void RaiseWebResourceRequested(FakeWebResourceRequestedEventArgs args) => RaiseWebResourceRequested(new CoreWebView2WebResourceRequestedEventArgs(args));

		public void RaiseWebResourceRequested(CoreWebView2WebResourceRequestedEventArgs args) => WebResourceRequested?.Invoke(this, args);

		public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
			=> FilterCalls.Add("add:" + uri);

		public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
			=> FilterCalls.Add("remove:" + uri);

		public void GoBack() { }

		public void GoForward() { }

		public void Stop() { }

		public void Reload() { }

		public void ProcessNavigation(Uri uri) => NavigatedUris.Add(uri.AbsoluteUri);

		public void ProcessNavigation(string html) { }

		public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => NavigatedUris.Add(httpRequestMessage.RequestUri!.AbsoluteUri);

		public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => Task.FromResult<string?>(null);

		public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => Task.FromResult<string?>(null);

		public void SetScrollingEnabled(bool isScrollingEnabled) { }
	}

	private sealed class FakeCustomSchemeNativeWebView : FakePlainNativeWebView, ISupportsCustomSchemeVirtualHosts
	{
		public WebViewVirtualHostMap? Map { get; private set; }

		public void SetVirtualHostMap(WebViewVirtualHostMap map) => Map = map;
	}

	internal sealed class FakeWebResourceRequestedEventArgs(string uri) : INativeWebResourceRequestedEventArgs
	{
		public INativeWebResourceRequest Request { get; } = new FakeRequest(uri);

		public INativeWebResourceResponse? Response { get; set; }

		public CoreWebView2WebResourceContext ResourceContext => CoreWebView2WebResourceContext.Script;

		public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => CoreWebView2WebResourceRequestSourceKinds.Document;

		public Deferral GetDeferral() => new(() => { });
	}

	private sealed class FakeRequest(string uri) : INativeWebResourceRequest
	{
		public string Uri { get; set; } = uri;

		public string Method { get; set; } = "GET";

		public IRandomAccessStream Content { get; set; } = null!;

		public INativeHttpRequestHeaders Headers { get; } = new FakeRequestHeaders();
	}

	private sealed class FakeRequestHeaders : INativeHttpRequestHeaders
	{
		private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

		public bool Contains(string name) => _headers.ContainsKey(name);

		public string GetHeader(string name) => _headers[name];

		public INativeHttpHeadersCollectionIterator GetHeaders(string name) => throw new NotSupportedException();

		public void RemoveHeader(string name) => _headers.Remove(name);

		public void SetHeader(string name, string value) => _headers[name] = value;

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
