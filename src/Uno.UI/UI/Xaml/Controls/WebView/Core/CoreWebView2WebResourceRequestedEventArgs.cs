#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private readonly INativeWebResourceRequestedEventArgs _nativeArgs;
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;

	internal CoreWebView2WebResourceRequestedEventArgs(INativeWebResourceRequestedEventArgs nativeArgs)
	{
		_nativeArgs = nativeArgs;
	}

	internal object NativeArgs => _nativeArgs;

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeArgs.Request);

	public CoreWebView2WebResourceResponse Response
	{
		get
		{
			var nativeResponse = _nativeArgs.Response;

			if ((_response is null || !ReferenceEquals(_response.NativeResponse, nativeResponse)) && nativeResponse is not null)
			{
				_response = new CoreWebView2WebResourceResponse(nativeResponse);
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
}
