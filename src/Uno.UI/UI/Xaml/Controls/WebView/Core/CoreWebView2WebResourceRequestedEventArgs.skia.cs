#nullable enable

#if __SKIA__
using System;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private readonly dynamic _nativeArgs;
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;

	internal CoreWebView2WebResourceRequestedEventArgs(object nativeArgs)
	{
		_nativeArgs = nativeArgs ?? throw new ArgumentNullException(nameof(nativeArgs));
	}

	internal dynamic NativeArgs => _nativeArgs;

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeArgs.Request);

	public CoreWebView2WebResourceResponse Response
	{
		get
		{
			var nativeResponse = _nativeArgs.Response;
			if (nativeResponse is null)
			{
				_response = null;
				return null!;
			}

			if (_response is null || !ReferenceEquals(_response.NativeResponse, nativeResponse))
			{
				_response = new CoreWebView2WebResourceResponse(nativeResponse);
			}

			return _response!;
		}
		set
		{
			_nativeArgs.Response = value?.NativeResponse;
			_response = value;
		}
	}

	public CoreWebView2WebResourceContext ResourceContext
		=> (CoreWebView2WebResourceContext)_nativeArgs.ResourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> (CoreWebView2WebResourceRequestSourceKinds)_nativeArgs.RequestedSourceKind;

	public Deferral GetDeferral() => _nativeArgs.GetDeferral();
}
#endif
