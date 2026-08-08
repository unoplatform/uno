#nullable enable

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Helpers;
using static __Microsoft.UI.Xaml.Controls.NativeWebView;

#if WASM_SKIA
using ElementId = System.String;
#else
using ElementId = System.IntPtr;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeWebView : ICleanableNativeWebView, Uno.UI.Xaml.Controls.ISupportsClose, Uno.UI.Xaml.Controls.ISupportsUserAgent, Uno.UI.Xaml.Controls.ISupportsScriptEnabled, Uno.UI.Xaml.Controls.ISupportsZoomControl, Uno.UI.Xaml.Controls.ISupportsPostWebMessage, Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts, Uno.UI.Xaml.Controls.ISupportsCookieManager, Uno.UI.Xaml.Controls.ISupportsPrint
{
	Task<global::System.IO.Stream> Uno.UI.Xaml.Controls.ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
		=> throw new NotSupportedException(
			"CoreWebView2.PrintToPdfStreamAsync is not supported on the WebAssembly browser host: " +
			"the browser does not expose an API for the parent document to render iframe content to a PDF stream.");

	async Task<CoreWebView2PrintStatus> Uno.UI.Xaml.Controls.ISupportsPrint.ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		NativeMethods.ShowPrintUI(_elementId);
		await Task.Yield();
		return CoreWebView2PrintStatus.Succeeded;
	}

	private const string WasmCookiesNotSupported =
		"CoreWebView2.CookieManager is not supported on the WebAssembly browser host: " +
		"the browser same-origin policy prevents the host from enumerating or mutating an iframe's cookie store.";

	Task<global::System.Collections.Generic.IReadOnlyList<CoreWebView2Cookie>> Uno.UI.Xaml.Controls.ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
		=> throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookies(string name, string? uri) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteAllCookies() => throw new NotSupportedException(WasmCookiesNotSupported);

	Task<string> Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> throw new NotSupportedException(
			"AddScriptToExecuteOnDocumentCreatedAsync is not supported on the WebAssembly browser host: " +
			"the browser does not expose an iframe document-start hook to its host page.");

	void Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts.RemoveScriptToExecuteOnDocumentCreated(string id) =>
		throw new NotSupportedException(
			"RemoveScriptToExecuteOnDocumentCreated is not supported on the WebAssembly browser host: " +
			"the browser does not expose an iframe document-start hook to its host page.");

	private string? _requestedUserAgent;
	private bool _requestedIsScriptEnabled = true;
	private bool _requestedIsZoomControlEnabled = true;

	string? Uno.UI.Xaml.Controls.ISupportsUserAgent.UserAgent
	{
		get => _requestedUserAgent;
		set
		{
			if (value is not null)
			{
				throw new NotSupportedException("CoreWebView2Settings.UserAgent cannot override the browser user agent on WebAssembly.");
			}

			_requestedUserAgent = value;
		}
	}

	bool Uno.UI.Xaml.Controls.ISupportsScriptEnabled.IsScriptEnabled
	{
		get => _requestedIsScriptEnabled;
		set
		{
			if (!value)
			{
				throw new NotSupportedException("CoreWebView2Settings.IsScriptEnabled cannot disable scripts in an iframe on WebAssembly.");
			}

			_requestedIsScriptEnabled = value;
		}
	}

	bool Uno.UI.Xaml.Controls.ISupportsZoomControl.IsZoomControlEnabled
	{
		get => _requestedIsZoomControlEnabled;
		set
		{
			if (!value)
			{
				throw new NotSupportedException("CoreWebView2Settings.IsZoomControlEnabled cannot disable browser zoom on WebAssembly.");
			}

			_requestedIsZoomControlEnabled = value;
		}
	}

	void Uno.UI.Xaml.Controls.ISupportsPostWebMessage.PostWebMessageAsJson(string json) =>
		NativeMethods.PostWebMessage(_elementId, json, isJson: true);

	void Uno.UI.Xaml.Controls.ISupportsPostWebMessage.PostWebMessageAsString(string message) =>
		NativeMethods.PostWebMessage(_elementId, message, isJson: false);

	private readonly CoreWebView2 _coreWebView;
	private readonly ElementId _elementId;
	private bool _navigationPending;
	private bool _isClosed;
	private Uri? _pendingNavigationUri;
	private static readonly ConcurrentDictionary<ElementId, NativeWebView> _elementIdToNativeWebView = new();

	public NativeWebView(CoreWebView2 coreWebView, ElementId elementId)
	{
		NativeMethods.BuildImports(
#if WASM_SKIA
			"Uno.UI.Runtime.Skia.WebAssembly.Browser"
#else
			"Uno.UI"
#endif
			);
		_coreWebView = coreWebView;
		_elementId = elementId;

		NativeMethods.InitializeStyling(elementId);
		NativeMethods.SetupEvents(elementId);
	}

	[JSExport]
	internal static bool DispatchNewWindowRequested(ElementId elementId, string targetUrl, string refererUrl)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			Uri refererUri;
			if (string.IsNullOrEmpty(refererUrl) || !Uri.TryCreate(refererUrl, UriKind.Absolute, out refererUri!))
			{
				refererUri = CoreWebView2.BlankUri;
			}

			nativeWebView._coreWebView.RaiseNewWindowRequested(
				targetUrl,
				refererUri,
				out bool handled);

			return handled;
		}

		return false;
	}

	[JSExport]
	internal static void DispatchLoadEvent(ElementId elementId, string? absoluteUrl)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			nativeWebView.OnNavigationCompleted(nativeWebView._coreWebView, absoluteUrl);
		}
	}

	[JSExport]
	internal static void DispatchWebMessage(ElementId elementId, string message)
	{
		if (_elementIdToNativeWebView.TryGetValue(elementId, out var nativeWebView))
		{
			nativeWebView._coreWebView.RaiseWebMessageReceived(message);
		}
	}

	public string DocumentTitle => NativeMethods.GetDocumentTitle(_elementId) ?? "";

	private void OnNavigationCompleted(object sender, string? absoluteUrl)
	{
		if (!_navigationPending)
		{
			return;
		}

		_navigationPending = false;
		var uri = _pendingNavigationUri ?? CoreWebView2.BlankUri;
		if (uri != CoreWebView2.BlankUri
			&& !string.IsNullOrEmpty(absoluteUrl)
			&& Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var actualUri))
		{
			uri = actualUri;
		}
		_pendingNavigationUri = null;

		_coreWebView.RaiseContentLoading();
		_coreWebView.RaiseDOMContentLoaded();
		_coreWebView.OnDocumentTitleChanged();
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		await Task.Yield();
		return NativeMethods.ExecuteScript(_elementId, script);
	}

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		var serializedArguments = arguments is null ? string.Empty : JsonSerializer.Serialize(arguments)[1..^1];
		return ExecuteScriptAsync($"{script}({serializedArguments})", token);
	}

	private void ScheduleNavigationStarting(object navigationData, Uri completionUri, Action loadAction)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationStarting(navigationData, out var cancel);

			if (!cancel)
			{
				_pendingNavigationUri = completionUri;
				_navigationPending = true;
				loadAction.Invoke();
			}
		});
	}

	public void ProcessNavigation(Uri uri)
	{
		var uriString = uri.OriginalString;

		// Handle virtual host mapping for local assets
		if (!string.IsNullOrEmpty(uri.Host) &&
			_coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			var relativePath = uri.AbsolutePath.TrimStart('/');
			var mappedPath = $"{folderName.TrimEnd('/')}/{relativePath}";

			if (!string.IsNullOrEmpty(relativePath))
			{
				var packageBase = NativeMethods.GetPackageBase();
				uriString = $"{packageBase.TrimEnd('/')}/{mappedPath.TrimStart('/')}";

				if (!string.IsNullOrEmpty(uri.Query))
				{
					uriString += uri.Query;
				}
				if (!string.IsNullOrEmpty(uri.Fragment))
				{
					uriString += uri.Fragment;
				}
			}
		}

		ScheduleNavigationStarting(uri, uri, () => NativeMethods.Navigate(_elementId, uriString));
	}

	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(html, CoreWebView2.BlankUri, () => NativeMethods.SetAttribute(_elementId, "srcdoc", AddWebMessageBridge(html)));
	}

	private static string AddWebMessageBridge(string html)
	{
		const string bridge = """
			<script>
			(function () {
				window.chrome = window.chrome || {};
				var webview = window.chrome.webview = window.chrome.webview || {};
				if (webview.__unoListeners) { return; }
				var listeners = webview.__unoListeners = [];
				webview.addEventListener = function (type, handler) {
					if (type === 'message' && typeof handler === 'function') { listeners.push(handler); }
				};
				webview.removeEventListener = function (type, handler) {
					if (type !== 'message') { return; }
					var index = listeners.indexOf(handler);
					if (index >= 0) { listeners.splice(index, 1); }
				};
				webview.postMessage = function (message) {
					var payload = JSON.stringify(message);
					window.parent.Microsoft.UI.Xaml.Controls.WebView.dispatchWebMessage(window.frameElement.id, payload === undefined ? 'null' : payload);
				};
				webview.__unoDispatchMessage = function (data) {
					var event = typeof MessageEvent === 'function' ? new MessageEvent('message', { data: data }) : { data: data };
					listeners.slice().forEach(function (handler) { try { handler(event); } catch (_) {} });
				};
			})();
			</script>
			""";

		var doctypeEnd = html.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase) ? html.IndexOf('>') : -1;
		return doctypeEnd >= 0 ? html.Insert(doctypeEnd + 1, bridge) : bridge + html;
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
		throw new NotSupportedException("NavigateWithHttpRequestMessage is not supported by an HTML iframe on WebAssembly.");
	}

	public void Reload() => NativeMethods.Reload(_elementId);

	public void Stop() => NativeMethods.Stop(_elementId);

	public void GoBack() => NativeMethods.GoBack(_elementId);

	public void GoForward() => NativeMethods.GoForward(_elementId);

	public void SetScrollingEnabled(bool isScrollingEnabled) { }

	void Uno.UI.Xaml.Controls.ISupportsClose.Close()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;
		_elementIdToNativeWebView.TryRemove(_elementId, out _);
		NativeMethods.Close(_elementId);
	}

	public void OnLoaded()
	{
		if (_isClosed)
		{
			return;
		}

		_elementIdToNativeWebView.TryAdd(_elementId, this);
		NativeMethods.SetupEvents(_elementId);
	}

	public void OnUnloaded()
	{
		NativeMethods.CleanupEvents(_elementId);
		_elementIdToNativeWebView.TryRemove(_elementId, out var _);
	}
}
