#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
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
	private readonly CoreWebView2Environment _defaultEnvironment = new(browserExecutableFolder: null, userDataFolder: null, options: null);

	private bool _scrollEnabled = true;
	private INativeWebView? _nativeWebView;
	private ISupportsWebResourceRequested? _webResourceRequestedSupport;
	private readonly List<WebResourceRequestedFilter> _webResourceRequestedFilters = new();
	internal long _navigationId;
	private object? _processedSource;
	private bool _initializationRequested;
	private bool _isClosed;
#if __SKIA__
	private INativeWebView? _loadedNativeWebView;
#endif

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
	internal bool HasNativeWebView => _nativeWebView is not null;

	internal CoreWebView2Environment? CustomEnvironment { get; private set; }

	internal CoreWebView2ControllerOptions? CustomControllerOptions { get; private set; }

	internal void SetCustomEnvironment(CoreWebView2Environment? environment, CoreWebView2ControllerOptions? controllerOptions)
	{
		if (environment is null && controllerOptions is null)
		{
			return;
		}

		environment ??= _defaultEnvironment;

		if (_nativeWebView is not null)
		{
			if (ReferenceEquals(Environment, environment) && ReferenceEquals(CustomControllerOptions, controllerOptions))
			{
				return;
			}

			throw new ArgumentException("CoreWebView2 has already been initialized with a different environment or controller options.", nameof(environment));
		}
		else if (_initializationRequested)
		{
			// WinUI lets concurrent EnsureCoreWebView2Async calls await the existing
			// creation request. Arguments supplied by later calls do not replace the
			// environment or controller options captured by the first request.
			return;
		}

		ValidateEnvironmentForCurrentPlatform(environment, controllerOptions);
		CustomEnvironment = environment;
		CustomControllerOptions = controllerOptions;
	}

	private static void ValidateEnvironmentForCurrentPlatform(CoreWebView2Environment environment, CoreWebView2ControllerOptions? controllerOptions)
	{
		if (OperatingSystem.IsBrowser())
		{
			if (!string.IsNullOrEmpty(environment.BrowserExecutableFolder)
				|| !string.IsNullOrEmpty(environment.UserDataFolder)
				|| environment.Options?.HasNonDefaultValues == true
				|| controllerOptions is { IsInPrivateModeEnabled: true }
				|| controllerOptions?.HasUnsupportedWebKitOptions == true)
			{
				throw new NotSupportedException("Custom CoreWebView2 environments and controller options are not supported by the WebAssembly browser host.");
			}
		}
		else if (OperatingSystem.IsMacOS())
		{
			if (!string.IsNullOrEmpty(environment.BrowserExecutableFolder)
				|| !string.IsNullOrEmpty(environment.UserDataFolder)
				|| environment.Options?.HasNonDefaultValues == true
				|| controllerOptions?.HasUnsupportedWebKitOptions == true)
			{
				throw new NotSupportedException("The Skia macOS WebKit host supports only the IsInPrivateModeEnabled controller option.");
			}
		}
	}

	internal IReadOnlyDictionary<string, string> HostToFolderMap { get; }

	private CoreWebView2CookieManager? _cookieManager;

	/// <summary>
	/// Gets the cookie manager for the running WebView. Calls into the manager
	/// will throw NotSupportedException on platforms that cannot enumerate cookies
	/// (notably the WebAssembly browser host).
	/// </summary>
	public CoreWebView2CookieManager CookieManager
	{
		get
		{
			ThrowIfClosed();
			return _cookieManager ??= new CoreWebView2CookieManager(this);
		}
	}

	public CoreWebView2Environment Environment => CustomEnvironment ?? _defaultEnvironment;

#if __SKIA__
	internal void OnLoaded()
	{
		if (!_isClosed)
		{
			if (_initializationRequested && _nativeWebView is null)
			{
				OnOwnerApplyTemplate();
			}

			EnsureNativeWebViewLoaded();
		}
	}

	internal void OnUnloaded()
	{
		(_loadedNativeWebView as ICleanableNativeWebView)?.OnUnloaded();
		_loadedNativeWebView = null;
	}

	private void EnsureNativeWebViewLoaded()
	{
		if (_owner.IsLoaded
			&& _nativeWebView is ICleanableNativeWebView cleanable
			&& !ReferenceEquals(_loadedNativeWebView, _nativeWebView))
		{
			cleanable.OnLoaded();
			_loadedNativeWebView = _nativeWebView;
		}
	}
#endif

	/// <summary>
	/// Gets the CoreWebView2Settings object contains various modifiable
	/// settings for the running WebView.
	/// </summary>
	public CoreWebView2Settings Settings { get; } = new();

	public void Navigate(string uri)
	{
		ThrowIfClosed();
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
		ThrowIfClosed();
		_processedSource = htmlContent;
		if (_owner.SwitchSourceBeforeNavigating)
		{
			Source = BlankUrl;
		}

		UpdateFromInternalSource();
	}

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
	{
		ThrowIfClosed();
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
		ThrowIfClosed();
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
		ThrowIfClosed();
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

	public void GoBack()
	{
		ThrowIfClosed();
		_nativeWebView?.GoBack();
	}

	public void GoForward()
	{
		ThrowIfClosed();
		_nativeWebView?.GoForward();
	}

	public void Stop()
	{
		ThrowIfClosed();
		_nativeWebView?.Stop();
	}

	public void Reload()
	{
		ThrowIfClosed();
		_nativeWebView?.Reload();
	}

	public IAsyncOperation<string?> ExecuteScriptAsync(string javaScript)
	{
		ThrowIfClosed();
		return AsyncOperation.FromTask(ct =>
		{
			if (_nativeWebView is null)
			{
				return Task.FromResult<string?>(null);
			}

			return _nativeWebView.ExecuteScriptAsync(javaScript, ct);
		});
	}

	/// <summary>
	/// Posts a message that is received by JavaScript code in the page via
	/// window.chrome.webview.addEventListener('message', handler). The argument
	/// is treated as a JSON-encoded value and made available as event.data.
	/// </summary>
	public void PostWebMessageAsJson(string webMessageAsJson)
	{
		ThrowIfClosed();
		if (webMessageAsJson is null)
		{
			throw new ArgumentNullException(nameof(webMessageAsJson));
		}

		try
		{
			using var _ = JsonDocument.Parse(webMessageAsJson);
		}
		catch (JsonException ex)
		{
			throw new ArgumentException("The message must contain one valid JSON value.", nameof(webMessageAsJson), ex);
		}

		EnsureWebMessagingEnabled();

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
		ThrowIfClosed();
		if (webMessageAsString is null)
		{
			throw new ArgumentNullException(nameof(webMessageAsString));
		}

		EnsureWebMessagingEnabled();

		if (_nativeWebView is ISupportsPostWebMessage native)
		{
			native.PostWebMessageAsString(webMessageAsString);
		}
		else
		{
			DispatchPostWebMessage(webMessageAsString, isJson: false);
		}
	}

	private void EnsureWebMessagingEnabled()
	{
		if (!Settings.IsWebMessageEnabled)
		{
			throw new UnauthorizedAccessException("Web messaging is disabled by CoreWebView2Settings.IsWebMessageEnabled.");
		}
	}

	private void DispatchPostWebMessage(string payload, bool isJson)
	{
		if (_nativeWebView is null)
		{
			return;
		}

		// Providers install this bridge at document start when possible. Keep the
		// setup here as a fallback for engines that can only inject on demand.
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
					"window.chrome.webview.__unoDispatchMessage=function(d){" +
						"var ev=typeof MessageEvent==='function'?new MessageEvent('message',{data:d}):{data:d};" +
						"window.chrome.webview.__unoListeners.slice().forEach(function(h){try{h(ev);}catch(e){}});" +
					"};" +
				"}" +
				"var d=" + literal + ";" +
				"window.chrome.webview.__unoDispatchMessage(d);" +
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
		ThrowIfClosed();
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
		ThrowIfClosed();
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
		ThrowIfClosed();
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
	public void ShowPrintUI(CoreWebView2PrintDialogKind printDialogKind)
	{
		ThrowIfClosed();
		if (_nativeWebView is not ISupportsPrint print)
		{
			throw new NotSupportedException("CoreWebView2.ShowPrintUI is not supported on this platform.");
		}

		_ = print.ShowPrintUIAsync(printDialogKind, CancellationToken.None);
	}

	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2ContentLoadingEventArgs>? ContentLoading;
	public event global::Windows.Foundation.TypedEventHandler<CoreWebView2, CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;

	internal void RaiseContentLoading(CoreWebView2ContentLoadingEventArgs args) => ContentLoading?.Invoke(this, args);
	internal void RaiseDOMContentLoaded(CoreWebView2DOMContentLoadedEventArgs args) => DOMContentLoaded?.Invoke(this, args);
	internal void RaiseContentLoading(bool isErrorPage = false) => RaiseContentLoading(new CoreWebView2ContentLoadingEventArgs(isErrorPage, (ulong)_navigationId));
	internal void RaiseDOMContentLoaded() => RaiseDOMContentLoaded(new CoreWebView2DOMContentLoadedEventArgs((ulong)_navigationId));

	internal void OnOwnerApplyTemplate()
	{
		if (_isClosed || (_owner.RequiresExplicitInitialization && !_initializationRequested))
		{
			return;
		}

		try
		{
			DetachWebResourceRequestedSupport();
			_nativeWebView = GetNativeWebViewFromTemplate();
			if (_nativeWebView is null)
			{
				_nativeWebViewInitializedTcs.TrySetException(
					new InvalidOperationException("The WebView2 control template did not create a native WebView."));
				return;
			}

			AttachWebResourceRequestedSupport();
			ApplySettingsToNativeWebView();
#if __SKIA__
			EnsureNativeWebViewLoaded();
#endif

			// Signal that native WebView is now initialized before applying a queued
			// Source, matching WinUI's CoreWebView2Initialized event ordering.
			_nativeWebViewInitializedTcs.TrySetResult(true);

			// The native WebView already navigates to a blank page if no source is set.
			// Avoid a bug where invoking GoBack() on WebView does nothing in Android 4.4.
			UpdateFromInternalSource();
			OnScrollEnabledChanged(_scrollEnabled);
		}
		catch (Exception error)
		{
			_nativeWebViewInitializedTcs.TrySetException(error);
			throw;
		}
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
		if (!Settings.IsWebMessageEnabled)
		{
			return;
		}

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
		ThrowIfClosed();
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
		ThrowIfClosed();
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



	private readonly TaskCompletionSource<bool> _nativeWebViewInitializedTcs = new();
	internal Task EnsureNativeWebViewAsync()
	{
		_initializationRequested = true;
		if (_owner.IsLoaded && _nativeWebView is null)
		{
			OnOwnerApplyTemplate();
		}

		return _nativeWebViewInitializedTcs.Task;
	}

	internal void Close()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;
		DetachWebResourceRequestedSupport();
		var nativeWebView = _nativeWebView;
		_nativeWebView = null;
		_processedSource = null;

		try
		{
			(nativeWebView as ICleanableNativeWebView)?.OnUnloaded();
#if __SKIA__
			_loadedNativeWebView = null;
#endif
			if (nativeWebView is ISupportsClose close)
			{
				close.Close();
			}
			else
			{
				nativeWebView?.Stop();
			}
		}
		finally
		{
			SetHistoryProperties(canGoBack: false, canGoForward: false);
			_nativeWebViewInitializedTcs.TrySetException(new ObjectDisposedException(nameof(CoreWebView2)));
		}
	}

	private void ThrowIfClosed()
	{
		if (_isClosed)
		{
			throw new ObjectDisposedException(nameof(CoreWebView2));
		}
	}
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

