#nullable enable

#if !NET10_0_OR_GREATER
extern alias WpfWebView;
#endif

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DirectN;
using WebView2;
using WebView2.Utilities;
#else
using WpfCoreWebView2HostResourceAccessKind = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind;
using WpfCoreWebView2InitializationCompletedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs;
using WpfCoreWebView2NavigationCompletedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs;
using WpfCoreWebView2NavigationStartingEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs;
using WpfCoreWebView2SourceChangedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs;
using WpfCoreWebView2WebMessageReceivedEventArgs = WpfWebView.Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs;
using WpfWebView2 = WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2;
#endif

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal sealed class WpfNativeWebView : INativeWebView, ISupportsVirtualHostMapping
{
	private readonly WpfWebView2 _nativeWebView;
	private readonly CoreWebView2 _coreWebView2;
	private readonly List<Func<Task>> _actions = new();
	private readonly Dictionary<ulong, string> _navigationIdToUriMap = new();
	private string _documentTitle = string.Empty;

	public WpfNativeWebView(WpfWebView2 nativeWebView, CoreWebView2 coreWebView2)
	{
		_coreWebView2 = coreWebView2;
		_nativeWebView = nativeWebView;
		nativeWebView.NavigationCompleted += NativeWebView_NavigationCompleted;
		nativeWebView.SourceChanged += NativeWebView_SourceChanged;
		nativeWebView.WebMessageReceived += NativeWebView_WebMessageReceived;
		nativeWebView.NavigationStarting += NativeWebView_NavigationStarting;
		nativeWebView.CoreWebView2InitializationCompleted += NativeWebView_CoreWebView2InitializationCompleted;
		_ = nativeWebView.EnsureCoreWebView2Async().ContinueWith(task =>
		{
			if (task.Exception is not null && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Failed to initialize native WPF WebView2.", task.Exception);
			}
		}, TaskContinuationOptions.OnlyOnFaulted);
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
		_coreWebView2.Source = _nativeWebView.Source?.ToString() ?? string.Empty;
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
		_coreWebView2.RaiseNavigationCompleted(uri is null ? null : new Uri(uri), e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
	}

	private async void NativeWebView_CoreWebView2InitializationCompleted(object? sender, WpfCoreWebView2InitializationCompletedEventArgs e)
	{
		if (_nativeWebView.CoreWebView2 is not null)
		{
#if NET10_0_OR_GREATER
			_nativeWebView.HistoryChanged += CoreWebView2_HistoryChanged;
			_nativeWebView.DocumentTitleChanged += OnNativeTitleChanged;
#else
			_nativeWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
			_nativeWebView.CoreWebView2.DocumentTitleChanged += OnNativeTitleChanged;
#endif
			UpdateDocumentTitle();
		}

		foreach (var action in _actions)
		{
			await action();
		}

		_actions.Clear();
	}

	private void OnNativeTitleChanged(object? sender, object e) => UpdateDocumentTitle();

	private void UpdateDocumentTitle()
	{
#if NET10_0_OR_GREATER
		if (_nativeWebView.CoreWebView2 is null)
		{
			DocumentTitle = string.Empty;
			return;
		}

		PWSTR title = default;
		_nativeWebView.CoreWebView2.get_DocumentTitle(out title).ThrowOnError(false);
		DocumentTitle = title.ToStringAndDispose() ?? string.Empty;
#else
		DocumentTitle = _nativeWebView.CoreWebView2?.DocumentTitle ?? string.Empty;
#endif
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
		=> ExecuteEnsuringCoreWebView2(() =>
		{
#if NET10_0_OR_GREATER
			_nativeWebView.Navigate(uri.ToString());
#else
			_nativeWebView.CoreWebView2.Navigate(uri.ToString());
#endif
		});

	public void ProcessNavigation(string html)
		=> ExecuteEnsuringCoreWebView2(() =>
		{
#if NET10_0_OR_GREATER
			_nativeWebView.NavigateToString(html);
#else
			_nativeWebView.CoreWebView2.NavigateToString(html);
#endif
		});

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
		=> ExecuteEnsuringCoreWebView2(() => ProcessNavigationCore(httpRequestMessage));

	private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
#if NET10_0_OR_GREATER
		_nativeWebView.NavigateWithWebResourceRequest(httpRequestMessage);
#else
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			if (header.Key != "Host")
			{
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
			}
		}

		var request = _nativeWebView.CoreWebView2.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content!.ReadAsStream(), builder.ToString());
		_nativeWebView.CoreWebView2.NavigateWithWebResourceRequest(request);
#endif
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
		=> ExecuteEnsuringCoreWebView2(() =>
		{
#if NET10_0_OR_GREATER
			_nativeWebView.ClearVirtualHostNameToFolderMapping(hostName);
#else
			_nativeWebView.CoreWebView2.ClearVirtualHostNameToFolderMapping(hostName);
#endif
		});

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
		=> ExecuteEnsuringCoreWebView2(() =>
		{
#if NET10_0_OR_GREATER
			_nativeWebView.SetVirtualHostNameToFolderMapping(hostName, folderPath, (COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND)accessKind);
#else
			_nativeWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, folderPath, (WpfCoreWebView2HostResourceAccessKind)accessKind);
#endif
		});
}

#if NET10_0_OR_GREATER
internal sealed class WpfWebView2 : HwndHost
{
	private HwndSource? _hwndSource;
	private ICoreWebView2Environment? _environment;
	private ICoreWebView2Controller? _controller;
	private bool _isInitializing;
	private TaskCompletionSource<bool>? _initializationTcs;

	private CoreWebView2NavigationCompletedEventHandler? _navigationCompletedHandler;
	private CoreWebView2NavigationStartingEventHandler? _navigationStartingHandler;
	private CoreWebView2SourceChangedEventHandler? _sourceChangedHandler;
	private CoreWebView2WebMessageReceivedEventHandler? _webMessageReceivedHandler;
	private CoreWebView2HistoryChangedEventHandler? _historyChangedHandler;
	private CoreWebView2DocumentTitleChangedEventHandler? _documentTitleChangedHandler;

	private EventRegistrationToken _navigationCompletedToken;
	private EventRegistrationToken _navigationStartingToken;
	private EventRegistrationToken _sourceChangedToken;
	private EventRegistrationToken _webMessageReceivedToken;
	private EventRegistrationToken _historyChangedToken;
	private EventRegistrationToken _documentTitleChangedToken;

	public ICoreWebView2? CoreWebView2 { get; private set; }

	public bool CanGoBack
	{
		get
		{
			BOOL canGoBack = default;
			if (CoreWebView2 is null || CoreWebView2.get_CanGoBack(ref canGoBack).IsError)
			{
				return false;
			}

			return canGoBack;
		}
	}

	public bool CanGoForward
	{
		get
		{
			BOOL canGoForward = default;
			if (CoreWebView2 is null || CoreWebView2.get_CanGoForward(ref canGoForward).IsError)
			{
				return false;
			}

			return canGoForward;
		}
	}

	public Uri? Source { get; private set; }

	public event EventHandler<WpfCoreWebView2InitializationCompletedEventArgs>? CoreWebView2InitializationCompleted;
	public event EventHandler<WpfCoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
	public event EventHandler<WpfCoreWebView2NavigationStartingEventArgs>? NavigationStarting;
	public event EventHandler<WpfCoreWebView2SourceChangedEventArgs>? SourceChanged;
	public event EventHandler<WpfCoreWebView2WebMessageReceivedEventArgs>? WebMessageReceived;
	public event EventHandler<object>? HistoryChanged;
	public event EventHandler<object>? DocumentTitleChanged;

	public Task EnsureCoreWebView2Async()
	{
		if (CoreWebView2 is not null)
		{
			return Task.CompletedTask;
		}

		_initializationTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		TryInitializeCoreWebView2();
		return _initializationTcs.Task;
	}

	public Task<string?> ExecuteScriptAsync(string script)
	{
		if (CoreWebView2 is null)
		{
			return Task.FromException<string?>(new InvalidOperationException("CoreWebView2 is not initialized."));
		}

		var scriptValue = PWSTR.From(script);
		var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		try
		{
			CoreWebView2.ExecuteScript(scriptValue, new CoreWebView2ExecuteScriptCompletedHandler((error, result) =>
			{
				if (error.IsError)
				{
					tcs.TrySetException(error.GetException() ?? new InvalidOperationException("WebView2 ExecuteScript failed."));
					return;
				}

				tcs.TrySetResult(result.ToStringAndDispose());
			})).ThrowOnError();
		}
		finally
		{
			PWSTR.Dispose(ref scriptValue);
		}

		return tcs.Task;
	}

	public void Navigate(string uri)
	{
		if (CoreWebView2 is null)
		{
			return;
		}

		var uriValue = PWSTR.From(uri);
		try
		{
			CoreWebView2.Navigate(uriValue).ThrowOnError();
		}
		finally
		{
			PWSTR.Dispose(ref uriValue);
		}
	}

	public void NavigateToString(string html)
	{
		if (CoreWebView2 is null)
		{
			return;
		}

		var htmlValue = PWSTR.From(html);
		try
		{
			CoreWebView2.NavigateToString(htmlValue).ThrowOnError();
		}
		finally
		{
			PWSTR.Dispose(ref htmlValue);
		}
	}

	public void NavigateWithWebResourceRequest(HttpRequestMessage httpRequestMessage)
	{
		if (CoreWebView2 is not ICoreWebView2_2 coreWebView22 || _environment is not ICoreWebView2Environment2 environment2)
		{
			Navigate(httpRequestMessage.RequestUri?.ToString() ?? string.Empty);
			return;
		}

		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			if (header.Key != "Host")
			{
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
			}
		}

		var uriValue = PWSTR.From(httpRequestMessage.RequestUri?.ToString() ?? string.Empty);
		var methodValue = PWSTR.From(httpRequestMessage.Method.Method);
		var headersValue = PWSTR.From(builder.ToString());
		DirectN.IStream? postData = null;
		var postDataPtr = IntPtr.Zero;
		try
		{
			byte[] bodyBytes;
			if (httpRequestMessage.Content is null)
			{
				bodyBytes = Array.Empty<byte>();
			}
			else
			{
				using var requestStream = httpRequestMessage.Content.ReadAsStream();
				using var memoryStream = new System.IO.MemoryStream();
				requestStream.CopyTo(memoryStream);
				bodyBytes = memoryStream.ToArray();
			}

			if (bodyBytes.Length > 0)
			{
				postDataPtr = Marshal.AllocHGlobal(bodyBytes.Length);
				Marshal.Copy(bodyBytes, 0, postDataPtr, bodyBytes.Length);
			}

			postData = DirectN.Functions.SHCreateMemStream(postDataPtr, (uint)bodyBytes.Length);
			environment2.CreateWebResourceRequest(uriValue, methodValue, postData, headersValue, out var request).ThrowOnError();
			coreWebView22.NavigateWithWebResourceRequest(request).ThrowOnError();
		}
		finally
		{
			if (postDataPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(postDataPtr);
			}
			PWSTR.Dispose(ref uriValue);
			PWSTR.Dispose(ref methodValue);
			PWSTR.Dispose(ref headersValue);
		}
	}

	public void GoBack() => CoreWebView2?.GoBack().ThrowOnError();

	public void GoForward() => CoreWebView2?.GoForward().ThrowOnError();

	public void Reload() => CoreWebView2?.Reload().ThrowOnError();

	public void Stop() => CoreWebView2?.Stop().ThrowOnError();

	public void ClearVirtualHostNameToFolderMapping(string hostName)
	{
		if (CoreWebView2 is not ICoreWebView2_3 coreWebView23)
		{
			return;
		}

		var hostNameValue = PWSTR.From(hostName);
		try
		{
			coreWebView23.ClearVirtualHostNameToFolderMapping(hostNameValue).ThrowOnError();
		}
		finally
		{
			PWSTR.Dispose(ref hostNameValue);
		}
	}

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND accessKind)
	{
		if (CoreWebView2 is not ICoreWebView2_3 coreWebView23)
		{
			return;
		}

		var hostNameValue = PWSTR.From(hostName);
		var folderPathValue = PWSTR.From(folderPath);
		try
		{
			coreWebView23.SetVirtualHostNameToFolderMapping(hostNameValue, folderPathValue, accessKind).ThrowOnError();
		}
		finally
		{
			PWSTR.Dispose(ref hostNameValue);
			PWSTR.Dispose(ref folderPathValue);
		}
	}

	protected override HandleRef BuildWindowCore(HandleRef hwndParent)
	{
		var parameters = new HwndSourceParameters(nameof(WpfWebView2))
		{
			ParentWindow = hwndParent.Handle,
			WindowStyle = unchecked((int)0x50000000), // WS_CHILD | WS_VISIBLE
			Width = Math.Max((int)ActualWidth, 1),
			Height = Math.Max((int)ActualHeight, 1)
		};

		_hwndSource = new HwndSource(parameters);
		TryInitializeCoreWebView2();

		return new HandleRef(this, _hwndSource.Handle);
	}

	protected override void DestroyWindowCore(HandleRef hwnd)
	{
		if (_controller is not null)
		{
			var result = _controller.Close();
			if (result.IsError && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"WebView2 controller close failed: {result}");
			}
			_controller = null;
		}

		CoreWebView2 = null;
		_environment = null;
		_hwndSource?.Dispose();
		_hwndSource = null;
	}

	protected override void OnWindowPositionChanged(Rect rcBoundingBox)
	{
		base.OnWindowPositionChanged(rcBoundingBox);
		UpdateBounds();
	}

	private void TryInitializeCoreWebView2()
	{
		if (_isInitializing || _hwndSource is null || _initializationTcs is null || CoreWebView2 is not null)
		{
			return;
		}

		_isInitializing = true;

		WebView2Utilities.Initialize(Assembly.GetEntryAssembly(), throwOnError: false);

		WebView2.Functions.CreateCoreWebView2EnvironmentWithOptions(PWSTR.Null, PWSTR.Null, null!, new CoreWebView2CreateCoreWebView2EnvironmentCompletedHandler((environmentResult, environment) =>
		{
			if (environmentResult.IsError)
			{
				var exception = environmentResult.GetException() ?? new InvalidOperationException("Unable to create WebView2 environment.");
				CoreWebView2InitializationCompleted?.Invoke(this, new WpfCoreWebView2InitializationCompletedEventArgs(false, exception));
				_initializationTcs.TrySetException(exception);
				return;
			}

			_environment = environment;
			environment.CreateCoreWebView2Controller((HWND)_hwndSource.Handle, new CoreWebView2CreateCoreWebView2ControllerCompletedHandler((controllerResult, controller) =>
			{
				if (controllerResult.IsError)
				{
					var exception = controllerResult.GetException() ?? new InvalidOperationException("Unable to create WebView2 controller.");
					CoreWebView2InitializationCompleted?.Invoke(this, new WpfCoreWebView2InitializationCompletedEventArgs(false, exception));
					_initializationTcs.TrySetException(exception);
					return;
				}

				_controller = controller;
				UpdateBounds();
				controller.get_CoreWebView2(out var coreWebView2).ThrowOnError();
				CoreWebView2 = coreWebView2;
				RegisterEventHandlers(coreWebView2);
				CoreWebView2InitializationCompleted?.Invoke(this, new WpfCoreWebView2InitializationCompletedEventArgs(true, null));
				_initializationTcs.TrySetResult(true);
			})).ThrowOnError();
		})).ThrowOnError();
	}

	private void RegisterEventHandlers(ICoreWebView2 coreWebView2)
	{
		_navigationStartingHandler = new CoreWebView2NavigationStartingEventHandler((sender, args) =>
		{
			args.get_Uri(out var uriValue).ThrowOnError();
			ulong navigationId = default;
			args.get_NavigationId(ref navigationId).ThrowOnError();
			var eventArgs = new WpfCoreWebView2NavigationStartingEventArgs(uriValue.ToStringAndDispose() ?? string.Empty, navigationId);
			NavigationStarting?.Invoke(this, eventArgs);
			args.put_Cancel(eventArgs.Cancel).ThrowOnError();
		});
		coreWebView2.add_NavigationStarting(_navigationStartingHandler, ref _navigationStartingToken).ThrowOnError();

		_navigationCompletedHandler = new CoreWebView2NavigationCompletedEventHandler((sender, args) =>
		{
			ulong navigationId = default;
			args.get_NavigationId(ref navigationId).ThrowOnError();
			BOOL isSuccess = default;
			args.get_IsSuccess(ref isSuccess).ThrowOnError();
			COREWEBVIEW2_WEB_ERROR_STATUS webErrorStatus = default;
			args.get_WebErrorStatus(ref webErrorStatus).ThrowOnError();
			var httpStatusCode = 0;
			if (args is ICoreWebView2NavigationCompletedEventArgs2 args2)
			{
				args2.get_HttpStatusCode(ref httpStatusCode).ThrowOnError(false);
			}
			else if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("ICoreWebView2NavigationCompletedEventArgs2 is unavailable; HttpStatusCode defaulting to 0.");
			}
			NavigationCompleted?.Invoke(this, new WpfCoreWebView2NavigationCompletedEventArgs(navigationId, isSuccess, httpStatusCode, webErrorStatus));
		});
		coreWebView2.add_NavigationCompleted(_navigationCompletedHandler, ref _navigationCompletedToken).ThrowOnError();

		_sourceChangedHandler = new CoreWebView2SourceChangedEventHandler((sender, args) =>
		{
			sender.get_Source(out var sourceValue).ThrowOnError();
			var source = sourceValue.ToStringAndDispose();
			Source = Uri.TryCreate(source, UriKind.Absolute, out var uri) ? uri : null;
			SourceChanged?.Invoke(this, new WpfCoreWebView2SourceChangedEventArgs());
		});
		coreWebView2.add_SourceChanged(_sourceChangedHandler, ref _sourceChangedToken).ThrowOnError();

		_webMessageReceivedHandler = new CoreWebView2WebMessageReceivedEventHandler((sender, args) =>
		{
			args.get_WebMessageAsJson(out var webMessageValue).ThrowOnError();
			WebMessageReceived?.Invoke(this, new WpfCoreWebView2WebMessageReceivedEventArgs(webMessageValue.ToStringAndDispose() ?? string.Empty));
		});
		coreWebView2.add_WebMessageReceived(_webMessageReceivedHandler, ref _webMessageReceivedToken).ThrowOnError();

		_historyChangedHandler = new CoreWebView2HistoryChangedEventHandler((sender, args) => HistoryChanged?.Invoke(this, EventArgs.Empty));
		coreWebView2.add_HistoryChanged(_historyChangedHandler, ref _historyChangedToken).ThrowOnError();

		_documentTitleChangedHandler = new CoreWebView2DocumentTitleChangedEventHandler((sender, args) => DocumentTitleChanged?.Invoke(this, EventArgs.Empty));
		coreWebView2.add_DocumentTitleChanged(_documentTitleChangedHandler, ref _documentTitleChangedToken).ThrowOnError();
	}

	private void UpdateBounds()
	{
		if (_controller is null)
		{
			return;
		}

		var width = Math.Max((int)ActualWidth, 1);
		var height = Math.Max((int)ActualHeight, 1);
		_controller.put_Bounds(new RECT { left = 0, top = 0, right = width, bottom = height }).ThrowOnError(false);
	}
}

internal sealed class WpfCoreWebView2InitializationCompletedEventArgs(bool isSuccess, Exception? initializationException) : EventArgs
{
	public bool IsSuccess { get; } = isSuccess;

	public Exception? InitializationException { get; } = initializationException;
}

internal sealed class WpfCoreWebView2NavigationStartingEventArgs(string uri, ulong navigationId) : EventArgs
{
	public string Uri { get; } = uri;

	public ulong NavigationId { get; } = navigationId;

	public bool Cancel { get; set; }
}

internal sealed class WpfCoreWebView2NavigationCompletedEventArgs(ulong navigationId, bool isSuccess, int httpStatusCode, COREWEBVIEW2_WEB_ERROR_STATUS webErrorStatus) : EventArgs
{
	public ulong NavigationId { get; } = navigationId;

	public bool IsSuccess { get; } = isSuccess;

	public int HttpStatusCode { get; } = httpStatusCode;

	public COREWEBVIEW2_WEB_ERROR_STATUS WebErrorStatus { get; } = webErrorStatus;
}

internal sealed class WpfCoreWebView2SourceChangedEventArgs : EventArgs;

internal sealed class WpfCoreWebView2WebMessageReceivedEventArgs(string webMessageAsJson) : EventArgs
{
	public string WebMessageAsJson { get; } = webMessageAsJson;
}
#endif
