#nullable enable

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
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

internal partial class NativeWebView : ICleanableNativeWebView, Uno.UI.Xaml.Controls.ISupportsUserAgent, Uno.UI.Xaml.Controls.ISupportsScriptEnabled, Uno.UI.Xaml.Controls.ISupportsZoomControl, Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts, Uno.UI.Xaml.Controls.ISupportsCookieManager, Uno.UI.Xaml.Controls.ISupportsPrint
{
	Task<System.IO.Stream> Uno.UI.Xaml.Controls.ISupportsPrint.PrintToPdfStreamAsync(CoreWebView2PrintSettings? settings, CancellationToken ct)
		=> throw new NotSupportedException(
			"CoreWebView2.PrintToPdfStreamAsync is not supported on the WebAssembly browser host: " +
			"the parent document cannot rasterize sandboxed iframe content to a PDF.");

	async Task<CoreWebView2PrintStatus> Uno.UI.Xaml.Controls.ISupportsPrint.ShowPrintUIAsync(CoreWebView2PrintDialogKind dialogKind, CancellationToken ct)
	{
		// Fire the iframe's window.print() which delegates to the browser's print stack.
		await ExecuteScriptAsync("try { (frames[0] || window).print(); } catch (e) { window.print(); }", ct);
		return CoreWebView2PrintStatus.Succeeded;
	}

	private const string WasmCookiesNotSupported =
		"CoreWebView2.CookieManager is not supported on the WebAssembly browser host: " +
		"a sandboxed iframe cannot enumerate or mutate its document.cookie store from the parent.";

	Task<System.Collections.Generic.IReadOnlyList<CoreWebView2Cookie>> Uno.UI.Xaml.Controls.ISupportsCookieManager.GetCookiesAsync(string uri, CancellationToken ct)
		=> throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookie(CoreWebView2Cookie cookie) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookies(string name, string uri) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path) => throw new NotSupportedException(WasmCookiesNotSupported);

	void Uno.UI.Xaml.Controls.ISupportsCookieManager.DeleteAllCookies() => throw new NotSupportedException(WasmCookiesNotSupported);

	Task<string> Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts.AddScriptToExecuteOnDocumentCreatedAsync(string javaScript, CancellationToken ct)
		=> throw new NotSupportedException(
			"AddScriptToExecuteOnDocumentCreatedAsync is not supported on the WebAssembly browser host: " +
			"the page runs in a sandboxed iframe whose document-start lifecycle the host cannot intercept.");

	void Uno.UI.Xaml.Controls.ISupportsDocumentCreatedScripts.RemoveScriptToExecuteOnDocumentCreated(string id) { }

	private string? _requestedUserAgent;
	private bool _requestedIsScriptEnabled = true;
	private bool _requestedIsZoomControlEnabled = true;

	string? Uno.UI.Xaml.Controls.ISupportsUserAgent.UserAgent
	{
		get => _requestedUserAgent;
		set
		{
			_requestedUserAgent = value;
			if (typeof(NativeWebView).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				typeof(NativeWebView).Log().Warn(
					"Setting CoreWebView2Settings.UserAgent has no effect on the WebAssembly browser host: " +
					"a sandboxed iframe cannot override the user agent string of its parent document.");
			}
		}
	}

	bool Uno.UI.Xaml.Controls.ISupportsScriptEnabled.IsScriptEnabled
	{
		get => _requestedIsScriptEnabled;
		set
		{
			_requestedIsScriptEnabled = value;
			if (!value && typeof(NativeWebView).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				typeof(NativeWebView).Log().Warn(
					"Disabling JavaScript via CoreWebView2Settings.IsScriptEnabled is not supported on the WebAssembly browser host.");
			}
		}
	}

	bool Uno.UI.Xaml.Controls.ISupportsZoomControl.IsZoomControlEnabled
	{
		get => _requestedIsZoomControlEnabled;
		set
		{
			_requestedIsZoomControlEnabled = value;
			if (typeof(NativeWebView).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				typeof(NativeWebView).Log().Warn(
					"CoreWebView2Settings.IsZoomControlEnabled is not honored on the WebAssembly browser host.");
			}
		}
	}

	private readonly CoreWebView2 _coreWebView;
	private readonly ElementId _elementId;
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
		if (_coreWebView is null)
		{
			return;
		}

		var uriString = string.IsNullOrEmpty(absoluteUrl) ? NativeMethods.GetAttribute(_elementId, "src") : absoluteUrl;
		Uri uri = CoreWebView2.BlankUri;
		if (!string.IsNullOrEmpty(uriString))
		{
			uri = new Uri(uriString);
		}

		_coreWebView.OnDocumentTitleChanged();
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		await Task.Yield();
		var result = NativeMethods.ExecuteScript(_elementId, script);

		// String needs to be wrapped in quotes to match Windows behavior
		return $"\"{result?.Replace("\"", "\\\"")}\"";
	}

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) =>
		throw new NotSupportedException("InvokeScriptAsync with arguments is not yet supported on this platform.");

	private void ScheduleNavigationStarting(string? url, Action loadAction)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationStarting(url, out var cancel);

			if (!cancel)
			{
				loadAction?.Invoke();
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

		ScheduleNavigationStarting(uriString, () => NativeMethods.Navigate(_elementId, uriString));
		OnNavigationCompleted(this, null);
	}

	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(null, () => NativeMethods.SetAttribute(_elementId, "srcdoc", html));
		OnNavigationCompleted(this, null);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
	}

	public void Reload() => NativeMethods.Reload(_elementId);

	public void Stop() => NativeMethods.Stop(_elementId);

	public void GoBack() => NativeMethods.GoBack(_elementId);

	public void GoForward() => NativeMethods.GoForward(_elementId);

	public void SetScrollingEnabled(bool isScrollingEnabled) { }

	public void Dispose()
	{
		// Todo call this and reattach if needed
		_elementIdToNativeWebView.TryRemove(_elementId, out _);
	}

	public void OnLoaded()
	{
		_elementIdToNativeWebView.TryAdd(_elementId, this);
		NativeMethods.SetupEvents(_elementId);
		DispatchLoadEvent(_elementId, null);
	}

	public void OnUnloaded()
	{
		NativeMethods.CleanupEvents(_elementId);
		_elementIdToNativeWebView.TryRemove(_elementId, out var _);
	}
}
