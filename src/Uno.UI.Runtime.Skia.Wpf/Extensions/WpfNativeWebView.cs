#nullable enable

extern alias WpfWebView;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

using WpfCoreWebView2HostResourceAccessKind = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind;
using WpfCoreWebView2InitializationCompletedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs;
using WpfCoreWebView2NavigationCompletedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs;
using WpfCoreWebView2NavigationStartingEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs;
using WpfCoreWebView2SourceChangedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs;
using WpfCoreWebView2WebMessageReceivedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs;
using WpfWebView2 = WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal sealed class WpfNativeWebView : INativeWebView, ISupportsVirtualHostMapping
{
	private WpfWebView2 _nativeWebView;
	private CoreWebView2 _coreWebView2;
	private List<Func<Task>> _actions = new();
	private Dictionary<ulong, string> _navigationIdToUriMap = new();
	private string _documentTitle = string.Empty;
	private bool _isDisposed;

	public WpfNativeWebView(WpfWebView2 nativeWebView, CoreWebView2 coreWebView2)
	{
		_coreWebView2 = coreWebView2;
		_nativeWebView = nativeWebView;
		nativeWebView.NavigationCompleted += NativeWebView_NavigationCompleted;
		nativeWebView.SourceChanged += NativeWebView_SourceChanged;
		nativeWebView.WebMessageReceived += NativeWebView_WebMessageReceived;
		nativeWebView.NavigationStarting += NativeWebView_NavigationStarting;
		nativeWebView.CoreWebView2InitializationCompleted += NativeWebView_CoreWebView2InitializationCompleted;
		nativeWebView.EnsureCoreWebView2Async();
	}

	public string DocumentTitle
	{
		get => _documentTitle;
		private set
		{
			if (_documentTitle != value)
			{
				_documentTitle = value;
				_coreWebView2.OnDocumentTitleChanged();
			}
		}
	}

	private void NativeWebView_SourceChanged(object? sender, WpfCoreWebView2SourceChangedEventArgs e)
	{
		_coreWebView2.Source = _nativeWebView.Source.ToString();
	}

	private void NativeWebView_WebMessageReceived(object? sender, WpfCoreWebView2WebMessageReceivedEventArgs e)
	{
		_coreWebView2.RaiseWebMessageReceived(e.WebMessageAsJson);
	}

	private void NativeWebView_NavigationStarting(object? sender, WpfCoreWebView2NavigationStartingEventArgs e)
	{
		_coreWebView2.RaiseNavigationStarting(e.Uri, out var cancel);
		_coreWebView2.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		e.Cancel = cancel;
		_navigationIdToUriMap[e.NavigationId] = e.Uri;
	}

	private void NativeWebView_NavigationCompleted(object? sender, WpfCoreWebView2NavigationCompletedEventArgs e)
	{
		if (!_navigationIdToUriMap.TryGetValue(e.NavigationId, out var uri))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Got NavigationCompleted for unknown navigation id");
			}
		}

		_navigationIdToUriMap.Remove(e.NavigationId);
		// The source is set through NativeWebView_SourceChanged
		// Note that when using NavigateToString on WinUI, the NavigationCompleted event on WinUI has uri containing base64 of the passed string, while source becomes about:blank.
		// On WPF, we already have the same behavior for free. _coreWebView.Source becomes about:blank and the event arguments of NavigationCompleted contains the base64 value.
		// So, we should skip setting the source from base64.
		_coreWebView2.RaiseNavigationCompleted(uri is null ? null : new Uri(uri), e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
	}

	private async void NativeWebView_CoreWebView2InitializationCompleted(object? sender, WpfCoreWebView2InitializationCompletedEventArgs e)
	{
		_nativeWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		_nativeWebView.CoreWebView2.DocumentTitleChanged += OnNativeTitleChanged;
		UpdateDocumentTitle();

		foreach (var action in _actions)
		{
			await action();
		}

		_actions.Clear();
	}

	private void OnNativeTitleChanged(object? sender, object e) => UpdateDocumentTitle();

	private void UpdateDocumentTitle()
	{
		DocumentTitle = _nativeWebView.CoreWebView2.DocumentTitle;
	}

	private void CoreWebView2_HistoryChanged(object? sender, object e)
	{
		_coreWebView2.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		_coreWebView2.RaiseHistoryChanged();
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.ExecuteScriptAsync(script));

	public void GoBack()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.GoBack);

	public void GoForward()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.GoForward);

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		if (arguments is null || arguments.Length == 0)
		{
			return ExecuteScriptAsync($"{script}()", token);
		}

		var adjustedScript = new StringBuilder(script);
		adjustedScript.Append('(');

		for (int i = 0; i < arguments.Length; i++)
		{
			adjustedScript.Append('"');
			adjustedScript.Append(arguments[i]);
			adjustedScript.Append('"');

			if (i < arguments.Length - 1)
			{
				adjustedScript.Append(',');
			}
		}

		adjustedScript.Append(')');
		return ExecuteScriptAsync(adjustedScript.ToString(), token);
	}

	public void ProcessNavigation(Uri uri)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.CoreWebView2.Navigate(uri.ToString()));

	public void ProcessNavigation(string html)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.CoreWebView2.NavigateToString(html));

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
		=> ExecuteEnsuringCoreWebView2(() => ProcessNavigationCore(httpRequestMessage));

	private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			// https://github.com/MicrosoftEdge/WebView2Feedback/issues/2250#issuecomment-1201765363
			// WebView2 doesn't like when you try to set some headers manually
			if (header.Key != "Host")
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
		}

		var request = _nativeWebView.CoreWebView2.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content!.ReadAsStream(), builder.ToString());
		_nativeWebView.CoreWebView2.NavigateWithWebResourceRequest(request);
	}

	public void Reload()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.Reload);

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
	}

	public void Stop()
		=> ExecuteEnsuringCoreWebView2(_nativeWebView.Stop);

	private void ExecuteEnsuringCoreWebView2(Action action)
	{
		if (_nativeWebView.CoreWebView2 is not null)
		{
			action();
			return;
		}

		_actions.Add(() =>
		{
			action();
			return Task.CompletedTask;
		});
	}

	private Task<T> ExecuteEnsuringCoreWebView2<T>(Func<Task<T>> task)
	{
		if (_nativeWebView.CoreWebView2 is not null)
		{
			return task();
		}

		var tcs = new TaskCompletionSource<T>();
		_actions.Add(async () =>
		{
			tcs.SetResult(await task());
		});

		return tcs.Task;
	}

	public void ClearVirtualHostNameToFolderMapping(string hostName)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.CoreWebView2.ClearVirtualHostNameToFolderMapping(hostName));

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
		=> ExecuteEnsuringCoreWebView2(() => _nativeWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, folderPath, (WpfCoreWebView2HostResourceAccessKind)accessKind));

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		_nativeWebView.NavigationCompleted -= NativeWebView_NavigationCompleted;
		_nativeWebView.SourceChanged -= NativeWebView_SourceChanged;
		_nativeWebView.WebMessageReceived -= NativeWebView_WebMessageReceived;
		_nativeWebView.NavigationStarting -= NativeWebView_NavigationStarting;
		_nativeWebView.CoreWebView2InitializationCompleted -= NativeWebView_CoreWebView2InitializationCompleted;

		if (_nativeWebView.CoreWebView2 is { } coreWebView2)
		{
			coreWebView2.HistoryChanged -= CoreWebView2_HistoryChanged;
			coreWebView2.DocumentTitleChanged -= OnNativeTitleChanged;
		}

		_actions.Clear();
		_navigationIdToUriMap.Clear();
	}
}
