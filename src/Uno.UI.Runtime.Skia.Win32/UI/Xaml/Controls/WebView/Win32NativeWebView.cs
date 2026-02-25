extern alias mswebview2;
using NativeWebView = mswebview2::Microsoft.Web.WebView2.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Win32;

// Heavily inspired/copied from WpfNativeWebView

internal class Win32NativeWebViewProvider(CoreWebView2 owner) : INativeWebViewProvider
{
	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		try
		{
			Assembly.Load("Microsoft.Web.WebView2.Core");
		}
		catch (Exception)
		{
			typeof(Win32Host).LogError()?.Error($"Failed to load Microsoft.Web.WebView2.Core needed for WebView support. Make sure that WebView is included in the project's UnoFeatures. For more details, see https://aka.platform.uno/webview2 and https://aka.platform.uno/using-uno-sdk.");
			return null!;
		}
		return new Win32NativeWebView(owner, contentPresenter);
	}
}

internal partial class Win32NativeWebView : INativeWebView, ISupportsVirtualHostMapping, ISupportsWebResourceRequested
{
	private const string WindowClassName = "UnoPlatformWebViewWindow";
	private const uint SC_MASK = 0xFFF0; // Mask to extract system command from wParam

	// _windowClass must be statically stored, otherwise lpfnWndProc will get collected and the CLR will throw some weird exceptions
	// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	private static readonly WNDCLASSEXW _windowClass;

	// This is necessary to be able to direct the very first WndProc call of a window to the correct window wrapper.
	// That first call is inside CreateWindow, so we don't have a HWND yet. The alternative would be to create a new
	// window class (and a new WndProc) per window, but that sounds excessive.
	private static WeakReference<Win32NativeWebView>? _webViewForNextCreateWindow;
	private static readonly Dictionary<HWND, WeakReference<Win32NativeWebView>> _hwndToWebView = new();

	private readonly ContentPresenter _presenter;
	private readonly HWND _hwnd;
	private readonly CoreWebView2 _coreWebView;
	private readonly NativeWebView.CoreWebView2 _nativeWebView;

	private Dictionary<ulong, string> _navigationIdToUriMap = new();
	private string _documentTitle = string.Empty;
	private readonly NativeWebView.CoreWebView2Controller _controller;

	private HWND ParentHwnd => (_presenter.XamlRoot?.HostWindow?.NativeWindow as Win32NativeWindow)?.Hwnd is IntPtr hwnd ? (HWND)hwnd : HWND.Null;

	static unsafe Win32NativeWebView()
	{
		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
			lpfnWndProc = &WndProc,
			hInstance = Win32Helper.GetHInstance(),
			lpszClassName = lpClassName,
			style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-class-styles
		};

		var classAtom = PInvoke.RegisterClassEx(_windowClass);
		if (classAtom is 0)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	public Win32NativeWebView(CoreWebView2 owner, ContentPresenter presenter)
	{
		_presenter = presenter;
		_coreWebView = owner;

		using var lpClassName = new Win32Helper.NativeNulTerminatedUtf16String(WindowClassName);

		_webViewForNextCreateWindow = new WeakReference<Win32NativeWebView>(this);
		unsafe
		{
			_hwnd = PInvoke.CreateWindowEx(
				0,
				lpClassName,
				new PCWSTR(),
				0,
				PInvoke.CW_USEDEFAULT,
				PInvoke.CW_USEDEFAULT,
				PInvoke.CW_USEDEFAULT,
				PInvoke.CW_USEDEFAULT,
				HWND.Null,
				HMENU.Null,
				Win32Helper.GetHInstance(),
				null);
		}
		_webViewForNextCreateWindow = null;

		_ = PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);

		if (_hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} failed: {Win32Helper.GetErrorMessage()}");
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("Created web view window.");
		}

		var success = PInvoke.RegisterTouchWindow(_hwnd, 0);
		if (!success && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(PInvoke.RegisterTouchWindow)} failed: {Win32Helper.GetErrorMessage()}");
		}

		var tcs = new TaskCompletionSource<NativeWebView.CoreWebView2Controller>();
		// ReSharper disable once AsyncVoidLambda
		NativeDispatcher.Main.EnqueueAsync(async () =>
		{
			var userDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2");
			var env = await NativeWebView.CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
			tcs.SetResult(await env.CreateCoreWebView2ControllerAsync(_hwnd));
		});

		while (!tcs.Task.IsCompleted)
		{
			Win32EventLoop.RunOnce();
		}

		_controller = tcs.Task.Result;
		_nativeWebView = _controller.CoreWebView2;
		_nativeWebView.Settings.IsScriptEnabled = true;
		_nativeWebView.Settings.IsWebMessageEnabled = true;
		_nativeWebView.Settings.AreDefaultScriptDialogsEnabled = true;
#if DEBUG
		_nativeWebView.Settings.AreDevToolsEnabled = true;
#endif
		_controller.Bounds = new Rectangle(0, 0, 500, 500);

		// This dance with weak refs is necessary because there seems like _nativeWebView when it has a ref back
		// to this.
		_nativeWebView.NavigationCompleted += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationCompletedEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationCompleted(o, a));
		_nativeWebView.NewWindowRequested += EventHandlerBuilder<NativeWebView.CoreWebView2NewWindowRequestedEventArgs>(static (@this, o, a) => @this.NativeWebView_NewWindowRequested(o, a));
		_nativeWebView.SourceChanged += EventHandlerBuilder<NativeWebView.CoreWebView2SourceChangedEventArgs>(static (@this, o, a) => @this.NativeWebView_SourceChanged(o, a));
		_nativeWebView.WebMessageReceived += EventHandlerBuilder<NativeWebView.CoreWebView2WebMessageReceivedEventArgs>(static (@this, o, a) => @this.NativeWebView_WebMessageReceived(o, a));
		_nativeWebView.NavigationStarting += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationStartingEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationStarting(o, a));
		_nativeWebView.HistoryChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.CoreWebView2_HistoryChanged(o, a));
		_nativeWebView.DocumentTitleChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.OnNativeTitleChanged(o, a));
		_nativeWebView.WebResourceRequested += NativeWebView2_WebResourceRequested;
		UpdateDocumentTitle();

		presenter.Content = new Win32NativeWindow(_hwnd);
	}

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	private void NativeWebView2_WebResourceRequested(object? sender, NativeWebView.CoreWebView2WebResourceRequestedEventArgs e)
	{
		WebResourceRequested?.Invoke(this, new(new Win32WebResourceRequestedEventArgsWrapper(e)));
	}

	private EventHandler<T> EventHandlerBuilder<T>(Action<Win32NativeWebView, object?, T> handler)
	{
		var weakRef = new WeakReference<Win32NativeWebView>(this);
		return (sender, args) =>
		{
			if (weakRef.TryGetTarget(out var target))
			{
				handler(target, sender, args);
			}
		};
	}

	~Win32NativeWebView()
	{
		_presenter.DispatcherQueue.TryEnqueue(() =>
		{
			var success = PInvoke.DestroyWindow(_hwnd);
			if (!success && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(PInvoke.DestroyWindow)} failed: {Win32Helper.GetErrorMessage()}");
			}

			lock (_hwndToWebView)
			{
				_hwndToWebView.Remove(_hwnd);
			}
		});
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		lock (_hwndToWebView)
		{
			if (_webViewForNextCreateWindow is { } webView)
			{
				_hwndToWebView[hwnd] = webView;
			}
			else if (!_hwndToWebView.TryGetValue(hwnd, out webView))
			{
				throw new Exception($"{nameof(WndProc)} was fired on a {nameof(HWND)} before it was added to, or after it was removed from, {nameof(_hwndToWebView)}.");
			}
			if (webView.TryGetTarget(out var target))
			{
				return target.WndProcInner(hwnd, msg, wParam, lParam);
			}
			return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
		}
	}

	private LRESULT WndProcInner(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		Debug.Assert(_hwnd == HWND.Null || hwnd == _hwnd); // the null check is for when this method gets called inside CreateWindow before setting _hwnd

		switch (msg)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			case PInvoke.WM_SIZE when _controller is not null:
				PInvoke.GetClientRect(_hwnd, out var bounds);
				_controller.Bounds = bounds;
				return new LRESULT(0);
			case PInvoke.WM_SYSCOMMAND:
				// When Alt+F4 is pressed on a focused WebView2, Windows sends WM_SYSCOMMAND with SC_CLOSE
				// to the WebView2's child window. We need to forward this to the parent window to close
				// the entire application instead of just the WebView2 control.
				var syscommand = (uint)wParam.Value & SC_MASK;
				if (syscommand == PInvoke.SC_CLOSE)
				{
					var parentHwnd = ParentHwnd;
					if (parentHwnd != HWND.Null)
					{
						PInvoke.SendMessage(parentHwnd, msg, wParam, lParam);
						return new LRESULT(0);
					}
				}
				break;
			case PInvoke.WM_CLOSE:
				// Prevent the WebView2 window from being closed directly. Instead, forward to the parent
				// window so the entire application can close properly.
				{
					var parentHwnd = ParentHwnd;
					if (parentHwnd != HWND.Null)
					{
						PInvoke.SendMessage(parentHwnd, msg, wParam, lParam);
						return new LRESULT(0);
					}
				}
				break;
		}
		return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public string DocumentTitle
	{
		get => _documentTitle;
		private set
		{
			if (_documentTitle != value)
			{
				_documentTitle = value;
				_coreWebView.OnDocumentTitleChanged();
			}
		}
	}

	private void NativeWebView_SourceChanged(object? sender, NativeWebView.CoreWebView2SourceChangedEventArgs e)
	{
		_coreWebView.Source = _nativeWebView.Source;
	}

	private void NativeWebView_WebMessageReceived(object? sender, NativeWebView.CoreWebView2WebMessageReceivedEventArgs e)
	{
		_coreWebView.RaiseWebMessageReceived(e.WebMessageAsJson);
	}

	private void NativeWebView_NavigationStarting(object? sender, NativeWebView.CoreWebView2NavigationStartingEventArgs e)
	{
		if (e.Uri is null)
		{
			return; // this is what the Wpf version does
		}

		bool cancel;
		if (Uri.TryCreate(e.Uri, UriKind.RelativeOrAbsolute, out var uri))
		{
			_coreWebView.RaiseNavigationStarting(uri, out cancel);
		}
		else
		{
			_coreWebView.RaiseNavigationStarting(e.Uri, out cancel);
		}
		_coreWebView.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		e.Cancel = cancel;
		_navigationIdToUriMap[e.NavigationId] = e.Uri;
	}

	private void NativeWebView_NavigationCompleted(object? sender, NativeWebView.CoreWebView2NavigationCompletedEventArgs e)
	{
		if (!_navigationIdToUriMap.TryGetValue(e.NavigationId, out var uriString))
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
		if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
		{
			if (e.WebErrorStatus == NativeWebView.CoreWebView2WebErrorStatus.ConnectionAborted)
			{
				_coreWebView.RaiseUnsupportedUriSchemeIdentified(uri, out _);
			}
			_coreWebView.RaiseNavigationCompleted(uri, e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
		}
		else
		{
			_coreWebView.RaiseNavigationCompleted(null, e.IsSuccess, e.HttpStatusCode, (CoreWebView2WebErrorStatus)e.WebErrorStatus, shouldSetSource: false);
		}
	}

	private void NativeWebView_NewWindowRequested(object? sender, NativeWebView.CoreWebView2NewWindowRequestedEventArgs e)
	{
		_coreWebView.RaiseNewWindowRequested(
			e.Uri,
			CoreWebView2.BlankUri,
			out var handled);

		e.Handled = handled;
	}

	private void OnNativeTitleChanged(object? sender, object e) => UpdateDocumentTitle();

	private void UpdateDocumentTitle()
	{
		DocumentTitle = _nativeWebView.DocumentTitle;
	}

	private void CoreWebView2_HistoryChanged(object? sender, object e)
	{
		_coreWebView.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
		_coreWebView.RaiseHistoryChanged();
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
		=> _nativeWebView.ExecuteScriptAsync(script);

	public void GoBack()
		=> _nativeWebView.GoBack();

	public void GoForward()
		=> _nativeWebView.GoForward();

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

	public void ProcessNavigation(Uri uri) => _nativeWebView.Navigate(uri.ToString());

	public void ProcessNavigation(string html) => _nativeWebView.NavigateToString(html);

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => ProcessNavigationCore(httpRequestMessage);

	private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
	{
		var builder = new StringBuilder();
		foreach (var header in httpRequestMessage.Headers)
		{
			// https://github.com/MicrosoftEdge/WebView2Feedback/issues/2250#issuecomment-1201765363
			// WebView2 doesn't like when you try to set some headers manually
			if (header.Key != "Host")
			{
				builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
			}
		}

		var request = _nativeWebView.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content?.ReadAsStream() ?? Stream.Null, builder.ToString());
		_nativeWebView.NavigateWithWebResourceRequest(request);
	}

	public void Reload()
		=> _nativeWebView.Reload();

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
	}

	public void Stop()
		=> _nativeWebView.Stop();

	public void ClearVirtualHostNameToFolderMapping(string hostName)
		=> _nativeWebView.ClearVirtualHostNameToFolderMapping(hostName);

	public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
		=> _nativeWebView.SetVirtualHostNameToFolderMapping(hostName, folderPath, (NativeWebView.CoreWebView2HostResourceAccessKind)accessKind);

	public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
		=> _nativeWebView.AddWebResourceRequestedFilter(uri, (NativeWebView.CoreWebView2WebResourceContext)resourceContext, (NativeWebView.CoreWebView2WebResourceRequestSourceKinds)requestSourceKinds);

	public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
		=> _nativeWebView.RemoveWebResourceRequestedFilter(uri, (NativeWebView.CoreWebView2WebResourceContext)resourceContext, (NativeWebView.CoreWebView2WebResourceRequestSourceKinds)requestSourceKinds);
}
