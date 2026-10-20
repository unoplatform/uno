#if __ANDROID__ || ANDROID_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// Android-specific implementation for WebResourceRequested event args.
/// </summary>
internal partial class NativeCoreWebView2WebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private NativeCoreWebView2WebResourceRequest? _request;
	private NativeCoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private Deferral? _deferral;
	private readonly IWebResourceRequest _nativeRequest;

	internal NativeCoreWebView2WebResourceRequestedEventArgs(
		IWebResourceRequest nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public INativeWebResourceRequest Request
		=> _request ??= new NativeCoreWebView2WebResourceRequest(_nativeRequest);

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
}
#endif
