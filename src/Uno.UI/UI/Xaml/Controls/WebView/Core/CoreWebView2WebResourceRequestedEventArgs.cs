#nullable enable

using System;
#if __SKIA__
using Windows.Foundation;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
#if __SKIA__
	private readonly INativeWebResourceRequestedEventArgs _nativeArgs;
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;

	internal CoreWebView2WebResourceRequestedEventArgs(object nativeArgs)
	{
		if (nativeArgs is INativeWebResourceRequestedEventArgs wrapper)
		{
			_nativeArgs = wrapper;
		}
		else
		{
			_nativeArgs = new ReflectionNativeWebResourceRequestedEventArgs(nativeArgs ?? throw new ArgumentNullException(nameof(nativeArgs)));
		}
	}

	internal object NativeArgs => _nativeArgs is ReflectionNativeWebResourceRequestedEventArgs r ? r.Target : _nativeArgs;

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeArgs.Request);

	public CoreWebView2WebResourceResponse Response
	{
		get
		{
			var nativeResponseWrapper = _nativeArgs.Response;
			if (nativeResponseWrapper is null)
			{
				_response = null;
				return null!;
			}

			var nativeResponseTarget = (nativeResponseWrapper as ReflectionNativeWebResourceResponse)?.Target ?? nativeResponseWrapper;

			if (_response is null || !ReferenceEquals(_response.NativeResponse, nativeResponseTarget))
			{
				_response = new CoreWebView2WebResourceResponse(nativeResponseWrapper);
			}

			return _response!;
		}
		set
		{
			_nativeArgs.Response = value?.NativeResponseInterface;
			_response = value;
		}
	}

	public CoreWebView2WebResourceContext ResourceContext
		=> _nativeArgs.ResourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> _nativeArgs.RequestedSourceKind;

	public Deferral GetDeferral() => _nativeArgs.GetDeferral();
#endif
}
