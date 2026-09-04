#nullable enable

using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// iOS/macOS-specific implementation for WebResourceRequested event args.
/// </summary>
internal partial class NativeCoreWebView2WebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private NativeCoreWebView2WebResourceRequest? _request;
	private NativeCoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private Deferral? _deferral;
	private readonly NSUrlRequest? _nativeRequest;

	internal NativeCoreWebView2WebResourceRequestedEventArgs(
		NSUrlRequest? nativeRequest,
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
		=> CoreWebView2WebResourceRequestSourceKinds.Document;

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}

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
}
