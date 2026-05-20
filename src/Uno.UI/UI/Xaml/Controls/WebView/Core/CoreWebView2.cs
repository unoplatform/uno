#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal const string BlankUrl = "about:blank";
	internal const string DataUriFormatString = "data:text/html;charset=utf-8;base64,{0}";
	internal static readonly Uri BlankUri = new Uri(BlankUrl);

	private readonly Dictionary<string, string> _hostToFolderMap = new();
	private readonly IWebView _owner;

	private bool _scrollEnabled = true;
	private INativeWebView? _nativeWebView;
	private ISupportsWebResourceRequested? _webResourceRequestedSupport;
	private readonly List<WebResourceRequestedFilter> _webResourceRequestedFilters = new();
	internal long _navigationId;
	private object? _processedSource;

	internal CoreWebView2(IWebView owner)
	{
		HostToFolderMap = _hostToFolderMap.AsReadOnly();
		_owner = owner;
		Settings.UserAgentChanged += OnSettingsUserAgentChanged;
		Settings.IsScriptEnabledChanged += OnSettingsIsScriptEnabledChanged;
		Settings.IsZoomControlEnabledChanged += OnSettingsIsZoomControlEnabledChanged;
	}

	private void OnSettingsUserAgentChanged(object? sender, EventArgs e)
	{
		if (_nativeWebView is ISupportsUserAgent ua)
		{
			ua.UserAgent = Settings.UserAgent;
		}
	}

	private void OnSettingsIsScriptEnabledChanged(object? sender, EventArgs e)
	{
		if (_nativeWebView is ISupportsScriptEnabled se)
		{
			se.IsScriptEnabled = Settings.IsScriptEnabled;
		}
	}

	private void OnSettingsIsZoomControlEnabledChanged(object? sender, EventArgs e)
	{
		if (_nativeWebView is ISupportsZoomControl zc)
		{
			zc.IsZoomControlEnabled = Settings.IsZoomControlEnabled;
		}
	}

	internal IWebView Owner => _owner;

	internal INativeWebView? NativeWebViewForCookies => _nativeWebView;

	internal CoreWebView2Environment? CustomEnvironment { get; private set; }

	internal CoreWebView2ControllerOptions? CustomControllerOptions { get; private set; }

	internal void SetCustomEnvironment(CoreWebView2Environment environment, CoreWebView2ControllerOptions? controllerOptions)
	{
		CustomEnvironment = environment;
		CustomControllerOptions = controllerOptions;
	}

	internal IReadOnlyDictionary<string, string> HostToFolderMap { get; }

	private CoreWebView2CookieManager? _cookieManager;

	/// <summary>
	/// Gets the cookie manager for the running WebView. Calls into the manager
	/// will throw NotSupportedException on platforms that cannot enumerate cookies
	/// (notably the WebAssembly browser host).
	/// </summary>
	public CoreWebView2CookieManager CookieManager => _cookieManager ??= new CoreWebView2CookieManager(this);

#if __SKIA__
	internal void OnLoaded() => (_nativeWebView as ICleanableNativeWebView)?.OnLoaded();

	internal void OnUnloaded() => (_nativeWebView as ICleanableNativeWebView)?.OnUnloaded();
#endif

	/// <summary>
	/// Gets the CoreWebView2Settings object contains various modifiable
	/// settings for the running WebView.
	/// </summary>
	public CoreWebView2Settings Settings { get; } = new();

	public void Navigate(string uri)
	{
		if (!Uri.TryCreate(uri, UriKind.Absolute, out var actualUri))
		{
			throw new ArgumentException("The passed in value is not an absolute URI", nameof(uri));
		}

		_processedSource = actualUri;
		if (_owner.SwitchSourceBeforeNavigating)
		{
			Source = actualUri.AbsoluteUri;
		}

		UpdateFromInternalSource();
	}

	public void NavigateToString(string htmlContent)
	{
		_processedSource = htmlContent;
		if (_owner.SwitchSourceBeforeNavigating)
		{
			Source = BlankUrl;
		}

		UpdateFromInternalSource();
	}

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
	{
		if (hostName is null)
		{
			throw new ArgumentNullException(nameof(hostName));
		}

		if (folderPath is null)
		{
			throw new ArgumentNullException(nameof(folderPath));
		}

		if (_nativeWebView is ISupportsVirtualHostMapping supportsVirtualHostMapping)
		{
			supportsVirtualHostMapping.SetVirtualHostNameToFolderMapping(hostName, folderPath, accessKind);
		}
		else
		{
			_hostToFolderMap.Add(hostName.ToLowerInvariant(), folderPath);
		}
	}

	public void ClearVirtualHostNameToFolderMapping(string hostName)
	{
		if (_nativeWebView is ISupportsVirtualHostMapping supportsVirtualHostMapping)
		{
			supportsVirtualHostMapping.ClearVirtualHostNameToFolderMapping(hostName);
		}
		else
		{
			_hostToFolderMap.Remove(hostName);
		}
	}

	internal void NavigateWithHttpRequestMessage(global::Windows.Web.Http.HttpRequestMessage requestMessage)
	{
		if (requestMessage?.RequestUri is null)
		{
			throw new ArgumentException("Invalid request message. It does not have a RequestUri.", nameof(requestMessage));
		}

		_processedSource = requestMessage;
		if (_owner.SwitchSourceBeforeNavigating)
		{
			var reqUri = requestMessage.RequestUri;
			Source = reqUri.IsAbsoluteUri ? reqUri.AbsoluteUri : reqUri.OriginalString;
		}

		UpdateFromInternalSource();
	}

	public void GoBack() => _nativeWebView?.GoBack();

	public void GoForward() => _nativeWebView?.GoForward();

	public void Stop() => _nativeWebView?.Stop();

	public void Reload() => _nativeWebView?.Reload();

	public IAsyncOperation<string?> ExecuteScriptAsync(string javaScript) =>
		AsyncOperation.FromTask(ct =>
		{
			if (_nativeWebView is null)
			{
				return Task.FromResult<string?>(null);
			}

			return _nativeWebView.ExecuteScriptAsync(javaScript, ct);
		});

	/// <summary>
	/// Posts a message that is received by JavaScript code in the page via
	/// window.chrome.webview.addEventListener('message', handler). The argument
	/// is treated as a JSON-encoded value and made available as event.data.
	/// </summary>
	public void PostWebMessageAsJson(string webMessageAsJson)
	{
		if (webMessageAsJson is null)
		{
			throw new ArgumentNullException(nameof(webMessageAsJson));
		}

		if (_nativeWebView is ISupportsPostWebMessage native)
		{
			native.PostWebMessageAsJson(webMessageAsJson);
		}
		else
		{
			DispatchPostWebMessage(webMessageAsJson, isJson: true);
		}
	}

	/// <summary>
	/// Posts a message that is received by JavaScript code in the page via
	/// window.chrome.webview.addEventListener('message', handler). The argument
	/// is treated as a plain string and made available as event.data.
	/// </summary>
	public void PostWebMessageAsString(string webMessageAsString)
	{
		if (webMessageAsString is null)
		{
			throw new ArgumentNullException(nameof(webMessageAsString));
		}

		if (_nativeWebView is ISupportsPostWebMessage native)
		{
			native.PostWebMessageAsString(webMessageAsString);
		}
		else
		{
			DispatchPostWebMessage(webMessageAsString, isJson: false);
		}
	}

	private void DispatchPostWebMessage(string payload, bool isJson)
	{
		if (_nativeWebView is null)
		{
			return;
		}

		// Polyfill window.chrome.webview.addEventListener('message', ...) on first use,
		// then dispatch the message to subscribed listeners.
		var literal = isJson ? payload : EscapeJsString(payload);
		var script =
			"(function(){" +
				"window.chrome=window.chrome||{};" +
				"window.chrome.webview=window.chrome.webview||{};" +
				"if(!window.chrome.webview.__unoListeners){" +
					"window.chrome.webview.__unoListeners=[];" +
					"var origAdd=window.chrome.webview.addEventListener;" +
					"window.chrome.webview.addEventListener=function(t,h){" +
						"if(t==='message'){window.chrome.webview.__unoListeners.push(h);}" +
						"else if(typeof origAdd==='function'){origAdd.call(window.chrome.webview,t,h);}" +
					"};" +
					"var origRemove=window.chrome.webview.removeEventListener;" +
					"window.chrome.webview.removeEventListener=function(t,h){" +
						"if(t==='message'){var i=window.chrome.webview.__unoListeners.indexOf(h);if(i>=0)window.chrome.webview.__unoListeners.splice(i,1);}" +
						"else if(typeof origRemove==='function'){origRemove.call(window.chrome.webview,t,h);}" +
					"};" +
				"}" +
				"var d=" + literal + ";" +
				"var ev={data:d};" +
				"window.chrome.webview.__unoListeners.forEach(function(h){try{h(ev);}catch(e){}});" +
			"})();";

		_ = _nativeWebView.ExecuteScriptAsync(script, CancellationToken.None);
	}

	private static string EscapeJsString(string input)
	{
		var sb = new System.Text.StringBuilder(input.Length + 2);
		sb.Append('"');
		foreach (var ch in input)
		{
			switch (ch)
			{
				case '\\': sb.Append("\\\\"); break;
				case '"': sb.Append("\\\""); break;
				case '\b': sb.Append("\\b"); break;
				case '\f': sb.Append("\\f"); break;
				case '\n': sb.Append("\\n"); break;
				case '\r': sb.Append("\\r"); break;
				case '\t': sb.Append("\\t"); break;
				case '/': sb.Append("\\/"); break;
				case '\u2028': sb.Append("\\u2028"); break;
				case '\u2029': sb.Append("\\u2029"); break;
				default:
					if (ch < 0x20)
					{
						sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
					}
					else
					{
						sb.Append(ch);
					}
					break;
			}
		}
		sb.Append('"');
		return sb.ToString();
	}

	internal async Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken ct)
	{
		if (_nativeWebView is null)
		{
			return null;
		}

		return await _nativeWebView.InvokeScriptAsync(script, arguments, ct);
	}

	/// <summary>
	/// Adds a JavaScript snippet that is run before any other scripts on every new
	/// top-level document (and any sub-documents). Returns an opaque identifier that
	/// can be passed to RemoveScriptToExecuteOnDocumentCreated to revoke it.
	/// </summary>
	public IAsyncOperation<string> AddScriptToExecuteOnDocumentCreatedAsync(string javaScript)
	{
		if (javaScript is null)
		{
			throw new ArgumentNullException(nameof(javaScript));
		}

		return AsyncOperation.FromTask(async ct =>
		{
			if (_nativeWebView is ISupportsDocumentCreatedScripts native)
			{
				return await native.AddScriptToExecuteOnDocumentCreatedAsync(javaScript, ct);
			}

			throw new NotSupportedException(
				"AddScriptToExecuteOnDocumentCreatedAsync is not supported on this platform.");
		});
	}

	/// <summary>
	/// Removes a previously registered document-created script. The identifier is
	/// the value returned by AddScriptToExecuteOnDocumentCreatedAsync.
	/// </summary>
	public void RemoveScriptToExecuteOnDocumentCreated(string id)
	{
		if (id is null)
		{
			throw new ArgumentNullException(nameof(id));
		}

		if (_nativeWebView is ISupportsDocumentCreatedScripts native)
		{
			native.RemoveScriptToExecuteOnDocumentCreated(id);
		}
	}

	/// <summary>
	/// Renders the current top-level document to a PDF and returns the stream.
	/// </summary>
	public IAsyncOperation<IRandomAccessStream> PrintToPdfStreamAsync(CoreWebView2PrintSettings? printSettings)
	{
		return AsyncOperation.FromTask(async ct =>
		{
			if (_nativeWebView is not ISupportsPrint print)
			{
				throw new NotSupportedException("CoreWebView2.PrintToPdfStreamAsync is not supported on this platform.");
			}

			var stream = await print.PrintToPdfStreamAsync(printSettings, ct);
			return stream.AsRandomAccessStream();
		});
	}

	/// <summary>
	/// Shows the platform print UI for the current page.
	/// </summary>
	public IAsyncOperation<CoreWebView2PrintStatus> ShowPrintUIAsync(CoreWebView2PrintDialogKind printDialogKind)
	{
		return AsyncOperation.FromTask(async ct =>
		{
			if (_nativeWebView is not ISupportsPrint print)
			{
				throw new NotSupportedException("CoreWebView2.ShowPrintUIAsync is not supported on this platform.");
			}

			return await print.ShowPrintUIAsync(printDialogKind, ct);
		});
	}

	// -------- Phase 7 adjacent events --------
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2ContentLoadingEventArgs>? ContentLoading;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2PermissionRequestedEventArgs>? PermissionRequested;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2DownloadStartingEventArgs>? DownloadStarting;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2ContextMenuRequestedEventArgs>? ContextMenuRequested;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2ServerCertificateErrorDetectedEventArgs>? ServerCertificateErrorDetected;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2FrameCreatedEventArgs>? FrameCreated;

	internal void RaiseContentLoading(CoreWebView2ContentLoadingEventArgs args) => ContentLoading?.Invoke(this, args);
	internal void RaiseDOMContentLoaded(CoreWebView2DOMContentLoadedEventArgs args) => DOMContentLoaded?.Invoke(this, args);
	internal void RaisePermissionRequested(CoreWebView2PermissionRequestedEventArgs args) => PermissionRequested?.Invoke(this, args);
	internal void RaiseDownloadStarting(CoreWebView2DownloadStartingEventArgs args) => DownloadStarting?.Invoke(this, args);
	internal void RaiseContextMenuRequested(CoreWebView2ContextMenuRequestedEventArgs args) => ContextMenuRequested?.Invoke(this, args);
	internal void RaiseServerCertificateErrorDetected(CoreWebView2ServerCertificateErrorDetectedEventArgs args) => ServerCertificateErrorDetected?.Invoke(this, args);
	internal void RaiseFrameCreated(CoreWebView2FrameCreatedEventArgs args) => FrameCreated?.Invoke(this, args);

	internal void OnOwnerApplyTemplate()
	{
		DetachWebResourceRequestedSupport();
		_nativeWebView = GetNativeWebViewFromTemplate();
		AttachWebResourceRequestedSupport();
		ApplySettingsToNativeWebView();

		// Signal that native WebView is now initialized
		_nativeWebViewInitializedTcs.TrySetResult(true);

		//The native WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		UpdateFromInternalSource();
		OnScrollEnabledChanged(_scrollEnabled);
	}

	private void ApplySettingsToNativeWebView()
	{
		if (_nativeWebView is ISupportsUserAgent ua && Settings.UserAgent is not null)
		{
			ua.UserAgent = Settings.UserAgent;
		}

		if (_nativeWebView is ISupportsScriptEnabled se)
		{
			se.IsScriptEnabled = Settings.IsScriptEnabled;
		}

		if (_nativeWebView is ISupportsZoomControl zc)
		{
			zc.IsZoomControlEnabled = Settings.IsZoomControlEnabled;
		}
	}

	internal void OnScrollEnabledChanged(bool newValue)
	{
		_scrollEnabled = newValue;
		_nativeWebView?.SetScrollingEnabled(newValue);
	}

	internal void OnDocumentTitleChanged()
	{
		DocumentTitleChanged?.Invoke(this, null);
	}

	internal void RaiseNavigationStarting(object? navigationData, out bool cancel)
	{
		string? uriString = null;
		if (navigationData is Uri uri)
		{
			uriString = uri.ToString();
		}
		else if (navigationData is string htmlContent)
		{
			// Convert to data URI string
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
			var base64String = System.Convert.ToBase64String(plainTextBytes);
			uriString = string.Format(CultureInfo.InvariantCulture, DataUriFormatString, base64String);
		}

		var newNavigationId = Interlocked.Increment(ref _navigationId);
		var args = new CoreWebView2NavigationStartingEventArgs((ulong)newNavigationId, uriString);
		NavigationStarting?.Invoke(this, args);

		cancel = args.Cancel;
	}

	internal void RaiseNewWindowRequested(string target, Uri referer, out bool handled)
	{
		var args = new CoreWebView2NewWindowRequestedEventArgs(target, referer);
		NewWindowRequested?.Invoke(this, args);

		handled = args.Handled;
	}

	internal void RaiseNavigationCompleted(Uri? uri, bool isSuccess, int httpStatusCode, CoreWebView2WebErrorStatus errorStatus, bool shouldSetSource = true)
	{
		if (shouldSetSource)
		{
			Source = (uri ?? BlankUri).ToString();
		}

		NavigationCompleted?.Invoke(this, new CoreWebView2NavigationCompletedEventArgs((ulong)_navigationId, uri, isSuccess, httpStatusCode, errorStatus));
	}

	internal void RaiseHistoryChanged() => HistoryChanged?.Invoke(this, null);

	internal void SetHistoryProperties(bool canGoBack, bool canGoForward)
	{
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;
	}

	internal void RaiseWebMessageReceived(string message)
	{
		// WebMessageReceived must be called on the UI thread.
		if (_owner.Dispatcher.HasThreadAccess)
		{
			WebMessageReceived?.Invoke(this, new(message));
		}
		else
		{
			_ = _owner.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() => WebMessageReceived?.Invoke(this, new(message)));
		}
	}

	internal void RaiseUnsupportedUriSchemeIdentified(Uri targetUri, out bool handled)
	{
		var args = new Microsoft.UI.Xaml.Controls.WebViewUnsupportedUriSchemeIdentifiedEventArgs(targetUri);
		UnsupportedUriSchemeIdentified?.Invoke(this, args);

		handled = args.Handled;
	}


	public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		if (uri is null)
		{
			throw new ArgumentNullException(nameof(uri));
		}

		UpsertFilter(uri, resourceContext, requestSourceKinds);

		if (_webResourceRequestedSupport is { })
		{
			_webResourceRequestedSupport.AddWebResourceRequestedFilter(uri, resourceContext, requestSourceKinds);
		}
	}

	public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext ResourceContext)
		=> AddWebResourceRequestedFilter(uri, ResourceContext, CoreWebView2WebResourceRequestSourceKinds.All);

	public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		if (uri is null)
		{
			throw new ArgumentNullException(nameof(uri));
		}

		RemoveFilter(uri, resourceContext, requestSourceKinds);

		if (_webResourceRequestedSupport is { })
		{
			_webResourceRequestedSupport.RemoveWebResourceRequestedFilter(uri, resourceContext, requestSourceKinds);
		}
	}

	public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext ResourceContext)
		=> RemoveWebResourceRequestedFilter(uri, ResourceContext, CoreWebView2WebResourceRequestSourceKinds.All);

	internal void RaiseWebResourceRequested(CoreWebView2WebResourceRequestedEventArgs eventArgs)
	{
		WebResourceRequested?.Invoke(this, eventArgs);
	}



	private TaskCompletionSource<bool> _nativeWebViewInitializedTcs = new TaskCompletionSource<bool>();
	internal Task EnsureNativeWebViewAsync() => _nativeWebViewInitializedTcs.Task;
	internal static bool GetIsHistoryEntryValid(string url) =>
		!url.IsNullOrWhiteSpace() &&
		!url.Equals(BlankUrl, StringComparison.OrdinalIgnoreCase);

	[MemberNotNullWhen(true, nameof(_nativeWebView))]
	private bool VerifyWebViewAvailability()
	{
		if (_nativeWebView == null)
		{
			if (_owner.IsLoaded)
			{
				_owner.Log().Warn(
					"This WebView control instance does not have a native WebView child, " +
					"the control template may be missing.");
			}

			return false;
		}

		return true;
	}

	private void UpdateFromInternalSource()
	{
		if (!VerifyWebViewAvailability())
		{
			return;
		}

		if (_processedSource is Uri uri)
		{
			_nativeWebView.ProcessNavigation(uri);
		}
		else if (_processedSource is string html)
		{
			_nativeWebView.ProcessNavigation(html);
		}
		else if (_processedSource is global::Windows.Web.Http.HttpRequestMessage requestMessage)
		{
			var httpRequestMessage = new HttpRequestMessage()
			{
				RequestUri = requestMessage.RequestUri
			};
			foreach (var header in requestMessage.Headers)
			{
				httpRequestMessage.Headers.Add(header.Key, header.Value);
			}
			_nativeWebView.ProcessNavigation(httpRequestMessage);
		}

		_processedSource = null;
	}


	private void AttachWebResourceRequestedSupport()
	{
		if (_nativeWebView is not ISupportsWebResourceRequested supports)
		{
			return;
		}

		_webResourceRequestedSupport = supports;
		supports.WebResourceRequested += OnNativeWebResourceRequested;

		foreach (var filter in _webResourceRequestedFilters)
		{
			supports.AddWebResourceRequestedFilter(filter.Uri, filter.ResourceContext, filter.RequestSourceKinds);
		}
	}

	private void OnNativeWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
	{
		RaiseWebResourceRequested(e);
	}

	private void UpsertFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceRequestedFilters.RemoveAll(filter => filter.Equals(uri, resourceContext, requestSourceKinds));
		_webResourceRequestedFilters.Add(new WebResourceRequestedFilter(uri, resourceContext, requestSourceKinds));
	}

	private void RemoveFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceRequestedFilters.RemoveAll(filter => filter.Equals(uri, resourceContext, requestSourceKinds));
	}

	private readonly struct WebResourceRequestedFilter
	{
		internal WebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
		{
			Uri = uri;
			ResourceContext = resourceContext;
			RequestSourceKinds = requestSourceKinds;
		}

		internal string Uri { get; }
		internal CoreWebView2WebResourceContext ResourceContext { get; }
		internal CoreWebView2WebResourceRequestSourceKinds RequestSourceKinds { get; }

		internal bool Equals(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
			=> string.Equals(Uri, uri, StringComparison.OrdinalIgnoreCase)
			&& ResourceContext == resourceContext
			&& RequestSourceKinds == requestSourceKinds;
	}

	private void DetachWebResourceRequestedSupport()
	{
		if (_webResourceRequestedSupport is { })
		{
			_webResourceRequestedSupport.WebResourceRequested -= OnNativeWebResourceRequested;
			_webResourceRequestedSupport = null;
		}
	}
}

