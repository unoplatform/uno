#if NET10_0_OR_GREATER
#pragma warning disable IDE0055
#pragma warning disable SYSLIB1099
extern alias mswebview2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DirectN;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Win32;
using NativeWebView = mswebview2::Microsoft.Web.WebView2.Core;
using WebView2Utilities = WebView2.Utilities.WebView2Utilities;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32NativeAotWebView : Win32NativeWebViewBase, ISupportsVirtualHostMapping, ISupportsWebResourceRequested
{
private readonly CoreWebView2 _coreWebView;
private readonly NativeWebView.CoreWebView2 _nativeWebView;
private readonly WebView2.ICoreWebView2Controller _controller;

private Dictionary<ulong, string> _navigationIdToUriMap = new();
private string _documentTitle = string.Empty;

public Win32NativeAotWebView(CoreWebView2 owner, ContentPresenter presenter)
: base(presenter)
{
_coreWebView = owner;

ForwardBackgroundToPresenter();

var tcs = new TaskCompletionSource<(WebView2.ICoreWebView2Controller controller, NativeWebView.CoreWebView2 webView)>();
NativeDispatcher.Main.EnqueueAsync(async () =>
{
try
{
WebView2Utilities.Initialize(Assembly.GetEntryAssembly());
var userDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2");

WebView2.Functions.CreateCoreWebView2EnvironmentWithOptions(default, PWSTR.From(userDataFolder), null!,
new WebView2.Utilities.CoreWebView2CreateCoreWebView2EnvironmentCompletedHandler((errorCode, environment) =>
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
var coreWebViewPtr = Marshal.GetIUnknownForObject(coreWebView);
try
{
var nativeWebView = NativeWebView.CoreWebView2.CreateFromComICoreWebView2(coreWebViewPtr);
tcs.TrySetResult((controller, nativeWebView));
}
finally
{
Marshal.Release(coreWebViewPtr);
}
})).ThrowOnError();
})).ThrowOnError();
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

_controller.put_IsVisible(false).ThrowOnError();
_controller.put_Bounds(new DirectN.RECT { left = 0, top = 0, right = 500, bottom = 500 }).ThrowOnError();

_nativeWebView.Settings.IsScriptEnabled = true;
_nativeWebView.Settings.IsWebMessageEnabled = true;
_nativeWebView.Settings.AreDefaultScriptDialogsEnabled = true;
_nativeWebView.Settings.AreDevToolsEnabled = FeatureConfiguration.WebView2.EnableDevTools;

_nativeWebView.NavigationCompleted += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationCompletedEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationCompleted(o, a));
_nativeWebView.NewWindowRequested += EventHandlerBuilder<NativeWebView.CoreWebView2NewWindowRequestedEventArgs>(static (@this, o, a) => @this.NativeWebView_NewWindowRequested(o, a));
_nativeWebView.SourceChanged += EventHandlerBuilder<NativeWebView.CoreWebView2SourceChangedEventArgs>(static (@this, o, a) => @this.NativeWebView_SourceChanged(o, a));
_nativeWebView.WebMessageReceived += EventHandlerBuilder<NativeWebView.CoreWebView2WebMessageReceivedEventArgs>(static (@this, o, a) => @this.NativeWebView_WebMessageReceived(o, a));
_nativeWebView.NavigationStarting += EventHandlerBuilder<NativeWebView.CoreWebView2NavigationStartingEventArgs>(static (@this, o, a) => @this.NativeWebView_NavigationStarting(o, a));
_nativeWebView.HistoryChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.CoreWebView2_HistoryChanged(o, a));
_nativeWebView.DocumentTitleChanged += EventHandlerBuilder<object>(static (@this, o, a) => @this.OnNativeTitleChanged(o, a));
_nativeWebView.WebResourceRequested += NativeWebView2_WebResourceRequested;
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

private void NativeWebView2_WebResourceRequested(object? sender, NativeWebView.CoreWebView2WebResourceRequestedEventArgs e)
{
WebResourceRequested?.Invoke(this, new(new Win32WebResourceRequestedEventArgsWrapper(e)));
}

private EventHandler<T> EventHandlerBuilder<T>(Action<Win32NativeAotWebView, object?, T> handler)
{
var weakRef = new WeakReference<Win32NativeAotWebView>(this);
return (sender, args) =>
{
if (weakRef.TryGetTarget(out var target))
{
handler(target, sender, args);
}
};
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
return;
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
_controller.put_IsVisible(true).ThrowOnError();

if (!_navigationIdToUriMap.TryGetValue(e.NavigationId, out var uriString) && this.Log().IsEnabled(LogLevel.Error))
{
this.Log().LogError("Got NavigationCompleted for unknown navigation id");
}

_navigationIdToUriMap.Remove(e.NavigationId);
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
SetDocumentTitle(_nativeWebView.DocumentTitle);
}

private void CoreWebView2_HistoryChanged(object? sender, object e)
{
_coreWebView.SetHistoryProperties(_nativeWebView.CanGoBack, _nativeWebView.CanGoForward);
_coreWebView.RaiseHistoryChanged();
}

public override Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
=> _nativeWebView.ExecuteScriptAsync(script);

public override void GoBack()
=> _nativeWebView.GoBack();

public override void GoForward()
=> _nativeWebView.GoForward();

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

public override void ProcessNavigation(Uri uri) => _nativeWebView.Navigate(uri.ToString());

public override void ProcessNavigation(string html) => _nativeWebView.NavigateToString(html);

public override void ProcessNavigation(HttpRequestMessage httpRequestMessage) => ProcessNavigationCore(httpRequestMessage);

private void ProcessNavigationCore(HttpRequestMessage httpRequestMessage)
{
var builder = new StringBuilder();
foreach (var header in httpRequestMessage.Headers)
{
if (header.Key != "Host")
{
builder.Append(header.Key + ": " + string.Join(", ", header.Value) + "\r\n");
}
}

var request = _nativeWebView.Environment.CreateWebResourceRequest(httpRequestMessage.RequestUri!.ToString(), httpRequestMessage.Method.Method, httpRequestMessage.Content?.ReadAsStream() ?? Stream.Null, builder.ToString());
_nativeWebView.NavigateWithWebResourceRequest(request);
}

public override void Reload()
=> _nativeWebView.Reload();

public override void SetScrollingEnabled(bool isScrollingEnabled)
{
}

public override void Stop()
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
#pragma warning restore IDE0055
#pragma warning restore SYSLIB1099
#endif
