#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;

using Windows.Storage;
using Windows.Win32;

using DirectN;

using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;

using WebView2Utilities = WebView2.Utilities.WebView2Utilities;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32NativeAotWebView : Win32NativeWebViewBase, ISupportsVirtualHostMapping, ISupportsWebResourceRequested
{
	private readonly CoreWebView2 _coreWebView;
	private readonly WebView2.ICoreWebView2_22 _nativeWebView;
	private readonly WebView2.ICoreWebView2Controller _controller;

	private Dictionary<ulong, string> _navigationIdToUriMap = new();
	private string _documentTitle = string.Empty;

	private static bool _webView2LoaderLoaded;

	private static void InitializeWebView2Loader()
	{
		if (_webView2LoaderLoaded)
		{
			return;
		}

		// WebView2Utilities.Initialize probes only next to Environment.ProcessPath, which is the
		// dotnet host's directory when launched as `dotnet app.dll`. Resolve through the runtime
		// first (honors deps.json and AppContext.BaseDirectory), then fall back to the library's
		// own probing (embedded resource, process directory).
		if (!NativeLibrary.TryLoad("WebView2Loader.dll", typeof(WebView2.Functions).Assembly, null, out _))
		{
			WebView2Utilities.Initialize(Assembly.GetEntryAssembly());
		}

		_webView2LoaderLoaded = true;
	}

	public Win32NativeAotWebView(CoreWebView2 owner, ContentPresenter presenter)
	: base(presenter)
	{
		_coreWebView = owner;

		ForwardBackgroundToPresenter();

		var tcs = new TaskCompletionSource<(WebView2.ICoreWebView2Controller controller, WebView2.ICoreWebView2_22 webView)>();
		NativeDispatcher.Main.EnqueueAsync(async () =>
		{
			try
			{
				InitializeWebView2Loader();
				var userDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2");

				// These options must be applied at environment creation time; CoreWebView2EnvironmentOptions cannot be
				// changed once the environment exists. They are surfaced through FeatureConfiguration.WebView2 because Uno
				// owns this CreateAsync call (the app never sees the CoreWebView2Environment), so it's the only injection point.
				var options = new WebView2.CoreWebView2EnvironmentOptions();
				options.put_AllowSingleSignOnUsingOSPrimaryAccount(
					FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount ? BOOL.TRUE : BOOL.FALSE
				).ThrowOnError();
				var additionalBrowserArguments = FeatureConfiguration.WebView2.AdditionalBrowserArguments;
				if (!string.IsNullOrEmpty(additionalBrowserArguments))
				{
					unsafe
					{
						fixed (char* p_args = additionalBrowserArguments)
						{
							options.put_AdditionalBrowserArguments(new PWSTR(p_args)).ThrowOnError();
						}
					}
				}

				CreateCoreWebView2Environment(userDataFolder, options, new WebView2.Utilities.CoreWebView2CreateCoreWebView2EnvironmentCompletedHandler((errorCode, environment) =>
				{
					if (errorCode.IsError)
					{
						tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("Failed to create CoreWebView2 environment."));
						return;
					}

					environment.CreateCoreWebView2Controller(new DirectN.HWND((IntPtr)Hwnd.Value), new WebView2.Utilities.CoreWebView2CreateCoreWebView2ControllerCompletedHandler((controllerError, controller) =>
					{
						if (controllerError.IsError)
						{
							tcs.TrySetException(controllerError.GetException() ?? new InvalidOperationException("Failed to create CoreWebView2 controller."));
							return;
						}

						controller.get_CoreWebView2(out var coreWebView).ThrowOnError();
						tcs.TrySetResult((controller, (WebView2.ICoreWebView2_22)coreWebView));
					})).ThrowOnError();
				}));
			}
			catch (Exception e)
			{
				tcs.TrySetException(e);
			}
		});

		while (!tcs.Task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		(_controller, _nativeWebView) = tcs.Task.Result;

		_controller.put_IsVisible(BOOL.FALSE).ThrowOnError();
		_controller.put_Bounds(new DirectN.RECT { left = 0, top = 0, right = 500, bottom = 500 }).ThrowOnError();

		_nativeWebView.get_Settings(out var settings).ThrowOnError();
		settings.put_IsScriptEnabled(BOOL.TRUE).ThrowOnError();
		settings.put_IsWebMessageEnabled(BOOL.TRUE).ThrowOnError();
		settings.put_AreDefaultScriptDialogsEnabled(BOOL.TRUE).ThrowOnError();
		settings.put_AreDevToolsEnabled(FeatureConfiguration.WebView2.EnableDevTools ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();

		var weakRef = new WeakReference<Win32NativeAotWebView>(this);
		var eventToken = new WebView2.EventRegistrationToken();

		_nativeWebView.add_NavigationCompleted(
		new WebView2.Utilities.CoreWebView2NavigationCompletedEventHandler((_, args) =>
		{
			if (weakRef.TryGetTarget(out var target)) target.NativeWebView_NavigationCompleted(args);
		}),
		ref eventToken).ThrowOnError();

		_nativeWebView.add_NewWindowRequested(
		new WebView2.Utilities.CoreWebView2NewWindowRequestedEventHandler((_, args) =>
		{
			if (weakRef.TryGetTarget(out var target)) target.NativeWebView_NewWindowRequested(args);
		}),
		ref eventToken).ThrowOnError();

		_nativeWebView.add_SourceChanged(
			new WebView2.Utilities.CoreWebView2SourceChangedEventHandler((_, args) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.NativeWebView_SourceChanged(args);
			}),
			ref eventToken
		).ThrowOnError();

		_nativeWebView.add_WebMessageReceived(
			new WebView2.Utilities.CoreWebView2WebMessageReceivedEventHandler((_, args) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.NativeWebView_WebMessageReceived(args);
			}),
			ref eventToken
		).ThrowOnError();

		_nativeWebView.add_NavigationStarting(
			new WebView2.Utilities.CoreWebView2NavigationStartingEventHandler((_, args) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.NativeWebView_NavigationStarting(args);
			}),
			ref eventToken
		).ThrowOnError();

		_nativeWebView.add_HistoryChanged(
			new WebView2.Utilities.CoreWebView2HistoryChangedEventHandler((_, _) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.CoreWebView2_HistoryChanged();
			}),
			ref eventToken
		).ThrowOnError();

		_nativeWebView.add_DocumentTitleChanged(
			new WebView2.Utilities.CoreWebView2DocumentTitleChangedEventHandler((_, _) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.UpdateDocumentTitle();
			}),
			ref eventToken
		).ThrowOnError();

		_nativeWebView.add_WebResourceRequested(
			new WebView2.Utilities.CoreWebView2WebResourceRequestedEventHandler((_, args) =>
			{
				if (weakRef.TryGetTarget(out var target)) target.NativeWebView2_WebResourceRequested(args);
			}),
			ref eventToken
		).ThrowOnError();

		UpdateDocumentTitle();
	}

	private void ForwardBackgroundToPresenter()
	{
		if (_coreWebView.Owner is Microsoft.UI.Xaml.Controls.WebView2 view)
		{
			Presenter.SetBinding(FrameworkElement.BackgroundProperty, new Binding()
			{
				Path = new(nameof(view.Background)),
				Source = view,
				Mode = BindingMode.OneWay
			});
		}
	}

	protected override void OnWindowSizeChanged()
	{
		if (_controller is not null)
		{
			PInvoke.GetClientRect(Hwnd, out var bounds);
			_controller.put_Bounds(new DirectN.RECT { left = bounds.left, top = bounds.top, right = bounds.right, bottom = bounds.bottom }).ThrowOnError();
		}
	}

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	private void NativeWebView2_WebResourceRequested(WebView2.ICoreWebView2WebResourceRequestedEventArgs e)
	{
		WebResourceRequested?.Invoke(this, new(new AotWebResourceRequestedEventArgsWrapper(e)));
	}

	public override string DocumentTitle => _documentTitle;

	private void SetDocumentTitle(string value)
	{
		if (_documentTitle != value)
		{
			_documentTitle = value;
			_coreWebView.OnDocumentTitleChanged();
		}
	}

	private void NativeWebView_SourceChanged(WebView2.ICoreWebView2SourceChangedEventArgs e)
	{
		_nativeWebView.get_Source(out var source).ThrowOnError();
		_coreWebView.Source = source.ToString();
	}

	private void NativeWebView_WebMessageReceived(WebView2.ICoreWebView2WebMessageReceivedEventArgs e)
	{
		e.get_WebMessageAsJson(out var json).ThrowOnError();
		_coreWebView.RaiseWebMessageReceived(json.ToString()!);
	}

	private void NativeWebView_NavigationStarting(WebView2.ICoreWebView2NavigationStartingEventArgs e)
	{
		e.get_Uri(out var uriPwstr).ThrowOnError();
		var uriString = uriPwstr.ToString();

		if (uriString is null)
		{
			return;
		}

		bool cancel;
		if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
		{
			_coreWebView.RaiseNavigationStarting(uri, out cancel);
		}
		else
		{
			_coreWebView.RaiseNavigationStarting(uriString, out cancel);
		}

		BOOL canGoBack = default;
		BOOL canGoForward = default;
		_nativeWebView.get_CanGoBack(ref canGoBack).ThrowOnError();
		_nativeWebView.get_CanGoForward(ref canGoForward).ThrowOnError();
		_coreWebView.SetHistoryProperties(canGoBack.Value != 0, canGoForward.Value != 0);

		e.put_Cancel(cancel ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();

		ulong navigationId = default;
		e.get_NavigationId(ref navigationId).ThrowOnError();
		_navigationIdToUriMap[navigationId] = uriString;
	}

	private void NativeWebView_NavigationCompleted(WebView2.ICoreWebView2NavigationCompletedEventArgs e)
	{
		_controller.put_IsVisible(BOOL.TRUE).ThrowOnError();

		ulong navigationId = default;
		e.get_NavigationId(ref navigationId).ThrowOnError();

		if (!_navigationIdToUriMap.TryGetValue(navigationId, out var uriString) && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().LogError("Got NavigationCompleted for unknown navigation id");
		}

		_navigationIdToUriMap.Remove(navigationId);

		BOOL isSuccess = default;
		e.get_IsSuccess(ref isSuccess).ThrowOnError();

		WebView2.COREWEBVIEW2_WEB_ERROR_STATUS webErrorStatus = default;
		e.get_WebErrorStatus(ref webErrorStatus).ThrowOnError();

		int httpStatusCode = 0;
		if (e is WebView2.ICoreWebView2NavigationCompletedEventArgs2 e2)
		{
			e2.get_HttpStatusCode(ref httpStatusCode).ThrowOnError();
		}

		if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
		{
			if (webErrorStatus == WebView2.COREWEBVIEW2_WEB_ERROR_STATUS.COREWEBVIEW2_WEB_ERROR_STATUS_CONNECTION_ABORTED)
			{
				_coreWebView.RaiseUnsupportedUriSchemeIdentified(uri, out _);
			}
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess.Value != 0, httpStatusCode, (CoreWebView2WebErrorStatus)(int)webErrorStatus, shouldSetSource: false);
		}
		else
		{
			_coreWebView.RaiseNavigationCompleted(null, isSuccess.Value != 0, httpStatusCode, (CoreWebView2WebErrorStatus)(int)webErrorStatus, shouldSetSource: false);
		}
	}

	private void NativeWebView_NewWindowRequested(WebView2.ICoreWebView2NewWindowRequestedEventArgs e)
	{
		e.get_Uri(out var uriPwstr).ThrowOnError();
		_coreWebView.RaiseNewWindowRequested(
		uriPwstr.ToString()!,
		CoreWebView2.BlankUri,
		out var handled);
		e.put_Handled(handled ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
	}

	private void UpdateDocumentTitle()
	{
		_nativeWebView.get_DocumentTitle(out var title).ThrowOnError();
		SetDocumentTitle(title.ToString()!);
	}

	private void CoreWebView2_HistoryChanged()
	{
		BOOL canGoBack = default;
		BOOL canGoForward = default;
		_nativeWebView.get_CanGoBack(ref canGoBack).ThrowOnError();
		_nativeWebView.get_CanGoForward(ref canGoForward).ThrowOnError();
		_coreWebView.SetHistoryProperties(canGoBack.Value != 0, canGoForward.Value != 0);
		_coreWebView.RaiseHistoryChanged();
	}

	public override unsafe Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
	{
		var tcs = new TaskCompletionSource<string?>();
		fixed (char* p_script = script)
			_nativeWebView.ExecuteScript(new PWSTR(p_script), new WebView2.Utilities.CoreWebView2ExecuteScriptCompletedHandler((errorCode, result) =>
			{
				if (errorCode.IsError)
					tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("ExecuteScript failed."));
				else
					tcs.TrySetResult(result.ToString());
			})).ThrowOnError();
		return tcs.Task;
	}

	public override void GoBack()
		=> _nativeWebView.GoBack().ThrowOnError();

	public override void GoForward()
		=> _nativeWebView.GoForward().ThrowOnError();

	public override Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
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

	public override unsafe void ProcessNavigation(Uri uri)
	{
		var uriString = uri.ToString();
		fixed (char* p_uri = uriString)
			_nativeWebView.Navigate(new PWSTR(p_uri)).ThrowOnError();
	}

	public override unsafe void ProcessNavigation(string html)
	{
		fixed (char* p_html = html)
			_nativeWebView.NavigateToString(new PWSTR(p_html)).ThrowOnError();
	}

	public override void ProcessNavigation(HttpRequestMessage httpRequestMessage) => ProcessNavigationCore(httpRequestMessage);

	private unsafe void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			if (header.Key != "Host")
			{
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
			}
		}

		_nativeWebView.get_Environment(out var environment).ThrowOnError();
		var env2 = (WebView2.ICoreWebView2Environment2)environment;

		var bodyStream = httpRequestMessage.Content?.ReadAsStream() ?? Stream.Null;
		IStream? bodyIStream = null;
		if (bodyStream != Stream.Null)
		{
			var ms = new MemoryStream();
			bodyStream.CopyTo(ms);
			bodyIStream = new ByteArrayIStream(ms.ToArray());
		}

		var requestUri = httpRequestMessage.RequestUri!.ToString();
		var requestMethod = httpRequestMessage.Method.Method;
		var requestHeaders = builder.ToString();
		fixed (char* p_requestUri = requestUri, p_requestMethod = requestMethod, p_requestHeaders = requestHeaders)
		{
			env2.CreateWebResourceRequest(
				new PWSTR(p_requestUri),
				new PWSTR(p_requestMethod),
				bodyIStream!,
				new PWSTR(p_requestHeaders),
				out var request).ThrowOnError();

			_nativeWebView.NavigateWithWebResourceRequest(request).ThrowOnError();
		}
	}

	public override void Reload()
		=> _nativeWebView.Reload().ThrowOnError();

	public override void SetScrollingEnabled(bool isScrollingEnabled)
	{
	}

	public override void Stop()
		=> _nativeWebView.Stop().ThrowOnError();

	public unsafe void ClearVirtualHostNameToFolderMapping(string hostName)
	{
		fixed (char* p_hostName = hostName)
			_nativeWebView.ClearVirtualHostNameToFolderMapping(new PWSTR(p_hostName)).ThrowOnError();
	}

	public unsafe void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
	{
		fixed (char* p_hostName = hostName, p_folderPath = folderPath)
			_nativeWebView.SetVirtualHostNameToFolderMapping(
				new PWSTR(p_hostName),
				new PWSTR(p_folderPath),
				(WebView2.COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND)(int)accessKind
			).ThrowOnError();
	}

	public unsafe void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		fixed (char* p_uri = uri)
			_nativeWebView.AddWebResourceRequestedFilterWithRequestSourceKinds(
				new PWSTR(p_uri),
				(WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT)(int)resourceContext,
				(WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS)(int)requestSourceKinds
			).ThrowOnError();
	}

	public unsafe void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		fixed (char* p_uri = uri)
			_nativeWebView.RemoveWebResourceRequestedFilterWithRequestSourceKinds(
				new PWSTR(p_uri),
				(WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT)(int)resourceContext,
				(WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS)(int)requestSourceKinds
			).ThrowOnError();
	}

	private static unsafe void CreateCoreWebView2Environment(string userDataFolder, WebView2.ICoreWebView2EnvironmentOptions options, WebView2.ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler handler)
	{
		fixed (char* p_userDataFolder = userDataFolder)
			WebView2.Functions.CreateCoreWebView2EnvironmentWithOptions(
				default,
				new PWSTR(p_userDataFolder),
				options,
				handler
			).ThrowOnError();
	}
}

#endif
