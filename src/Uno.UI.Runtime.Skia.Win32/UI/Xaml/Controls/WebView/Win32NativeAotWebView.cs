#if NET10_0_OR_GREATER
// Intentionally suppress formatting diagnostics in this file while we keep the generated-like COM interop layout stable.
#pragma warning disable IDE0055
// Source-generated COM class implementation
#pragma warning disable SYSLIB1099
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
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
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using WebView2Utilities = WebView2.Utilities.WebView2Utilities;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32NativeAotWebView : Win32NativeWebViewBase, ISupportsVirtualHostMapping, ISupportsWebResourceRequested
{
private readonly CoreWebView2 _coreWebView;
private readonly WebView2.ICoreWebView2_22 _nativeWebView;
private readonly WebView2.ICoreWebView2Controller _controller;

private Dictionary<ulong, string> _navigationIdToUriMap = new();
private string _documentTitle = string.Empty;

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
WebView2Utilities.Initialize(Assembly.GetEntryAssembly());
var userDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2");

WebView2.Functions.CreateCoreWebView2EnvironmentWithOptions(default, PWSTR.From(userDataFolder), null!, // no custom options needed
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
tcs.TrySetResult((controller, (WebView2.ICoreWebView2_22)coreWebView));
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
ref eventToken).ThrowOnError();

_nativeWebView.add_WebMessageReceived(
new WebView2.Utilities.CoreWebView2WebMessageReceivedEventHandler((_, args) =>
{
if (weakRef.TryGetTarget(out var target)) target.NativeWebView_WebMessageReceived(args);
}),
ref eventToken).ThrowOnError();

_nativeWebView.add_NavigationStarting(
new WebView2.Utilities.CoreWebView2NavigationStartingEventHandler((_, args) =>
{
if (weakRef.TryGetTarget(out var target)) target.NativeWebView_NavigationStarting(args);
}),
ref eventToken).ThrowOnError();

_nativeWebView.add_HistoryChanged(
new WebView2.Utilities.CoreWebView2HistoryChangedEventHandler((_, _) =>
{
if (weakRef.TryGetTarget(out var target)) target.CoreWebView2_HistoryChanged();
}),
ref eventToken).ThrowOnError();

_nativeWebView.add_DocumentTitleChanged(
new WebView2.Utilities.CoreWebView2DocumentTitleChangedEventHandler((_, _) =>
{
if (weakRef.TryGetTarget(out var target)) target.UpdateDocumentTitle();
}),
ref eventToken).ThrowOnError();

_nativeWebView.add_WebResourceRequested(
new WebView2.Utilities.CoreWebView2WebResourceRequestedEventHandler((_, args) =>
{
if (weakRef.TryGetTarget(out var target)) target.NativeWebView2_WebResourceRequested(args);
}),
ref eventToken).ThrowOnError();

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

public override Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
{
var tcs = new TaskCompletionSource<string?>();
_nativeWebView.ExecuteScript(PWSTR.From(script), new WebView2.Utilities.CoreWebView2ExecuteScriptCompletedHandler((errorCode, result) =>
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

public override void ProcessNavigation(Uri uri) => _nativeWebView.Navigate(PWSTR.From(uri.ToString())).ThrowOnError();

public override void ProcessNavigation(string html) => _nativeWebView.NavigateToString(PWSTR.From(html)).ThrowOnError();

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

env2.CreateWebResourceRequest(
PWSTR.From(httpRequestMessage.RequestUri!.ToString()),
PWSTR.From(httpRequestMessage.Method.Method),
bodyIStream!,
PWSTR.From(builder.ToString()),
out var request).ThrowOnError();

_nativeWebView.NavigateWithWebResourceRequest(request).ThrowOnError();
}

public override void Reload()
=> _nativeWebView.Reload().ThrowOnError();

public override void SetScrollingEnabled(bool isScrollingEnabled)
{
}

public override void Stop()
=> _nativeWebView.Stop().ThrowOnError();

public void ClearVirtualHostNameToFolderMapping(string hostName)
=> _nativeWebView.ClearVirtualHostNameToFolderMapping(PWSTR.From(hostName)).ThrowOnError();

public void SetVirtualHostNameToFolderMapping(string hostName, string folderPath, CoreWebView2HostResourceAccessKind accessKind)
=> _nativeWebView.SetVirtualHostNameToFolderMapping(
PWSTR.From(hostName),
PWSTR.From(folderPath),
(WebView2.COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND)(int)accessKind).ThrowOnError();

public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
=> _nativeWebView.AddWebResourceRequestedFilterWithRequestSourceKinds(
PWSTR.From(uri),
(WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT)(int)resourceContext,
(WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS)(int)requestSourceKinds).ThrowOnError();

public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
=> _nativeWebView.RemoveWebResourceRequestedFilterWithRequestSourceKinds(
PWSTR.From(uri),
(WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT)(int)resourceContext,
(WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS)(int)requestSourceKinds).ThrowOnError();
}

// --- AOT-specific WebResourceRequested wrappers ---
// These implement the same INative* interfaces as Win32NativeWebView.WebResourceRequested.cs
// for the NET10+ NativeAOT code path, working directly with WebView2Aot COM interfaces.

internal sealed class AotWebResourceRequestedEventArgsWrapper : INativeWebResourceRequestedEventArgs
{
private readonly WebView2.ICoreWebView2WebResourceRequestedEventArgs _args;
private readonly WebView2.ICoreWebView2WebResourceRequestedEventArgs2? _args2;

public AotWebResourceRequestedEventArgsWrapper(WebView2.ICoreWebView2WebResourceRequestedEventArgs args)
{
_args = args;
_args2 = args as WebView2.ICoreWebView2WebResourceRequestedEventArgs2;
_args.get_Request(out var request).ThrowOnError();
Request = new AotWebResourceRequest(request);
}

public INativeWebResourceRequest Request { get; }

public INativeWebResourceResponse? Response
{
get
{
_args.get_Response(out var response).ThrowOnError();
return response is null ? null : new AotWebResourceResponse(response);
}
set
{
if (value is AotWebResourceResponse wr)
{
_args.put_Response(wr.NativeResponse).ThrowOnError();
}
else
{
_args.put_Response(null!);
}
}
}

public CoreWebView2WebResourceContext ResourceContext
{
get
{
WebView2.COREWEBVIEW2_WEB_RESOURCE_CONTEXT context = default;
_args.get_ResourceContext(ref context);
return (CoreWebView2WebResourceContext)(int)context;
}
}

public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
{
get
{
if (_args2 is null) return default;
WebView2.COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS kinds = default;
_args2.get_RequestedSourceKind(ref kinds);
return (CoreWebView2WebResourceRequestSourceKinds)(int)kinds;
}
}

public Deferral GetDeferral()
{
_args.GetDeferral(out var deferral).ThrowOnError();
return new Deferral(() => deferral.Complete());
}
}

internal sealed class AotWebResourceRequest : INativeWebResourceRequest
{
private readonly WebView2.ICoreWebView2WebResourceRequest _request;

public AotWebResourceRequest(WebView2.ICoreWebView2WebResourceRequest request)
{
_request = request;
_request.get_Headers(out var headers).ThrowOnError();
Headers = new AotHttpRequestHeaders(headers);
}

public string Uri
{
get { _request.get_Uri(out var v).ThrowOnError(); return v.ToString()!; }
set => _request.put_Uri(PWSTR.From(value)).ThrowOnError();
}

public string Method
{
get { _request.get_Method(out var v).ThrowOnError(); return v.ToString()!; }
set => _request.put_Method(PWSTR.From(value)).ThrowOnError();
}

public IRandomAccessStream Content
{
get
{
_request.get_Content(out var stream).ThrowOnError();
return stream is null ? new InMemoryRandomAccessStream() : AotStreamHelpers.ConvertIStream(stream);
}
set
{
var bytes = AotStreamHelpers.ReadIRandomAccessStream(value);
_request.put_Content(bytes.Length > 0 ? new ByteArrayIStream(bytes) : null!).ThrowOnError();
}
}

public INativeHttpRequestHeaders Headers { get; }
}

internal sealed class AotHttpRequestHeaders : INativeHttpRequestHeaders
{
private readonly WebView2.ICoreWebView2HttpRequestHeaders _headers;

public AotHttpRequestHeaders(WebView2.ICoreWebView2HttpRequestHeaders headers) => _headers = headers;

public string GetHeader(string name)
{
_headers.GetHeader(PWSTR.From(name), out var value).ThrowOnError();
return value.ToString()!;
}

public INativeHttpHeadersCollectionIterator GetHeaders(string name)
{
_headers.GetHeaders(PWSTR.From(name), out var iter).ThrowOnError();
return new AotHttpHeadersCollectionIterator(iter);
}

public bool Contains(string name)
{
BOOL result = default;
_headers.Contains(PWSTR.From(name), ref result);
return result.Value != 0;
}

public void SetHeader(string name, string value) => _headers.SetHeader(PWSTR.From(name), PWSTR.From(value)).ThrowOnError();

public void RemoveHeader(string name) => _headers.RemoveHeader(PWSTR.From(name)).ThrowOnError();

public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
{
_headers.GetIterator(out var iter).ThrowOnError();
return new AotHeadersEnumerator(iter);
}

IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class AotHttpHeadersCollectionIterator : INativeHttpHeadersCollectionIterator
{
private readonly WebView2.ICoreWebView2HttpHeadersCollectionIterator _iterator;
private bool _hasCurrent;
private KeyValuePair<string, string> _current;

public AotHttpHeadersCollectionIterator(WebView2.ICoreWebView2HttpHeadersCollectionIterator iterator)
{
_iterator = iterator;
BOOL hasCurrent = default;
_iterator.get_HasCurrentHeader(ref hasCurrent);
_hasCurrent = hasCurrent.Value != 0;
if (_hasCurrent) LoadCurrent();
}

private void LoadCurrent()
{
_iterator.GetCurrentHeader(out var name, out var value).ThrowOnError();
_current = new(name.ToString()!, value.ToString()!);
}

public object Current => _current;
public bool HasCurrent => _hasCurrent;

public bool MoveNext()
{
if (!_hasCurrent) return false;
BOOL hasNext = default;
_iterator.MoveNext(ref hasNext);
_hasCurrent = hasNext.Value != 0;
if (_hasCurrent) LoadCurrent();
return _hasCurrent;
}

public uint GetMany(object items)
{
if (items is not KeyValuePair<string, string>[] array) return 0;
uint count = 0;
while (count < array.Length && _hasCurrent)
{
array[count++] = _current;
MoveNext();
}
return count;
}
}

internal sealed class AotHeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
{
private readonly WebView2.ICoreWebView2HttpHeadersCollectionIterator _iterator;
private bool _started;
private bool _hasCurrent;
private KeyValuePair<string, string> _current;

public AotHeadersEnumerator(WebView2.ICoreWebView2HttpHeadersCollectionIterator iterator)
{
_iterator = iterator;
BOOL hasCurrent = default;
_iterator.get_HasCurrentHeader(ref hasCurrent);
_hasCurrent = hasCurrent.Value != 0;
}

public KeyValuePair<string, string> Current => _current;
object IEnumerator.Current => _current;

public bool MoveNext()
{
if (!_started)
{
_started = true;
if (!_hasCurrent) return false;
_iterator.GetCurrentHeader(out var name, out var value).ThrowOnError();
_current = new(name.ToString()!, value.ToString()!);
BOOL hasNext = default;
_iterator.MoveNext(ref hasNext);
_hasCurrent = hasNext.Value != 0;
return true;
}

if (!_hasCurrent) return false;
_iterator.GetCurrentHeader(out var n, out var v).ThrowOnError();
_current = new(n.ToString()!, v.ToString()!);
BOOL hn = default;
_iterator.MoveNext(ref hn);
_hasCurrent = hn.Value != 0;
return true;
}

public void Reset() => throw new NotSupportedException();
public void Dispose() { }
}

internal sealed class AotWebResourceResponse : INativeWebResourceResponse
{
internal WebView2.ICoreWebView2WebResourceResponse NativeResponse { get; }

public AotWebResourceResponse(WebView2.ICoreWebView2WebResourceResponse response)
{
NativeResponse = response;
response.get_Headers(out var headers).ThrowOnError();
Headers = new AotHttpResponseHeaders(headers);
}

public IRandomAccessStream Content
{
get
{
NativeResponse.get_Content(out var stream).ThrowOnError();
return stream is null ? new InMemoryRandomAccessStream() : AotStreamHelpers.ConvertIStream(stream);
}
set
{
var bytes = AotStreamHelpers.ReadIRandomAccessStream(value);
NativeResponse.put_Content(bytes.Length > 0 ? new ByteArrayIStream(bytes) : null!).ThrowOnError();
}
}

public INativeHttpResponseHeaders Headers { get; }

public int StatusCode
{
get { int v = default; NativeResponse.get_StatusCode(ref v); return v; }
set => NativeResponse.put_StatusCode(value).ThrowOnError();
}

public string ReasonPhrase
{
get { NativeResponse.get_ReasonPhrase(out var v).ThrowOnError(); return v.ToString()!; }
set => NativeResponse.put_ReasonPhrase(PWSTR.From(value)).ThrowOnError();
}
}

internal sealed class AotHttpResponseHeaders : INativeHttpResponseHeaders
{
private readonly WebView2.ICoreWebView2HttpResponseHeaders _headers;

public AotHttpResponseHeaders(WebView2.ICoreWebView2HttpResponseHeaders headers) => _headers = headers;

public void AppendHeader(string name, string value) => _headers.AppendHeader(PWSTR.From(name), PWSTR.From(value)).ThrowOnError();

public bool Contains(string name)
{
BOOL result = default;
_headers.Contains(PWSTR.From(name), ref result);
return result.Value != 0;
}

public string GetHeader(string name)
{
_headers.GetHeader(PWSTR.From(name), out var value).ThrowOnError();
return value.ToString()!;
}

public object GetHeaders(string name)
{
_headers.GetHeaders(PWSTR.From(name), out var iter).ThrowOnError();
return new AotHttpHeadersCollectionIterator(iter);
}

public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
{
_headers.GetIterator(out var iter).ThrowOnError();
return new AotHeadersEnumerator(iter);
}

IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class AotStreamHelpers
{
internal static unsafe IRandomAccessStream ConvertIStream(IStream stream)
{
var ms = new MemoryStream();
var buffer = new byte[4096];
fixed (byte* pBuffer = buffer)
{
while (true)
{
uint bytesRead = 0;
stream.Read((nint)pBuffer, (uint)buffer.Length, (nint)(&bytesRead));
if (bytesRead == 0) break;
ms.Write(buffer, 0, (int)bytesRead);
}
}
var ras = new InMemoryRandomAccessStream();
var dataWriter = new DataWriter(ras);
dataWriter.WriteBytes(ms.ToArray());
dataWriter.StoreAsync().AsTask().Wait();
ras.Seek(0);
return ras;
}

internal static byte[] ReadIRandomAccessStream(IRandomAccessStream? stream)
{
if (stream is null) return Array.Empty<byte>();
using var ms = new MemoryStream();
stream.AsStreamForRead().CopyTo(ms);
return ms.ToArray();
}
}

[GeneratedComClass]
internal sealed partial class ByteArrayIStream : IStream, ISequentialStream
{
private readonly byte[] _data;
private long _position;

public ByteArrayIStream(byte[] data) => _data = data;

public unsafe HRESULT Read(nint pv, uint cb, nint pcbRead)
{
int count = (int)Math.Min((long)cb, _data.Length - _position);
if (count <= 0)
{
if (pcbRead != nint.Zero) *(uint*)pcbRead = 0;
return Constants.S_OK;
}
fixed (byte* src = _data)
{
System.Runtime.CompilerServices.Unsafe.CopyBlock((byte*)pv, src + _position, (uint)count);
}
_position += count;
if (pcbRead != nint.Zero) *(uint*)pcbRead = (uint)count;
return Constants.S_OK;
}

public unsafe HRESULT Write(nint pv, uint cb, nint pcbWritten)
{
if (pcbWritten != nint.Zero) *(uint*)pcbWritten = 0;
return Constants.E_NOTIMPL;
}

public unsafe HRESULT Seek(long dlibMove, STREAM_SEEK dwOrigin, nint plibNewPosition)
{
_position = dwOrigin switch
{
STREAM_SEEK.STREAM_SEEK_SET => dlibMove,
STREAM_SEEK.STREAM_SEEK_CUR => _position + dlibMove,
STREAM_SEEK.STREAM_SEEK_END => _data.Length + dlibMove,
_ => _position
};
_position = Math.Clamp(_position, 0, _data.Length);
if (plibNewPosition != nint.Zero) *(ulong*)plibNewPosition = (ulong)_position;
return Constants.S_OK;
}

public HRESULT SetSize(ulong libNewSize) => Constants.E_NOTIMPL;

public HRESULT CopyTo(IStream pstm, ulong cb, nint pcbRead, nint pcbWritten) => Constants.E_NOTIMPL;

public HRESULT Commit(uint grfCommitFlags) => Constants.S_OK;

public HRESULT Revert() => Constants.S_OK;

public HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType) => Constants.E_NOTIMPL;

public HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType) => Constants.E_NOTIMPL;

public HRESULT Stat(out STATSTG pstatstg, uint grfStatFlag)
{
pstatstg = new STATSTG { cbSize = (ulong)_data.Length };
return Constants.S_OK;
}

public HRESULT Clone(out IStream ppstm)
{
ppstm = null!;
return Constants.E_NOTIMPL;
}
}
#pragma warning restore IDE0055
#pragma warning restore SYSLIB1099
#endif
