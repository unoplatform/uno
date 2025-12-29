#nullable enable

using System;
#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
using System.Collections.Generic;
#endif
using Windows.Foundation;
#if __ANDROID__ || ANDROID_SKIA
using Android.Webkit;
#elif __IOS__ || __MACOS__ || UIKIT_SKIA
using Foundation;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
#if __SKIA__
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
#elif __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private Deferral? _deferral;

#if __ANDROID__ || ANDROID_SKIA
	private readonly IWebResourceRequest? _nativeRequest;

	internal CoreWebView2WebResourceRequestedEventArgs(
		IWebResourceRequest? nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeRequest);

	/// <summary>
	/// Indicates whether headers have been modified and require re-fetching.
	/// </summary>
	internal bool RequiresRefetch => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the native Android WebResourceResponse if a custom response was set.
	/// </summary>
	internal WebResourceResponse? GetNativeResponse() => _response?.ToNativeResponse();

	/// <summary>
	/// Gets the effective headers for re-fetching if headers were modified.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();

#elif __IOS__ || __MACOS__ || UIKIT_SKIA
	private readonly NSUrlRequest? _nativeRequest;

	internal CoreWebView2WebResourceRequestedEventArgs(
		NSUrlRequest? nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeRequest);

	/// <summary>
	/// Indicates whether headers were modified.
	/// NOTE: Modifications are tracked but cannot be applied on iOS/macOS.
	/// </summary>
	internal bool HasHeaderModifications => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();

#elif __WASM__
	private readonly string _url;
	private readonly string _method;
	private readonly IDictionary<string, string>? _requestHeaders;

	internal CoreWebView2WebResourceRequestedEventArgs(
		string url,
		string method,
		IDictionary<string, string>? headers,
		CoreWebView2WebResourceContext resourceContext)
	{
		_url = url;
		_method = method;
		_requestHeaders = headers;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_url, _method, _requestHeaders);

	/// <summary>
	/// Indicates whether headers have been modified.
	/// </summary>
	internal bool HasHeaderModifications => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal IDictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();
#endif

	public CoreWebView2WebResourceResponse Response
	{
		get => _response ??= new CoreWebView2WebResourceResponse();
		set => _response = value;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
#if __IOS__ || __MACOS__ || UIKIT_SKIA
		=> CoreWebView2WebResourceRequestSourceKinds.Document;
#else
		=> CoreWebView2WebResourceRequestSourceKinds.All;
#endif

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}
#endif
}
