#if __IOS__ || __MACOS__ || UIKIT_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using Foundation;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// iOS/macOS-specific implementation for WebResourceRequested event args.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private Deferral? _deferral;
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

	public CoreWebView2WebResourceResponse Response
	{
		get => _response ??= new CoreWebView2WebResourceResponse();
		set => _response = value;
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
#endif
