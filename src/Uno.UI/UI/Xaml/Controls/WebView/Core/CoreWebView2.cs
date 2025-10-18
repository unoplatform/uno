#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
	internal long _navigationId;
	private object? _processedSource;

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
			Source = actualUri.ToString();
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
			Source = requestMessage.RequestUri.ToString();
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

	internal void OnOwnerApplyTemplate()
	{
		_nativeWebView = GetNativeWebViewFromTemplate();

		// Signal that native WebView is now initialized
		_nativeWebViewInitializedTcs?.TrySetResult(true);

		//The native WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		UpdateFromInternalSource();
		OnScrollEnabledChanged(_scrollEnabled);
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

	private TaskCompletionSource<bool>? _nativeWebViewInitializedTcs;
	internal Task EnsureNativeWebViewAsync()
	{
		if (_nativeWebView != null)
		{
			return Task.CompletedTask;
		}

		_nativeWebViewInitializedTcs ??= new TaskCompletionSource<bool>();

		return _nativeWebViewInitializedTcs.Task;
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
}
