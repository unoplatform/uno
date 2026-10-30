#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.Web.WebView2.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Extensibility;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal const string BlankUrl = "about:blank";
	internal const string DataUriFormatString = "data:text/html;charset=utf-8;base64,{0}";
	internal static readonly Uri BlankUri = new Uri(BlankUrl);

	// WinUI's message for a member called on a closed WebView2 (s_error_wv2_closed).
	internal const string ClosedMessage = "Cannot create CoreWebView2 for Closed WebView2 element.";

	private readonly Dictionary<string, string> _hostToFolderMap = new();
	private readonly IWebView _owner;

	private bool _scrollEnabled = true;
	private INativeWebView? _nativeWebView;
	private ISupportsWebResourceRequested? _webResourceRequestedSupport;
	private readonly List<WebResourceRequestedFilter> _webResourceRequestedFilters = new();
	internal long _navigationId;
	private object? _processedSource;
	// As in WinUI, a closed CoreWebView2 raises no more events, even when a handler closes it during an earlier event of the
	// same navigation.
	private bool _isClosed;

	internal CoreWebView2(IWebView owner)
	{
		HostToFolderMap = _hostToFolderMap.AsReadOnly();
		_owner = owner;
	}

	internal IWebView Owner => _owner;

	internal IReadOnlyDictionary<string, string> HostToFolderMap { get; }

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

	/// <summary>
	/// Resolves an optional capability of the native WebView, or throws describing why it is unavailable.
	/// </summary>
	internal T RequireCapability<T>(string typeName, string memberName)
		where T : class
		=> _nativeWebView as T ?? throw CapabilityUnavailable(typeName, memberName);

	/// <summary>
	/// Builds the exception for a capability the native WebView cannot serve, distinguishing "not initialized
	/// yet" from "this platform does not implement it". The latter reproduces the generated stub exactly, so
	/// platforms that do not opt in keep their current behaviour and not-implemented reporting.
	/// </summary>
	internal Exception CapabilityUnavailable(string typeName, string memberName)
		=> _nativeWebView is null
			? new InvalidOperationException(
				$"The native WebView is not initialized, so {typeName}.{memberName} is unavailable. " +
				"Await the WebView2 control's EnsureCoreWebView2Async() before using this member.")
			: global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(typeName, memberName);

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

	internal async Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken ct)
	{
		if (_nativeWebView is null)
		{
			return null;
		}

		return await _nativeWebView.InvokeScriptAsync(script, arguments, ct);
	}

	/// <summary>
	/// Whether <see cref="Close"/> can release the native WebView: there is none yet, or it can release its browser.
	/// </summary>
	internal bool CanClose => _nativeWebView is null or ISupportsClose;

	/// <summary>
	/// Releases the native WebView when its WebView2 is closed. Check <see cref="CanClose"/> first.
	/// </summary>
	internal void Close()
	{
		Debug.Assert(CanClose, "The caller must check CanClose first, or the native WebView wouldn't be released.");

		var nativeWebView = _nativeWebView;
		_isClosed = true;
		_nativeWebView = null;
		DetachWebResourceRequestedSupport();

		// Unlike WinUI, which completes a creation already in flight, Uno waits for the template to create the native WebView,
		// which a closed WebView2 never does. So a pending EnsureCoreWebView2Async fails, as a call made after Close does.
		if (_nativeWebViewInitializedTcs.TrySetException(CreateClosedException()))
		{
			// Nothing may be waiting for it; don't report the exception as unobserved.
			_ = _nativeWebViewInitializedTcs.Task.Exception;
		}

		// Release the browser last, so the WebView2 is closed even if releasing it fails.
		ReleaseNativeWebView(nativeWebView);
	}

	// Without an object name, as the exception WinUI's RO_E_CLOSED maps to, so the message is WinUI's alone.
	internal static ObjectDisposedException CreateClosedException() => new(objectName: null, ClosedMessage);

	internal void OnOwnerApplyTemplate()
	{
		if (_isClosed)
		{
			// As in WinUI, a closed WebView2 doesn't create a browser again.
			return;
		}

		DetachWebResourceRequestedSupport();

		// The new template gets its own native WebView, so release the one it replaces.
		ReleaseNativeWebView(_nativeWebView);
		_nativeWebView = GetNativeWebViewFromTemplate();
		AttachWebResourceRequestedSupport();

		// Signal that native WebView is now initialized
		_nativeWebViewInitializedTcs.TrySetResult(true);

		//The native WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		UpdateFromInternalSource();
		OnScrollEnabledChanged(_scrollEnabled);
	}

	// WinUI's Close has no documented failure, so closing or retemplating the control doesn't fail when its browser can't be released.
	private void ReleaseNativeWebView(INativeWebView? nativeWebView)
	{
		try
		{
			(nativeWebView as ISupportsClose)?.Close();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Could not release the native WebView.", e);
			}
		}
	}

	internal void OnScrollEnabledChanged(bool newValue)
	{
		_scrollEnabled = newValue;
		_nativeWebView?.SetScrollingEnabled(newValue);
	}

	internal void OnDocumentTitleChanged()
	{
		if (!_isClosed)
		{
			DocumentTitleChanged?.Invoke(this, null);
		}
	}

	internal void RaiseNavigationStarting(object? navigationData, out bool cancel)
	{
		if (_isClosed)
		{
			cancel = true;
			return;
		}

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
		if (_isClosed)
		{
			handled = false;
			return;
		}

		var args = new CoreWebView2NewWindowRequestedEventArgs(target, referer);
		NewWindowRequested?.Invoke(this, args);

		handled = args.Handled;
	}

	internal void RaiseNavigationCompleted(Uri? uri, bool isSuccess, int httpStatusCode, CoreWebView2WebErrorStatus errorStatus, bool shouldSetSource = true)
	{
		if (_isClosed)
		{
			return;
		}

		if (shouldSetSource)
		{
			Source = (uri ?? BlankUri).ToString();
		}

		// A SourceChanged handler may have closed it.
		if (_isClosed)
		{
			return;
		}

		NavigationCompleted?.Invoke(this, new CoreWebView2NavigationCompletedEventArgs((ulong)_navigationId, uri, isSuccess, httpStatusCode, errorStatus));
	}

	internal void RaiseHistoryChanged()
	{
		if (!_isClosed)
		{
			HistoryChanged?.Invoke(this, null);
		}
	}

	internal void SetHistoryProperties(bool canGoBack, bool canGoForward)
	{
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;
	}

	internal void RaiseWebMessageReceived(string message)
	{
		if (_isClosed)
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
				() =>
				{
					if (!_isClosed)
					{
						WebMessageReceived?.Invoke(this, new(message));
					}
				});
		}
	}

	internal void RaiseUnsupportedUriSchemeIdentified(Uri targetUri, out bool handled)
	{
		if (_isClosed)
		{
			handled = false;
			return;
		}

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
		if (!_isClosed)
		{
			WebResourceRequested?.Invoke(this, eventArgs);
		}
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
			if (_owner.IsLoaded && !_isClosed)
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

	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		if (VisualTreeHelper.GetChild((DependencyObject)_owner, 0) is not ContentPresenter { Name: "WebViewTemplateRoot" } contentPresenter)
		{
			return null;
		}

		if (ApiExtensibility.CreateInstance<INativeWebViewProvider>(this, out var nativeWebViewProvider))
		{
			return nativeWebViewProvider.CreateNativeWebView(contentPresenter);
		}

		return null;
	}
}

