#if __WASM__ || WASM_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// WASM-specific implementation for WebResourceRequested event args.
/// </summary>
internal partial class NativeCoreWebView2WebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private NativeCoreWebView2WebResourceRequest? _request;
	private NativeCoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private Deferral? _deferral;
	private readonly string _url;
	private readonly string _method;
	private readonly IDictionary<string, string>? _headers;

	internal NativeCoreWebView2WebResourceRequestedEventArgs(
		string url,
		string method,
		IDictionary<string, string>? headers,
		CoreWebView2WebResourceContext resourceContext)
	{
		_url = url;
		_method = method;
		_headers = headers;
		_resourceContext = resourceContext;
	}

	public INativeWebResourceRequest Request
		=> _request ??= new NativeCoreWebView2WebResourceRequest(_url, _method, _headers);

	public INativeWebResourceResponse? Response
	{
		get => _response;
		set => _response = value as NativeCoreWebView2WebResourceResponse;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> CoreWebView2WebResourceRequestSourceKinds.All;

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}

	/// <summary>
	/// Indicates whether headers were modified.
	/// </summary>
	internal bool HasHeaderModifications => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();
}
#endif
